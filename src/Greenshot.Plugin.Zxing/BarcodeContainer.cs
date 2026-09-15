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
using System.Drawing;
using System.Drawing.Drawing2D;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Helpers;
using ZXing;

namespace Greenshot.Plugin.Zxing
{
    /// <summary>
    /// A vector-like container for 1D and 2D barcodes that dynamically re-renders its bitmap
    /// at the exact target dimensions upon resizing, maintaining crisp pixel clarity and
    /// locking aspect ratio for 2D matrix formats (e.g. QR codes).
    /// </summary>
    [Serializable]
    public class BarcodeContainer : ImageContainer, IHaveScaleOptions
    {
        private ZxingModel _model;
        private int _margin = 4;

        public ZxingModel Model
        {
            get => _model;
            set
            {
                _model = value;
                Tag = value;
                RegenerateBarcode();
            }
        }

        public int Margin
        {
            get => _margin;
            set
            {
                _margin = value;
                RegenerateBarcode();
            }
        }

        public bool Is2D => _model == null || _model.FormatIndex <= 3;

        public BarcodeContainer(ISurface parent, ZxingModel model, int margin = 4) : base(parent)
        {
            _model = model;
            _margin = margin;
            Tag = model;
        }

        public BarcodeContainer(ISurface parent) : base(parent)
        {
        }

        public void RegenerateBarcode()
        {
            if (_model == null) return;

            int targetW = Math.Max(1, Width);
            int targetH = Math.Max(1, Height);

            if (Is2D)
            {
                // Ensure 1:1 square matrix for 2D barcodes to prevent distortion
                int side = Math.Min(targetW, targetH);
                targetW = side;
                targetH = side;
            }

            var format = ZxingEditorForm.MapFormatIndex(_model.FormatIndex);
            string payload = _model.GetPayloadString();
            if (string.IsNullOrEmpty(payload))
            {
                payload = _model.RawText ?? "https://getgreenshot.org";
            }

            var bmp = ZxingBarcodeGenerator.Generate(
                payload,
                format,
                _model.ForeColor,
                _model.BackColor,
                _model.RoundedDots,
                targetW,
                targetH,
                _margin);

            if (bmp != null)
            {
                Image = bmp;
            }
        }

        public new ScaleOptions GetScaleOptions()
        {
            return Is2D ? ScaleOptions.Rational : ScaleOptions.Default;
        }

        public override void OnDoubleClick()
        {
            if (_model != null)
            {
                _model.OnDoubleClick(this);
            }
            else
            {
                base.OnDoubleClick();
            }
        }

        public override void Draw(Graphics graphics, RenderMode rm)
        {
            int targetW = Math.Max(1, Width);
            int targetH = Math.Max(1, Height);
            if (Is2D)
            {
                int side = Math.Min(targetW, targetH);
                targetW = side;
                targetH = side;
            }

            // If the cached bitmap does not match current bounds (e.g. after user resized it), re-render cleanly
            if (Image == null || Image.Width != targetW || Image.Height != targetH)
            {
                RegenerateBarcode();
            }

            if (Image == null) return;

            // Barcode rendering quality:
            // For rounded dots: smooth AntiAlias edges.
            // For standard square modules: NearestNeighbor ensures razor-sharp 1:1 pixel grid without bicubic blur.
            if (_model != null && _model.RoundedDots)
            {
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            }
            else
            {
                graphics.SmoothingMode = SmoothingMode.None;
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            }
            graphics.PixelOffsetMode = PixelOffsetMode.Half;

            // Center square in bounds if 2D
            if (Is2D && (Width != Image.Width || Height != Image.Height))
            {
                int drawX = Left + (Width - Image.Width) / 2;
                int drawY = Top + (Height - Image.Height) / 2;
                graphics.DrawImage(Image, new Rectangle(drawX, drawY, Image.Width, Image.Height));
            }
            else
            {
                graphics.DrawImage(Image, Bounds);
            }
        }
    }
}
