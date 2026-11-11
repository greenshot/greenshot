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

namespace Greenshot.Document.Editing
{
    /// <summary>
    /// Which element is at a point or in an area, in canvas pixels. Only elements of visible layers which aren't locked
    /// can be hit: the capture layer is locked.
    /// </summary>
    public static class HitTesting
    {
        /// <summary>
        /// The elements which can be selected, in drawing order (bottom first)
        /// </summary>
        public static IEnumerable<Element> Selectable(GreenshotDocument document) =>
            document.Layers.Where(layer => layer.Visible && !layer.Locked).SelectMany(layer => layer.Elements).Where(element => element is not UnknownElement);

        /// <summary>
        /// The top element at the point, null when there is none. Tolerance is how far from a line a click still hits it,
        /// in canvas pixels.
        /// </summary>
        public static Element ElementAt(GreenshotDocument document, PointD point, double tolerance) =>
            Selectable(document).Reverse().FirstOrDefault(element => Hits(document, element, point, tolerance));

        /// <summary>
        /// The elements in the area: those inside of it, or with touching also those which only touch it
        /// </summary>
        public static IReadOnlyList<Element> ElementsIn(GreenshotDocument document, RectD area, bool touching) =>
            Selectable(document).Where(element => touching ? Intersects(area, element.Bounds) : Contains(area, element.Bounds)).ToList();

        /// <summary>
        /// True when the point is on the element: on its line or, when it is filled or shows something inside, in it
        /// </summary>
        public static bool Hits(GreenshotDocument document, Element element, PointD point, double tolerance)
        {
            var style = document.StyleOf(element);
            double reach = StrokeWidth(style.Stroke) / 2 + tolerance;
            bool filled = style.Fill is { Enabled: not false, Color: Rgba { A: > 0 } };
            switch (element)
            {
                case LineElement or ArrowElement:
                    var (start, end) = ElementShapes.EndsOf(element).Value;
                    return DistanceToSegment(point, start, end) <= Math.Max(reach, 4 + tolerance) || LabelHit(element, style, point);
                case FreehandElement freehand:
                    return freehand.Points.Count == 1
                        ? Distance(point, freehand.Points[0]) <= reach
                        : freehand.Points.Zip(freehand.Points.Skip(1), (a, b) => DistanceToSegment(point, a, b)).Any(distance => distance <= reach);
                case RectangleElement rectangle:
                {
                    var local = Unturned(point, rectangle.Area, rectangle.Rotation);
                    return filled || rectangle.Label != null
                        ? Contains(rectangle.Area.Inflate(reach), local)
                        : Contains(rectangle.Area.Inflate(reach), local) && !Contains(rectangle.Area.Inflate(-reach), local);
                }
                case EllipseElement ellipse:
                {
                    var local = Unturned(point, ellipse.Area, ellipse.Rotation);
                    double inside = EllipseDistance(ellipse.Area, local);
                    return filled || ellipse.Label != null ? inside <= reach : Math.Abs(inside) <= reach;
                }
                case SpeechBubbleElement bubble:
                {
                    var local = Unturned(point, bubble.Area, bubble.Rotation);
                    return Contains(bubble.Area.Inflate(reach), local) || DistanceToSegment(point, bubble.Area.Center, bubble.Tail) <= Math.Max(reach, 6);
                }
                default:
                    if (ElementShapes.AreaOf(element) is RectD area)
                    {
                        return Contains(area.Inflate(reach), Unturned(point, area, element.Rotation)) || LabelHit(element, style, point);
                    }

                    return Contains(element.Bounds.Inflate(tolerance), point);
            }
        }

        /// <summary>
        /// The point in the unturned area of an element turned around the centre of its area
        /// </summary>
        public static PointD Unturned(PointD point, RectD area, double rotation) => rotation % 360 == 0 ? point : point.Turned(-rotation, area.Center);

        public static bool Contains(RectD area, PointD point) => point.X >= area.X && point.X <= area.Right && point.Y >= area.Y && point.Y <= area.Bottom;

        public static bool Contains(RectD outer, RectD inner) => inner.X >= outer.X && inner.Right <= outer.Right && inner.Y >= outer.Y && inner.Bottom <= outer.Bottom;

        public static bool Intersects(RectD a, RectD b) => a.X <= b.Right && b.X <= a.Right && a.Y <= b.Bottom && b.Y <= a.Bottom;

        public static double Distance(PointD a, PointD b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

        /// <summary>
        /// The distance of the point to the line from a to b
        /// </summary>
        public static double DistanceToSegment(PointD point, PointD a, PointD b)
        {
            double dx = b.X - a.X, dy = b.Y - a.Y, lengthSquared = dx * dx + dy * dy;
            if (lengthSquared == 0)
            {
                return Distance(point, a);
            }

            double t = Math.Max(0, Math.Min(1, ((point.X - a.X) * dx + (point.Y - a.Y) * dy) / lengthSquared));
            return Distance(point, new PointD(a.X + t * dx, a.Y + t * dy));
        }

        /// <summary>
        /// About how far the point is outside of the ellipse in the area, negative inside
        /// </summary>
        private static double EllipseDistance(RectD area, PointD point)
        {
            double rx = area.Width / 2, ry = area.Height / 2;
            if (rx <= 0 || ry <= 0)
            {
                return DistanceToSegment(point, new PointD(area.X, area.Y), new PointD(area.Right, area.Bottom));
            }

            double nx = (point.X - area.Center.X) / rx, ny = (point.Y - area.Center.Y) / ry;
            return (Math.Sqrt(nx * nx + ny * ny) - 1) * Math.Min(rx, ry);
        }

        private static bool LabelHit(Element element, Style style, PointD point)
        {
            string text = GreenshotDocument.LabelOf(element);
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            double size = style.Text?.Size ?? 11;
            string[] lines = text.Split('\n');
            return GreenshotDocument.LabelPlacement(element, lines.Max(line => line.Length) * size * 0.6, lines.Length * size * 1.2) is var (box, rotation, pivot)
                   && Contains(box, rotation % 360 == 0 ? point : point.Turned(-rotation, pivot));
        }

        private static double StrokeWidth(StrokeStyle stroke) => stroke is { Enabled: not false, Width: double width } ? width : 0;
    }
}
