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
    /// The place of a new image container: 20 px after the last one, inside the visible part of the canvas
    /// </summary>
    public class ImageContainerPlacementTests
    {
        private static readonly Rectangle Visible = new Rectangle(0, 0, 800, 600);
        private static readonly Size Small = new Size(100, 100);

        [Fact]
        public void Next_EmptySurface_StartsAtMargin()
        {
            Assert.Equal(new Point(10, 10), ImageContainerPlacement.Next(null, Visible, Small));
        }

        [Fact]
        public void Next_AfterLastContainer_IsOffsetBy20()
        {
            Assert.Equal(new Point(130, 150), ImageContainerPlacement.Next(new Point(110, 130), Visible, Small));
        }

        [Fact]
        public void Next_LeavingVisibleArea_WrapsToTopLeftCorner()
        {
            // 700 + 20 + 100 is beyond the right edge (800)
            Assert.Equal(new Point(10, 10), ImageContainerPlacement.Next(new Point(700, 100), Visible, Small));
        }

        [Fact]
        public void Next_ScrolledCanvas_StaysInVisibleAreaWhenEmpty()
        {
            // The visible part starts at x = 500 of the image: a start at 10 would be outside
            var scrolled = new Rectangle(500, 300, 800, 600);
            Assert.Equal(new Point(510, 310), ImageContainerPlacement.Next(null, scrolled, Small));
        }

        [Fact]
        public void Next_ImageBiggerThanVisibleArea_UsesTopLeftCorner()
        {
            var big = new Size(900, 700);
            Assert.Equal(new Point(10, 10), ImageContainerPlacement.Next(new Point(10, 10), Visible, big));
        }

        [Fact]
        public void Next_NothingVisible_ReturnsOffsetOnly()
        {
            var none = Rectangle.Empty;
            Assert.Equal(new Point(30, 30), ImageContainerPlacement.Next(new Point(10, 10), none, Small));
        }
    }
}
