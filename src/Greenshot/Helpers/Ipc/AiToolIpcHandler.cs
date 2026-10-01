/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.User32;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Native;
using log4net;

namespace Greenshot.Helpers.Ipc
{
    /// <summary>
    /// LIST_WINDOWS and CAPTURE: the screen content commands for AI tools (greenshot-mcp.exe).
    /// The caller (<see cref="IpcSecurityDispatcher"/>) already checked the whitelist and the user's consent.
    /// </summary>
    /// <remarks>
    /// CAPTURE parameters (all optional):
    /// <list type="bullet">
    /// <item>target: window, active, screen or region (default: window when handle, title or process is given, otherwise active)</item>
    /// <item>handle: window handle from LIST_WINDOWS (hex with 0x, or decimal)</item>
    /// <item>title: part of the window title; process: process name (without .exe)</item>
    /// <item>display: index of the display for target=screen (default: all displays)</item>
    /// <item>x, y, width, height: the region in screen coordinates for target=region</item>
    /// <item>ocr: true to add the recognized text</item>
    /// <item>max_size: scale the image down so its longest side is at most this many pixels (0 = original size)</item>
    /// </list>
    /// </remarks>
    public static class AiToolIpcHandler
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AiToolIpcHandler));

        /// <summary>
        /// LIST_WINDOWS: the top-level windows in Z-order (top first) and the displays, without windows of excluded processes.
        /// </summary>
        public static async Task HandleListWindowsAsync(IpcRequestContext context, CancellationToken cancellationToken = default)
        {
            IntPtr activeHandle = WindowDetails.GetActiveWindow()?.Handle ?? IntPtr.Zero;
            var windows = new List<object>();
            int excludedCount = 0;
            foreach (var window in WindowDetails.GetTopLevelWindows())
            {
                string processName = GetProcessName(window);
                if (AiToolAccess.IsProcessExcluded(processName))
                {
                    excludedCount++;
                    continue;
                }
                var bounds = window.WindowRectangle;
                windows.Add(new
                {
                    handle = FormatHandle(window.Handle),
                    title = window.Text,
                    process = processName,
                    @class = window.ClassName,
                    x = bounds.X,
                    y = bounds.Y,
                    width = bounds.Width,
                    height = bounds.Height,
                    minimized = window.Iconic,
                    active = window.Handle == activeHandle
                });
            }

            var displays = DisplayInfo.AllDisplayInfos.Select((d, index) => new
            {
                index,
                primary = d.IsPrimary,
                x = d.Bounds.X,
                y = d.Bounds.Y,
                width = d.Bounds.Width,
                height = d.Bounds.Height
            }).ToList();

            await context.ReplyAsync(new
            {
                status = "ok",
                exit_code = 0,
                windows,
                displays,
                excluded_windows = excludedCount,
                stdout = $"{windows.Count} windows, {displays.Count} displays."
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// CAPTURE: captures a window, the active window, a display or all displays, or a region, and replies with a PNG (base64).
        /// </summary>
        public static async Task HandleCaptureAsync(IpcRequestContext context, CancellationToken cancellationToken = default)
        {
            var parameters = context.Envelope.Parameters ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string handleParameter = GetParameter(parameters, "handle");
            string titleParameter = GetParameter(parameters, "title");
            string processParameter = GetParameter(parameters, "process");
            bool hasWindowCriteria = handleParameter != null || titleParameter != null || processParameter != null;
            string target = (GetParameter(parameters, "target") ?? (hasWindowCriteria ? "window" : "active")).ToLowerInvariant();
            bool ocr = GetBool(parameters, "ocr");
            int maxSize = Math.Max(0, GetInt(parameters, "max_size") ?? 0);

            Bitmap image = null;
            try
            {
                string title;
                string processName = null;
                string handle = null;
                NativeRect bounds;
                int redactedWindows = 0;
                string what;

                switch (target)
                {
                    case "window":
                    case "active":
                        WindowDetails window;
                        if (target == "active")
                        {
                            window = WindowDetails.GetActiveWindow();
                        }
                        else
                        {
                            window = FindWindow(handleParameter, titleParameter, processParameter, out string findError);
                            if (window == null)
                            {
                                await ReplyErrorAsync(context, findError, cancellationToken).ConfigureAwait(false);
                                return;
                            }
                        }
                        if (window == null || window.Handle == IntPtr.Zero)
                        {
                            await ReplyErrorAsync(context, "There is no active window.", cancellationToken).ConfigureAwait(false);
                            return;
                        }
                        processName = GetProcessName(window);
                        if (AiToolAccess.IsProcessExcluded(processName))
                        {
                            await ReplyErrorAsync(context, $"Windows of '{processName}' are excluded from AI tools (AiToolsExcludedProcesses).", cancellationToken).ConfigureAwait(false);
                            return;
                        }
                        title = window.Text;
                        handle = FormatHandle(window.Handle);
                        image = await CaptureWindowAsync(window, cancellationToken).ConfigureAwait(false);
                        // After the capture: a restored window may have moved
                        bounds = window.WindowRectangle;
                        what = $"the window '{title}'";
                        break;

                    case "screen":
                        bounds = DisplayInfo.ScreenBounds;
                        int? displayIndex = GetInt(parameters, "display");
                        if (displayIndex.HasValue)
                        {
                            var displayInfos = DisplayInfo.AllDisplayInfos;
                            if (displayIndex.Value < 0 || displayIndex.Value >= displayInfos.Length)
                            {
                                await ReplyErrorAsync(context, $"There is no display {displayIndex.Value}, there are {displayInfos.Length}.", cancellationToken).ConfigureAwait(false);
                                return;
                            }
                            bounds = displayInfos[displayIndex.Value].Bounds;
                        }
                        title = displayIndex.HasValue ? $"Display {displayIndex.Value}" : "Screen";
                        image = await CaptureBoundsAsync(bounds, cancellationToken).ConfigureAwait(false);
                        redactedWindows = RedactExcludedWindows(image, bounds);
                        what = displayIndex.HasValue ? $"display {displayIndex.Value}" : "the screen";
                        break;

                    case "region":
                        int? x = GetInt(parameters, "x");
                        int? y = GetInt(parameters, "y");
                        int? width = GetInt(parameters, "width");
                        int? height = GetInt(parameters, "height");
                        if (!x.HasValue || !y.HasValue || !width.HasValue || !height.HasValue || width.Value <= 0 || height.Value <= 0)
                        {
                            await ReplyErrorAsync(context, "A region capture needs x, y, width and height (screen coordinates, width and height above 0).", cancellationToken).ConfigureAwait(false);
                            return;
                        }
                        bounds = new NativeRect(x.Value, y.Value, width.Value, height.Value).Intersect(DisplayInfo.ScreenBounds);
                        if (bounds.IsEmpty)
                        {
                            await ReplyErrorAsync(context, "The region is not on any display.", cancellationToken).ConfigureAwait(false);
                            return;
                        }
                        title = "Region";
                        image = await CaptureBoundsAsync(bounds, cancellationToken).ConfigureAwait(false);
                        redactedWindows = RedactExcludedWindows(image, bounds);
                        what = "a screen region";
                        break;

                    default:
                        await ReplyErrorAsync(context, $"Unknown capture target '{target}', use window, active, screen or region.", cancellationToken).ConfigureAwait(false);
                        return;
                }

                if (image == null)
                {
                    await ReplyErrorAsync(context, $"Capturing {what} failed.", cancellationToken).ConfigureAwait(false);
                    return;
                }

                // OCR on the original image, the best quality
                List<IOcrLineFeature> ocrLines = null;
                string ocrError = null;
                if (ocr)
                {
                    (ocrLines, ocrError) = await RunOcrAsync(image).ConfigureAwait(false);
                }

                int originalWidth = image.Width;
                int originalHeight = image.Height;
                double scale = 1.0;
                if (maxSize > 0 && Math.Max(originalWidth, originalHeight) > maxSize)
                {
                    scale = (double)maxSize / Math.Max(originalWidth, originalHeight);
                    var scaled = Scale(image, scale);
                    image.Dispose();
                    image = scaled;
                }

                string data;
                using (var stream = new MemoryStream())
                {
                    image.Save(stream, ImageFormat.Png);
                    data = Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length);
                }

                AiToolAccess.NotifyCapture(context.ConnectionOrigin, what);

                await context.ReplyAsync(new
                {
                    status = "ok",
                    exit_code = 0,
                    mime_type = "image/png",
                    data,
                    width = image.Width,
                    height = image.Height,
                    original_width = originalWidth,
                    original_height = originalHeight,
                    scale,
                    target,
                    title,
                    process = processName,
                    handle,
                    bounds = new { x = bounds.X, y = bounds.Y, width = bounds.Width, height = bounds.Height },
                    redacted_windows = redactedWindows,
                    ocr_text = ocrLines == null ? null : string.Join(Environment.NewLine, ocrLines.Select(l => l.Text)),
                    // OCR bounds are in pixels of the original (unscaled) image
                    ocr_lines = ocrLines?.Select(l => new
                    {
                        text = l.Text,
                        x = l.Bounds.X,
                        y = l.Bounds.Y,
                        width = l.Bounds.Width,
                        height = l.Bounds.Height
                    }),
                    ocr_error = ocrError,
                    stdout = $"Captured {what} ({originalWidth}x{originalHeight})."
                }, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                image?.Dispose();
            }
        }

        /// <summary>
        /// Finds a top-level window by handle, or by title (part of it) and / or process name, in Z-order.
        /// Only top-level windows are considered, a handle of any other window is rejected.
        /// </summary>
        internal static WindowDetails FindWindow(string handleParameter, string titleParameter, string processParameter, out string error)
        {
            error = null;
            IntPtr wantedHandle = IntPtr.Zero;
            if (handleParameter != null && !TryParseHandle(handleParameter, out wantedHandle))
            {
                error = $"'{handleParameter}' is not a window handle, use a handle from list_windows (e.g. 0x1A2B3C).";
                return null;
            }

            foreach (var window in WindowDetails.GetTopLevelWindows())
            {
                if (wantedHandle != IntPtr.Zero && window.Handle != wantedHandle)
                {
                    continue;
                }
                if (titleParameter != null && (window.Text ?? string.Empty).IndexOf(titleParameter, StringComparison.CurrentCultureIgnoreCase) < 0)
                {
                    continue;
                }
                if (processParameter != null && !string.Equals(StripExe(GetProcessName(window)), StripExe(processParameter), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                return window;
            }

            error = "No window matches, use list_windows to see the windows.";
            return null;
        }

        internal static bool TryParseHandle(string value, out IntPtr handle)
        {
            handle = IntPtr.Zero;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
            string text = value.Trim();
            long number;
            bool parsed = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? long.TryParse(text.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out number)
                : long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out number);
            if (!parsed || number == 0)
            {
                return false;
            }
            handle = new IntPtr(number);
            return true;
        }

        internal static string FormatHandle(IntPtr handle)
        {
            return "0x" + handle.ToInt64().ToString("X", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// The process name (without .exe) of the window; works for elevated processes too.
        /// </summary>
        internal static string GetProcessName(WindowDetails window)
        {
            try
            {
                string path = window.ProcessPath;
                if (!string.IsNullOrEmpty(path))
                {
                    return Path.GetFileNameWithoutExtension(path);
                }
            }
            catch (Exception ex)
            {
                Log.Debug($"Could not get the process path of window {window.Handle}", ex);
            }

            try
            {
                using var process = window.Process;
                return process?.ProcessName ?? string.Empty;
            }
            catch (Exception ex)
            {
                Log.Debug($"Could not get the process of window {window.Handle}", ex);
                return string.Empty;
            }
        }

        private static string StripExe(string processName)
        {
            string name = (processName ?? string.Empty).Trim();
            return name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name.Substring(0, name.Length - 4) : name;
        }

        /// <summary>
        /// The exact contents of the window: Windows Graphics Capture when supported (also for covered windows, independent of
        /// the UseWindowsGraphicsCapture setting), otherwise the legacy window capture.
        /// </summary>
        private static async Task<Bitmap> CaptureWindowAsync(WindowDetails window, CancellationToken cancellationToken)
        {
            if (WindowsGraphicsCaptureInterop.IsSupported)
            {
                try
                {
                    var bitmap = await WindowsGraphicsCaptureInterop.CaptureWindowToBitmapAsync(window.Handle, cancellationToken).ConfigureAwait(false);
                    if (bitmap != null)
                    {
                        return bitmap;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Log.Warn($"Windows Graphics Capture failed for window {window.Handle}, using the legacy window capture.", ex);
                }
            }

            using var capture = await WindowCaptureHelper.CaptureWindowAsync(window, new Capture(), WindowCaptureMode.Auto, null, cancellationToken).ConfigureAwait(false);
            return capture?.Image == null ? null : new Bitmap(capture.Image);
        }

        private static async Task<Bitmap> CaptureBoundsAsync(NativeRect bounds, CancellationToken cancellationToken)
        {
            using var capture = await WindowCapture.CaptureRectangleAsync(new Capture(), bounds, cancellationToken).ConfigureAwait(false);
            return capture?.Image == null ? null : new Bitmap(capture.Image);
        }

        /// <summary>
        /// Blacks out the visible windows of excluded processes in a screen or region capture.
        /// </summary>
        /// <returns>The number of windows which were blacked out</returns>
        private static int RedactExcludedWindows(Bitmap image, NativeRect bounds)
        {
            if (image == null)
            {
                return 0;
            }
            int redacted = 0;
            using var graphics = Graphics.FromImage(image);
            // All visible windows, also untitled popups and tool windows (e.g. a password manager's quick access)
            foreach (var window in WindowDetails.GetVisibleWindows())
            {
                if (window.Iconic || !AiToolAccess.IsProcessExcluded(GetProcessName(window)))
                {
                    continue;
                }
                var overlap = window.WindowRectangle.Intersect(bounds);
                if (overlap.IsEmpty)
                {
                    continue;
                }
                graphics.FillRectangle(Brushes.Black, overlap.X - bounds.X, overlap.Y - bounds.Y, overlap.Width, overlap.Height);
                redacted++;
            }
            return redacted;
        }

        private static async Task<(List<IOcrLineFeature> Lines, string Error)> RunOcrAsync(Image image)
        {
            var ocrProvider = SimpleServiceProvider.Current?.GetInstance<IOcrProvider>(isOptional: true);
            if (ocrProvider == null)
            {
                return (null, "OCR is not available on this system.");
            }
            try
            {
                var lines = await ocrProvider.DoOcrAsync(image).ConfigureAwait(false);
                return (lines ?? new List<IOcrLineFeature>(), null);
            }
            catch (Exception ex)
            {
                Log.Warn("OCR for an AI tool capture failed", ex);
                return (null, $"OCR failed: {ex.Message}");
            }
        }

        private static Bitmap Scale(Image image, double scale)
        {
            int width = Math.Max(1, (int)Math.Round(image.Width * scale));
            int height = Math.Max(1, (int)Math.Round(image.Height * scale));
            var result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(result);
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.DrawImage(image, 0, 0, width, height);
            return result;
        }

        private static Task ReplyErrorAsync(IpcRequestContext context, string message, CancellationToken cancellationToken)
        {
            return context.ReplyAsync(new
            {
                status = "error",
                exit_code = 1,
                stderr = message
            }, cancellationToken);
        }

        private static string GetParameter(IDictionary<string, string> parameters, string name)
        {
            return parameters.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;
        }

        private static int? GetInt(IDictionary<string, string> parameters, string name)
        {
            string value = GetParameter(parameters, name);
            return value != null && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) ? result : (int?)null;
        }

        private static bool GetBool(IDictionary<string, string> parameters, string name)
        {
            string value = GetParameter(parameters, name);
            return value != null && (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1" || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase));
        }
    }
}
