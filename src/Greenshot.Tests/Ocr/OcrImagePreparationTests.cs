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

using System.Linq;
using Greenshot.Plugin.Win10;
using Xunit;

namespace Greenshot.Tests.Ocr
{
    /// <summary>
    /// What Windows OCR gets: underlines painted over, small captures enlarged
    /// </summary>
    public class OcrImagePreparationTests
    {
        private const int White = unchecked((int)0xFFFFFFFF);
        private const int Blue = unchecked((int)0xFF0645AD);
        private const int Black = unchecked((int)0xFF000000);

        private static int[] Canvas(int width, int height, int color = White) => Enumerable.Repeat(color, width * height).ToArray();

        private static void Row(int[] pixels, int width, int y, int from, int to, int color)
        {
            for (int x = from; x < to; x++) pixels[y * width + x] = color;
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        public void Underline_IsPaintedOver(int thickness)
        {
            const int width = 60, height = 10;
            var pixels = Canvas(width, height);
            for (int t = 0; t < thickness; t++) Row(pixels, width, 5 + t, 5, 55, Blue);

            Assert.Equal(1, OcrImagePreparation.RemoveThinLines(pixels, width, height));
            Assert.All(pixels, pixel => Assert.Equal(White, pixel));
        }

        [Fact]
        public void Underline_CrossedByADescender_IsPaintedOverAroundIt()
        {
            const int width = 60, height = 12;
            var pixels = Canvas(width, height);
            Row(pixels, width, 5, 5, 55, Blue);
            // The descender of a g: 2 pixels wide, from above the line to below it
            for (int y = 3; y < 8; y++) Row(pixels, width, y, 20, 22, Blue);

            Assert.Equal(1, OcrImagePreparation.RemoveThinLines(pixels, width, height));
            Assert.Equal(White, pixels[5 * width + 10]);
            Assert.Equal(White, pixels[5 * width + 40]);
            // The descender stays
            Assert.Equal(Blue, pixels[4 * width + 20]);
            Assert.Equal(Blue, pixels[6 * width + 21]);
        }

        [Fact]
        public void ShortLine_LikeAnUnderscore_IsKept()
        {
            const int width = 40, height = 10;
            var pixels = Canvas(width, height);
            Row(pixels, width, 5, 5, 5 + OcrImagePreparation.MinimumLineLength - 1, Black);
            Assert.Equal(0, OcrImagePreparation.RemoveThinLines(pixels, width, height));
        }

        [Fact]
        public void BottomRowOfAWord_IsKept()
        {
            // Small text with 1 pixel strokes: the bottom row of "mmm" is a stroke, a gap of 2 pixels, a stroke, ...
            const int width = 60, height = 10;
            var pixels = Canvas(width, height);
            for (int x = 5; x < 55; x += 3)
            {
                for (int y = 2; y < 6; y++) pixels[y * width + x] = Black;
            }

            // The tops of the strokes are joined, like the arches of an m
            Row(pixels, width, 2, 5, 55, Black);
            Assert.Equal(0, OcrImagePreparation.RemoveThinLines(pixels, width, height));
            Assert.Equal(Black, pixels[5 * width + 8]);
        }

        [Fact]
        public void ThickBar_IsKept()
        {
            const int width = 60, height = 10;
            var pixels = Canvas(width, height);
            for (int y = 3; y < 6; y++) Row(pixels, width, y, 5, 55, Black);
            Assert.Equal(0, OcrImagePreparation.RemoveThinLines(pixels, width, height));
        }

        [Fact]
        public void EdgeOfAnArea_IsKept()
        {
            // A dark area above a white one: its last row has the same color above it, it is no line
            const int width = 60, height = 10;
            var pixels = Canvas(width, height);
            for (int y = 0; y < 5; y++) Row(pixels, width, y, 0, width, Black);
            Assert.Equal(0, OcrImagePreparation.RemoveThinLines(pixels, width, height));
            Assert.Equal(Black, pixels[4 * width + 30]);
        }

        [Theory]
        [InlineData(800, 600, 2)]
        [InlineData(2560, 1440, 2)]
        [InlineData(3840, 2160, 1)]
        [InlineData(6000, 100, 1)]
        public void Upscale_OnlySmallCaptures(int width, int height, int expected)
        {
            Assert.Equal(expected, OcrImagePreparation.UpscaleFactor(width, height, 10000));
        }
    }
}
