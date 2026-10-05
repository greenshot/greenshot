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
        /// A panel may cover this part of what it should only loosely avoid (the selection) before it moves: small overlaps are not worth a jump
        /// </summary>
        public const double ToleratedOverlap = 0.25;

        /// <summary>
        /// The bounds for a panel. A panel stays where it is unless it comes close to the cursor, overlaps another panel,
        /// or covers a good part of the selection while a completely free corner exists: moving from one overlap to another isn't worth it.
        /// </summary>
        /// <param name="size">Size of the panel</param>
        /// <param name="monitor">The monitor the cursor is on</param>
        /// <param name="cursor">The cursor</param>
        /// <param name="current">Where the panel is now, empty for a new panel</param>
        /// <param name="avoid">What the panel must not cover, e.g. the other panels</param>
        /// <param name="avoidLoosely">What the panel should not cover, e.g. the selection; a small overlap is tolerated, see ToleratedOverlap</param>
        public static NativeRect Place(NativeSize size, NativeRect monitor, NativePoint cursor, NativeRect current, IEnumerable<NativeRect> avoid,
            IEnumerable<NativeRect> avoidLoosely = null)
        {
            var avoidList = (avoid ?? Enumerable.Empty<NativeRect>()).Where(rect => !rect.IsEmpty).ToList();
            var looseList = (avoidLoosely ?? Enumerable.Empty<NativeRect>()).Where(rect => !rect.IsEmpty).ToList();
            var cursorArea = new NativeRect(cursor.X - CursorClearance, cursor.Y - CursorClearance, 2 * CursorClearance, 2 * CursorClearance);

            bool AwayFromCursor(NativeRect rect) => monitor.Contains(rect) && !rect.IntersectsWith(cursorArea);
            bool ClearOfPanels(NativeRect rect) => !avoidList.Any(other => rect.IntersectsWith(other));
            bool Free(NativeRect rect) => AwayFromCursor(rect) && ClearOfPanels(rect) && !looseList.Any(other => rect.IntersectsWith(other));

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
            var free = corners.FirstOrDefault(Free);

            if (!current.IsEmpty)
            {
                // A panel which changed size grows or shrinks away from its corner, so its outer edges stay where they are
                var resized = Resize(current, size, monitor);
                if (AwayFromCursor(resized) && ClearOfPanels(resized)
                    && (free.IsEmpty || CoveredPart(resized, looseList) <= ToleratedOverlap))
                {
                    return resized;
                }
            }

            return !free.IsEmpty
                ? free
                // Everything is covered: only keep away from the cursor and the other panels, or at least the cursor
                : corners.FirstOrDefault(corner => AwayFromCursor(corner) && ClearOfPanels(corner)) is { IsEmpty: false } clearOfPanels
                    ? clearOfPanels
                    : corners.FirstOrDefault(AwayFromCursor) is { IsEmpty: false } awayFromCursor
                        ? awayFromCursor
                        : corners[0];
        }

        /// <summary>
        /// The largest part of the panel, or of one of the rectangles, which they share: covering half of a small selection counts as much as being half covered
        /// </summary>
        private static double CoveredPart(NativeRect panel, IEnumerable<NativeRect> rects)
        {
            double covered = 0;
            foreach (var rect in rects)
            {
                var overlap = panel.Intersect(rect);
                if (overlap.IsEmpty)
                {
                    continue;
                }
                double area = (double)overlap.Width * overlap.Height;
                covered = Math.Max(covered, area / Math.Max(1.0, Math.Min((double)panel.Width * panel.Height, (double)rect.Width * rect.Height)));
            }
            return covered;
        }

        /// <summary>
        /// The panel with a new size, keeping the edges which face the nearest corner of the monitor
        /// </summary>
        public static NativeRect Resize(NativeRect current, NativeSize size, NativeRect monitor)
        {
            bool right = current.Left + current.Width / 2 > monitor.Left + monitor.Width / 2;
            bool bottom = current.Top + current.Height / 2 > monitor.Top + monitor.Height / 2;
            int x = right ? current.Right - size.Width : current.Left;
            int y = bottom ? current.Bottom - size.Height : current.Top;
            return new NativeRect(x, y, size.Width, size.Height);
        }

        private static long DistanceSquared(NativeRect rect, NativePoint point)
        {
            long dx = rect.Left + rect.Width / 2 - point.X;
            long dy = rect.Top + rect.Height / 2 - point.Y;
            return dx * dx + dy * dy;
        }
    }
}
