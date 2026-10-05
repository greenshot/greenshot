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
using Greenshot.Base.Core;
using Greenshot.Base.Capturing;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Pipeline;
using Greenshot.Base.Triggers;
using log4net;

namespace Greenshot.Ai
{
    /// <summary>
    /// The capture rules for recipes started by AI tools (AI tool triggers), used by the Source step:
    /// a window is captured with its exact contents, and windows of excluded processes are blacked out in everything else
    /// taken from the screen.
    /// </summary>
    public static class AiToolCapture
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(AiToolCapture));

        /// <summary>
        /// True when the flow was started by an AI tool (only greenshot-mcp.exe can fire an AI tool trigger)
        /// </summary>
        public static bool IsAiToolRun(CaptureFlowContext context)
        {
            return string.Equals(context?.Trigger?.TriggerType, TriggerConfig.TypeAiTool, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The window for a WindowHandle parameter: a window (from a Window argument), a handle, or a handle as text (hex with 0x, or decimal).
        /// Only top-level windows.
        /// </summary>
        public static WindowDetails ResolveWindow(object windowHandle)
        {
            IntPtr handle;
            switch (windowHandle)
            {
                case null:
                    return null;
                case WindowDetails window:
                    handle = window.Handle;
                    break;
                case IntPtr intPtr:
                    handle = intPtr;
                    break;
                case int number:
                    handle = new IntPtr(number);
                    break;
                case long number:
                    handle = new IntPtr(number);
                    break;
                default:
                    if (!TryParseHandle(windowHandle.ToString(), out handle))
                    {
                        return null;
                    }
                    break;
            }
            if (handle == IntPtr.Zero)
            {
                return null;
            }
            var result = new WindowDetails(handle);
            return result.ProcessId == 0 || result.HasParent ? null : result;
        }

        /// <summary>
        /// Capture the window: Windows Graphics Capture when supported (only the window's contents, also for covered windows),
        /// otherwise the window's area of the screen.
        /// </summary>
        public static async Task<ICapture> CaptureWindowAsync(WindowDetails window, CancellationToken cancellationToken)
        {
            var capture = await WindowCapture.CaptureWindowAsync(window, null, cancellationToken).ConfigureAwait(false);
            capture?.CaptureDetails.AddMetaData("source", "Window");
            return capture;
        }

        /// <summary>
        /// True when the capture was taken by a backend which captures only the window's contents, nothing that covers it
        /// </summary>
        public static bool IsWindowContentOnly(ICapture capture)
        {
            if (capture?.CaptureDetails?.MetaData == null || !capture.CaptureDetails.MetaData.TryGetValue(ScreenCapture.CaptureMethodKey, out var method))
            {
                return false;
            }
            return ScreenCapture.Backends.Any(backend => backend.CapturesWindowContentOnly && string.Equals(backend.Name, method, StringComparison.Ordinal));
        }

        /// <summary>
        /// Blacks out the visible windows of excluded processes (AiToolsExcludedProcesses) in an image taken from the screen.
        /// </summary>
        /// <param name="image">The image</param>
        /// <param name="origin">Screen coordinates of the image's top left corner</param>
        /// <returns>The number of windows which were blacked out</returns>
        public static int RedactExcludedWindows(Image image, NativePoint origin)
        {
            if (image == null)
            {
                return 0;
            }
            var bounds = new NativeRect(origin.X, origin.Y, image.Width, image.Height);
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

        /// <summary>
        /// PNG of the image, scaled down so the longest side is at most maxSize pixels (0: original size).
        /// </summary>
        public static byte[] EncodePng(Image image, int maxSize, out int width, out int height)
        {
            Image toEncode = image;
            Bitmap scaled = null;
            try
            {
                if (maxSize > 0 && Math.Max(image.Width, image.Height) > maxSize)
                {
                    scaled = Scale(image, (double)maxSize / Math.Max(image.Width, image.Height));
                    toEncode = scaled;
                }
                width = toEncode.Width;
                height = toEncode.Height;
                using var stream = new MemoryStream();
                toEncode.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
            finally
            {
                scaled?.Dispose();
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

        /// <summary>
        /// The process name (without .exe) of the window; works for elevated processes too.
        /// </summary>
        public static string GetProcessName(WindowDetails window)
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
    }
}
