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
using Greenshot.Document;
using Greenshot.Editor.Standalone.Rendering;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Xunit;

namespace Greenshot.Editor.Standalone.Tests.Rendering
{
    public class DocumentRendererTests
    {
        [Fact]
        public void TheGallery_IsDrawn()
        {
            using var image = DocumentRenderer.Render(Samples.Gallery());
            Samples.Keep(image, "gallery");

            Assert.Equal(640, image.Width);
            Assert.Equal(420, image.Height);
        }

        /// <summary>
        /// The biggest difference of a colour channel between two images of the same size
        /// </summary>
        private static int Difference(Image<Rgba32> a, Image<Rgba32> b)
        {
            Assert.Equal(a.Size, b.Size);
            int worst = 0;
            for (int y = 0; y < a.Height; y++)
            {
                for (int x = 0; x < a.Width; x++)
                {
                    Rgba32 p = a[x, y], q = b[x, y];
                    worst = Math.Max(worst, Math.Max(Math.Max(Math.Abs(p.R - q.R), Math.Abs(p.G - q.G)), Math.Max(Math.Abs(p.B - q.B), Math.Abs(p.A - q.A))));
                }
            }

            return worst;
        }

        [Theory]
        [InlineData(400, 130, 30, 30)]
        [InlineData(10, 10, 300, 200)]
        [InlineData(205.5, 285.5, 100, 60)]
        [InlineData(500, 255, 20, 20)]
        public void APartOfTheDocument_IsDrawnLikeTheWholeDocument(double x, double y, double width, double height)
        {
            var document = Samples.Gallery();
            using var cache = new RenderCache();
            using var whole = DocumentRenderer.Render(document, cache);
            var area = DocumentRenderer.Affected(document, new RectD(x, y, width, height));

            using var part = DocumentRenderer.Render(document, area, cache);
            Samples.Keep(part, $"part-{x}-{y}");

            using var expected = whole.Clone(context => context.Crop(new Rectangle((int)area.X, (int)area.Y, (int)area.Width, (int)area.Height)));
            Assert.True(Difference(expected, part) <= 2, $"A part differs from the whole by {Difference(expected, part)}");
        }
    }
}
