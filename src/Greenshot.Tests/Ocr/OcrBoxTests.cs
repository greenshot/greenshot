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

using System.Windows;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Interfaces.Plugin;
using Xunit;

namespace Greenshot.Tests.Ocr
{
    /// <summary>
    /// The rotated box of OCR text: the rectangle around it, hit tests and the selection
    /// </summary>
    public class OcrBoxTests
    {
        [Fact]
        public void FromRect_Horizontal_BoundsAreTheRectRoundedOutwards()
        {
            var box = OcrBox.FromRect(new Rect(40.2, 30, 79.6, 20));

            Assert.False(box.IsRotated);
            Assert.Equal(new NativeRect(40, 30, 80, 20), box.Bounds);
            Assert.Equal(20, box.PixelHeight);
        }

        [Fact]
        public void FromRect_90Degrees_RotatesClockwiseAroundTheCentre()
        {
            // Centre (100, 50). Rotating clockwise by 90 degrees (y points down): (x, y) -> (100 - (y - 50), 50 + (x - 100))
            // The corners (110, 50), (150, 50), (110, 60), (150, 60) become (100, 60), (100, 100), (90, 60), (90, 100)
            var box = OcrBox.FromRect(new Rect(110, 50, 40, 10), 90, new Point(100, 50));

            Assert.Equal(new NativeRect(90, 60, 10, 40), box.Bounds);
            // The text keeps its height, the rectangle around it doesn't
            Assert.Equal(10, box.PixelHeight);
        }

        [Fact]
        public void FromCorners_IsTheBoxInReadingDirection()
        {
            // A line going down right at 45 degrees, 20 long and 5 high
            var box = OcrBox.FromCorners(new Point(10, 10), new Point(10 + 20 / System.Math.Sqrt(2), 10 + 20 / System.Math.Sqrt(2)),
                new Point(10 - 5 / System.Math.Sqrt(2), 10 + 5 / System.Math.Sqrt(2)));

            Assert.Equal(45, box.Angle, 6);
            Assert.Equal(20, box.Width, 6);
            Assert.Equal(5, box.Height, 6);
            var corners = box.Corners();
            Assert.Equal(10, corners[0].X, 6);
            Assert.Equal(10, corners[0].Y, 6);
        }

        [Fact]
        public void Contains_RotatedText_OnlyOnTheText()
        {
            // A long thin line at 45 degrees through (100, 100)
            var box = new OcrBox(new Point(100, 100), 100, 10, 45);

            Assert.True(box.Contains(new Point(100, 100)));
            Assert.True(box.Contains(new Point(130, 130)));
            // In the rectangle around it, but next to the text
            Assert.True(box.Bounds.Contains(new NativePoint(130, 70)));
            Assert.False(box.Contains(new Point(130, 70)));
        }

        [Fact]
        public void IntersectsWith_RotatedText_SeparatesWhatOnlyTheBoundsTouch()
        {
            var box = new OcrBox(new Point(100, 100), 100, 10, 45);

            Assert.True(box.IntersectsWith(new Rect(125, 125, 10, 10)));
            // A selection in the corner of the rectangle around the text, away from the text
            Assert.True(box.Bounds.IntersectsWith(new NativeRect(125, 60, 15, 15)));
            Assert.False(box.IntersectsWith(new Rect(125, 60, 15, 15)));
        }

        [Fact]
        public void IntersectsWith_Horizontal_TouchingEdgesDontCount()
        {
            var box = OcrBox.FromRect(new NativeRect(10, 10, 20, 10));

            Assert.True(box.IntersectsWith(new NativeRect(29, 19, 5, 5)));
            Assert.False(box.IntersectsWith(new NativeRect(30, 10, 5, 5)));
            Assert.False(box.IntersectsWith(new NativeRect(10, 20, 5, 5)));
        }

        [Fact]
        public void Union_KeepsTheAngle()
        {
            var center = new Point(0, 0);
            var first = OcrBox.FromRect(new Rect(0, 0, 40, 10), 20, center);
            var second = OcrBox.FromRect(new Rect(50, 0, 30, 12), 20, center);

            var union = first.Union(second);

            Assert.Equal(20, union.Angle, 6);
            Assert.Equal(80, union.Width, 6);
            Assert.Equal(12, union.Height, 6);
        }

        [Fact]
        public void Offset_MovesTheBox()
        {
            var box = new OcrBox(new Point(50, 50), 40, 10, 30).Offset(-10, 5);

            Assert.Equal(new Point(40, 55), box.Center);
            Assert.Equal(30, box.Angle);
        }
    }
}
