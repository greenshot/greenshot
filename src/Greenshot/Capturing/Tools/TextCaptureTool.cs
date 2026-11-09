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
            var cropAreaWhenFound = capture.CaptureDetails.CropArea;
            var ocrLines = await ocrProvider.DoOcrAsync(capture.Image).ConfigureAwait(true);
            if (ocrLines != null && ocrLines.Any())
            {
                // Cropped to the selection in the meantime: the lines are moved with it
                capture.CaptureDetails.AddFeatures(ocrLines, cropAreaWhenFound);

                if (capture.CaptureDetails is CaptureDetails concreteDetails)
                {
                    concreteDetails.NotifyFeaturesChanged();
                }
            }
            Host.Redraw();
        }
    }
}
