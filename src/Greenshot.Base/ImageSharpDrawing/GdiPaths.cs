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

using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;

namespace Greenshot.Base.ImageSharpDrawing
{
    /// <summary>
    /// The bridge from the GDI+ shapes of the editor to ImageSharp: the same points, so a shape drawn by ImageSharp has the geometry the editor shows
    /// </summary>
    public static class GdiPaths
    {
        /// <summary>
        /// The figures of the GDI+ path (curves flattened to lines) as one ImageSharp shape; null for an empty path
        /// </summary>
        public static IPath ToPath(GraphicsPath graphicsPath)
        {
            if (graphicsPath == null || graphicsPath.PointCount == 0)
            {
                return null;
            }
            using var flat = (GraphicsPath)graphicsPath.Clone();
            flat.Flatten();
            var points = flat.PathPoints;
            var types = flat.PathTypes;
            var figures = new List<IPath>();
            var figure = new List<PointF>();
            for (int i = 0; i < points.Length; i++)
            {
                // A start point begins the next figure
                if ((types[i] & (byte)PathPointType.PathTypeMask) == (byte)PathPointType.Start && figure.Count > 0)
                {
                    AddFigure(figures, figure);
                    figure = new List<PointF>();
                }
                figure.Add(new PointF(points[i].X, points[i].Y));
            }
            AddFigure(figures, figure);
            return figures.Count == 0 ? null : new ComplexPolygon(figures);
        }

        /// <summary>
        /// A polygon of the points, e.g. the outline of torn edges
        /// </summary>
        public static IPath ToPolygon(IEnumerable<System.Drawing.PointF> points) =>
            new Polygon(points.Select(point => new PointF(point.X, point.Y)).ToArray());

        private static void AddFigure(List<IPath> figures, List<PointF> figure)
        {
            if (figure.Count >= 3)
            {
                figures.Add(new Polygon(figure.ToArray()));
            }
        }
    }
}
