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
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Dapplo.Ini;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Capture;
using Greenshot.Base.Languages;

namespace Greenshot.Capturing.Overlays
{
    /// <summary>
    /// P shows or hides a panel with what will be captured: the selection with its proportions, scaled to fit a box, so a small area
    /// is shown enlarged (whole pixels, crisp) and a large one reduced. Works with every tool which shows a selection.
    /// The window calls Update when the selection changes, also from a key (e.g. another size of the fixed size capture).
    /// Whether it is shown is remembered (CaptureSelectionPreviewVisible), so it comes back with the next capture.
    /// </summary>
    public class SelectionPreviewOverlay : CaptureOverlay
    {
        private static readonly ICoreConfiguration Conf = IniConfigRegistry.GetSection<ICoreConfiguration>();
        // The box the preview fits into, device independent units; it is at least half as large in one direction
        private const double MaxWidth = 320;
        private const double MaxHeight = 200;

        private readonly BitmapSource _image;
        private bool _visible;
        private bool _panelShown;
        private NativeRect _shownArea = NativeRect.Empty;

        /// <param name="image">The frozen capture, one pixel per capture pixel</param>
        public SelectionPreviewOverlay(BitmapSource image)
        {
            _image = image;
        }

        public override void Attach(ICaptureToolHost host)
        {
            base.Attach(host);
            host.RegisterKey(this, Key.P, ModifierKeys.None, () => Texts.Core.CaptureKeySelectionPreview, Toggle);
            _visible = Conf.CaptureSelectionPreviewVisible;
        }

        public override void OnMouseMove() => Update();

        public override void OnToolChanged() => Update();

        private void Toggle()
        {
            _visible = !_visible;
            Conf.CaptureSelectionPreviewVisible = _visible;
            _shownArea = NativeRect.Empty;
            Update();
        }

        /// <summary>
        /// Show the selection as it is now, hide the panel without one; nothing happens when it didn't change
        /// </summary>
        public void Update()
        {
            if (Host == null)
            {
                return;
            }
            var area = GetArea(Host.Selection, Host.SelectionSize, new NativeRect(0, 0, _image?.PixelWidth ?? 0, _image?.PixelHeight ?? 0));
            if (!_visible || area.IsEmpty || Host.ToolStyle == null)
            {
                if (_panelShown)
                {
                    Host.HidePanel(this);
                    _panelShown = false;
                }
                _shownArea = NativeRect.Empty;
                return;
            }
            if (_panelShown && area == _shownArea)
            {
                return;
            }
            _shownArea = area;
            _panelShown = true;
            Show(area);
        }

        /// <summary>
        /// What is captured: the selection with the size the labels show (the region tool includes the pixel under the cursor), inside the capture
        /// </summary>
        public static NativeRect GetArea(NativeRect selection, NativeSize labelSize, NativeRect captureBounds)
        {
            if (selection.IsEmpty)
            {
                return NativeRect.Empty;
            }
            var size = labelSize.IsEmpty ? selection.Size : labelSize;
            var area = new NativeRect(selection.X, selection.Y, size.Width, size.Height).Intersect(captureBounds);
            return area.Width > 0 && area.Height > 0 ? area : NativeRect.Empty;
        }

        /// <summary>
        /// The factor the area is drawn with, to fit the box: a whole number when enlarging, so every pixel is a square of the same size
        /// </summary>
        public static double GetScale(NativeSize area, double maxWidth, double maxHeight)
        {
            if (area.Width <= 0 || area.Height <= 0)
            {
                return 1;
            }
            double scale = Math.Min(maxWidth / area.Width, maxHeight / area.Height);
            return scale >= 1 ? Math.Floor(scale) : scale;
        }

        private void Show(NativeRect area)
        {
            var style = Host.ToolStyle;
            double gap = style.Scale(6);
            double scale = GetScale(area.Size, style.Scale(MaxWidth), style.Scale(MaxHeight));
            // Whole pixels for the box, so the border is sharp
            double width = Math.Max(1, Math.Round(area.Width * scale));
            double height = Math.Max(1, Math.Round(area.Height * scale));
            string zoom = (scale * 100).ToString(scale < 0.1 ? "0.#" : "0", CultureInfo.CurrentCulture);
            var sizeText = style.CreateText(string.Format(CultureInfo.CurrentCulture, "{0} × {1}", area.Width, area.Height), 9);
            var zoomText = style.CreateText($"{zoom} %", 9, style.MutedForeground);

            double textHeight = Math.Max(sizeText.Height, zoomText.Height);
            double contentWidth = Math.Max(width, sizeText.Width + gap + zoomText.Width);
            var pen = new Pen(style.MutedForeground, 1);
            pen.Freeze();

            Host.ShowPanel(this, new Size(contentWidth, height + gap + textHeight), dc =>
            {
                var target = new Rect(0, 0, width, height);
                if (_image != null)
                {
                    // Like the zoomer: the area of the capture stretched onto the target
                    var brush = new ImageBrush(_image)
                    {
                        ViewboxUnits = BrushMappingMode.Absolute,
                        Viewbox = new Rect(area.X, area.Y, area.Width, area.Height),
                        Stretch = Stretch.Fill
                    };
                    RenderOptions.SetBitmapScalingMode(brush, scale >= 1 ? BitmapScalingMode.NearestNeighbor : BitmapScalingMode.HighQuality);
                    dc.DrawRectangle(brush, null, target);
                }
                dc.DrawRectangle(null, pen, new Rect(target.X - 0.5, target.Y - 0.5, target.Width + 1, target.Height + 1));
                double y = height + gap;
                dc.DrawText(sizeText, new Point(0, y + (textHeight - sizeText.Height) / 2));
                dc.DrawText(zoomText, new Point(contentWidth - zoomText.Width, y + (textHeight - zoomText.Height) / 2));
            });
        }
    }
}
