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
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Greenshot.Base.Wpf
{
    /// <summary>
    /// A small picture of all of the content with the visible part marked, for a view which zooms and pans (the viewer, the
    /// editor). Clicking or dragging on it asks the view to show that point in its middle.
    /// </summary>
    public class ZoomOverview : FrameworkElement
    {
        /// <summary>
        /// The longest side of the overview in device independent pixels
        /// </summary>
        public const double DefaultSize = 160;

        public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(nameof(Content), typeof(Brush), typeof(ZoomOverview),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ContentSizeProperty = DependencyProperty.Register(nameof(ContentSize), typeof(Size), typeof(ZoomOverview),
            new FrameworkPropertyMetadata(Size.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty VisibleAreaProperty = DependencyProperty.Register(nameof(VisibleArea), typeof(Rect), typeof(ZoomOverview),
            new FrameworkPropertyMetadata(Rect.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty LongestSideProperty = DependencyProperty.Register(nameof(LongestSide), typeof(double), typeof(ZoomOverview),
            new FrameworkPropertyMetadata(DefaultSize, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        private bool _dragging;

        public ZoomOverview()
        {
            Cursor = Cursors.Hand;
            ThemeManager.Instance.PaletteChanged += () => _ = Dispatcher.BeginInvoke(new Action(InvalidateVisual));
        }

        /// <summary>
        /// What is shown, stretched over the overview: an ImageBrush of the capture, a DrawingBrush of a document
        /// </summary>
        public Brush Content
        {
            get => (Brush)GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        /// <summary>
        /// The size of the content in its own pixels
        /// </summary>
        public Size ContentSize
        {
            get => (Size)GetValue(ContentSizeProperty);
            set => SetValue(ContentSizeProperty, value);
        }

        /// <summary>
        /// The part of the content which the view shows, in pixels of the content
        /// </summary>
        public Rect VisibleArea
        {
            get => (Rect)GetValue(VisibleAreaProperty);
            set => SetValue(VisibleAreaProperty, value);
        }

        /// <summary>
        /// The longest side of the overview
        /// </summary>
        public double LongestSide
        {
            get => (double)GetValue(LongestSideProperty);
            set => SetValue(LongestSideProperty, value);
        }

        /// <summary>
        /// Asks the view to show this point of the content (in its pixels) in its middle
        /// </summary>
        public event EventHandler<Point> CenterRequested;

        private double Scale
        {
            get
            {
                var content = ContentSize;
                return content.IsEmpty || content.Width <= 0 || content.Height <= 0 ? 0 : Math.Min(LongestSide / content.Width, LongestSide / content.Height);
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var content = ContentSize;
            double scale = Scale;
            // One pixel of border around the picture
            return scale <= 0 ? new Size(0, 0) : new Size(Math.Round(content.Width * scale) + 2, Math.Round(content.Height * scale) + 2);
        }

        protected override void OnRender(DrawingContext dc)
        {
            var theme = ThemeManager.Instance;
            double scale = Scale;
            if (scale <= 0)
            {
                return;
            }

            var content = ContentSize;
            var picture = new Rect(1, 1, Math.Round(content.Width * scale), Math.Round(content.Height * scale));
            dc.DrawRectangle(theme.BackgroundBrush, new Pen(theme.BorderBrush, 1), new Rect(0.5, 0.5, picture.Width + 1, picture.Height + 1));
            if (Content != null)
            {
                dc.DrawRectangle(Content, null, picture);
            }

            var visible = VisibleArea;
            if (!visible.IsEmpty)
            {
                var frame = new Rect(picture.X + visible.X * scale, picture.Y + visible.Y * scale, visible.Width * scale, visible.Height * scale);
                frame.Intersect(picture);
                if (!frame.IsEmpty)
                {
                    dc.DrawRectangle(null, new Pen(theme.AccentBrush, 2), frame);
                }
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            _dragging = CaptureMouse();
            RequestCenter(e.GetPosition(this));
            e.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragging)
            {
                RequestCenter(e.GetPosition(this));
            }
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            _dragging = false;
            ReleaseMouseCapture();
            e.Handled = true;
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            base.OnLostMouseCapture(e);
            _dragging = false;
        }

        private void RequestCenter(Point point)
        {
            double scale = Scale;
            if (scale > 0)
            {
                CenterRequested?.Invoke(this, new Point((point.X - 1) / scale, (point.Y - 1) / scale));
            }
        }
    }
}
