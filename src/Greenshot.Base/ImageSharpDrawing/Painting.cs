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
using SixLabors.ImageSharp.Processing;

namespace Greenshot.Base.ImageSharpDrawing
{
    /// <summary>
    /// Filling shapes into an image of any pixel type: the color is put over the pixels by the coverage of the shape (straight alpha), in the
    /// values of the image (see <see cref="ImageValues"/>). The base of the drawing routines of the editor's elements.
    /// </summary>
    public static class Painting
    {
        /// <summary>
        /// GDI+ with PixelOffsetMode.None (the default, most elements of the editor) has the centre of a pixel at its whole coordinates, ImageSharp at
        /// a half: a point of the editor is this much further right and down in ImageSharp. With PixelOffsetMode.Half/HighQuality both are the same.
        /// </summary>
        public const float GdiPixelCenter = 0.5f;

        // More than the vertical step of ImageSharp's anti-aliasing (16 rows per pixel), less than half a pixel
        private const float AliasedNudge = 0.1f;

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
        /// The width of a pen in GDI+: a pen of width 0 draws one pixel wide
        /// </summary>
        public static float PenWidth(float width) => width > 0 ? width : 1;

        /// <summary>
        /// Put the color over what the shape covers
        /// </summary>
        /// <param name="image">The image, any pixel type</param>
        /// <param name="shape">The shape in the pixel coordinates of ImageSharp</param>
        /// <param name="color">The color in the values of the image, straight alpha</param>
        /// <param name="antialias">False: whole pixels, like GDI+ with SmoothingMode.HighSpeed</param>
        /// <param name="clip">Only in this area, e.g. the box of a text</param>
        /// <param name="exclude">Not in this shape, by whole pixels like a GDI+ clip region (Graphics.SetClip with CombineMode.Exclude)</param>
        public static void Fill(Image image, IPath shape, Vector4 color, bool antialias = true, Rectangle? clip = null, IPath exclude = null) =>
            Fill(image, shape == null ? Array.Empty<IPath>() : new[] { shape }, color, antialias, clip, exclude);

        /// <summary>
        /// Put the color over what the shapes cover together, once also where they overlap (like one GDI+ call draws a line with its arrow heads, or
        /// the glyphs of a text). Each shape is filled by the non-zero rule, so their figures may turn either way.
        /// </summary>
        /// <param name="image">The image, any pixel type</param>
        /// <param name="shapes">The shapes in the pixel coordinates of ImageSharp</param>
        /// <param name="color">The color in the values of the image, straight alpha</param>
        /// <param name="antialias">False: whole pixels, like GDI+ with SmoothingMode.HighSpeed</param>
        /// <param name="clip">Only in this area, e.g. the box of a text</param>
        /// <param name="exclude">Not in this shape, by whole pixels like a GDI+ clip region (Graphics.SetClip with CombineMode.Exclude)</param>
        public static void Fill(Image image, IReadOnlyList<IPath> shapes, Vector4 color, bool antialias = true, Rectangle? clip = null, IPath exclude = null)
        {
            if (image == null || shapes == null || shapes.Count == 0 || !(color.W > 0))
            {
                return;
            }
            var area = Rectangle.Empty;
            foreach (var shape in shapes)
            {
                var bounds = shape.Bounds;
                var shapeArea = Rectangle.FromLTRB((int)Math.Floor(bounds.Left), (int)Math.Floor(bounds.Top), (int)Math.Ceiling(bounds.Right), (int)Math.Ceiling(bounds.Bottom));
                area = area.IsEmpty ? shapeArea : Rectangle.Union(area, shapeArea);
            }
            area = Rectangle.Intersect(area, new Rectangle(0, 0, image.Width, image.Height));
            if (clip is Rectangle only)
            {
                area = Rectangle.Intersect(area, only);
            }
            if (area.Width <= 0 || area.Height <= 0)
            {
                return;
            }
            float[] coverage = null;
            foreach (var shape in shapes)
            {
                var shapeCoverage = CoverageOf(shape, area, antialias);
                if (coverage == null)
                {
                    coverage = shapeCoverage;
                    continue;
                }
                for (int i = 0; i < coverage.Length; i++)
                {
                    coverage[i] = Math.Max(coverage[i], shapeCoverage[i]);
                }
            }
            if (exclude != null)
            {
                var excluded = CoverageOf(exclude, area, false);
                for (int i = 0; i < coverage.Length; i++)
                {
                    coverage[i] *= 1f - excluded[i];
                }
            }
            float alpha = Math.Min(color.W, 1f);
            image.Mutate(context => context.ProcessPixelRowsAsVector4((row, start) =>
            {
                int offset = (start.Y - area.Y) * area.Width + start.X - area.X;
                for (int x = 0; x < row.Length; x++)
                {
                    float covered = coverage[offset + x] * alpha;
                    if (covered > 0f)
                    {
                        row[x] = Over(new Vector4(color.X, color.Y, color.Z, covered), row[x]);
                    }
                }
            }, area));
        }

        /// <summary>
        /// The coverage of the shape in the area (non-zero rule)
        /// </summary>
        private static float[] CoverageOf(IPath shape, Rectangle area, bool antialias)
        {
            if (antialias)
            {
                return Coverage.Of(shape.Translate(-area.X, -area.Y), area.Width, area.Height, IntersectionRule.NonZero);
            }
            // Without anti-aliasing a pixel is inside when more than half of it is covered; one cut in half by an edge is inside on the left and
            // top edge only, like GDI+ (a line of width 2 is two pixels wide, not one or three): the shape is moved a little up and left
            var coverage = Coverage.Of(shape.Translate(-area.X - AliasedNudge, -area.Y - AliasedNudge), area.Width, area.Height, IntersectionRule.NonZero);
            for (int i = 0; i < coverage.Length; i++)
            {
                coverage[i] = coverage[i] > 0.5f ? 1f : 0f;
            }
            return coverage;
        }

        /// <summary>
        /// Draw the line along the path with the pen, like Graphics.DrawPath
        /// </summary>
        /// <param name="image">The image, any pixel type</param>
        /// <param name="path">The path in the pixel coordinates of ImageSharp, closed figures are drawn closed</param>
        /// <param name="width">The width of the pen (see <see cref="PenWidth"/>)</param>
        /// <param name="color">The color in the values of the image, straight alpha</param>
        /// <param name="join">How the line goes around corners, GDI+ pens have miter joins</param>
        /// <param name="cap">How open lines end, GDI+ pens have flat ends</param>
        /// <param name="antialias">False: whole pixels, like GDI+ with SmoothingMode.HighSpeed</param>
        public static void Stroke(Image image, IPath path, float width, Vector4 color, StrokeJoin join = StrokeJoin.Miter, StrokeCap cap = StrokeCap.Flat,
            bool antialias = true)
        {
            if (path == null || !(width > 0) || !(color.W > 0))
            {
                return;
            }
            Fill(image, Strokes.Outline(path, width, join, cap), color, antialias);
        }

        /// <summary>
        /// The pixel over the one under it, straight alpha
        /// </summary>
        public static Vector4 Over(Vector4 top, Vector4 bottom)
        {
            float topAlpha = Math.Max(Math.Min(top.W, 1f), 0f);
            float bottomAlpha = Math.Max(Math.Min(bottom.W, 1f), 0f) * (1f - topAlpha);
            float alpha = topAlpha + bottomAlpha;
            if (alpha <= 0f)
            {
                return Vector4.Zero;
            }
            var color = (new Vector3(top.X, top.Y, top.Z) * topAlpha + new Vector3(bottom.X, bottom.Y, bottom.Z) * bottomAlpha) / alpha;
            return new Vector4(color, alpha);
        }
    }
}
