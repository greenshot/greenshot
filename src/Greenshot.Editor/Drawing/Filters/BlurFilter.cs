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
using System.Drawing.Imaging;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Gdi32;
using Greenshot.Base.Core;
using Greenshot.Base.Effects;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing.Fields;
using SixLabors.ImageSharp.PixelFormats;

namespace Greenshot.Editor.Drawing.Filters
{
    [Serializable]
    public class BlurFilter : AbstractFilter
    {
        // Read and written by .greenshot files (the editor once had a preview quality, it was never used): kept so files stay compatible
        public double previewQuality;

        public BlurFilter(DrawableContainer parent) : base(parent)
        {
            AddField(GetType(), FieldType.BLUR_RADIUS, 3);
        }

        // The box blur runs twice, so a pixel is influenced by pixels up to twice the radius away
        protected override int ClipMargin => 2 * GetFieldValueAsInt(FieldType.BLUR_RADIUS);

        protected override void ApplyFilter(Graphics graphics, Bitmap applyBitmap, NativeRect applyRect, RenderMode renderMode)
        {
            int blurRadius = GetFieldValueAsInt(FieldType.BLUR_RADIUS);
            Bitmap blurred;
            if (GdiPlusApi.IsBlurPossible(blurRadius))
            {
                blurred = new Bitmap(applyRect.Width, applyRect.Height, PixelFormat.Format32bppArgb);
                using var blurredGraphics = Graphics.FromImage(blurred);
                blurredGraphics.TranslateTransform(-applyRect.X, -applyRect.Y);
                GdiPlusApi.DrawWithBlur(blurredGraphics, applyBitmap, applyRect, null, null, blurRadius, false);
            }
            else
            {
                // Blurring premultiplied pixels keeps the color of transparent pixels out of the visible ones
                var pixelFormat = Image.IsAlphaPixelFormat(applyBitmap.PixelFormat) ? PixelFormat.Format32bppPArgb : PixelFormat.DontCare;
                blurred = ImageHelper.CloneArea(applyBitmap, applyRect, pixelFormat);
                ImageHelper.ApplyBoxBlur(blurred, blurRadius);
            }
            using (blurred)
            {
                var (area, offsets) = Noise(new NativeRect(0, 0, applyBitmap.Width, applyBitmap.Height));
                bool premultiplied = blurred.PixelFormat == PixelFormat.Format32bppPArgb;
                var noiseArea = new SixLabors.ImageSharp.Rectangle(area.X, area.Y, area.Width, area.Height);
                BitmapPixels.ProcessPixelRows<Bgra32>(blurred, pixels =>
                {
                    for (int y = 0; y < pixels.Height; y++)
                    {
                        BlurNoise.AddTo(pixels.GetRowSpan(y), new SixLabors.ImageSharp.Point(applyRect.X, applyRect.Y + y), noiseArea, offsets, premultiplied);
                    }
                });
                graphics.DrawImage(blurred, applyRect, new Rectangle(0, 0, blurred.Width, blurred.Height), GraphicsUnit.Pixel);
            }
        }

        // The noise, with a key which is made once per filter and never stored (see BlurNoise)
        [NonSerialized] private BlurNoise _noise;

        /// <summary>
        /// The noise over the blur (see BlurNoise): its area is the element (the noise moves with it), for an inverted blur the image. The HDR file
        /// adds the same noise (see ElementImageFilters).
        /// </summary>
        /// <param name="imageBounds">The image</param>
        internal (NativeRect Area, sbyte[] Offsets) Noise(NativeRect imageBounds)
        {
            var area = Invert ? imageBounds : parent.Bounds.Normalize();
            _noise ??= new BlurNoise();
            return (area, _noise.For(area.Width, area.Height));
        }
    }
}