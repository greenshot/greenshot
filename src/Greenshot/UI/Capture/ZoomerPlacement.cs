/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;

namespace Greenshot.UI.Capture
{
    /// <summary>
    /// Where the zoomer goes: next to the cursor, on the screen, and if possible not over the selection.
    /// All coordinates are in the same space (pixels of the capture).
    /// </summary>
    public static class ZoomerPlacement
    {
        /// <summary>
        /// Distance between the cursor and the zoomer
        /// </summary>
        public const int Distance = 20;

        /// <summary>
        /// The size of the zoomer for a screen: a fifth of its smaller side, a multiple of 4
        /// </summary>
        public static int GetSize(NativeRect screenBounds)
        {
            int size = Math.Min(screenBounds.Width, screenBounds.Height) / 5;
            return size - size % 4;
        }

        /// <summary>
        /// The offset of the zoomer's top left corner from the cursor
        /// </summary>
        /// <param name="cursor">The cursor</param>
        /// <param name="currentOffset">Where the zoomer is now, it stays there when that is fine</param>
        /// <param name="size">Size of the zoomer</param>
        /// <param name="screenBounds">The screen the cursor is on</param>
        /// <param name="selection">The selection, empty when there is none</param>
        /// <returns>NativePoint with the offset</returns>
        public static NativePoint GetOffset(NativePoint cursor, NativePoint currentOffset, int size, NativeRect screenBounds, NativeRect selection)
        {
            bool Fits(NativePoint offset, bool allowOverSelection)
            {
                var zoomer = new NativeRect(cursor.X + offset.X, cursor.Y + offset.Y, size, size);
                return screenBounds.Contains(zoomer) && (allowOverSelection || selection.IsEmpty || !selection.IntersectsWith(zoomer));
            }

            if (Fits(currentOffset, false))
            {
                return currentOffset;
            }

            // Bottom right, bottom left, top right, top left
            var candidates = new[]
            {
                new NativePoint(Distance, Distance),
                new NativePoint(-Distance - size, Distance),
                new NativePoint(Distance, -Distance - size),
                new NativePoint(-Distance - size, -Distance - size)
            };
            foreach (bool allowOverSelection in new[] { false, true })
            {
                foreach (var candidate in candidates)
                {
                    if (Fits(candidate, allowOverSelection))
                    {
                        return candidate;
                    }
                }
            }

            // Nothing fits (a tiny screen), keep it where it is
            return currentOffset;
        }
    }
}
