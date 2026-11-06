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
    /// The captured mouse cursor drawn with ImageSharp into an image of any pixel type
    /// </summary>
    public static class MouseCursors
    {
        /// <summary>
        /// Draw the cursor at its place. A cursor with alpha is a picture. A cursor with a mask (e.g. the I-beam) works like Windows draws it: where
        /// the mask is black the cursor has its color, where it is white the color says which channels of the pixel under it are inverted (none for
        /// a transparent pixel).
        /// </summary>
        /// <param name="image">The image, any pixel type</param>
        /// <param name="values">The values of the image's pixels, an inverted pixel is its sRGB color inverted (see <see cref="ImageValues.Inverted"/>)</param>
        /// <param name="color">The color layer of the cursor, 8-bit sRGB</param>
        /// <param name="mask">The mask layer (AND mask), null for a cursor with alpha</param>
        /// <param name="place">Where it goes and how big it is</param>
        public static void Draw(Image image, ImageValues values, Image<Rgba32> color, Image<Rgba32> mask, Rectangle place)
        {
            if (image == null || values == null || color == null || place.Width <= 0 || place.Height <= 0)
            {
                return;
            }
            if (mask == null)
            {
                Pictures.Draw(image, values, color, place);
                return;
            }
            var area = Rectangle.Intersect(place, new Rectangle(0, 0, image.Width, image.Height));
            if (area.Width <= 0 || area.Height <= 0)
            {
                return;
            }
            // The masks are bits: scaled without mixing
            using var scaledColor = color.Clone(context => context.Resize(place.Size, KnownResamplers.NearestNeighbor, false));
            using var scaledMask = mask.Clone(context => context.Resize(place.Size, KnownResamplers.NearestNeighbor, false));
            image.Mutate(context => context.ProcessPixelRowsAsVector4((row, start) =>
            {
                int y = start.Y - place.Y;
                for (int x = 0; x < row.Length; x++)
                {
                    var cursorColor = scaledColor[start.X + x - place.X, y];
                    if (scaledMask[start.X + x - place.X, y].B < 128)
                    {
                        row[x] = values.Of(new Rgba32(cursorColor.R, cursorColor.G, cursorColor.B, 255));
                        continue;
                    }
                    bool invertRed = cursorColor.R >= 128, invertGreen = cursorColor.G >= 128, invertBlue = cursorColor.B >= 128;
                    if (!invertRed && !invertGreen && !invertBlue)
                    {
                        continue;
                    }
                    var inverted = values.Inverted(row[x]);
                    row[x] = new Vector4(invertRed ? inverted.X : row[x].X, invertGreen ? inverted.Y : row[x].Y, invertBlue ? inverted.Z : row[x].Z, 1f);
                }
            }, area));
        }
    }
}
