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

using System.Collections.Generic;
using System.Drawing;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.Drawing.Fields;
using Xunit;
using static Greenshot.Editor.Drawing.FilterContainer;

namespace Greenshot.Tests.Editor
{
    /// <summary>
    /// The blur adds noise, so it can't be undone to read what is under it: hardly visible, the same at every repaint, different for each element
    /// </summary>
    public class BlurNoiseTests
    {
        public BlurNoiseTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static readonly Rectangle BlurredArea = new Rectangle(10, 10, 30, 20);

        private static Surface CreateGraySurface(int blurRadius, params Rectangle[] areas)
        {
            var bitmap = new Bitmap(100, 40, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.FromArgb(128, 128, 128));
            }
            var surface = new Surface(bitmap);
            foreach (var area in areas)
            {
                var obfuscate = new ObfuscateContainer(surface) { Left = area.X, Top = area.Y, Width = area.Width, Height = area.Height };
                obfuscate.SetFieldValue(FieldType.PREPARED_FILTER_OBFUSCATE, PreparedFilter.BLUR);
                obfuscate.SetFieldValue(FieldType.BLUR_RADIUS, blurRadius);
                surface.AddElement(obfuscate, false, false);
            }
            return surface;
        }

        private static List<Color> Pixels(Bitmap bitmap, Rectangle area)
        {
            var pixels = new List<Color>();
            for (int y = area.Top; y < area.Bottom; y++)
            {
                for (int x = area.Left; x < area.Right; x++)
                {
                    pixels.Add(bitmap.GetPixel(x, y));
                }
            }
            return pixels;
        }

        [Theory]
        // The box blur of the editor, and the one of GDI+ (from radius 20)
        [InlineData(3)]
        [InlineData(30)]
        public void Blur_HasNoise_TheSameAtEachExport(int blurRadius)
        {
            using var surface = CreateGraySurface(blurRadius, BlurredArea);

            using var first = (Bitmap)surface.GetImageForExport();
            using var second = (Bitmap)surface.GetImageForExport();

            var pixels = Pixels(first, BlurredArea);
            // Up to 3 levels up or down of the gray (the blur of the gray itself is the gray, give or take a level)
            Assert.All(pixels, pixel =>
            {
                Assert.InRange(pixel.R, 124, 132);
                Assert.InRange(pixel.G, 124, 132);
                Assert.InRange(pixel.B, 124, 132);
            });
            Assert.True(new HashSet<int>(pixels.ConvertAll(pixel => pixel.ToArgb())).Count > 10, "the blurred area has noise");
            Assert.Equal(pixels, Pixels(second, BlurredArea));
            // Outside of the element nothing changes
            Assert.Equal(Color.FromArgb(128, 128, 128).ToArgb(), first.GetPixel(70, 20).ToArgb());
        }

        [Fact]
        public void Blur_HasOtherNoise_ForEachElement()
        {
            var other = new Rectangle(55, 10, 30, 20);
            using var surface = CreateGraySurface(3, BlurredArea, other);

            using var exported = (Bitmap)surface.GetImageForExport();

            Assert.NotEqual(Pixels(exported, BlurredArea), Pixels(exported, other));
        }
    }
}
