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


using System.Drawing;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// Where a new image container is placed: 20 px after the last one, and mostly inside the surface
    /// </summary>
    public static class ImageContainerPlacement
    {
        /// <summary>
        /// The distance between two image containers which are added one after another
        /// </summary>
        public const int Offset = 20;

        /// <summary>
        /// The distance to the edge of the surface, and the start on an empty surface
        /// </summary>
        public const int Margin = 10;

        /// <summary>
        /// The part of the width and of the height which has to stay visible, else the image goes back to the top left corner
        /// </summary>
        public const int MinimumVisiblePercent = 30;

        /// <summary>
        /// Calculates the location of a new image container (in image coordinates).
        /// </summary>
        /// <param name="lastLocation">The location of the last added image container, null when there is none</param>
        /// <param name="canvas">The surface the image has to be shown on (image coordinates)</param>
        /// <param name="size">The size of the new image</param>
        /// <returns>The location of the top left corner</returns>
        public static Point Next(Point? lastLocation, Rectangle canvas, Size size)
        {
            Point start = lastLocation.HasValue
                ? new Point(lastLocation.Value.X + Offset, lastLocation.Value.Y + Offset)
                : new Point(canvas.Left + Margin, canvas.Top + Margin);

            // No surface (e.g. no image yet): the offset is all we know
            if (canvas.Width <= 0 || canvas.Height <= 0)
            {
                return start;
            }

            // Stays where it is while at least MinimumVisiblePercent of its width and of its height are visible
            Rectangle shown = Rectangle.Intersect(new Rectangle(start, size), canvas);
            bool mostlyVisible = shown.Width * 100 >= size.Width * MinimumVisiblePercent
                                 && shown.Height * 100 >= size.Height * MinimumVisiblePercent;
            if (mostlyVisible)
            {
                return start;
            }

            // Back to the top left corner of the surface
            return new Point(canvas.Left + Margin, canvas.Top + Margin);
        }
    }
}
