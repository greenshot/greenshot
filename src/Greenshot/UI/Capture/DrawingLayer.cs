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

using System.Windows;
using System.Windows.Media;

namespace Greenshot.UI.Capture
{
    /// <summary>
    /// A layer which is drawn directly: redrawing it doesn't cause a layout pass, which keeps the selection smooth.
    /// </summary>
    public sealed class DrawingLayer : FrameworkElement
    {
        private readonly DrawingVisual _visual = new DrawingVisual();

        public DrawingLayer()
        {
            IsHitTestVisible = false;
            AddVisualChild(_visual);
        }

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index) => _visual;

        /// <summary>
        /// Replace the content of the layer, dispose the DrawingContext to show it
        /// </summary>
        public DrawingContext Open() => _visual.RenderOpen();

        /// <summary>
        /// Remove the content of the layer
        /// </summary>
        public void Clear()
        {
            using (_visual.RenderOpen())
            {
            }
        }
    }
}
