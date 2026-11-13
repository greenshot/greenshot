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
using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;

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
