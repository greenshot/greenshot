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
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Greenshot.ImageSharpDrawing
{
    /// <summary>
    /// The rectangle and the ellipse of the editor (RectangleContainer, EllipseContainer, the border of FilterContainer) drawn with ImageSharp.
    /// Coordinates are those of the editor (whole pixels), colors 8-bit sRGB like the editor's, put into the image by its <see cref="ImageValues"/>.
    /// </summary>
    public static class Shapes
    {
        /// <summary>
        /// Like RectangleContainer: the shadow, the fill, then the line. The editor draws the line without anti-aliasing, here it is anti-aliased like
        /// all lines: lines of even width get a soft edge instead of the editor's hard one.
        /// </summary>
        /// <param name="image">The image</param>
        /// <param name="values">The values of the image's pixels</param>
        /// <param name="rect">The rectangle, normalized (positive width and height)</param>
        /// <param name="lineThickness">The width of the line, 0 for none</param>
        /// <param name="lineColor">The color of the line</param>
        /// <param name="fillColor">The color of the inside, transparent for none</param>
        /// <param name="shadow">Draw the shadow</param>
        public static void Rectangle(Image image, ImageValues values, Rectangle rect, int lineThickness, Rgba32 lineColor, Rgba32 fillColor, bool shadow) =>
            Draw(image, values, Box(rect), lineThickness, lineColor, fillColor, shadow);

        /// <summary>
        /// Like EllipseContainer: the shadow, the fill, then the line
        /// </summary>
        /// <param name="image">The image</param>
        /// <param name="values">The values of the image's pixels</param>
        /// <param name="rect">The rectangle around the ellipse, normalized</param>
        /// <param name="lineThickness">The width of the line, 0 for none</param>
        /// <param name="lineColor">The color of the line</param>
        /// <param name="fillColor">The color of the inside, transparent for none</param>
        /// <param name="shadow">Draw the shadow</param>
        public static void Ellipse(Image image, ImageValues values, Rectangle rect, int lineThickness, Rgba32 lineColor, Rgba32 fillColor, bool shadow) =>
            Draw(image, values, Oval(rect), lineThickness, lineColor, fillColor, shadow);

        /// <summary>
        /// Like FilterContainer, the border of a highlight or obfuscation: the shadow in gray from 1 to 5 pixels away, then the line (anti-aliased, see
        /// <see cref="Rectangle"/>). Nothing without a visible line.
        /// </summary>
        /// <param name="image">The image</param>
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
            var box = Box(rect);
            image.Mutate(context =>
            {
                if (shadow)
                {
                    foreach (var (offset, alpha) in Painting.ShadowSteps())
                    {
                        var gray = values.ColorOf(new Rgba32(100, 100, 100, (byte)Math.Round(alpha * 255)));
                        context.Draw(Painting.Pen(gray, lineThickness), box.Translate(offset + 1, offset + 1));
                    }
                }
                context.Draw(Painting.Pen(values.ColorOf(lineColor), lineThickness), box);
            });
        }

        /// <summary>
        /// The shadow (the outline five times), the fill, then the line
        /// </summary>
        internal static void Draw(Image image, ImageValues values, IPath shape, int lineThickness, Rgba32 lineColor, Rgba32 fillColor, bool shadow,
            int shadowOffset = 0)
        {
            bool lineVisible = lineThickness > 0 && lineColor.A > 0;
            image.Mutate(context =>
            {
                if (shadow && (lineVisible || fillColor.A > 0))
                {
                    foreach (var (offset, alpha) in Painting.ShadowSteps())
                    {
                        context.Draw(Painting.Pen(Painting.Black(alpha), Painting.PenWidth(lineThickness)), shape.Translate(offset + shadowOffset, offset + shadowOffset));
                    }
                }
                if (fillColor.A > 0)
                {
                    context.Fill(values.ColorOf(fillColor), Painting.OnPixelCenters(shape));
                }
                if (lineVisible)
                {
                    context.Draw(Painting.Pen(values.ColorOf(lineColor), lineThickness), shape);
                }
            });
        }

        /// <summary>
        /// The rectangle
        /// </summary>
        internal static IPath Box(Rectangle rect) =>
            new RectangularPolygon(rect.X, rect.Y, rect.Width, rect.Height);

        private static IPath Oval(Rectangle rect) =>
            new EllipsePolygon(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f, rect.Width, rect.Height);
    }
}
