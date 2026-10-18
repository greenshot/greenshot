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
using System.Collections.Generic;
using System.Linq;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;

namespace Greenshot.Capturing
{
    /// <summary>
    /// Where the zoomer goes: next to the cursor, on the screen, and if possible not over the selection or the panels.
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
            => GetOffset(cursor, currentOffset, size, screenBounds, new[] { selection });

        /// <summary>
        /// The offset of the zoomer's top left corner from the cursor
        /// </summary>
        /// <param name="cursor">The cursor</param>
        /// <param name="currentOffset">Where the zoomer is now, it stays there when that is fine</param>
        /// <param name="size">Size of the zoomer</param>
        /// <param name="screenBounds">The screen the cursor is on</param>
        /// <param name="avoid">What the zoomer should not cover if possible: the selection, panels of tools and overlays</param>
        /// <returns>NativePoint with the offset</returns>
        public static NativePoint GetOffset(NativePoint cursor, NativePoint currentOffset, int size, NativeRect screenBounds, IReadOnlyCollection<NativeRect> avoid)
            => GetOffset(cursor, currentOffset, new NativeSize(size, size), screenBounds, avoid);

        /// <summary>
        /// The offset of the zoomer's top left corner from the cursor, for a zoomer which isn't square (the preview of the selection in its place)
        /// </summary>
        /// <param name="cursor">The cursor</param>
        /// <param name="currentOffset">Where the zoomer is now, it stays there when that is fine</param>
        /// <param name="size">Width and height of the zoomer</param>
        /// <param name="screenBounds">The screen the cursor is on</param>
        /// <param name="avoid">What the zoomer should not cover if possible: the selection, panels of tools and overlays</param>
        /// <returns>NativePoint with the offset</returns>
        public static NativePoint GetOffset(NativePoint cursor, NativePoint currentOffset, NativeSize size, NativeRect screenBounds, IReadOnlyCollection<NativeRect> avoid)
        {
            bool Fits(NativePoint offset, bool allowOverlap)
            {
                var zoomer = new NativeRect(cursor.X + offset.X, cursor.Y + offset.Y, size.Width, size.Height);
                return screenBounds.Contains(zoomer) && (allowOverlap || !avoid.Any(rect => !rect.IsEmpty && rect.IntersectsWith(zoomer)));
            }

            if (Fits(currentOffset, false))
            {
                return currentOffset;
            }

            // Bottom right, bottom left, top right, top left
            var candidates = new[]
            {
                new NativePoint(Distance, Distance),
                new NativePoint(-Distance - size.Width, Distance),
                new NativePoint(Distance, -Distance - size.Height),
                new NativePoint(-Distance - size.Width, -Distance - size.Height)
            };
            foreach (bool allowOverlap in new[] { false, true })
            {
                foreach (var candidate in candidates)
                {
                    if (Fits(candidate, allowOverlap))
                    {
                        return candidate;
                    }
                }
            }

            // Nothing fits (a tiny screen), keep it where it is
            return currentOffset;
        }

        /// <summary>
        /// The same corner of the cursor for another size: left or above the cursor keeps the distance to it
        /// </summary>
        public static NativePoint Resize(NativePoint offset, NativeSize size) =>
            new NativePoint(offset.X < 0 ? -Distance - size.Width : offset.X, offset.Y < 0 ? -Distance - size.Height : offset.Y);
    }
}
