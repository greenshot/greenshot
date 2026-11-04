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
using System.Linq;
using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.PixelFormats;

namespace Greenshot.Base.ImageSharpDrawing
{
    /// <summary>
    /// The ends of a line which have an arrow head (ArrowContainer.ArrowHeadCombination)
    /// </summary>
    public enum ArrowHeads
    {
        None,
        Start,
        End,
        Both
    }

    /// <summary>
    /// The line, the arrow and the freehand line of the editor (LineContainer, ArrowContainer, FreehandContainer) drawn with ImageSharp into an image
    /// of any pixel type, colors 8-bit sRGB like the editor's, put into the image by its <see cref="ImageValues"/>.
    /// </summary>
    public static class Lines
    {
        // The arrow head of the editor: AdjustableArrowCap(4, 6), filled, 4 line widths wide and 6 long, the tip on the end point, the line ends at its base
        private const float HeadWidth = 4;
        private const float HeadLength = 6;

        /// <summary>
        /// Like LineContainer.Draw and ArrowContainer.Draw: the shadow (the line five times), then the line with flat ends and its arrow heads.
        /// Nothing without a line thickness.
        /// </summary>
        /// <param name="image">The image, any pixel type</param>
        /// <param name="values">The values of the image's pixels</param>
        /// <param name="start">Where the line starts, in the editor's coordinates (GDI+ PixelOffsetMode.None)</param>
        /// <param name="end">Where it ends</param>
        /// <param name="lineThickness">The width of the line</param>
        /// <param name="lineColor">The color of the line</param>
        /// <param name="heads">The ends with an arrow head</param>
        /// <param name="shadow">Draw the shadow</param>
        public static void Line(Image image, ImageValues values, PointF start, PointF end, int lineThickness, Rgba32 lineColor, ArrowHeads heads, bool shadow)
        {
            if (lineThickness <= 0)
            {
                return;
            }
            if (shadow)
            {
                foreach (var (offset, alpha) in Painting.ShadowSteps())
                {
                    var move = new Vector2(offset, offset);
                    Painting.Fill(image, LineShape((Vector2)start + move, (Vector2)end + move, lineThickness, heads), new Vector4(0, 0, 0, alpha));
                }
            }
            Painting.Fill(image, LineShape(start, end, lineThickness, heads), values.Of(lineColor));
        }

        /// <summary>
        /// Like FreehandContainer.Draw: the smoothed line with round ends and joins, anti-aliased. Nothing without a line thickness.
        /// </summary>
        /// <param name="image">The image, any pixel type</param>
        /// <param name="values">The values of the image's pixels</param>
        /// <param name="points">The points of the image as for GraphicsPath.AddBeziers (start, then two control points and an end point per curve), or
        /// two points for a straight line; the editor draws them with PixelOffsetMode.HighQuality, so they are ImageSharp's coordinates as they are</param>
        /// <param name="lineThickness">The width of the line</param>
        /// <param name="lineColor">The color of the line</param>
        public static void Freehand(Image image, ImageValues values, IReadOnlyList<PointF> points, int lineThickness, Rgba32 lineColor)
        {
            if (points == null || points.Count < 2 || lineThickness <= 0)
            {
                return;
            }
            ILineSegment segment;
            if (points.Count == 2)
            {
                segment = new LinearLineSegment(points[0], points[1]);
            }
            else if ((points.Count - 1) % 3 == 0)
            {
                segment = new CubicBezierLineSegment(points.ToArray());
            }
            else
            {
                throw new ArgumentException("Bezier points are a start point and three points per curve", nameof(points));
            }
            Painting.Stroke(image, new Path(segment), lineThickness, values.Of(lineColor), JointStyle.Round, EndCapStyle.Round);
        }

        /// <summary>
        /// The line and its heads, in ImageSharp's coordinates
        /// </summary>
        private static IReadOnlyList<IPath> LineShape(Vector2 start, Vector2 end, float width, ArrowHeads heads)
        {
            var from = start + new Vector2(Painting.GdiPixelCenter);
            var to = end + new Vector2(Painting.GdiPixelCenter);
            float length = Vector2.Distance(from, to);
            if (length <= 0)
            {
                // GDI+ draws nothing for a line without length
                return Array.Empty<IPath>();
            }
            var direction = (to - from) / length;
            var parts = new List<IPath>();
            Vector2 lineStart = from, lineEnd = to;
            if (heads is ArrowHeads.Start or ArrowHeads.Both)
            {
                parts.Add(Head(from, -direction, width));
                lineStart = from + direction * HeadLength * width;
            }
            if (heads is ArrowHeads.End or ArrowHeads.Both)
            {
                parts.Add(Head(to, direction, width));
                lineEnd = to - direction * HeadLength * width;
            }
            if (Vector2.Dot(lineEnd - lineStart, direction) > 0)
            {
                parts.Add(new Path(new LinearLineSegment(lineStart, lineEnd)).GenerateOutline(width));
            }
            return parts;
        }

        /// <summary>
        /// An arrow head: the tip, and its base the head length back, half the head width to each side
        /// </summary>
        private static IPath Head(Vector2 tip, Vector2 direction, float width)
        {
            var across = new Vector2(-direction.Y, direction.X) * (HeadWidth / 2 * width);
            var headBase = tip - direction * (HeadLength * width);
            return new Polygon(new LinearLineSegment(tip, headBase + across, headBase - across));
        }
    }
}
