/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using System.Windows;
using System.Windows.Controls;
using Greenshot.Editor.Panels.ViewModels;

namespace Greenshot.Editor.Panels.Views
{
    /// <summary>
    /// Stacks the sections of the Selection panel. A section which fills the height (e.g. the emoji) gets what the others leave of the
    /// visible height, at least <see cref="MinimumFillHeight"/>; only when that doesn't fit, the panel becomes higher and scrolls.
    /// It is measured with a fixed height, so a list in it keeps its virtualization.
    /// </summary>
    public sealed class SectionsPanel : Panel
    {
        /// <summary>
        /// The height which is visible, the viewport of the ScrollViewer around the panel (that measures its content without a limit)
        /// </summary>
        public static readonly DependencyProperty VisibleHeightProperty = DependencyProperty.Register(
            nameof(VisibleHeight), typeof(double), typeof(SectionsPanel), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsMeasure));

        /// <summary>
        /// The smallest height of a section which fills the height
        /// </summary>
        public const double MinimumFillHeight = 160;

        private double _fillHeight;

        public double VisibleHeight
        {
            get => (double)GetValue(VisibleHeightProperty);
            set => SetValue(VisibleHeightProperty, value);
        }

        /// <summary>
        /// The first child which shows an expanded section that fills the height
        /// </summary>
        private UIElement FindFillChild()
        {
            foreach (UIElement child in InternalChildren)
            {
                if (child is FrameworkElement { DataContext: EditorSectionItem { FillsHeight: true, IsExpanded: true } } && child.Visibility != Visibility.Collapsed)
                {
                    return child;
                }
            }

            return null;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var fillChild = FindFillChild();
            var natural = new Size(availableSize.Width, double.PositiveInfinity);
            double width = 0;
            double othersHeight = 0;
            foreach (UIElement child in InternalChildren)
            {
                if (ReferenceEquals(child, fillChild))
                {
                    continue;
                }

                child.Measure(natural);
                width = Math.Max(width, child.DesiredSize.Width);
                othersHeight += child.DesiredSize.Height;
            }

            if (fillChild == null)
            {
                _fillHeight = 0;
                return new Size(width, othersHeight);
            }

            double visibleHeight = double.IsInfinity(availableSize.Height) ? VisibleHeight : availableSize.Height;
            _fillHeight = Math.Max(MinimumFillHeight, visibleHeight - othersHeight);
            fillChild.Measure(new Size(availableSize.Width, _fillHeight));
            width = Math.Max(width, fillChild.DesiredSize.Width);
            return new Size(width, othersHeight + _fillHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var fillChild = FindFillChild();
            double top = 0;
            foreach (UIElement child in InternalChildren)
            {
                double height = ReferenceEquals(child, fillChild) ? _fillHeight : child.DesiredSize.Height;
                child.Arrange(new Rect(0, top, finalSize.Width, height));
                top += height;
            }

            return finalSize;
        }
    }
}
