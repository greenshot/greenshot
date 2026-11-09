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

namespace Greenshot.ImageSharpDrawing
{
    /// <summary>
    /// The speech bubble of the editor (SpeechbubbleContainer) drawn with ImageSharp into an image of any pixel type: the box with round corners and
    /// the tail to its target. Its text is <see cref="Texts.Draw"/> with the box. Colors 8-bit sRGB like the editor's, put into the image by its
    /// <see cref="ImageValues"/>.
    /// </summary>
    public static class SpeechBubbles
    {
        /// <summary>
        /// Like SpeechbubbleContainer.Draw without the text: the shadow (the outline of box and tail, 1 to 5 pixels away), the inside, then the line
        /// around box and tail
        /// </summary>
        /// <param name="image">The image, any pixel type</param>
        /// <param name="values">The values of the image's pixels</param>
        /// <param name="rect">The box, normalized, in the editor's coordinates (GDI+ PixelOffsetMode.None)</param>
        /// <param name="target">Where the tail points to</param>
        /// <param name="lineThickness">The width of the line, 0 for none</param>
        /// <param name="lineColor">The color of the line</param>
        /// <param name="fillColor">The color of the inside, transparent for none</param>
        /// <param name="shadow">Draw the shadow</param>
        public static void Bubble(Image image, ImageValues values, Rectangle rect, Point target, int lineThickness, Rgba32 lineColor, Rgba32 fillColor, bool shadow)
        {
            // The editor draws the line of the tail outside of the box and the line of the box outside of the tail: the line around both together
            var bubble = BoxShape(rect, lineThickness).Clip(new ShapeOptions { ClippingOperation = ClippingOperation.Union }, TailShape(rect, target));
            // The editor's bubble shadow starts one pixel further than that of the other elements
            Shapes.Draw(image, values, bubble, lineThickness, lineColor, fillColor, shadow, shadowOffset: 1);
        }

        /// <summary>
        /// The box with round corners, like SpeechbubbleContainer.CreateBubble: the corners are quarter circles of a diameter up to 30 which leaves
        /// room for the line
        /// </summary>
        internal static IPath BoxShape(Rectangle rect, int lineThickness)
        {
            int smallerSideLength = Math.Min(rect.Width, rect.Height);
            // The diameter of the round corners
            int cornerSize = Math.Min(30, smallerSideLength / 2 - lineThickness);
            float left = rect.X, top = rect.Y;
            if (cornerSize <= 0)
            {
                return new RectangularPolygon(left, top, rect.Width, rect.Height);
            }
            // Quarter circles, clockwise from the top left corner
            var radius = new SizeF(cornerSize / 2f, cornerSize / 2f);
            float right = left + rect.Width - radius.Width, bottom = top + rect.Height - radius.Height;
            return new Polygon(
                new ArcLineSegment(new PointF(left + radius.Width, top + radius.Height), radius, 0, 180, 90),
                new ArcLineSegment(new PointF(right, top + radius.Height), radius, 0, 270, 90),
                new ArcLineSegment(new PointF(right, bottom), radius, 0, 0, 90),
                new ArcLineSegment(new PointF(left + radius.Width, bottom), radius, 0, 90, 90));
        }

        /// <summary>
        /// The tail, like SpeechbubbleContainer.CreateTail: a triangle from the middle of the box (its base across the direction, as wide as a tenth
        /// of width and height together, at most half of either) with its tip exactly on the target
        /// </summary>
        internal static IPath TailShape(Rectangle rect, Point target)
        {
            float centerX = rect.Left + rect.Width / 2f;
            float centerY = rect.Top + rect.Height / 2f;
            float tailWidth = (Math.Abs(rect.Width) + Math.Abs(rect.Height)) / 20;
            tailWidth = Math.Min(Math.Abs(rect.Width) / 2, tailWidth);
            tailWidth = Math.Min(Math.Abs(rect.Height) / 2, tailWidth);
            float dx = target.X - centerX;
            float dy = target.Y - centerY;
            double length = Math.Sqrt(dx * dx + dy * dy);
            float normalX = length > 0 ? (float)(-dy / length) : 1;
            float normalY = length > 0 ? (float)(dx / length) : 0;
            return new Polygon(new LinearLineSegment(
                new PointF(centerX + normalX * tailWidth, centerY + normalY * tailWidth),
                new PointF(centerX - normalX * tailWidth, centerY - normalY * tailWidth),
                new PointF(target.X, target.Y)));
        }
    }
}
