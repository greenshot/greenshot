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
        private IOcrLineFeature _hoveredLine;

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

        public override void OnMouseMove()
        {
            base.OnMouseMove();
            var hoveredLine = IsSelecting ? null : FindOcrLine(Host.CursorPosition);
            // While selecting the highlighted words change with every move
            bool redraw = IsSelecting || hoveredLine != _hoveredLine;
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
            // A click on a single line selects it
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
            foreach (var line in GetOcrLines())
            {
                var lineBounds = line.RotatedBounds;
                if (lineBounds.IsEmpty)
                {
                    continue;
                }
                DrawRotatedRect(drawingContext, null, linePen, lineBounds, true);
                if (IsSelecting)
                {
                    // Highlight the words which are selected
                    if (!lineBounds.IntersectsWith(Selection))
                    {
                        continue;
                    }
                    foreach (var word in line.Words)
                    {
                        if (word.RotatedBounds.IntersectsWith(Selection))
                        {
                            DrawRotatedRect(drawingContext, highlightBrush, null, word.RotatedBounds, false);
                        }
                    }
                }
                else if (line == _hoveredLine)
                {
                    DrawRotatedRect(drawingContext, highlightBrush, null, lineBounds, false);
                }
            }
        }

        /// <summary>
        /// Text which is not rotated as a rectangle on the pixels, rotated text as the rotated rectangle around it
        /// </summary>
        /// <param name="drawingContext">DrawingContext</param>
        /// <param name="brush">Brush to fill with, or null</param>
        /// <param name="pen">Pen for the outline, or null</param>
        /// <param name="rotatedRect">RotatedRect</param>
        /// <param name="outline">true to draw a 1 pixel outline on the pixels instead of between them</param>
        private static void DrawRotatedRect(DrawingContext drawingContext, Brush brush, Pen pen, RotatedRect rotatedRect, bool outline)
        {
            if (rotatedRect.Angle == 0)
            {
                var bounds = rotatedRect.Bounds;
                double offset = outline ? 0.5 : 0;
                drawingContext.DrawRectangle(brush, pen, new Rect(bounds.X + offset, bounds.Y + offset, bounds.Width, bounds.Height));
                return;
            }
            var corners = rotatedRect.Corners;
            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                context.BeginFigure(corners[0], brush != null, true);
                context.PolyLineTo(corners.Skip(1).ToList(), true, true);
            }
            geometry.Freeze();
            drawingContext.DrawGeometry(brush, pen, geometry);
        }

        private List<IOcrLineFeature> GetOcrLines()
        {
            var features = Host.Capture.CaptureDetails.Features;
            lock (features)
            {
                return features.OfType<IOcrLineFeature>().ToList();
            }
        }

        private IOcrLineFeature FindOcrLine(NativePoint location) => GetOcrLines().FirstOrDefault(line => line.RotatedBounds.Contains(location.X + 0.5, location.Y + 0.5));

        /// <summary>
        /// Start the OCR when there are no text lines yet, and it isn't running in the background already
        /// </summary>
        private void EnsureOcr()
        {
            if (Host.ActiveTool != this || GetOcrLines().Any())
            {
                return;
            }
            var captureDetails = Host.Capture.CaptureDetails;
            var processingTask = captureDetails.ProcessingTask;
            if (processingTask != null && !processingTask.IsCompleted)
            {
                // Already processing in the background, the features changed event redraws when finished
                return;
            }
            var ocrProvider = SimpleServiceProvider.Current.GetInstance<IOcrProvider>(isOptional: true);
            if (ocrProvider == null)
            {
                return;
            }
            // Started on the UI thread: the OCR result is merged and the window redrawn there
            var ocrTask = RunOcrAsync(ocrProvider);
            captureDetails.ProcessingTask = processingTask != null ? Task.WhenAll(processingTask, ocrTask) : ocrTask;
        }

        private async Task RunOcrAsync(IOcrProvider ocrProvider)
        {
            var capture = Host.Capture;
            var ocrLines = await ocrProvider.DoOcrAsync(capture.Image).ConfigureAwait(true);
            if (ocrLines != null && ocrLines.Any())
            {
                lock (capture.CaptureDetails.Features)
                {
                    capture.CaptureDetails.Features.AddRange(ocrLines);
                }

                if (capture.CaptureDetails is CaptureDetails concreteDetails)
                {
                    concreteDetails.NotifyFeaturesChanged();
                }
            }
            Host.Redraw();
        }
    }
}
