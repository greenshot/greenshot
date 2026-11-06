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

using System;
using System.Drawing;
using System.Linq;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;
using Xunit;

namespace Greenshot.Tests.Editor
{
    /// <summary>
    /// What a plugin inserts (e.g. a stamp) lands in the middle of the view, selected, as one undo step
    /// </summary>
    public class SurfaceInsertElementsTests
    {
        public SurfaceInsertElementsTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static RectangleContainer Rectangle(Surface surface, int width, int height) =>
            new RectangleContainer(surface) { Width = width, Height = height };

        [Fact]
        public void InsertElements_CentersTheElement_SelectsOnlyIt_AndOneUndoRemovesIt()
        {
            using var surface = new Surface(new Bitmap(400, 300));
            var before = Rectangle(surface, 20, 20);
            surface.AddElement(before, false, false);
            surface.SelectElement(before);
            var inserted = Rectangle(surface, 100, 50);

            surface.InsertElements(new IDrawableContainer[] { inserted });

            Assert.Equal(new NativeRect(150, 125, 100, 50), inserted.Bounds);
            Assert.Same(inserted, Assert.Single(surface.SelectedElements));
            Assert.False(before.Selected);

            surface.Undo();
            Assert.Same(before, Assert.Single(surface.Elements));
        }

        [Fact]
        public void InsertElements_MakesAnElementSmaller_WhenItDoesNotFitTheView()
        {
            using var surface = new Surface(new Bitmap(400, 300));
            var inserted = Rectangle(surface, 800, 150);

            surface.InsertElements(new IDrawableContainer[] { inserted });

            // Half the size: as wide as the view, the proportions kept
            Assert.Equal(new NativeRect(0, 112, 400, 75), inserted.Bounds);
        }

        [Fact]
        public void InsertElements_PlacesEachFurtherElementLowerAndRight_AndIgnoresNull()
        {
            using var surface = new Surface(new Bitmap(400, 300));
            var first = Rectangle(surface, 100, 50);
            var second = Rectangle(surface, 100, 50);

            surface.InsertElements(new IDrawableContainer[] { first, null, second });

            Assert.Equal(new NativeRect(150, 125, 100, 50), first.Bounds);
            Assert.Equal(new NativeRect(160, 135, 100, 50), second.Bounds);
            Assert.Equal(2, surface.SelectedElements.Count);
            Assert.Equal(2, surface.Elements.Count());
        }
    }
}
