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
using System.Collections.Generic;
using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Greenshot.ImageSharpDrawing
{
    /// <summary>
    /// What the drawing routines of the editor's elements share: the editor's coordinates, its shadow, and ImageSharp pens.
    /// </summary>
    public static class Painting
    {
        /// <summary>
        /// The shape moved half a pixel right and down, for filling: the editor (GDI+ PixelOffsetMode.None) has pixel centres at whole coordinates.
        /// ImageSharp's pens draw like that by themselves, its fills have pixel edges at whole coordinates.
        /// </summary>
        public static IPath OnPixelCenters(IPath shape) => shape.Translate(0.5f, 0.5f);

        /// <inheritdoc cref="OnPixelCenters(IPath)"/>
        public static IPathCollection OnPixelCenters(IPathCollection shapes) => shapes.Translate(0.5f, 0.5f);

        /// <summary>
        /// The shadow of the editor's elements (DrawableContainer.DrawShadow): five steps, each one pixel further right and down, from 100 to 20 of 255
        /// opaque black, drawn over each other
        /// </summary>
        public static IEnumerable<(int Offset, float Alpha)> ShadowSteps()
        {
            for (int step = 0; step < 5; step++)
            {
                yield return (step, (100 - 20 * step) / 255f);
            }
        }

        /// <summary>
        /// Draws the shadow steps of a shape, like painting it once per step: the shape is painted once, opaque, and the steps
        /// are blended from that coverage. ImageSharp fills and pens are several times faster with an opaque color, and run once
        /// instead of five times
        /// </summary>
        /// <param name="context">What the shadow is drawn on</param>
        /// <param name="color">The color of the shadow, its alpha is ignored: each step has its own</param>
        /// <param name="shift">Where the first step is, right and down from the shape</param>
        /// <param name="paint">Paints the shape in the color, moved right and down by the offset</param>
        public static void Shadow(IImageProcessingContext context, Color color, int shift, Action<IImageProcessingContext, Color, int> paint)
        {
            const int last = 4;
            var size = context.GetCurrentSize();
            // The coverage of the shape moved by the furthest step, with room so that the nearer steps find it too
            using var coverage = new Image<Rgba32>(size.Width + last, size.Height + last);
            coverage.Mutate(mask => paint(mask, Color.Black, shift + last));
            // What of the shadow lets through, per step and coverage
            var through = new float[last + 1][];
            foreach (var (offset, alpha) in ShadowSteps())
            {
                through[offset] = new float[256];
                for (int covered = 0; covered < 256; covered++)
                {
                    through[offset][covered] = 1 - alpha * covered / 255f;
                }
            }

            var shade = color.ToPixel<RgbaVector>();
            // 8 bits are enough for an sRGB shadow and blend fastest, brighter values (HDR) need floats
            if (shade.R <= 1 && shade.G <= 1 && shade.B <= 1)
            {
                Shadow(context, coverage, through, alpha => new Rgba32(shade.R, shade.G, shade.B, alpha));
            }
            else
            {
                Shadow(context, coverage, through, alpha => new RgbaVector(shade.R, shade.G, shade.B, alpha));
            }
        }

        private static void Shadow<TPixel>(IImageProcessingContext context, Image<Rgba32> coverage, float[][] through, Func<float, TPixel> shaded)
            where TPixel : unmanaged, IPixel<TPixel>
        {
            const int last = 4;
            var size = context.GetCurrentSize();
            using var shadow = new Image<TPixel>(size.Width, size.Height);
            bool any = false;
            coverage.ProcessPixelRows(shadow, (masks, target) =>
            {
                for (int y = 0; y < target.Height; y++)
                {
                    var row = target.GetRowSpan(y);
                    // Step n is the coverage moved n pixels back from the furthest step
                    var mask0 = masks.GetRowSpan(y + last).Slice(last);
                    var mask1 = masks.GetRowSpan(y + last - 1).Slice(last - 1);
                    var mask2 = masks.GetRowSpan(y + last - 2).Slice(last - 2);
                    var mask3 = masks.GetRowSpan(y + last - 3).Slice(last - 3);
                    var mask4 = masks.GetRowSpan(y);
                    for (int x = 0; x < row.Length; x++)
                    {
                        byte c0 = mask0[x].A, c1 = mask1[x].A, c2 = mask2[x].A, c3 = mask3[x].A, c4 = mask4[x].A;
                        if ((c0 | c1 | c2 | c3 | c4) == 0)
                        {
                            continue;
                        }

                        // Each step blended over the ones before it: what still shows through is the product
                        float shows = through[0][c0] * through[1][c1] * through[2][c2] * through[3][c3] * through[4][c4];
                        any = true;
                        row[x] = shaded(1 - shows);
                    }
                }
            });
            if (any)
            {
                context.DrawImage(shadow, 1);
            }
        }

        /// <summary>
        /// Black with the alpha, the color of the shadow
        /// </summary>
        public static Color Black(float alpha) => Color.FromPixel(new RgbaVector(0, 0, 0, alpha));

        /// <summary>
        /// The width of a pen in GDI+: a pen of width 0 draws one pixel wide
        /// </summary>
        public static float PenWidth(float width) => width > 0 ? width : 1;

        /// <summary>
        /// A solid pen, by default with pointed corners and flat ends like a GDI+ pen
        /// </summary>
        public static Pen Pen(Color color, float width, JointStyle join = JointStyle.Miter, EndCapStyle cap = EndCapStyle.Butt) =>
            new SolidPen(new PenOptions(color, width) { JointStyle = join, EndCapStyle = cap });

        /// <summary>
        /// The shape turned (moved, scaled) by the transform, in the editor's coordinates: turned before <see cref="OnPixelCenters(IPath)"/> and before
        /// the offset of a shadow, so the shadow falls the same way as that of an element which isn't turned
        /// </summary>
        public static IPath Turned(IPath shape, Matrix3x2? turn) => turn is Matrix3x2 transform ? shape.Transform(transform) : shape;

        /// <inheritdoc cref="Turned(IPath, Matrix3x2?)"/>
        public static IPathCollection Turned(IPathCollection shapes, Matrix3x2? turn) => turn is Matrix3x2 transform ? shapes.Transform(transform) : shapes;
    }
}
