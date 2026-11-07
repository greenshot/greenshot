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
using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Greenshot.Base.Effects
{
    /// <summary>
    /// ImageSharp processing which mixes pixels (resize, blur) for HDR images: done on float pixels (RgbaVector) with the values as they are.
    /// ImageSharp 2.1 mixes on "scaled" values, for HalfVector4 those are mapped from -1..1 to 0..1 with their alpha (a transparent pixel counts
    /// as half opaque, highlights are changed); CloneAs converts through them too. On RgbaVector the scaled values are the values themselves.
    /// </summary>
    public static class FloatPixels
    {
        /// <summary>
        /// An area of the image as float pixels, any pixel type, the values as they are (e.g. scRGB of an HDR image)
        /// </summary>
        public static Image<RgbaVector> Of(Image source, Rectangle area)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (source is Image<HalfVector4> halfVectors)
            {
                // Only read: the pixels of an HDR image are shared
                return Of<HalfVector4>(halfVectors, area);
            }
            area = Rectangle.Intersect(area, new Rectangle(0, 0, source.Width, source.Height));
            var values = new Vector4[Math.Max(area.Width, 0) * Math.Max(area.Height, 0)];
            if (values.Length > 0)
            {
                // Reads the rows; they are written back unchanged (lossless for HalfVector4 and 8-bit pixels)
                source.Mutate(context => context.ProcessPixelRowsAsVector4((row, start) =>
                    row.CopyTo(values.AsSpan((start.Y - area.Y) * area.Width, area.Width)), area));
            }
            var floats = new Image<RgbaVector>(Math.Max(area.Width, 1), Math.Max(area.Height, 1));
            floats.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < area.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < area.Width; x++)
                    {
                        var value = values[y * area.Width + x];
                        row[x] = new RgbaVector(value.X, value.Y, value.Z, value.W);
                    }
                }
            });
            return floats;
        }

        /// <summary>
        /// An area of the image as float pixels, the values as they are; the image is only read
        /// </summary>
        public static Image<RgbaVector> Of<TPixel>(Image<TPixel> source, Rectangle area) where TPixel : unmanaged, IPixel<TPixel>
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            area = Rectangle.Intersect(area, new Rectangle(0, 0, source.Width, source.Height));
            var floats = new Image<RgbaVector>(Math.Max(area.Width, 1), Math.Max(area.Height, 1));
            if (area.Width <= 0 || area.Height <= 0)
            {
                return floats;
            }
            floats.ProcessPixelRows(source, (target, pixels) =>
            {
                for (int y = 0; y < area.Height; y++)
                {
                    var targetRow = target.GetRowSpan(y);
                    var pixelRow = pixels.GetRowSpan(area.Y + y).Slice(area.X, area.Width);
                    for (int x = 0; x < area.Width; x++)
                    {
                        var value = pixelRow[x].ToVector4();
                        targetRow[x] = new RgbaVector(value.X, value.Y, value.Z, value.W);
                    }
                }
            });
            return floats;
        }

        /// <summary>
        /// Change the float pixels by the processing (e.g. a blur), and clean up after it: a bicubic resize overshoots the alpha (it is kept between
        /// 0 and 1), and a fully transparent pixel can come out as 0/0 (NaN, it is transparent black then)
        /// </summary>
        public static void Mutate(Image<RgbaVector> floats, Action<IImageProcessingContext> process)
        {
            floats.Mutate(process);
            floats.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < row.Length; x++)
                    {
                        var pixel = row[x];
                        if (!(pixel.A > 0f) || float.IsNaN(pixel.R) || float.IsNaN(pixel.G) || float.IsNaN(pixel.B))
                        {
                            row[x] = default;
                        }
                        else if (pixel.A > 1f)
                        {
                            row[x].A = 1f;
                        }
                    }
                }
            });
        }

        /// <summary>
        /// A copy of the image changed by the processing (e.g. a resize), done on float pixels (see <see cref="Mutate"/>), in the pixel type and the
        /// configuration of the image
        /// </summary>
        public static Image<TPixel> Clone<TPixel>(Image<TPixel> source, Action<IImageProcessingContext> process) where TPixel : unmanaged, IPixel<TPixel>
        {
            using var floats = Of<TPixel>(source, new Rectangle(0, 0, source.Width, source.Height));
            Mutate(floats, process);
            var result = new Image<TPixel>(source.GetConfiguration(), floats.Width, floats.Height);
            result.ProcessPixelRows(floats, (target, values) =>
            {
                for (int y = 0; y < target.Height; y++)
                {
                    var targetRow = target.GetRowSpan(y);
                    var valueRow = values.GetRowSpan(y);
                    for (int x = 0; x < targetRow.Length; x++)
                    {
                        targetRow[x].FromVector4(valueRow[x].ToVector4());
                    }
                }
            });
            return result;
        }
    }
}
