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
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Greenshot.Base.ImageSharpDrawing
{
    /// <summary>
    /// The pictures of the editor (ImageContainer with its shadow, IconContainer, the bitmaps of SVG, metafile and emoji elements) drawn with ImageSharp
    /// into an image of any pixel type: the 8-bit sRGB picture scaled to its place, put into the image by its <see cref="ImageValues"/>.
    /// </summary>
    public static class Pictures
    {
        /// <summary>
        /// Like ImageContainer.Draw (with the shadow) and VectorGraphicsContainer.Draw: the shadow (the shape of the picture in black, 1 to 5 pixels
        /// away), then the picture scaled to the place (bicubic, or nearest neighbor like IconContainer)
        /// </summary>
        /// <param name="image">The image, any pixel type</param>
        /// <param name="values">The values of the image's pixels</param>
        /// <param name="picture">The picture, 8-bit sRGB with straight alpha</param>
        /// <param name="place">Where it goes and how big it is, in the editor's coordinates (drawn with PixelOffsetMode.HighQuality, whole pixels)</param>
        /// <param name="nearestNeighbor">Scale without mixing pixels, like IconContainer</param>
        /// <param name="shadow">Draw the shadow, like ImageContainer</param>
        public static void Draw(Image image, ImageValues values, Image<Rgba32> picture, Rectangle place, bool nearestNeighbor = false, bool shadow = false)
        {
            if (image == null || picture == null || place.Width <= 0 || place.Height <= 0)
            {
                return;
            }
            using var scaled = picture.Width == place.Width && picture.Height == place.Height
                ? picture.Clone()
                : picture.Clone(context => context.Resize(new ResizeOptions
                {
                    Size = new Size(place.Width, place.Height),
                    Mode = ResizeMode.Stretch,
                    Sampler = nearestNeighbor ? KnownResamplers.NearestNeighbor : KnownResamplers.Bicubic
                }));
            var pixels = new Vector4[place.Width * place.Height];
            scaled.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < row.Length; x++)
                    {
                        pixels[y * place.Width + x] = values.Of(row[x]);
                    }
                }
            });
            if (shadow)
            {
                foreach (var (offset, alpha) in Painting.ShadowSteps())
                {
                    Put(image, pixels, new Rectangle(place.X + offset + 1, place.Y + offset + 1, place.Width, place.Height), pixel => new Vector4(0, 0, 0, pixel.W * alpha));
                }
            }
            Put(image, pixels, place, pixel => pixel);
        }

        /// <summary>
        /// The pixels (the values of the image, straight alpha) over the image at the place
        /// </summary>
        private static void Put(Image image, Vector4[] pixels, Rectangle place, Func<Vector4, Vector4> shown)
        {
            var area = Rectangle.Intersect(place, new Rectangle(0, 0, image.Width, image.Height));
            if (area.Width <= 0 || area.Height <= 0)
            {
                return;
            }
            image.Mutate(context => context.ProcessPixelRowsAsVector4((row, start) =>
            {
                int offset = (start.Y - place.Y) * place.Width + start.X - place.X;
                for (int x = 0; x < row.Length; x++)
                {
                    var pixel = shown(pixels[offset + x]);
                    if (pixel.W > 0f)
                    {
                        row[x] = Painting.Over(pixel, row[x]);
                    }
                }
            }, area));
        }
    }
}
