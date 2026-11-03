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
using System.Drawing.Drawing2D;
using Greenshot.Base.Core;
using Greenshot.Base.Effects;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// A width or height of 0 means "not given": with the aspect ratio kept it is calculated from the other side,
    /// without it the side keeps its size.
    /// </summary>
    public class ResizeImageTests
    {
        [Theory]
        // Aspect ratio kept: the missing side is calculated
        [InlineData(400, 200, 200, 0, true, 200, 100)]
        [InlineData(400, 200, 0, 100, true, 200, 100)]
        [InlineData(400, 200, 800, 0, true, 800, 400)]
        [InlineData(400, 200, 0, 300, true, 600, 300)]
        [InlineData(400, 200, 0, 0, true, 400, 200)]
        // Aspect ratio kept, both given: fit into the given size
        [InlineData(400, 200, 200, 200, true, 200, 100)]
        [InlineData(400, 200, 1000, 300, true, 600, 300)]
        [InlineData(400, 200, 300, 150, true, 300, 150)]
        // Aspect ratio not kept: a missing side keeps its size
        [InlineData(400, 200, 100, 0, false, 100, 200)]
        [InlineData(400, 200, 0, 50, false, 400, 50)]
        [InlineData(400, 200, 0, 0, false, 400, 200)]
        [InlineData(400, 200, 100, 50, false, 100, 50)]
        public void ResizeEffect_ZeroMeansNotGiven(int sourceWidth, int sourceHeight, int width, int height, bool maintainAspectRatio, int expectedWidth, int expectedHeight)
        {
            using var source = new Bitmap(sourceWidth, sourceHeight);
            var effect = new ResizeEffect(width, height, maintainAspectRatio);
            using var matrix = new Matrix();

            using var result = effect.Apply(source, matrix);

            Assert.Equal(new Size(expectedWidth, expectedHeight), result.Size);
            Assert.Equal((float)expectedWidth / sourceWidth, matrix.Elements[0], 3);
            Assert.Equal((float)expectedHeight / sourceHeight, matrix.Elements[3], 3);
        }

        [Fact]
        public void ResizeImage_CanvasUseNewSize_CentersTheImageInTheGivenSize()
        {
            using var source = new Bitmap(20, 10);
            using (var graphics = Graphics.FromImage(source))
            {
                graphics.Clear(Color.Red);
            }

            using var result = (Bitmap)ImageHelper.ResizeImage(source, true, true, Color.Blue, 32, 32, null);

            Assert.Equal(new Size(32, 32), result.Size);
            // 20x10 scaled to 32x16, centered vertically
            Assert.Equal(Color.Blue.ToArgb(), result.GetPixel(16, 2).ToArgb());
            Assert.Equal(Color.Red.ToArgb(), result.GetPixel(16, 16).ToArgb());
            Assert.Equal(Color.Blue.ToArgb(), result.GetPixel(16, 29).ToArgb());
        }
    }
}
