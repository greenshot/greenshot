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
using Greenshot.Editor.Drawing;
using Xunit;

namespace Greenshot.Tests.Editor
{
    /// <summary>
    /// The place of a new image container: 20 px after the last one, mostly inside the surface
    /// </summary>
    public class ImageContainerPlacementTests
    {
        private static readonly Rectangle Canvas = new Rectangle(0, 0, 800, 600);
        private static readonly Size Small = new Size(100, 100);

        [Fact]
        public void Next_EmptySurface_StartsAtMargin()
        {
            Assert.Equal(new Point(10, 10), ImageContainerPlacement.Next(null, Canvas,Small));
        }

        [Fact]
        public void Next_AfterLastContainer_IsOffsetBy20()
        {
            Assert.Equal(new Point(130, 150), ImageContainerPlacement.Next(new Point(110, 130), Canvas,Small));
        }

        [Fact]
        public void Next_LessThan30PercentVisible_WrapsToTopLeftCorner()
        {
            // start 780: only 20 of the 100 px width are inside the right edge (800)
            Assert.Equal(new Point(10, 10), ImageContainerPlacement.Next(new Point(760, 100), Canvas,Small));
        }

        [Fact]
        public void Next_Exactly30PercentVisible_StaysAtOffset()
        {
            // start 770: 30 of the 100 px width are inside the right edge (800)
            Assert.Equal(new Point(770, 120), ImageContainerPlacement.Next(new Point(750, 100), Canvas,Small));
        }

        [Fact]
        public void Next_LessThan30PercentOfHeightVisible_WrapsToTopLeftCorner()
        {
            // start y 580: only 20 of the 100 px height are inside the bottom edge (600)
            Assert.Equal(new Point(10, 10), ImageContainerPlacement.Next(new Point(100, 560), Canvas,Small));
        }

        [Fact]
        public void Next_ImageMostlyBiggerThanVisibleArea_UsesTopLeftCorner()
        {
            // At (30, 30): 770 of 3000 px width are visible, 26%
            var big = new Size(3000, 3000);
            Assert.Equal(new Point(10, 10), ImageContainerPlacement.Next(new Point(10, 10), Canvas,big));
        }

        [Fact]
        public void Next_ImageLargerThanVisibleButMostlyShown_StaysAtOffset()
        {
            // At (30, 30): 770 of 900 px width and 570 of 700 px height are visible
            var big = new Size(900, 700);
            Assert.Equal(new Point(30, 30), ImageContainerPlacement.Next(new Point(10, 10), Canvas,big));
        }

        [Fact]
        public void Next_NoSurface_ReturnsOffsetOnly()
        {
            var none = Rectangle.Empty;
            Assert.Equal(new Point(30, 30), ImageContainerPlacement.Next(new Point(10, 10), none, Small));
        }
    }
}
