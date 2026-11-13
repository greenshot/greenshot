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

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Greenshot.ImageSharpDrawing
{
    /// <summary>
    /// How much of each pixel a shape covers, drawn anti-aliased by ImageSharp: the base of drawing a shape into an image of any pixel type
    /// (the pixels are mixed by the coverage, in the values of the image, e.g. linear light for an HDR image)
    /// </summary>
    public static class Coverage
    {
        /// <summary>
        /// The coverage of the shape for an image of the size, row by row, 0 (outside) to 1 (inside)
        /// </summary>
        /// <param name="shape">The shape in pixel coordinates</param>
        /// <param name="width">The width of the image</param>
        /// <param name="height">The height of the image</param>
        /// <param name="rule">Which parts of overlapping figures are inside, EvenOdd like FillMode.Alternate of GDI+</param>
        public static float[] Of(IPath shape, int width, int height, IntersectionRule rule = IntersectionRule.EvenOdd)
        {
            var coverage = new float[width * height];
            if (width <= 0 || height <= 0)
            {
                return coverage;
            }
            // RgbaVector: its values are the coverage as they are, no 8-bit steps
            using var mask = new Image<RgbaVector>(width, height);
            var options = new DrawingOptions
            {
                GraphicsOptions = new GraphicsOptions { Antialias = true },
                ShapeOptions = new ShapeOptions { IntersectionRule = rule }
            };
            mask.Mutate(context => context.Fill(options, Color.White, shape));
            mask.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < width; x++)
                    {
                        coverage[y * width + x] = row[x].A;
                    }
                }
            });
            return coverage;
        }
    }
}
