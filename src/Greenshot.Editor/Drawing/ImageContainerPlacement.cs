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
    /// Where a new image container is placed: 20 px after the last one, and inside the visible part of the canvas
    /// </summary>
    public static class ImageContainerPlacement
    {
        /// <summary>
        /// The distance between two image containers which are added one after another
        /// </summary>
        public const int Offset = 20;

        /// <summary>
        /// The distance to the edge of the visible part, and the start on an empty surface
        /// </summary>
        public const int Margin = 10;

        /// <summary>
        /// Calculates the location of a new image container (in image coordinates).
        /// </summary>
        /// <param name="lastLocation">The location of the last added image container, null when there is none</param>
        /// <param name="visible">The visible part of the canvas (image coordinates)</param>
        /// <param name="size">The size of the new image</param>
        /// <returns>The location of the top left corner</returns>
        public static Point Next(Point? lastLocation, Rectangle visible, Size size)
        {
            Point start = lastLocation.HasValue
                ? new Point(lastLocation.Value.X + Offset, lastLocation.Value.Y + Offset)
                : new Point(visible.Left + Margin, visible.Top + Margin);

            // Nothing visible (e.g. the editor is not shown yet): the offset is all we know
            if (visible.Width <= 0 || visible.Height <= 0)
            {
                return start;
            }

            bool fits = start.X >= visible.Left && start.Y >= visible.Top
                        && start.X + size.Width <= visible.Right && start.Y + size.Height <= visible.Bottom;
            if (fits)
            {
                return start;
            }

            // Back to the top left corner of the visible part, also for an image which is bigger than the visible part
            return new Point(visible.Left + Margin, visible.Top + Margin);
        }
    }
}
