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
using Greenshot.Base.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Greenshot.Base.Effects
{
    /// <summary>
    /// The noise over a blur, so it can't be undone to read what is under it (undoing a blur makes small differences big): per pixel an offset
    /// of red, green and blue of up to <see cref="Levels"/> 8-bit sRGB levels, hardly visible. It comes from a random key which is never stored,
    /// so the same blur gets the same noise at every repaint, in the export and in the HDR file, and every blur gets its own.
    /// </summary>
    public sealed class BlurNoise
    {
        /// <summary>
        /// How many 8-bit levels the noise changes a channel at most, up or down
        /// </summary>
        public const int Levels = 3;

        private readonly byte[] _key;
        private readonly byte[] _iv;
        private Offsets _offsets;

        private sealed class Offsets
        {
            public int Width;
            public int Height;
            public sbyte[] Values;
        }

        public BlurNoise()
        {
            (_key, _iv) = CryptoRandomBuffer.NewKey();
        }

        /// <summary>
        /// The offsets for an area of the size: red, green and blue per pixel, row by row. The same for the same size, made again for another one
        /// (e.g. while an element is resized).
        /// </summary>
        public sbyte[] For(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                return Array.Empty<sbyte>();
            }
            var offsets = _offsets;
            if (offsets != null && offsets.Width == width && offsets.Height == height)
            {
                return offsets.Values;
            }
            var values = new sbyte[width * height * 3];
            using (var random = new CryptoRandomBuffer(_key, _iv, 4096))
            {
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = (sbyte)random.GetNextSmallInt(-Levels, Levels);
                }
            }
            _offsets = new Offsets { Width = width, Height = height, Values = values };
            return values;
        }

        /// <summary>
        /// Add the noise of the area to the pixels: they are at the place of the image, the noise is only where its area is
        /// </summary>
        /// <param name="pixels">The 8-bit pixels</param>
        /// <param name="place">Where the pixels are in the image</param>
        /// <param name="area">Where the noise is in the image</param>
        /// <param name="offsets">The offsets of the area, see <see cref="For"/></param>
        /// <param name="premultiplied">The pixels have premultiplied alpha: the channels, and so the noise, go up to the alpha</param>
        public static void AddTo(BitmapPixelAccessor<Bgra32> pixels, Point place, Rectangle area, sbyte[] offsets, bool premultiplied)
        {
            if (offsets.Length < area.Width * area.Height * 3)
            {
                return;
            }
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
                    int max = premultiplied ? row[x].A : 255;
                    row[x].R = AddLevels(row[x].R, offsets[index], max);
                    row[x].G = AddLevels(row[x].G, offsets[index + 1], max);
                    row[x].B = AddLevels(row[x].B, offsets[index + 2], max);
                }
            }
        }

        private static byte AddLevels(byte value, int levels, int max) => (byte)Math.Min(Math.Max(value + levels * max / 255, 0), max);
    }
}
