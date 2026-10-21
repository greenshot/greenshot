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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Dapplo.Windows.User32.Enums;

namespace Greenshot.Editor.Tabs
{
    /// <summary>
    /// The image of a dragged tab, which follows the mouse. Away from the tabs of a window it shows the tab with a small
    /// picture of the capture below it, this is the new window the tab gets when it is dropped there. Over the tabs of a window
    /// it is only the tab, placed where the tab lands.
    /// The image doesn't take the focus or the mouse, it is only shown.
    /// </summary>
    internal sealed class EditorTabDragImage : Form
    {
        private const int MaximumPreviewSize = 200;

        private readonly EditorTab _tab;
        private readonly Image _preview;
        private EditorTabStrip _strip;
        private Size _tabSize;
        private bool _showsPreview;

        public EditorTabDragImage(EditorTab tab)
        {
            _tab = tab;
            _preview = CreatePreview(tab);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            Opacity = 0.85;
            BackColor = SystemColors.Window;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
        }

        protected override bool ShowWithoutActivation => true;

        protected override CreateParams CreateParams
        {
            get
            {
                var createParams = base.CreateParams;
                createParams.ExStyle |= (int)(ExtendedWindowStyleFlags.WS_EX_TOOLWINDOW | ExtendedWindowStyleFlags.WS_EX_NOACTIVATE
                                              | ExtendedWindowStyleFlags.WS_EX_TRANSPARENT | ExtendedWindowStyleFlags.WS_EX_TOPMOST);
                return createParams;
            }
        }

        /// <summary>
        /// Show only the tab, at the place (screen coordinates) where it lands in the strip
        /// </summary>
        public void ShowInStrip(EditorTabStrip strip, Rectangle tabBounds)
        {
            Show(strip, tabBounds.Size, false, tabBounds.Location);
        }

        /// <summary>
        /// Show the tab with the picture of the capture below it, the tab is at the location (screen coordinates)
        /// </summary>
        public void ShowAsWindow(EditorTabStrip strip, Size tabSize, Point location)
        {
            Show(strip, tabSize, true, location);
        }

        private void Show(EditorTabStrip strip, Size tabSize, bool showsPreview, Point location)
        {
            bool changed = strip != _strip || tabSize != _tabSize || showsPreview != _showsPreview;
            _strip = strip;
            _tabSize = tabSize;
            _showsPreview = showsPreview;
            var previewBounds = PreviewBounds(tabSize);
            var size = showsPreview ? new Size(Math.Max(tabSize.Width, previewBounds.Right), previewBounds.Bottom) : tabSize;
            Bounds = new Rectangle(location, size);
            if (changed)
            {
                Invalidate();
            }

            if (!Visible)
            {
                Show();
            }
        }

        private Rectangle PreviewBounds(Size tabSize)
        {
            int maximum = MaximumPreviewSize * _strip.DeviceDpi / 96;
            var previewSize = _preview == null ? new Size(maximum, maximum * 2 / 3) : Fit(_preview.Size, maximum);
            return new Rectangle(0, tabSize.Height, previewSize.Width + 2, previewSize.Height + 2);
        }

        private static Size Fit(Size size, int maximum)
        {
            double factor = Math.Min(1d, (double)maximum / Math.Max(size.Width, size.Height));
            return new Size(Math.Max(1, (int)(size.Width * factor)), Math.Max(1, (int)(size.Height * factor)));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var graphics = e.Graphics;
            graphics.Clear(_showsPreview ? SystemColors.Control : SystemColors.Window);
            if (_strip == null)
            {
                return;
            }

            _strip.PaintTabImage(graphics, new Rectangle(Point.Empty, _tabSize), _tab);
            if (!_showsPreview)
            {
                return;
            }

            var previewBounds = PreviewBounds(_tabSize);
            graphics.FillRectangle(SystemBrushes.Window, previewBounds);
            if (_preview != null)
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.DrawImage(_preview, previewBounds.Left + 1, previewBounds.Top + 1, previewBounds.Width - 2, previewBounds.Height - 2);
            }

            graphics.DrawRectangle(SystemPens.ControlDark, previewBounds.Left, previewBounds.Top, previewBounds.Width - 1, previewBounds.Height - 1);
        }

        /// <summary>
        /// A small picture of the capture with its elements
        /// </summary>
        private static Image CreatePreview(EditorTab tab)
        {
            using var image = tab.GetImageForExport();
            if (image == null)
            {
                return null;
            }

            var size = Fit(image.Size, MaximumPreviewSize * 2);
            var preview = new Bitmap(size.Width, size.Height);
            using var graphics = Graphics.FromImage(preview);
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.DrawImage(image, 0, 0, size.Width, size.Height);
            return preview;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _preview?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
