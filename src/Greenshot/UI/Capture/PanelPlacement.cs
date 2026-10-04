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

namespace Greenshot.UI.Capture
{
    /// <summary>
    /// Where a panel of a capture tool or overlay goes (e.g. a help text): in a corner of the monitor with the cursor,
    /// away from the cursor and from everything else which should stay visible.
    /// All coordinates are in the same space (pixels of the capture).
    /// </summary>
    public static class PanelPlacement
    {
        /// <summary>
        /// Distance between a panel and the edge of the monitor
        /// </summary>
        public const int Margin = 10;

        /// <summary>
        /// A panel keeps at least this distance from the cursor
        /// </summary>
        public const int CursorClearance = 40;

        /// <summary>
        /// The bounds for a panel
        /// </summary>
        /// <param name="size">Size of the panel</param>
        /// <param name="monitor">The monitor the cursor is on</param>
        /// <param name="cursor">The cursor</param>
        /// <param name="current">Where the panel is now, it stays there when that is fine; empty for a new panel</param>
        /// <param name="avoid">What the panel should not cover, e.g. the selection, the zoomer and other panels</param>
        public static NativeRect Place(NativeSize size, NativeRect monitor, NativePoint cursor, NativeRect current, IEnumerable<NativeRect> avoid)
        {
            var avoidList = (avoid ?? Enumerable.Empty<NativeRect>()).Where(rect => !rect.IsEmpty).ToList();
            var cursorArea = new NativeRect(cursor.X - CursorClearance, cursor.Y - CursorClearance, 2 * CursorClearance, 2 * CursorClearance);

            bool Free(NativeRect rect, bool checkAvoid) =>
                monitor.Contains(rect) && !rect.IntersectsWith(cursorArea) && (!checkAvoid || !avoidList.Any(other => rect.IntersectsWith(other)));

            if (!current.IsEmpty && current.Width == size.Width && current.Height == size.Height && Free(current, true))
            {
                return current;
            }

            int left = monitor.Left + Margin;
            int top = monitor.Top + Margin;
            int right = monitor.Right - Margin - size.Width;
            int bottom = monitor.Bottom - Margin - size.Height;
            // The corner farthest from the cursor first
            var corners = new[]
                {
                    new NativeRect(left, top, size.Width, size.Height),
                    new NativeRect(right, top, size.Width, size.Height),
                    new NativeRect(left, bottom, size.Width, size.Height),
                    new NativeRect(right, bottom, size.Width, size.Height)
                }
                .OrderByDescending(corner => DistanceSquared(corner, cursor))
                .ToList();

            return corners.FirstOrDefault(corner => Free(corner, true)) is { IsEmpty: false } free
                ? free
                // Everything is covered: only keep away from the cursor
                : corners.FirstOrDefault(corner => Free(corner, false)) is { IsEmpty: false } awayFromCursor
                    ? awayFromCursor
                    : corners[0];
        }

        private static long DistanceSquared(NativeRect rect, NativePoint point)
        {
            long dx = rect.Left + rect.Width / 2 - point.X;
            long dy = rect.Top + rect.Height / 2 - point.Y;
            return dx * dx + dy * dy;
        }
    }
}
