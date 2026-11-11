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
using SixLabors.ImageSharp;

namespace Greenshot.Document.Rendering
{
    /// <summary>
    /// The outlines of frames and cut marks: an edge is a list of points along it with how deep they go in, as the
    /// editor makes them (CutOutHelper.CreateEdge)
    /// </summary>
    internal static class Outlines
    {
        /// <summary>
        /// Points along an edge of the length: X along, Y the depth from 0 to the height of the shape
        /// </summary>
        public static List<PointF> Of(EdgeShape shape, double length, double height, Random random)
        {
            var points = new List<PointF>();
            switch (shape?.Style ?? EdgeStyle.None)
            {
                case EdgeStyle.ZigZag:
                    for (int i = 0; ; i++)
                    {
                        double along = Math.Min(i * Math.Max(1, shape.Range) / 2.0, length);
                        points.Add(new PointF((float)along, i % 2 == 0 ? 0 : (float)height));
                        if (along >= length)
                        {
                            break;
                        }
                    }

                    break;
                case EdgeStyle.Wave:
                    for (double along = 0; ; along = Math.Min(along + 2, length))
                    {
                        points.Add(new PointF((float)along, (float)(height / 2 * (1 + Math.Sin(Math.PI * along / Math.Max(1, shape.Range))))));
                        if (along >= length)
                        {
                            break;
                        }
                    }

                    break;
                case EdgeStyle.Torn:
                    for (int i = 0; ; i++)
                    {
                        double along = Math.Min(i * (double)Math.Max(1, shape.Range), length);
                        points.Add(new PointF((float)along, random.Next(1, (int)Math.Max(1, height) + 1)));
                        if (along >= length)
                        {
                            break;
                        }
                    }

                    break;
                default:
                    points.Add(new PointF(0, 0));
                    points.Add(new PointF((float)length, 0));
                    break;
            }

            return points;
        }

        /// <summary>
        /// The outline of a frame on the box: the sides clockwise from the top left corner, the teeth cut inward. The
        /// same seed gives the same outline.
        /// </summary>
        public static PointF[] Outline(FrameElement frame, RectD box)
        {
            var random = new Random(frame.Seed);
            float x = (float)box.X, y = (float)box.Y, w = (float)box.Width, h = (float)box.Height;
            var outline = new List<PointF>();
            foreach (var side in new[] { Edges.Top, Edges.Right, Edges.Bottom, Edges.Left })
            {
                var shape = frame.ShapeOf(side);
                bool horizontal = side is Edges.Top or Edges.Bottom;
                double height = Math.Min(shape.Height, Math.Min(w, h) / 3);
                foreach (var point in Of(shape, horizontal ? w : h, height, random))
                {
                    float a = point.X, d = point.Y;
                    outline.Add(side switch
                    {
                        Edges.Top => new PointF(x + a, y + d),
                        Edges.Right => new PointF(x + w - d, y + a),
                        Edges.Bottom => new PointF(x + w - a, y + h - d),
                        _ => new PointF(x + d, y + h - a)
                    });
                }
            }

            // Sides meet in the corners, a point twice in a row confuses the clipping
            return outline.Where((point, index) => index == 0 || point != outline[index - 1]).ToArray();
        }

        /// <summary>
        /// A box with round corners as points
        /// </summary>
        public static PointF[] RoundedBox(RectD box, double radius)
        {
            radius = Math.Min(radius, Math.Min(box.Width, box.Height) / 2);
            var corners = new[]
            {
                (new PointD(box.Right - radius, box.Y + radius), -90.0),
                (new PointD(box.Right - radius, box.Bottom - radius), 0.0),
                (new PointD(box.X + radius, box.Bottom - radius), 90.0),
                (new PointD(box.X + radius, box.Y + radius), 180.0)
            };
            return corners.SelectMany(corner => Enumerable.Range(0, 9).Select(i =>
            {
                double angle = (corner.Item2 + i * 10) * Math.PI / 180;
                return new PointF((float)(corner.Item1.X + radius * Math.Cos(angle)), (float)(corner.Item1.Y + radius * Math.Sin(angle)));
            })).ToArray();
        }
    }
}
