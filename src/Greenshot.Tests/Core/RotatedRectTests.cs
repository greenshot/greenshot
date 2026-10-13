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

using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// The geometry of RotatedRect, which the OCR uses for rotated text
    /// </summary>
    public class RotatedRectTests
    {
        // 100x10 around (50, 5), rotated by 90 degrees around (50, 5): a vertical bar of 10x100 from (45, -45) to (55, 55)
        private static readonly RotatedRect VerticalBar = new(0, 0, 100, 10, 90, 50, 5);

        [Fact]
        public void NotRotated_BoundsAndCornersAreTheRect()
        {
            var rect = new RotatedRect(new NativeRect(5, 6, 70, 12));

            Assert.Equal(new NativeRect(5, 6, 70, 12), rect.Bounds);
            Assert.Equal(new System.Windows.Point(5, 6), rect.Corners[0]);
            Assert.Equal(new System.Windows.Point(75, 18), rect.Corners[2]);
            Assert.True(rect.Contains(5, 6));
            Assert.False(rect.Contains(75, 6));
        }

        [Fact]
        public void Rotated_Bounds_AreAroundTheRotatedCorners()
        {
            Assert.Equal(new NativeRect(45, -45, 10, 100), VerticalBar.Bounds);
        }

        [Fact]
        public void Rotated_Contains_UsesTheRotatedShape()
        {
            Assert.True(VerticalBar.Contains(50, -40));
            Assert.True(VerticalBar.Contains(50, 50));
            // Inside the rectangle before the rotation, but not after it
            Assert.False(VerticalBar.Contains(90, 5));
        }

        [Fact]
        public void Rotated_IntersectsWith_UsesTheRotatedShape()
        {
            Assert.True(VerticalBar.IntersectsWith(new NativeRect(40, 0, 10, 10)));
            Assert.False(VerticalBar.IntersectsWith(new NativeRect(60, 0, 30, 10)));
            // 45 degrees: the corner of the bounds is empty
            var diagonal = new RotatedRect(0, 0, 100, 10, 45, 50, 5);
            var bounds = diagonal.Bounds;
            Assert.True(diagonal.IntersectsWith(new NativeRect(45, 0, 10, 10)));
            Assert.False(diagonal.IntersectsWith(new NativeRect(bounds.Left, bounds.Bottom - 10, 10, 10)));
            Assert.False(diagonal.IntersectsWith(new NativeRect(bounds.Right - 10, bounds.Top, 10, 10)));
        }

        [Fact]
        public void Offset_MovesTheRotatedShape()
        {
            var moved = VerticalBar.Offset(-5, 20);

            Assert.Equal(new NativeRect(40, -25, 10, 100), moved.Bounds);
            Assert.Equal(VerticalBar.Corners[0].X - 5, moved.Corners[0].X, 9);
            Assert.Equal(VerticalBar.Corners[0].Y + 20, moved.Corners[0].Y, 9);
        }

        [Fact]
        public void Union_SameRotation_StaysRotated()
        {
            var first = new RotatedRect(0, 0, 40, 10, 30, 100, 100);
            var second = new RotatedRect(60, 2, 40, 10, 30, 100, 100);

            var union = first.Union(second);

            Assert.Equal(new RotatedRect(0, 0, 100, 12, 30, 100, 100), union);
            Assert.Equal(first, default(RotatedRect).Union(first));
        }

        [Fact]
        public void Union_DifferentRotation_IsTheUnionOfTheBounds()
        {
            var union = VerticalBar.Union(new RotatedRect(new NativeRect(100, 100, 10, 10)));

            Assert.Equal(0, union.Angle);
            Assert.Equal(new NativeRect(45, -45, 65, 155), union.Bounds);
        }
    }
}
