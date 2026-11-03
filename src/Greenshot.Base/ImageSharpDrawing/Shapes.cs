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

using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.PixelFormats;

namespace Greenshot.Base.ImageSharpDrawing
{
    /// <summary>
    /// The rectangle and the ellipse of the editor (RectangleContainer.DrawRectangle, EllipseContainer.DrawEllipse, the border of FilterContainer)
    /// drawn with ImageSharp into an image of any pixel type. Coordinates are those of the editor (whole pixels, GDI+ PixelOffsetMode.None), colors
    /// 8-bit sRGB like the editor's, put into the image by its <see cref="ImageValues"/>.
    /// </summary>
    public static class Shapes
    {
        /// <summary>
        /// Like RectangleContainer.DrawRectangle: the shadow (the outline five times), the fill, then the line (not anti-aliased, like the editor)
        /// </summary>
        /// <param name="image">The image, any pixel type</param>
        /// <param name="values">The values of the image's pixels</param>
        /// <param name="rect">The rectangle, normalized (positive width and height)</param>
        /// <param name="lineThickness">The width of the line, 0 for none</param>
        /// <param name="lineColor">The color of the line</param>
        /// <param name="fillColor">The color of the inside, transparent for none</param>
        /// <param name="shadow">Draw the shadow</param>
        public static void Rectangle(Image image, ImageValues values, Rectangle rect, int lineThickness, Rgba32 lineColor, Rgba32 fillColor, bool shadow)
        {
            bool lineVisible = lineThickness > 0 && lineColor.A > 0;
            if (shadow && (lineVisible || fillColor.A > 0))
            {
                foreach (var (offset, alpha) in Painting.ShadowSteps())
                {
                    Painting.Stroke(image, Box(rect, offset), Painting.PenWidth(lineThickness), new Vector4(0, 0, 0, alpha));
                }
            }
            Painting.Fill(image, Box(rect, 0), values.Of(fillColor));
            if (lineVisible)
            {
                Painting.Stroke(image, Box(rect, 0), lineThickness, values.Of(lineColor), antialias: false);
            }
        }

        /// <summary>
        /// Like EllipseContainer.DrawEllipse: the shadow (the outline five times), the fill, then the line, all anti-aliased
        /// </summary>
        /// <param name="image">The image, any pixel type</param>
        /// <param name="values">The values of the image's pixels</param>
        /// <param name="rect">The rectangle around the ellipse, normalized</param>
        /// <param name="lineThickness">The width of the line, 0 for none</param>
        /// <param name="lineColor">The color of the line</param>
        /// <param name="fillColor">The color of the inside, transparent for none</param>
        /// <param name="shadow">Draw the shadow</param>
        public static void Ellipse(Image image, ImageValues values, Rectangle rect, int lineThickness, Rgba32 lineColor, Rgba32 fillColor, bool shadow)
        {
            bool lineVisible = lineThickness > 0 && lineColor.A > 0;
            if (shadow && (lineVisible || fillColor.A > 0))
            {
                foreach (var (offset, alpha) in Painting.ShadowSteps())
                {
                    Painting.Stroke(image, Oval(rect, offset), Painting.PenWidth(lineThickness), new Vector4(0, 0, 0, alpha));
                }
            }
            Painting.Fill(image, Oval(rect, 0), values.Of(fillColor));
            if (lineVisible)
            {
                Painting.Stroke(image, Oval(rect, 0), lineThickness, values.Of(lineColor));
            }
        }

        /// <summary>
        /// Like FilterContainer.Draw, the border of a highlight or obfuscation: the shadow in gray (100, 100, 100) from 1 to 5 pixels away, then the
        /// line, both not anti-aliased. Nothing without a visible line.
        /// </summary>
        /// <param name="image">The image, any pixel type</param>
        /// <param name="values">The values of the image's pixels</param>
        /// <param name="rect">The rectangle, normalized</param>
        /// <param name="lineThickness">The width of the line</param>
        /// <param name="lineColor">The color of the line</param>
        /// <param name="shadow">Draw the shadow</param>
        public static void FilterBorder(Image image, ImageValues values, Rectangle rect, int lineThickness, Rgba32 lineColor, bool shadow)
        {
            if (lineThickness <= 0 || lineColor.A == 0)
            {
                return;
            }
            if (shadow)
            {
                foreach (var (offset, alpha) in Painting.ShadowSteps())
                {
                    var gray = values.Of(new Rgba32(100, 100, 100, (byte)System.Math.Round(alpha * 255)));
                    Painting.Stroke(image, Box(rect, offset + 1), lineThickness, gray, antialias: false);
                }
            }
            Painting.Stroke(image, Box(rect, 0), lineThickness, values.Of(lineColor), antialias: false);
        }

        /// <summary>
        /// The rectangle in ImageSharp's pixel coordinates, moved by the offset to the right and down
        /// </summary>
        internal static IPath Box(Rectangle rect, int offset) =>
            new RectangularPolygon(rect.X + offset + Painting.GdiPixelCenter, rect.Y + offset + Painting.GdiPixelCenter, rect.Width, rect.Height);

        private static IPath Oval(Rectangle rect, int offset) =>
            new EllipsePolygon(rect.X + offset + Painting.GdiPixelCenter + rect.Width / 2f, rect.Y + offset + Painting.GdiPixelCenter + rect.Height / 2f, rect.Width, rect.Height);
    }
}
