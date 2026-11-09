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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Capture;
using Greenshot.Base.Languages;
using Greenshot.Base.Threading;
using CaptureMode = Greenshot.Base.Interfaces.CaptureMode;
using Greenshot.Base.Interfaces.Ocr;
using Greenshot.Base.Interfaces.Plugin;

namespace Greenshot.Capturing.Tools
{
    /// <summary>
    /// Select text: the text lines of the OCR are shown, a click selects a line, a drag the words in the rectangle
    /// </summary>
    public class TextCaptureTool : RegionCaptureTool
    {
        private static readonly TimeSpan OcrHintDelay = TimeSpan.FromMilliseconds(300);
        private IOcrLineFeature _hoveredLine;
        private Task _pendingOcr;
        private bool _showOcrHint;
        private readonly System.Diagnostics.Stopwatch _ocrHintTime = new();
        private DispatcherTimer _ocrHintTimer;
        private NativePoint _ocrHintOffset = new(ZoomerPlacement.Distance, ZoomerPlacement.Distance);

        public override CaptureMode Mode => CaptureMode.Text;

        /// <summary>
        /// T switches to this tool from every other tool, Enter (from the region tool) selects while it is active
        /// </summary>
        public override void Attach(ICaptureToolHost host)
        {
            base.Attach(host);
            host.RegisterKey(this, Key.T, ModifierKeys.None, () => Texts.Core.CaptureKeyText, () => host.ActivateTool(this));
        }

        public override void Activate(ICaptureToolHost host)
        {
            base.Activate(host);
            _hoveredLine = null;
            // After the window is shown, the OCR can take a moment
            _ = Dispatcher.CurrentDispatcher.BeginInvoke(new Action(EnsureOcr), DispatcherPriority.ContextIdle);
        }

        public override void Deactivate()
        {
            HideOcrHint();
            base.Deactivate();
        }

        public override void OnMouseMove()
        {
            base.OnMouseMove();
            var hoveredLine = IsSelecting ? null : FindOcrLine(Host.CursorPosition);
            // While selecting the highlighted words change with every move
            // The OCR hint follows the cursor
            bool redraw = IsSelecting || _showOcrHint || hoveredLine != _hoveredLine;
            _hoveredLine = hoveredLine;
            if (redraw)
            {
                Host.Redraw();
            }
        }

        protected override bool OnClick()
        {
            if (FindOcrLine(Host.CursorPosition) is not { } clickedLine)
            {
                return false;
            }
            // A click on a single line selects it. The rectangle around rotated text also covers parts of the lines next to it:
            // only the clicked line is kept, so its text is what was selected
            if (clickedLine.Box.IsRotated)
            {
                var features = Host.Capture.CaptureDetails.Features;
                lock (features)
                {
                    features.RemoveAll(feature => feature is IOcrLineFeature && feature != clickedLine);
                }
            }
            AcceptSelection(clickedLine.Bounds);
            return true;
        }

        protected override void AcceptSelection(NativeRect rect)
        {
            Host.Capture.CaptureDetails.CaptureMode = CaptureMode.Text;
            base.AcceptSelection(rect);
        }

        /// <summary>
        /// The text lines, with the hovered line or the selected words highlighted
        /// </summary>
        public override void Draw(DrawingContext drawingContext)
        {
            var linePen = new Pen((Brush)Host.FindResource("OcrLineBrush"), 1);
            linePen.Freeze();
            var highlightBrush = (Brush)Host.FindResource("OcrHighlightBrush");
            if (_showOcrHint)
            {
                DrawOcrHint(drawingContext);
            }
            foreach (var line in GetOcrLines())
            {
                var lineBox = line.Box;
                if (lineBox.IsEmpty)
                {
                    continue;
                }
                // The outline on the pixel grid for horizontal text, on the text for rotated text
                var lineRect = lineBox.Unrotated;
                var outline = lineBox.IsRotated ? lineRect : new Rect(line.Bounds.X + 0.5, line.Bounds.Y + 0.5, line.Bounds.Width, line.Bounds.Height);
                DrawRotated(drawingContext, lineBox, () => drawingContext.DrawRectangle(null, linePen, outline));
                if (IsSelecting)
                {
                    // Highlight the words which are selected
                    if (!lineBox.IntersectsWith(Selection))
                    {
                        continue;
                    }
                    foreach (var word in line.Words)
                    {
                        var wordBox = word.Box;
                        if (wordBox.IntersectsWith(Selection))
                        {
                            DrawRotated(drawingContext, wordBox, () => drawingContext.DrawRectangle(highlightBrush, null, wordBox.Unrotated));
                        }
                    }
                }
                else if (line == _hoveredLine)
                {
                    DrawRotated(drawingContext, lineBox, () => drawingContext.DrawRectangle(highlightBrush, null, lineRect));
                }
            }
        }

        /// <summary>
        /// Draw the unrotated rectangle of the box turned to where the text is
        /// </summary>
        private static void DrawRotated(DrawingContext drawingContext, OcrBox box, Action draw)
        {
            if (!box.IsRotated)
            {
                draw();
                return;
            }
            drawingContext.PushTransform(new RotateTransform(box.Angle, box.Center.X, box.Center.Y));
            draw();
            drawingContext.Pop();
        }

        private List<IOcrLineFeature> GetOcrLines()
        {
            var features = Host.Capture.CaptureDetails.Features;
            lock (features)
            {
                return features.OfType<IOcrLineFeature>().ToList();
            }
        }

        /// <summary>
        /// The line under the pixel: on the rotated text, not only in the rectangle around it
        /// </summary>
        private IOcrLineFeature FindOcrLine(NativePoint location)
        {
            var center = new Point(location.X + 0.5, location.Y + 0.5);
            return GetOcrLines().FirstOrDefault(line => line.Box.Contains(center));
        }

        /// <summary>
        /// Start the OCR when it didn't run yet: the text read from the applications (UI Automation) leaves parts of the capture
        /// without text, the OCR adds those
        /// </summary>
        private void EnsureOcr()
        {
            if (Host.ActiveTool != this)
            {
                return;
            }
            var captureDetails = Host.Capture.CaptureDetails;
            var processingTask = captureDetails.ProcessingTask;
            var ocrProvider = SimpleServiceProvider.Current.GetInstance<IOcrProvider>(isOptional: true);
            if (ocrProvider == null || !OcrLines.TryStartOcr(captureDetails))
            {
                if (processingTask != null && !processingTask.IsCompleted)
                {
                    // Still reading in the background (the application text, the OCR), the features changed event redraws when finished
                    ShowOcrHintAsync(processingTask).FireAndLog("Showing the OCR hint");
                }
                return;
            }
            // Started on the UI thread: the OCR result is merged and the window redrawn there
            var ocrTask = RunOcrAsync(ocrProvider, processingTask);
            captureDetails.ProcessingTask = processingTask != null ? Task.WhenAll(processingTask, ocrTask) : ocrTask;
            ShowOcrHintAsync(ocrTask).FireAndLog("Showing the OCR hint");
        }

        /// <summary>
        /// When the OCR takes longer than a moment, a panel says that the text is being read, until it is done
        /// </summary>
        private async Task ShowOcrHintAsync(Task ocr)
        {
            _pendingOcr = ocr;
            var delay = Task.Delay(OcrHintDelay);
            if (await Task.WhenAny(ocr, delay).ConfigureAwait(true) == delay && _pendingOcr == ocr && Host?.ActiveTool == this)
            {
                _showOcrHint = true;
                StartOcrHintAnimation();
            }
            // Failures are handled where the OCR runs, here only its end counts
            await Task.WhenAny(ocr).ConfigureAwait(true);
            if (_pendingOcr == ocr)
            {
                HideOcrHint();
            }
        }

        private void HideOcrHint()
        {
            _pendingOcr = null;
            if (!_showOcrHint)
            {
                return;
            }
            _showOcrHint = false;
            _ocrHintTimer?.Stop();
            Host?.Redraw();
        }

        /// <summary>
        /// The mark in the hint turns, like the busy mark of the window tool, so it is clear that something happens
        /// </summary>
        private void StartOcrHintAnimation()
        {
            _ocrHintTime.Restart();
            if (_ocrHintTimer == null)
            {
                _ocrHintTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(40) };
                _ocrHintTimer.Tick += (_, _) =>
                {
                    if (!_showOcrHint || Host?.ActiveTool != this)
                    {
                        _ocrHintTimer.Stop();
                        return;
                    }
                    Host.Redraw();
                };
            }
            _ocrHintTimer.Start();
            Host.Redraw();
        }

        /// <summary>
        /// The hint next to the cursor, where the user looks: in a corner of the cursor which is on the monitor and not under the zoomer
        /// </summary>
        private void DrawOcrHint(DrawingContext drawingContext)
        {
            var style = Host.ToolStyle;
            var text = style.CreateText(Texts.Core.OcrRunning, 10, bold: true);
            double padding = style.Scale(8);
            double radius = style.Scale(7);
            double markWidth = 2 * radius + padding;
            var size = new NativeSize((int)Math.Ceiling(markWidth + text.Width + 2 * padding), (int)Math.Ceiling(Math.Max(text.Height, 2 * radius) + 2 * padding));
            var cursor = Host.CursorPosition;
            _ocrHintOffset = ZoomerPlacement.GetOffset(cursor, _ocrHintOffset, size, Host.GetMonitorBounds(), new[] { Host.ZoomerBounds });
            var bounds = new Rect(cursor.X + _ocrHintOffset.X, cursor.Y + _ocrHintOffset.Y, size.Width, size.Height);
            style.DrawPanel(drawingContext, bounds);
            // An accent border: the hint has to be noticed
            double border = style.Scale(2);
            var accentRect = new Rect(bounds.X + border / 2, bounds.Y + border / 2, bounds.Width - border, bounds.Height - border);
            drawingContext.DrawRoundedRectangle(null, new Pen(style.Accent, border), accentRect, style.PanelCornerRadius, style.PanelCornerRadius);

            // The turning arc, once a second
            var center = new Point(bounds.X + padding + radius, bounds.Y + bounds.Height / 2);
            var angle = _ocrHintTime.ElapsedMilliseconds % 1000 * 2 * Math.PI / 1000;
            Point OnCircle(double a) => new(center.X + radius * Math.Cos(a), center.Y + radius * Math.Sin(a));
            var arc = new StreamGeometry();
            using (var context = arc.Open())
            {
                context.BeginFigure(OnCircle(angle), false, false);
                context.ArcTo(OnCircle(angle + 1.5 * Math.PI), new Size(radius, radius), 0, true, SweepDirection.Clockwise, true, false);
            }
            arc.Freeze();
            drawingContext.DrawGeometry(null, new Pen(style.Accent, style.Scale(2.5)), arc);

            drawingContext.DrawText(text, new Point(bounds.X + padding + markWidth, bounds.Y + (bounds.Height - text.Height) / 2));
        }

        private async Task RunOcrAsync(IOcrProvider ocrProvider, Task processingTask)
        {
            var capture = Host.Capture;
            var cropAreaWhenFound = capture.CaptureDetails.CropArea;
            var ocrLines = await ocrProvider.DoOcrAsync(capture.Image).ConfigureAwait(true);
            if (processingTask != null)
            {
                // The application text read in the background decides where the OCR lines are needed; its failures are logged where it runs
                await Task.WhenAny(processingTask).ConfigureAwait(true);
            }
            if (ocrLines != null && ocrLines.Any())
            {
                // Cropped to the selection in the meantime: the lines are moved with it
                OcrLines.AddRecognized(capture.CaptureDetails, ocrLines, cropAreaWhenFound);

                if (capture.CaptureDetails is CaptureDetails concreteDetails)
                {
                    concreteDetails.NotifyFeaturesChanged();
                }
            }
            Host.Redraw();
        }
    }
}
