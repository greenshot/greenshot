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
    /// A preview of what will be captured: the selection with its proportions, scaled to fit a box, so a small area is shown
    /// enlarged (whole pixels as sharp blocks) and a large one reduced. Works with every tool which shows a selection.
    /// P goes through: a panel in a corner, in the place of the zoomer (the window draws it there with DrawInZoomer, placed and
    /// animated like the zoomer), and off. Where the zoomer isn't shown (Z, or a tool without it) the panel is used instead.
    /// The window calls Update when the selection changes, also from a key (e.g. another size of the fixed size capture).
    /// The choice is remembered (CaptureSelectionPreviewVisible, ZoomerShowsPreview), so it comes back with the next capture.
    /// </summary>
    public class SelectionPreviewOverlay : CaptureOverlay
    {
        private static readonly ICoreConfiguration Conf = IniConfigRegistry.GetSection<ICoreConfiguration>();
        // The box the preview fits into, device independent units; it is at least half as large in one direction
        private const double MaxWidth = 320;
        private const double MaxHeight = 200;

        private BitmapSource _image;
        private readonly Func<bool> _isZoomerShown;
        private readonly Action _modeChanged;
        private bool _panelShown;
        private NativeRect _shownArea = NativeRect.Empty;

        /// <param name="image">The frozen capture, one pixel per capture pixel</param>
        /// <param name="isZoomerShown">True when the window shows the zoomer, so the preview can take its place</param>
        /// <param name="modeChanged">P changed where the preview is, the window updates the zoomer</param>
        public SelectionPreviewOverlay(BitmapSource image, Func<bool> isZoomerShown = null, Action modeChanged = null)
        {
            _image = image;
            _isZoomerShown = isZoomerShown ?? (() => false);
            _modeChanged = modeChanged;
        }

        /// <summary>
        /// The capture changed (L switches between the frozen and the live screen), null: there is nothing to preview
        /// </summary>
        public void SetImage(BitmapSource image)
        {
            _image = image;
            _shownArea = NativeRect.Empty;
            Update();
        }

        /// <summary>
        /// True when the preview is to be shown in the place of the zoomer, instead of the magnifier
        /// </summary>
        public bool ShowsInZoomer => Conf.CaptureSelectionPreviewVisible && Conf.ZoomerShowsPreview;

        public override void Attach(ICaptureToolHost host)
        {
            base.Attach(host);
            host.RegisterKey(this, Key.P, ModifierKeys.None, () => Texts.Core.CaptureKeySelectionPreview, NextMode);
        }

        public override void OnMouseMove() => Update();

        public override void OnToolChanged() => Update();

        /// <summary>
        /// P: in a corner, in the place of the zoomer, off
        /// </summary>
        private void NextMode()
        {
            if (!Conf.CaptureSelectionPreviewVisible)
            {
                Conf.CaptureSelectionPreviewVisible = true;
                Conf.ZoomerShowsPreview = false;
            }
            else if (!Conf.ZoomerShowsPreview)
            {
                Conf.ZoomerShowsPreview = true;
            }
            else
            {
                Conf.CaptureSelectionPreviewVisible = false;
                Conf.ZoomerShowsPreview = false;
            }
            _shownArea = NativeRect.Empty;
            _modeChanged?.Invoke();
            Update();
        }

        /// <summary>
        /// What is captured now, empty without a selection
        /// </summary>
        public NativeRect CurrentArea => Host == null
            ? NativeRect.Empty
            : GetArea(Host.Selection, Host.SelectionSize, new NativeRect(0, 0, _image?.PixelWidth ?? 0, _image?.PixelHeight ?? 0));

        /// <summary>
        /// Show the selection as it is now, hide the panel without one; nothing happens when it didn't change
        /// </summary>
        public void Update()
        {
            if (Host == null)
            {
                return;
            }
            var area = CurrentArea;
            bool inCorner = Conf.CaptureSelectionPreviewVisible && !(Conf.ZoomerShowsPreview && _isZoomerShown());
            if (!inCorner || area.IsEmpty || Host.ToolStyle == null)
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
                DrawImage(dc, area, target, scale);
                dc.DrawRectangle(null, pen, new Rect(target.X - 0.5, target.Y - 0.5, target.Width + 1, target.Height + 1));
                double y = height + gap;
                dc.DrawText(sizeText, new Point(0, y + (textHeight - sizeText.Height) / 2));
                dc.DrawText(zoomText, new Point(contentWidth - zoomText.Width, y + (textHeight - zoomText.Height) / 2));
            });
        }

        /// <summary>
        /// The area of the capture stretched onto the target, like the zoomer: enlarged every pixel is a sharp block, reduced it is smooth
        /// </summary>
        private void DrawImage(DrawingContext dc, NativeRect area, Rect target, double scale)
        {
            if (_image == null)
            {
                return;
            }
            var brush = new ImageBrush(_image)
            {
                ViewboxUnits = BrushMappingMode.Absolute,
                Viewbox = new Rect(area.X, area.Y, area.Width, area.Height),
                Stretch = Stretch.Fill
            };
            // The scaling mode works on a DrawingGroup, not on a brush drawn directly (nearest neighbor)
            var drawing = new DrawingGroup();
            RenderOptions.SetBitmapScalingMode(drawing, scale >= 1 ? BitmapScalingMode.NearestNeighbor : BitmapScalingMode.HighQuality);
            drawing.Children.Add(new GeometryDrawing(brush, null, new RectangleGeometry(target)));
            dc.DrawDrawing(drawing);
        }

        /// <summary>
        /// The size of the preview in the place of the zoomer: the area's proportions, fitting a square of the zoomer's size
        /// (including the frame, see DrawInZoomer)
        /// </summary>
        public static NativeSize GetZoomerSize(NativeSize area, int zoomerSize, out double scale)
        {
            int inner = Math.Max(1, zoomerSize - 2 * ZoomerFrame);
            scale = GetScale(area, inner, inner);
            return new NativeSize(
                (int)Math.Max(1, Math.Round(area.Width * scale)) + 2 * ZoomerFrame,
                (int)Math.Max(1, Math.Round(area.Height * scale)) + 2 * ZoomerFrame);
        }

        // The frame around the preview in the place of the zoomer, pixels
        private const int ZoomerFrame = 2;

        /// <summary>
        /// Draw the preview in the place of the zoomer, 0,0 is its top left; size from GetZoomerSize
        /// </summary>
        public void DrawInZoomer(DrawingContext dc, NativeRect area, NativeSize size, double scale)
        {
            // A white frame with a dark line inside, like the ring of the zoomer: visible on every background
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, size.Width, size.Height));
            var target = new Rect(ZoomerFrame, ZoomerFrame, size.Width - 2 * ZoomerFrame, size.Height - 2 * ZoomerFrame);
            dc.DrawRectangle(Brushes.Black, null, new Rect(target.X - 1, target.Y - 1, target.Width + 2, target.Height + 2));
            DrawImage(dc, area, target, scale);
        }
    }
}
