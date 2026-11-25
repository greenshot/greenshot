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
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;
using Greenshot.Plugin.Zxing.Processing;

namespace Greenshot.Plugin.Zxing.Drawing
{
    /// <summary>
    /// A vector-like container for 1D and 2D barcodes that dynamically re-renders its bitmap
    /// at the exact target dimensions upon resizing, maintaining crisp pixel clarity and
    /// locking aspect ratio for 2D matrix formats (e.g. QR codes).
    /// </summary>
    [Serializable]
    public class BarcodeContainer : ImageContainer, IHaveScaleOptions, IHdrDrawable
    {
        private ZxingModel _model;
        private int _margin = 1;

        public ZxingModel Model
        {
            get => _model;
            set
            {
                _model = value;
                if (value != null)
                {
                    _margin = value.Margin;
                }
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
                if (_model != null)
                {
                    _model.Margin = value;
                }
                RegenerateBarcode();
            }
        }

        public bool Is2D => _model == null || _model.FormatIndex <= 3;

        public BarcodeContainer(ISurface parent, ZxingModel model, int margin = 1) : base(parent)
        {
            _model = model;
            _margin = model != null ? model.Margin : margin;
            Tag = model;
        }

        public BarcodeContainer(ISurface parent) : base(parent)
        {
        }

        /// <summary>
        /// No shadow: the barcode is drawn as it is (see Draw), a shadow setting would do nothing
        /// </summary>
        protected override void InitializeFields()
        {
        }

        public void RegenerateBarcode()
        {
            var bmp = ZxingBarcodeGenerator.Render(CreateLayout());
            if (bmp != null)
            {
                Image = bmp;
            }
        }

        /// <summary>
        /// The layout of the barcode at the current size of the element, null without a model
        /// </summary>
        private BarcodeLayout CreateLayout()
        {
            if (_model == null) return null;

            int targetW = Math.Max(1, Width);
            int targetH = Math.Max(1, Height);

            if (Is2D)
            {
                // Ensure 1:1 square matrix for 2D barcodes to prevent distortion
                int side = Math.Min(targetW, targetH);
                targetW = side;
                targetH = side;
            }

            var format = ZxingBarcodeGenerator.MapFormatIndex(_model.FormatIndex);
            string payload = _model.GetPayloadString();
            if (string.IsNullOrEmpty(payload))
            {
                payload = _model.RawText ?? "https://getgreenshot.org";
            }

            return ZxingBarcodeGenerator.CreateLayout(
                payload,
                format,
                _model.ForeColor,
                _model.BackColor,
                _model.RoundedDots,
                targetW,
                targetH,
                _margin);
        }

        /// <summary>
        /// The barcode drawn with ImageSharp (e.g. in an HDR file), from the same layout as the bitmap of the editor (see Draw): filling its
        /// place, a 2D code which isn't as big as its place in the middle of it. Without a model (nothing to lay out) its bitmap as a picture.
        /// </summary>
        public Action<SixLabors.ImageSharp.Image, Greenshot.ImageSharpDrawing.ImageValues> CreateHdrDrawing(Dapplo.Windows.Common.Structs.NativePoint origin)
        {
            var layout = CreateLayout();
            if (layout == null)
            {
                if (Image is not Bitmap bitmap)
                {
                    return null;
                }
                // The pixels as a managed copy (taken now, the bitmap may change): the picture is only made while it is drawn, and disposed then
                SixLabors.ImageSharp.PixelFormats.Rgba32[] pixels;
                int pictureWidth, pictureHeight;
                using (var converted = Greenshot.Base.Core.ImageSharpHelper.ConvertToImageSharp(bitmap))
                using (var rgba = converted.CloneAs<SixLabors.ImageSharp.PixelFormats.Rgba32>())
                {
                    pictureWidth = rgba.Width;
                    pictureHeight = rgba.Height;
                    pixels = new SixLabors.ImageSharp.PixelFormats.Rgba32[pictureWidth * pictureHeight];
                    rgba.CopyPixelDataTo(pixels);
                }
                var bounds = new SixLabors.ImageSharp.Rectangle(Left - origin.X, Top - origin.Y, Width, Height);
                return (image, values) =>
                {
                    using var picture = SixLabors.ImageSharp.Image.LoadPixelData<SixLabors.ImageSharp.PixelFormats.Rgba32>(pixels, pictureWidth, pictureHeight);
                    Greenshot.ImageSharpDrawing.Pictures.Draw(image, values, picture, bounds, true, false);
                };
            }
            var place = Is2D && (Width != layout.Width || Height != layout.Height)
                ? new SixLabors.ImageSharp.Rectangle(Left + (Width - layout.Width) / 2 - origin.X, Top + (Height - layout.Height) / 2 - origin.Y, layout.Width, layout.Height)
                : new SixLabors.ImageSharp.Rectangle(Left - origin.X, Top - origin.Y, Width, Height);
            return (image, values) => ZxingBarcodeGenerator.Draw(image, values, layout, place);
        }

        public new ScaleOptions GetScaleOptions()
        {
            return Is2D ? ScaleOptions.Rational : ScaleOptions.Default;
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
