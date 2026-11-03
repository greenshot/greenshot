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
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Greenshot.Base.ImageSharpDrawing
{
    /// <summary>
    /// Cutting parts out of an image, drawn by ImageSharp for any pixel type: the torn edges and cut marks of the editor. The pixels are mixed by
    /// the coverage of the shape in the values of the image (straight alpha), for an HDR image in linear light, so an edge keeps its HDR color.
    /// </summary>
    public static class CutDrawing
    {
        /// <summary>
        /// Make what the shape covers transparent (anti-aliased), e.g. the torn off parts or the gap of a cut mark
        /// </summary>
        public static void CutAway(Image image, IPath shape, IntersectionRule rule = IntersectionRule.EvenOdd)
        {
            if (image == null || shape == null)
            {
                return;
            }
            // The coverage only where the shape is
            var bounds = shape.Bounds;
            var area = Rectangle.Intersect(Rectangle.FromLTRB((int)Math.Floor(bounds.Left), (int)Math.Floor(bounds.Top), (int)Math.Ceiling(bounds.Right),
                (int)Math.Ceiling(bounds.Bottom)), new Rectangle(0, 0, image.Width, image.Height));
            if (area.Width <= 0 || area.Height <= 0)
            {
                return;
            }
            var coverage = Coverage.Of(shape.Translate(-area.X, -area.Y), area.Width, area.Height, rule);
            image.Mutate(context => context.ProcessPixelRowsAsVector4((row, start) =>
            {
                int offset = (start.Y - area.Y) * area.Width + start.X - area.X;
                for (int x = 0; x < row.Length; x++)
                {
                    row[x].W *= 1f - coverage[offset + x];
                }
            }, area));
        }

        /// <summary>
        /// The torn edge effect: the image cut by the outline of its teeth (anti-aliased), over its shadow and the background, like
        /// ImageHelper.CreateTornEdge and ImageHelper.CreateShadow do it for the 8-bit image
        /// </summary>
        /// <typeparam name="TPixel">The pixel type, the result has it too</typeparam>
        /// <param name="source">The image</param>
        /// <param name="outline">What stays of the image, in its coordinates (ImageHelper.CreateTornEdgeOutline)</param>
        /// <param name="size">The size of the result (bigger with a shadow)</param>
        /// <param name="imageOffset">Where the image is in the result</param>
        /// <param name="shadow">Darkness and size of the shadow (the size before it is made odd, like CreateShadow), null for none</param>
        /// <param name="background">The color under everything (straight alpha, in the values of the image), transparent for none</param>
        public static Image<TPixel> TornEdge<TPixel>(Image<TPixel> source, IPath outline, Size size, Point imageOffset, (float Darkness, int Size)? shadow,
            Vector4 background) where TPixel : unmanaged, IPixel<TPixel>
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var coverage = Coverage.Of(outline, source.Width, source.Height, IntersectionRule.NonZero);
            float[] shadowAlpha = shadow.HasValue ? CreateShadow(coverage, source.Width, source.Height, size, shadow.Value.Darkness, shadow.Value.Size) : null;

            var result = new Image<TPixel>(source.GetConfiguration(), size.Width, size.Height);
            result.ProcessPixelRows(source, (target, sourceRows) =>
            {
                for (int y = 0; y < size.Height; y++)
                {
                    var targetRow = target.GetRowSpan(y);
                    int sourceY = y - imageOffset.Y;
                    var sourceRow = sourceY >= 0 && sourceY < source.Height ? sourceRows.GetRowSpan(sourceY) : default;
                    for (int x = 0; x < size.Width; x++)
                    {
                        var color = background;
                        if (shadowAlpha != null)
                        {
                            color = Painting.Over(new Vector4(0, 0, 0, shadowAlpha[y * size.Width + x]), color);
                        }
                        int sourceX = x - imageOffset.X;
                        if (!sourceRow.IsEmpty && sourceX >= 0 && sourceX < source.Width)
                        {
                            var pixel = sourceRow[sourceX].ToVector4();
                            pixel.W *= coverage[sourceY * source.Width + sourceX];
                            color = Painting.Over(pixel, color);
                        }
                        TPixel value = default;
                        value.FromVector4(color);
                        targetRow[x] = value;
                    }
                }
            });
            return result;
        }

        /// <summary>
        /// The shadow of what stays, like ImageHelper.CreateShadow: its coverage at the darkness, at (size, size) with the odd size, blurred
        /// with a box of the odd size twice (ImageHelper.ApplyBoxBlur)
        /// </summary>
        private static float[] CreateShadow(float[] coverage, int width, int height, Size resultSize, float darkness, int shadowSize)
        {
            int oddSize = (shadowSize & 1) == 0 ? shadowSize + 1 : shadowSize;
            // Alpha 1 everywhere: the blur premultiplies, the shadow is in the red channel
            using var mask = new Image<RgbaVector>(resultSize.Width, resultSize.Height, new RgbaVector(0, 0, 0, 1));
            mask.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < height; y++)
                {
                    int maskY = y + oddSize;
                    if (maskY >= resultSize.Height)
                    {
                        break;
                    }
                    var row = accessor.GetRowSpan(maskY);
                    for (int x = 0; x < width && x + oddSize < resultSize.Width; x++)
                    {
                        float alpha = coverage[y * width + x] * darkness;
                        row[x + oddSize] = new RgbaVector(alpha, 0, 0, 1);
                    }
                }
            });
            int radius = oddSize / 2;
            if (radius > 0)
            {
                mask.Mutate(context => context.BoxBlur(radius).BoxBlur(radius));
            }
            var shadowAlpha = new float[resultSize.Width * resultSize.Height];
            mask.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < resultSize.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < resultSize.Width; x++)
                    {
                        shadowAlpha[y * resultSize.Width + x] = row[x].R;
                    }
                }
            });
            return shadowAlpha;
        }
    }
}
