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

using System.Drawing;
using System.Drawing.Imaging;
using Greenshot.Base.Core;
using Xunit;

namespace Greenshot.Tests.Core
{
    public class IconSizingTests
    {
        [Theory]
        [InlineData(16, 96, 16)]
        [InlineData(16, 120, 20)]
        [InlineData(16, 144, 24)]
        [InlineData(16, 168, 28)]
        [InlineData(16, 192, 32)]
        [InlineData(16, 240, 40)]
        [InlineData(16, 288, 48)]
        [InlineData(20, 144, 30)]
        [InlineData(24, 144, 36)]
        [InlineData(32, 144, 48)]
        [InlineData(0, 144, 24)]
        [InlineData(16, 0, 16)]
        public void PixelSize_ScalesTheBaseSizeWithTheDpi(int baseSize, int dpi, int expected)
        {
            Assert.Equal(expected, IconSizing.PixelSize(baseSize, dpi));
        }

        [Fact]
        public void Scale_WholeMultiple_KeepsThePixelsSharp()
        {
            using var source = new Bitmap(2, 2, PixelFormat.Format32bppArgb);
            source.SetPixel(0, 0, Color.Red);
            source.SetPixel(1, 0, Color.Lime);
            source.SetPixel(0, 1, Color.Blue);
            source.SetPixel(1, 1, Color.Yellow);

            using var scaled = SizedIconCache.Scale(source, 4);

            Assert.Equal(new Size(4, 4), scaled.Size);
            Assert.Equal(Color.Red.ToArgb(), scaled.GetPixel(0, 0).ToArgb());
            Assert.Equal(Color.Red.ToArgb(), scaled.GetPixel(1, 1).ToArgb());
            Assert.Equal(Color.Lime.ToArgb(), scaled.GetPixel(2, 0).ToArgb());
            Assert.Equal(Color.Blue.ToArgb(), scaled.GetPixel(0, 3).ToArgb());
            Assert.Equal(Color.Yellow.ToArgb(), scaled.GetPixel(3, 3).ToArgb());
        }

        [Theory]
        [InlineData(16, 16, 24)]
        [InlineData(16, 16, 20)]
        [InlineData(32, 32, 24)]
        [InlineData(23, 24, 24)]
        public void Scale_ReturnsASquareOfTheSize(int width, int height, int pixelSize)
        {
            using var source = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(source);
            graphics.Clear(Color.Green);

            using var scaled = SizedIconCache.Scale(source, pixelSize);

            Assert.Equal(new Size(pixelSize, pixelSize), scaled.Size);
            // The middle is the icon, not transparent
            Assert.Equal(255, scaled.GetPixel(pixelSize / 2, pixelSize / 2).A);
        }

        [Fact]
        public void GetCopy_ReturnsCopiesWhichBelongToTheCaller()
        {
            var cache = new SizedIconCache();
            var source = IconSource.FromKey(DestinationIcons.Resource("Close.Image"));

            var first = cache.GetCopy(source, 24);
            var second = cache.GetCopy(source, 24);
            Assert.NotNull(first);
            Assert.NotSame(first, second);
            Assert.Equal(new Size(24, 24), first.Size);

            // Disposing a copy doesn't affect the cache or other copies
            first.Dispose();
            Assert.Equal(24, second.Width);
            using var third = cache.GetCopy(source, 24);
            Assert.Equal(24, third.Width);
            second.Dispose();
        }

        [Fact]
        public void GetCopy_UnknownResource_ReturnsNull()
        {
            var cache = new SizedIconCache();
            var source = IconSource.FromResource(typeof(IconSizingTests), "DoesNotExist.Image");

            Assert.Null(cache.GetCopy(source, 24));
            Assert.False(cache.NeedsLoading(source, 24));
        }

        [Fact]
        public void ExeIcons_HaveASmallAndALargeOriginal()
        {
            var source = IconSource.FromKey(DestinationIcons.Exe(@"C:\Windows\notepad.exe", 0));

            Assert.False(source.IsSynchronous);
            Assert.NotEqual(source.OriginalKey(16), source.OriginalKey(24));
            Assert.Equal(source.OriginalKey(24), source.OriginalKey(32));
        }
    }
}
