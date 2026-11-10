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
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Greenshot.Editor.Panels.Effects
{
    /// <summary>
    /// Shows a bitmap which can be zoomed and moved: the mouse wheel or + and − zoom at the mouse, dragging or the arrow keys move it,
    /// a double-click or 0 and 1 switch between all of it and the real size (one pixel of the bitmap is one pixel of the screen).
    /// A new bitmap of the same size keeps the zoom and the visible part, so a change of a setting can be compared at the same place.
    /// </summary>
    public sealed class ZoomPanImage : Border
    {
        private const double MaximumZoom = 16;
        private const double ZoomStep = 1.25;

        private readonly Canvas _canvas = new() { ClipToBounds = true, Background = Brushes.Transparent };
        private readonly Image _image = new() { Stretch = Stretch.Fill };
        private BitmapSource _source;
        private bool _fit = true;
        private double _zoom = 1;
        // Where the top left corner of the bitmap is in the view, in device independent pixels
        private Point _offset;
        private Point? _dragStart;
        private Point _dragStartOffset;

        public ZoomPanImage()
        {
            Focusable = true;
            _canvas.Children.Add(_image);
            Child = _canvas;
        }

        /// <summary>
        /// The zoom or the visible part changed
        /// </summary>
        public event EventHandler ZoomChanged;

        /// <summary>
        /// 1 is the real size
        /// </summary>
        public double Zoom => _zoom;

        /// <summary>
        /// All of the bitmap is visible, it follows the size of the view
        /// </summary>
        public bool IsFit => _fit;

        /// <summary>
        /// The bitmap to show, null shows nothing
        /// </summary>
        public BitmapSource Source
        {
            get => _source;
            set
            {
                bool sameSize = _source != null && value != null && _source.PixelWidth == value.PixelWidth && _source.PixelHeight == value.PixelHeight;
                _source = value;
                _image.Source = value;
                if (!sameSize)
                {
                    _fit = true;
                }

                UpdateLayoutOfImage();
            }
        }

        /// <summary>
        /// Show all of the bitmap
        /// </summary>
        public void ShowAll()
        {
            _fit = true;
            UpdateLayoutOfImage();
        }

        /// <summary>
        /// Show the bitmap in its real size, around the anchor (in the view) or the center
        /// </summary>
        public void ShowActualSize(Point? anchor = null) => ZoomTo(1, anchor);

        private Size Viewport => new(Math.Max(0, ActualWidth - BorderThickness.Left - BorderThickness.Right - Padding.Left - Padding.Right),
            Math.Max(0, ActualHeight - BorderThickness.Top - BorderThickness.Bottom - Padding.Top - Padding.Bottom));

        /// <summary>
        /// Device pixels per device independent pixel
        /// </summary>
        private DpiScale Dpi => VisualTreeHelper.GetDpi(this);

        private double FitZoom
        {
            get
            {
                var viewport = Viewport;
                if (_source == null || _source.PixelWidth == 0 || _source.PixelHeight == 0 || viewport.Width <= 0 || viewport.Height <= 0)
                {
                    return 1;
                }

                var dpi = Dpi;
                return Math.Min(1, Math.Min(viewport.Width * dpi.DpiScaleX / _source.PixelWidth, viewport.Height * dpi.DpiScaleY / _source.PixelHeight));
            }
        }

        /// <summary>
        /// The size of the zoomed bitmap in device independent pixels
        /// </summary>
        private Size ZoomedSize(double zoom)
        {
            var dpi = Dpi;
            return _source == null ? new Size(0, 0) : new Size(_source.PixelWidth * zoom / dpi.DpiScaleX, _source.PixelHeight * zoom / dpi.DpiScaleY);
        }

        private void ZoomTo(double zoom, Point? anchor = null)
        {
            if (_source == null)
            {
                return;
            }

            double fitZoom = FitZoom;
            zoom = Math.Max(fitZoom, Math.Min(MaximumZoom, zoom));
            var viewport = Viewport;
            var at = anchor ?? new Point(viewport.Width / 2, viewport.Height / 2);
            var oldSize = ZoomedSize(_zoom);
            var newSize = ZoomedSize(zoom);
            // The point of the bitmap under the anchor stays there, as far as the bitmap allows
            double relativeX = oldSize.Width > 0 ? (at.X - _offset.X) / oldSize.Width : 0.5;
            double relativeY = oldSize.Height > 0 ? (at.Y - _offset.Y) / oldSize.Height : 0.5;
            _offset = new Point(at.X - relativeX * newSize.Width, at.Y - relativeY * newSize.Height);
            _zoom = zoom;
            _fit = zoom <= fitZoom;
            UpdateLayoutOfImage();
        }

        private void PanBy(double dx, double dy)
        {
            _offset = new Point(_offset.X + dx, _offset.Y + dy);
            UpdateLayoutOfImage();
        }

        private void UpdateLayoutOfImage()
        {
            if (_source == null)
            {
                _image.Visibility = Visibility.Collapsed;
                Cursor = null;
                ZoomChanged?.Invoke(this, EventArgs.Empty);
                return;
            }

            if (_fit)
            {
                _zoom = FitZoom;
            }

            var viewport = Viewport;
            var size = ZoomedSize(_zoom);
            // Smaller than the view: centered, else no empty space at the sides
            double x = size.Width <= viewport.Width ? (viewport.Width - size.Width) / 2 : Math.Max(viewport.Width - size.Width, Math.Min(0, _offset.X));
            double y = size.Height <= viewport.Height ? (viewport.Height - size.Height) / 2 : Math.Max(viewport.Height - size.Height, Math.Min(0, _offset.Y));
            _offset = new Point(x, y);
            _image.Visibility = Visibility.Visible;
            _image.Width = size.Width;
            _image.Height = size.Height;
            Canvas.SetLeft(_image, x);
            Canvas.SetTop(_image, y);
            // Zoomed in the pixels stay sharp, so the details can be seen
            RenderOptions.SetBitmapScalingMode(_image, _zoom >= 1 ? BitmapScalingMode.NearestNeighbor : BitmapScalingMode.HighQuality);
            Cursor = size.Width > viewport.Width + 0.5 || size.Height > viewport.Height + 0.5 ? Cursors.SizeAll : null;
            ZoomChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            UpdateLayoutOfImage();
        }

        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
        {
            base.OnDpiChanged(oldDpi, newDpi);
            UpdateLayoutOfImage();
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            if (_source == null || e.Delta == 0)
            {
                return;
            }

            ZoomTo(e.Delta > 0 ? _zoom * ZoomStep : _zoom / ZoomStep, e.GetPosition(_canvas));
            // The panel doesn't scroll while zooming
            e.Handled = true;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (_source == null)
            {
                return;
            }

            Focus();
            if (e.ClickCount == 2)
            {
                if (_fit)
                {
                    ShowActualSize(e.GetPosition(_canvas));
                }
                else
                {
                    ShowAll();
                }

                e.Handled = true;
                return;
            }

            _dragStart = e.GetPosition(this);
            _dragStartOffset = _offset;
            CaptureMouse();
            e.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_dragStart is not { } start || !IsMouseCaptured)
            {
                return;
            }

            var position = e.GetPosition(this);
            _offset = new Point(_dragStartOffset.X + position.X - start.X, _dragStartOffset.Y + position.Y - start.Y);
            UpdateLayoutOfImage();
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (_dragStart != null)
            {
                _dragStart = null;
                ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            base.OnLostMouseCapture(e);
            _dragStart = null;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (_source == null || Keyboard.Modifiers != ModifierKeys.None && Keyboard.Modifiers != ModifierKeys.Shift)
            {
                return;
            }

            var viewport = Viewport;
            switch (e.Key)
            {
                case Key.Add:
                case Key.OemPlus:
                    ZoomTo(_zoom * ZoomStep);
                    break;
                case Key.Subtract:
                case Key.OemMinus:
                    ZoomTo(_zoom / ZoomStep);
                    break;
                case Key.D0:
                case Key.NumPad0:
                    ShowAll();
                    break;
                case Key.D1:
                case Key.NumPad1:
                    ShowActualSize();
                    break;
                case Key.Left:
                    PanBy(viewport.Width / 10, 0);
                    break;
                case Key.Right:
                    PanBy(-viewport.Width / 10, 0);
                    break;
                case Key.Up:
                    PanBy(0, viewport.Height / 10);
                    break;
                case Key.Down:
                    PanBy(0, -viewport.Height / 10);
                    break;
                default:
                    return;
            }

            e.Handled = true;
        }
    }
}
