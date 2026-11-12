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
using System.IO;
using System.Linq;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Interfaces.Drawing.Adorners;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Adorners;
using Xunit;

namespace Greenshot.Tests.Editor
{
    /// <summary>
    /// The adorners are drawn in screen pixels, whatever the zoom: grabbing them and their size must follow that
    /// </summary>
    public class AdornerTests
    {
        public AdornerTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static (Surface surface, RectangleContainer rectangle) CreateSurfaceWithRectangle()
        {
            var surface = new Surface(new Bitmap(400, 400));
            var rectangle = new RectangleContainer(surface)
            {
                Left = 100,
                Top = 100,
                Width = 50,
                Height = 50
            };
            surface.AddElement(rectangle, false, false);
            return (surface, rectangle);
        }

        private static IAdorner TopLeft(IDrawableContainer element)
            => element.Adorners.OfType<ResizeAdorner>().Single(adorner => adorner.Position == Positions.TopLeft);

        [Theory]
        [InlineData(4u, 1u)]
        [InlineData(1u, 1u)]
        [InlineData(1u, 4u)]
        public void HitTest_IsInScreenPixelsAtEveryZoom(uint numerator, uint denominator)
        {
            var (surface, rectangle) = CreateSurfaceWithRectangle();
            using (surface)
            {
                surface.ZoomFactor = new Fraction(numerator, denominator);
                var adorner = TopLeft(rectangle);
                NativePoint onScreen = surface.ToSurfaceCoordinates(new NativePoint(100, 100));

                Assert.True(adorner.HitTest(onScreen));
                // The adorner is 6 pixels with a margin of 3 around it, whatever the zoom
                Assert.True(adorner.HitTest(new NativePoint(onScreen.X + 5, onScreen.Y + 5)));
                Assert.False(adorner.HitTest(new NativePoint(onScreen.X + 8, onScreen.Y)));
            }
        }

        [Fact]
        public void AddElement_GivesTheAdornersTheSurfaceDpi()
        {
            var (surface, rectangle) = CreateSurfaceWithRectangle();
            using (surface)
            {
                surface.AdjustToDpi(192);
                using var stream = new MemoryStream();
                surface.SaveElementsToStream(stream);
                stream.Position = 0;
                surface.LoadElementsFromStream(stream);

                var loaded = surface.Elements.OfType<RectangleContainer>().Last();
                Assert.NotSame(rectangle, loaded);
                Assert.Equal(12, TopLeft(loaded).Bounds.Width);
            }
        }

        [Fact]
        public void DrawingBounds_DoNotDependOnTheAdorners()
        {
            var (surface, rectangle) = CreateSurfaceWithRectangle();
            using (surface)
            {
                var before = rectangle.DrawingBounds;
                surface.AdjustToDpi(192);
                Assert.Equal(before, rectangle.DrawingBounds);
            }
        }

        [Fact]
        public void HasFilters_OnlyForFilterElements()
        {
            var (surface, rectangle) = CreateSurfaceWithRectangle();
            using (surface)
            {
                var obfuscate = new ObfuscateContainer(surface);
                Assert.True(obfuscate.HasFilters);
                Assert.False(obfuscate.HasInvertedFilter);
                Assert.False(rectangle.HasFilters);
                Assert.False(rectangle.HasInvertedFilter);
            }
        }
    }
}
