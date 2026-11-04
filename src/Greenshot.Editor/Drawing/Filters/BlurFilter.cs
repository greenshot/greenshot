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
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing.Fields;
using SixLabors.ImageSharp.PixelFormats;

namespace Greenshot.Editor.Drawing.Filters
{
    [Serializable]
    public class BlurFilter : AbstractFilter
    {
        public double previewQuality;

        public double PreviewQuality
        {
            get { return previewQuality; }
            set
            {
                previewQuality = value;
                OnPropertyChanged("PreviewQuality");
            }
        }

        public BlurFilter(DrawableContainer parent) : base(parent)
        {
            AddField(GetType(), FieldType.BLUR_RADIUS, 3);
            AddField(GetType(), FieldType.PREVIEW_QUALITY, 1.0d);
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
                AddNoise(blurred, applyRect, Noise(new NativeRect(0, 0, applyBitmap.Width, applyBitmap.Height)));
                graphics.DrawImage(blurred, applyRect, new Rectangle(0, 0, blurred.Width, blurred.Height), GraphicsUnit.Pixel);
            }
        }

        /// <summary>
        /// How many 8-bit levels the noise changes a channel at most, up or down: hardly visible, but the blur can't be undone to read what is
        /// under it (undoing a blur makes small differences big)
        /// </summary>
        internal const int NoiseLevels = 3;

        // Secret key and IV for the noise, generated once per filter and never stored, so every repaint and the export use the same noise
        [NonSerialized] private byte[] _noiseKey;
        [NonSerialized] private byte[] _noiseIv;
        [NonSerialized] private sbyte[] _noise;
        [NonSerialized] private Size _noiseSize;

        /// <summary>
        /// The noise over the blur: per pixel of the area the offset of red, green and blue in 8-bit sRGB levels, row by row. The area is the
        /// element (its noise moves with it), for an inverted blur the image. The same for every repaint, the export and the HDR file
        /// (see ElementImageFilters), which adds it in the levels of the 8-bit image.
        /// </summary>
        /// <param name="imageBounds">The image</param>
        internal (NativeRect Area, sbyte[] Offsets) Noise(NativeRect imageBounds)
        {
            var area = Invert ? imageBounds : parent.Bounds.Normalize();
            if (area.Width <= 0 || area.Height <= 0)
            {
                return (area, Array.Empty<sbyte>());
            }
            var size = new Size(area.Width, area.Height);
            var noise = _noise;
            if (noise == null || _noiseSize != size)
            {
                if (_noiseKey == null)
                {
                    (_noiseKey, _noiseIv) = CryptoRandomBuffer.NewKey();
                }
                noise = new sbyte[size.Width * size.Height * 3];
                using var random = new CryptoRandomBuffer(_noiseKey, _noiseIv, 4096);
                for (int i = 0; i < noise.Length; i++)
                {
                    noise[i] = (sbyte)random.GetNextInt(-NoiseLevels, NoiseLevels);
                }
                _noiseSize = size;
                _noise = noise;
            }
            return (area, noise);
        }

        /// <summary>
        /// Add the noise to the blurred pixels, which are at the place of the image (the noise is only where its area is)
        /// </summary>
        private static void AddNoise(Bitmap blurred, NativeRect place, (NativeRect Area, sbyte[] Offsets) noise)
        {
            var area = noise.Area;
            var offsets = noise.Offsets;
            if (offsets.Length == 0)
            {
                return;
            }
            bool premultiplied = blurred.PixelFormat == PixelFormat.Format32bppPArgb;
            BitmapPixels.ProcessPixelRows<Bgra32>(blurred, pixels =>
            {
                for (int y = 0; y < pixels.Height; y++)
                {
                    int noiseY = place.Y + y - area.Y;
                    if (noiseY < 0 || noiseY >= area.Height)
                    {
                        continue;
                    }
                    var row = pixels.GetRowSpan(y);
                    for (int x = 0; x < row.Length; x++)
                    {
                        int noiseX = place.X + x - area.X;
                        if (noiseX < 0 || noiseX >= area.Width || row[x].A == 0)
                        {
                            continue;
                        }
                        int index = (noiseY * area.Width + noiseX) * 3;
                        // Premultiplied the channels go up to the alpha, and so does the noise
                        int max = premultiplied ? row[x].A : 255;
                        row[x].R = AddLevels(row[x].R, offsets[index], max);
                        row[x].G = AddLevels(row[x].G, offsets[index + 1], max);
                        row[x].B = AddLevels(row[x].B, offsets[index + 2], max);
                    }
                }
            });
        }

        private static byte AddLevels(byte value, int levels, int max) => (byte)Math.Min(Math.Max(value + levels * max / 255, 0), max);
    }
}