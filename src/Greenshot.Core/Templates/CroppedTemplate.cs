// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Greenshot.Core.Enums;
using Greenshot.Core.Interfaces;

namespace Greenshot.Core.Templates
{
    /// <summary>
    /// A template to create a FrameworkElement from a capture, with a crop applied
    /// </summary>
    public class CroppedTemplate : ITemplate<BitmapSource>
    {
        /// <summary>
        /// Specify if the mouse cursor should be displayed
        /// </summary>
        public bool DisplayMouse { get; set; } = true;

        /// <inheritdoc/>
        public FrameworkElement Apply(ICapture<BitmapSource> capture)
        {
            var width = (int)(capture.CropRect.Width + 0.5);
            var height = (int)(capture.CropRect.Height + 0.5);
            var result = new Rectangle
            {
                Width = width,
                Height = height,
                Stretch = Stretch.Fill
            };
            var visualBrush = new VisualBrush
            {
                Viewbox = capture.CropRect.IsEmpty ? capture.Bounds : capture.CropRect,
                ViewboxUnits = BrushMappingMode.Absolute,
                Viewport = new Rect(0, 0, 1, 1),
                ViewportUnits = BrushMappingMode.RelativeToBoundingBox
            };

            var canvas = new Canvas
            {
                Width = capture.Bounds.Width,
                Height = capture.Bounds.Height,
                // TODO: What clip?
                //Clip = capture.ClipArea
            };

            foreach (var captureCaptureElement in capture.CaptureElements)
            {
                // Skip mouse cursor
                if (captureCaptureElement.ElementType == CaptureElementType.Cursor && !DisplayMouse)
                {
                    continue;
                }
                var image = new Image
                {
                    Source = captureCaptureElement.Content,
                    Width = captureCaptureElement.Bounds.Width,
                    Height = captureCaptureElement.Bounds.Height
                };
                Canvas.SetLeft(image, captureCaptureElement.Bounds.Left);
                Canvas.SetTop(image, captureCaptureElement.Bounds.Top);
                canvas.Children.Add(image);
            }

            visualBrush.Visual = canvas;
            result.Fill = visualBrush;

            result.Measure(new Size(width, height));
            result.Arrange(new Rect(0, 0, width, height));
            result.UpdateLayout();

            return result;
        }
    }
}
