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
using SixLabors.ImageSharp.Drawing.Processing;

namespace Greenshot.Base.ImageSharpDrawing
{
    /// <summary>
    /// How a line goes around a corner, like LineJoin of GDI+
    /// </summary>
    public enum StrokeJoin
    {
        /// <summary>
        /// Pointed, cut off flat (beveled) where the point would be longer than 10 half widths, like a GDI+ pen (LineJoin.Miter, MiterLimit 10)
        /// </summary>
        Miter,

        /// <summary>
        /// Round
        /// </summary>
        Round
    }

    /// <summary>
    /// How an open line ends, like LineCap of GDI+
    /// </summary>
    public enum StrokeCap
    {
        /// <summary>
        /// At the end point (LineCap.Flat, a GDI+ pen's default)
        /// </summary>
        Flat,

        /// <summary>
        /// Half a width further, round
        /// </summary>
        Round
    }

    /// <summary>
    /// The shape of a line drawn along points with a width, like a GDI+ pen draws it: its segments, joins and caps put together into one shape.
    /// (ImageSharp's own outlines cut corners off at a miter limit of 2 half widths and squared, GDI+ pens at 10 and beveled.)
    /// </summary>
    public static class Strokes
    {
        /// <summary>
        /// The miter limit of a GDI+ pen, in half line widths
        /// </summary>
        public const float MiterLimit = 10;

        // Points of a round join or cap per full circle
        private const int CirclePoints = 32;

        /// <summary>
        /// The shape of the line along the points
        /// </summary>
        /// <param name="points">The points of the line (curves flattened)</param>
        /// <param name="closed">From the last point back to the first, joined there too</param>
        /// <param name="width">The width of the line</param>
        /// <param name="join">How it goes around corners</param>
        /// <param name="cap">How an open line ends</param>
        /// <returns>The shape, null when there is nothing to draw</returns>
        public static IPath Outline(IReadOnlyList<PointF> points, bool closed, float width, StrokeJoin join = StrokeJoin.Miter, StrokeCap cap = StrokeCap.Flat)
        {
            if (points == null || !(width > 0))
            {
                return null;
            }
            // Points which are on top of each other make no segment
            var line = new List<Vector2>();
            foreach (var point in points)
            {
                var vector = (Vector2)point;
                if (line.Count == 0 || Vector2.DistanceSquared(line[line.Count - 1], vector) > 1e-8f)
                {
                    line.Add(vector);
                }
            }
            if (closed && line.Count > 1 && Vector2.DistanceSquared(line[0], line[line.Count - 1]) <= 1e-8f)
            {
                line.RemoveAt(line.Count - 1);
            }
            if (line.Count < 2)
            {
                return null;
            }
            float half = width / 2;
            var parts = new List<IPath>();
            int segments = closed ? line.Count : line.Count - 1;
            for (int i = 0; i < segments; i++)
            {
                var from = line[i];
                var to = line[(i + 1) % line.Count];
                var normal = Normal(from, to) * half;
                parts.Add(Figure(from + normal, to + normal, to - normal, from - normal));
            }
            // The joins at the inner points, for a closed line at all of them
            for (int i = closed ? 0 : 1; i < (closed ? line.Count : line.Count - 1); i++)
            {
                var before = line[(i - 1 + line.Count) % line.Count];
                var at = line[i];
                var after = line[(i + 1) % line.Count];
                var join1 = Join(before, at, after, half, join);
                if (join1 != null)
                {
                    parts.Add(join1);
                }
            }
            if (!closed && cap == StrokeCap.Round)
            {
                parts.Add(Circle(line[0], half));
                parts.Add(Circle(line[line.Count - 1], half));
            }
            return Union(parts);
        }

        /// <summary>
        /// The points of the path flattened to lines, each figure on its own with whether it is closed
        /// </summary>
        public static IEnumerable<(IReadOnlyList<PointF> Points, bool Closed)> Figures(IPath path) =>
            path.Flatten().Select(figure => ((IReadOnlyList<PointF>)figure.Points.ToArray(), figure.IsClosed));

        /// <summary>
        /// The shape of the line along all figures of the path
        /// </summary>
        public static IPath Outline(IPath path, float width, StrokeJoin join = StrokeJoin.Miter, StrokeCap cap = StrokeCap.Flat)
        {
            if (path == null)
            {
                return null;
            }
            var figures = Figures(path).Select(figure => Outline(figure.Points, figure.Closed, width, join, cap)).Where(outline => outline != null).ToList();
            return figures.Count == 0 ? null : Union(figures);
        }

        /// <summary>
        /// The parts as one shape without overlaps (anti-aliasing of overlapping figures would count a pixel's coverage more than once)
        /// </summary>
        private static IPath Union(IReadOnlyList<IPath> parts) =>
            parts.Count == 1 ? parts[0] : parts[0].Clip(new ShapeOptions { ClippingOperation = ClippingOperation.Union, IntersectionRule = IntersectionRule.NonZero }, parts.Skip(1));

        /// <summary>
        /// The left side of the direction, as a unit vector (y goes down, so it is the right side on screen)
        /// </summary>
        private static Vector2 Normal(Vector2 from, Vector2 to)
        {
            var direction = Vector2.Normalize(to - from);
            return new Vector2(-direction.Y, direction.X);
        }

        /// <summary>
        /// The outer side of the corner: a point (miter), cut off (bevel) or round
        /// </summary>
        private static IPath Join(Vector2 before, Vector2 at, Vector2 after, float half, StrokeJoin join)
        {
            if (join == StrokeJoin.Round)
            {
                return Circle(at, half);
            }
            var normalIn = Normal(before, at);
            var normalOut = Normal(at, after);
            float cross = (at.X - before.X) * (after.Y - at.Y) - (at.Y - before.Y) * (after.X - at.X);
            if (Math.Abs(cross) < 1e-6f && Vector2.Dot(at - before, after - at) > 0)
            {
                // Straight on, the segments meet without a gap
                return null;
            }
            // The outer side is opposite to the turn
            float side = cross > 0 ? -1 : 1;
            var outerIn = at + normalIn * half * side;
            var outerOut = at + normalOut * half * side;
            var bisector = normalIn + normalOut;
            float cosHalf = bisector.Length() / 2;
            if (cosHalf > 1e-6f && 1 / cosHalf <= MiterLimit)
            {
                var tip = at + Vector2.Normalize(bisector) * side * (half / cosHalf);
                return Figure(at, outerIn, tip, outerOut);
            }
            return Figure(at, outerIn, outerOut);
        }

        private static IPath Circle(Vector2 center, float radius)
        {
            var points = new PointF[CirclePoints];
            for (int i = 0; i < CirclePoints; i++)
            {
                double angle = 2 * Math.PI * i / CirclePoints;
                points[i] = new PointF(center.X + radius * (float)Math.Cos(angle), center.Y + radius * (float)Math.Sin(angle));
            }
            return Figure(points.Select(point => (Vector2)point).ToArray());
        }

        /// <summary>
        /// A figure of the points which always turns the same way (positive area), so overlapping figures add up under the non-zero rule
        /// </summary>
        private static IPath Figure(params Vector2[] points)
        {
            float area = 0;
            for (int i = 0; i < points.Length; i++)
            {
                var a = points[i];
                var b = points[(i + 1) % points.Length];
                area += a.X * b.Y - b.X * a.Y;
            }
            var ordered = area < 0 ? Enumerable.Reverse(points) : points;
            return new Polygon(new LinearLineSegment(ordered.Select(point => new PointF(point.X, point.Y)).ToArray()));
        }
    }
}
