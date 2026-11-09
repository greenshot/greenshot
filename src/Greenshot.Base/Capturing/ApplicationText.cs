/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Automation;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Enums;
using Dapplo.Windows.Desktop;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Interfaces.Plugin;
using log4net;

namespace Greenshot.Base.Capturing
{
    /// <summary>
    /// The text the applications show in the capture, read through UI Automation when the screen is captured: exact, with the targets
    /// of links, where OCR guesses from the pixels. The lines are kept with the capture like OCR lines (<see cref="TextSource.UiAutomation"/>),
    /// the OCR only adds lines where there is none of this text (text in images, remote desktops, applications without UI Automation text).
    /// Only text which is visible in the captured pixels: not covered by another window, no password fields, nothing scrolled away.
    /// Neither text nor link targets are logged.
    /// </summary>
    public static class ApplicationText
    {
        /// <summary>In <see cref="ICaptureDetails.StartedProcessors"/> when the text of the applications is read for the capture</summary>
        public const string Processor = "ApplicationText";

        /// <summary>
        /// The longest the OCR waits for the applications; a window which takes longer (a browser building its accessibility tree on the
        /// first request) still adds its text when it comes, in place of the OCR lines under it
        /// </summary>
        public static readonly TimeSpan MaxWait = TimeSpan.FromSeconds(2);

        /// <summary>The longest a window gets for its text, a hanging application only costs this in the background</summary>
        public static readonly TimeSpan MaxRead = TimeSpan.FromSeconds(15);

        /// <summary>The windows read, from the top: below that the windows are mostly covered</summary>
        private const int MaxWindows = 8;

        private static readonly ILog Log = LogManager.GetLogger(typeof(ApplicationText));

        /// <summary>
        /// Start reading the text of the windows the capture shows, in the background (the task is part of
        /// <see cref="ICaptureDetails.ProcessingTask"/>, which the OCR waits for). Call it right after the screen was captured, before
        /// anything moves; the windows are those visible at that moment.
        /// </summary>
        /// <param name="capture">a capture of the screen, with its image and location</param>
        public static void Start(ICapture capture)
        {
            var captureDetails = capture?.CaptureDetails;
            if (captureDetails == null || capture.Image == null)
            {
                return;
            }
            lock (captureDetails.StartedProcessors)
            {
                if (!captureDetails.StartedProcessors.Add(Processor))
                {
                    return;
                }
            }

            var captureBounds = new NativeRect(capture.Location, new NativeSize(capture.Image.Width, capture.Image.Height));
            IReadOnlyList<NativeRect> windows;
            IReadOnlyList<IntPtr> handles;
            try
            {
                // In Z-order, the topmost first, without Greenshot's own windows
                var visible = WindowHelper.GetVisibleWindows();
                windows = visible.Select(window => window.GetInfo().Bounds).ToList();
                handles = visible.Select(window => window.Handle).ToList();
            }
            catch (Exception ex)
            {
                Log.Warn("Can't list the windows for their text", ex);
                return;
            }

            var cropAreaWhenFound = captureDetails.CropArea;
            var task = ReadAsync(captureDetails, captureBounds, handles, windows, cropAreaWhenFound);
            captureDetails.ProcessingTask = captureDetails.ProcessingTask != null ? Task.WhenAll(captureDetails.ProcessingTask, task) : task;
        }

        private static Task ReadAsync(ICaptureDetails captureDetails, NativeRect captureBounds, IReadOnlyList<IntPtr> handles,
            IReadOnlyList<NativeRect> windows, NativeRect cropAreaWhenFound)
        {
            var reads = new List<Task>();
            for (int i = 0; i < windows.Count && reads.Count < MaxWindows; i++)
            {
                var area = windows[i].Intersect(captureBounds);
                if (area.IsEmpty || IsCovered(area, windows.Take(i)))
                {
                    continue;
                }
                reads.Add(ReadWindowAsync(captureDetails, handles[i], area, captureBounds, windows.Take(i).ToList(), cropAreaWhenFound));
            }
            Log.Info($"Reading the application text of {reads.Count} windows");
            // The OCR waits at most MaxWait, the windows which take longer add their text later
            return Task.WhenAny(Task.WhenAll(reads), Task.Delay(MaxWait));
        }

        private static async Task ReadWindowAsync(ICaptureDetails captureDetails, IntPtr handle, NativeRect area, NativeRect captureBounds,
            IReadOnlyList<NativeRect> windowsAbove, NativeRect cropAreaWhenFound)
        {
            // The program (not the text) says which window is slow or gives nothing
            string program = handle.ToString();
            try
            {
                program = InteropWindowFactory.CreateFor(handle).GetProcessName() ?? program;
            }
            catch (Exception ex)
            {
                Log.Debug("No program name for the window", ex);
            }

            var watch = System.Diagnostics.Stopwatch.StartNew();
            using var timeout = new CancellationTokenSource(MaxRead);
            try
            {
                var snapshot = await UiAutomationText.ReadVisibleTextAsync(handle, area, null, null, timeout.Token).ConfigureAwait(false);
                if (snapshot == null || snapshot.RemoteDisplay != RemoteDisplayKinds.None)
                {
                    // A remote desktop only shows pixels here: the OCR reads them
                    Log.Info($"{program}: remote display ({snapshot?.RemoteDisplay}), the OCR reads it");
                    return;
                }
                var lines = ToLines(snapshot.Lines, captureBounds, windowsAbove);
                Log.Info($"{program}: {lines.Count} lines of application text ({snapshot.Lines.Count} read, complete: {snapshot.IsComplete}) in {watch.ElapsedMilliseconds} ms");
                if (lines.Count == 0)
                {
                    return;
                }
                // Cropped to the selection in the meantime: only what is in the selection is added; OCR lines under it give way
                OcrLines.AddApplicationText(captureDetails, lines, cropAreaWhenFound);
                if (captureDetails is CaptureDetails concreteDetails)
                {
                    concreteDetails.NotifyFeaturesChanged();
                }
            }
            catch (OperationCanceledException)
            {
                Log.Info($"{program}: no application text after {watch.ElapsedMilliseconds} ms, the OCR reads it");
            }
            catch (Exception ex)
            {
                Log.Warn($"{program}: no application text, the OCR reads it", ex);
            }
        }

        /// <summary>
        /// The lines in pixels of the capture, without the words another window covers
        /// </summary>
        /// <param name="lines">the lines of a window, in screen pixels</param>
        /// <param name="captureBounds">where the capture is on the screen</param>
        /// <param name="windowsAbove">the windows above the window, which cover what is below them</param>
        internal static List<IOcrLineFeature> ToLines(IEnumerable<UiAutomationTextLine> lines, NativeRect captureBounds, IReadOnlyList<NativeRect> windowsAbove)
        {
            var result = new List<IOcrLineFeature>();
            foreach (var line in lines ?? Enumerable.Empty<UiAutomationTextLine>())
            {
                var words = new List<OcrWordInfo>();
                foreach (var word in line.Words)
                {
                    if (string.IsNullOrWhiteSpace(word.Text) || word.Bounds.IsEmpty)
                    {
                        continue;
                    }
                    var center = new NativePoint(word.Bounds.Left + word.Bounds.Width / 2, word.Bounds.Top + word.Bounds.Height / 2);
                    if (!captureBounds.Contains(center) || windowsAbove.Any(window => window.Contains(center)))
                    {
                        continue;
                    }
                    words.AddRange(Split(word.Text, word.Bounds.Offset(-captureBounds.X, -captureBounds.Y), word.LinkTarget));
                }
                if (words.Count == 0)
                {
                    continue;
                }
                result.Add(new DetectedOcrLine(default(OcrBox), string.Join(" ", words.Select(word => word.Text)), words)
                {
                    Source = TextSource.UiAutomation
                });
            }
            return result;
        }

        /// <summary>
        /// The words of a text with one rectangle (an element without TextPattern gives its whole name): each word gets the share of the
        /// width its characters have, enough to find labels and to select words
        /// </summary>
        internal static IEnumerable<OcrWordInfo> Split(string text, NativeRect bounds, string linkTarget)
        {
            var parts = text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length <= 1)
            {
                yield return new OcrWordInfo(text.Trim(), bounds) { LinkTarget = linkTarget };
                yield break;
            }
            // The spaces count as characters, so the words keep their place
            double total = parts.Sum(part => part.Length) + parts.Length - 1;
            double x = bounds.Left;
            foreach (var part in parts)
            {
                double width = bounds.Width * part.Length / total;
                var box = OcrBox.FromRect(new System.Windows.Rect(x, bounds.Top, width, bounds.Height));
                yield return new OcrWordInfo(part, box) { LinkTarget = linkTarget };
                x += width + bounds.Width / total;
            }
        }

        private static bool IsCovered(NativeRect area, IEnumerable<NativeRect> windowsAbove) =>
            windowsAbove.Any(window => window.Contains(area));
    }
}
