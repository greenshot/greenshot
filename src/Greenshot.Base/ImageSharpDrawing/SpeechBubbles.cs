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
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;

namespace Greenshot.Base.ImageSharpDrawing
{
    /// <summary>
    /// The speech bubble of the editor (SpeechbubbleContainer) drawn with ImageSharp into an image of any pixel type: the box with round corners and
    /// the tail to its target. Its text is <see cref="Texts.Draw"/> with the box. Colors 8-bit sRGB like the editor's, put into the image by its
    /// <see cref="ImageValues"/>.
    /// </summary>
    public static class SpeechBubbles
    {
        // Points per quarter circle of a round corner
        private const int ArcPoints = 16;

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
            bool lineVisible = lineThickness > 0 && lineColor.A > 0;
            if (shadow && (lineVisible || fillColor.A > 0))
            {
                foreach (var (offset, alpha) in Painting.ShadowSteps())
                {
                    Painting.Stroke(image, bubble.Translate(offset + 1, offset + 1), Painting.PenWidth(lineThickness), new Vector4(0, 0, 0, alpha));
                }
            }
            Painting.Fill(image, bubble, values.Of(fillColor));
            if (lineVisible)
            {
                Painting.Stroke(image, bubble, lineThickness, values.Of(lineColor));
            }
        }

        /// <summary>
        /// The box with round corners, like SpeechbubbleContainer.CreateBubble: the corners are quarter circles of a diameter up to 30 which leaves
        /// room for the line, in ImageSharp's coordinates
        /// </summary>
        internal static IPath BoxShape(Rectangle rect, int lineThickness)
        {
            int smallerSideLength = Math.Min(rect.Width, rect.Height);
            int cornerRadius = Math.Min(30, smallerSideLength / 2 - lineThickness);
            float left = rect.X + Painting.GdiPixelCenter, top = rect.Y + Painting.GdiPixelCenter;
            if (cornerRadius <= 0)
            {
                return new RectangularPolygon(left, top, rect.Width, rect.Height);
            }
            // GDI+ AddArc with a box of cornerRadius: a circle of that diameter; angles clockwise from the x axis (y goes down)
            var points = new List<PointF>();
            void Arc(float boxLeft, float boxTop, float startAngle)
            {
                float radius = cornerRadius / 2f;
                var center = new Vector2(boxLeft + radius, boxTop + radius);
                for (int i = 0; i <= ArcPoints; i++)
                {
                    double angle = (startAngle + 90.0 * i / ArcPoints) * Math.PI / 180;
                    points.Add(center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius);
                }
            }
            Arc(left, top, 180);
            Arc(left + rect.Width - cornerRadius, top, 270);
            Arc(left + rect.Width - cornerRadius, top + rect.Height - cornerRadius, 0);
            Arc(left, top + rect.Height - cornerRadius, 90);
            return new Polygon(new LinearLineSegment(points.ToArray()));
        }

        /// <summary>
        /// The tail, like SpeechbubbleContainer.CreateTail: a triangle from the middle of the box (its base across, as wide as a tenth of width and
        /// height together, at most half of either) to the target, with the editor's whole-number rounding, in ImageSharp's coordinates
        /// </summary>
        internal static IPath TailShape(Rectangle rect, Point target)
        {
            int centerX = rect.Left + rect.Width / 2;
            int centerY = rect.Top + rect.Height / 2;
            int tailLength = (int)Math.Sqrt(Math.Pow(target.X - centerX, 2) + Math.Pow(target.Y - centerY, 2));
            int tailWidth = (Math.Abs(rect.Width) + Math.Abs(rect.Height)) / 20;
            tailWidth = Math.Min(Math.Abs(rect.Width) / 2, tailWidth);
            tailWidth = Math.Min(Math.Abs(rect.Height) / 2, tailWidth);
            int tailAngle = 90 + (int)(Math.Atan2(target.Y - centerY, target.X - centerX) * 180 / Math.PI);
            var turn = Matrix3x2.CreateRotation((float)(tailAngle * Math.PI / 180))
                       * Matrix3x2.CreateTranslation(centerX + Painting.GdiPixelCenter, centerY + Painting.GdiPixelCenter);
            return new Polygon(new LinearLineSegment(new PointF(-tailWidth, 0), new PointF(tailWidth, 0), new PointF(0, -tailLength))).Transform(turn);
        }
    }
}
