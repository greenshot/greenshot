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
using System.Windows.Media.Imaging;
using Greenshot.Core.Enums;
using Greenshot.Core.Interfaces;

namespace Greenshot.Core.Templates
{
    /// <summary>
    /// A template to create a FrameworkElement from a capture
    /// </summary>
    public class SimpleTemplate : ITemplate<BitmapSource>
    {
        /// <summary>
        /// Specify if the mouse should be shown
        /// </summary>
        public bool DisplayMouse { get; set; } = true;

        /// <inheritdoc/>
        public FrameworkElement Apply(ICapture<BitmapSource> capture)
        {
            var canvas = new Canvas
            {
                Width = capture.Bounds.Width,
                Height = capture.Bounds.Height,
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
            canvas.Measure(capture.Bounds.Size);
            canvas.Arrange(capture.Bounds);
            canvas.UpdateLayout();

            return canvas;
        }
    }
}
