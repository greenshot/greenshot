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
using Greenshot.Base.Effects;
using Greenshot.Base.ImageSharpDrawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Greenshot.Tests.ImageSharpDrawing
{
    /// <summary>
    /// What cuts (torn edges, cut marks) and the mouse cursor draw with ImageSharp
    /// </summary>
    public class CutAndCursorDrawingTests
    {
        private static readonly Rgba32 Red = new Rgba32(220, 30, 40, 255);
        private static readonly Rgba32 Blue = new Rgba32(30, 60, 200, 255);

        private static IPath Band(int left, int width) => new RectangularPolygon(left, 0, width, 20);

        [Fact]
        public void Cut_FillsItsArea()
        {
            using var image = new Image<Rgba32>(40, 20, new Rgba32(255, 255, 255, 255));

            CutDrawing.Cut(image, ImageValues.Srgb, new[] { Band(10, 10) }, 0, new PointF[0][], Red, 0, default, null);

            Assert.Equal(Red, image[15, 10]);
            Assert.Equal(new Rgba32(255, 255, 255, 255), image[5, 10]);
        }

        [Fact]
        public void Cut_EdgeLines_DontShowInAnotherCut()
        {
            using var image = new Image<Rgba32>(40, 20, new Rgba32(255, 255, 255, 255));
            var edges = new[]
            {
                new[] { new PointF(10, 0), new PointF(10, 20) },
                new[] { new PointF(25, 0), new PointF(25, 20) }
            };

            // The second cut covers the second edge
            CutDrawing.Cut(image, ImageValues.Srgb, new[] { Band(10, 20), Band(22, 10) }, 0, edges, default, 2, Blue, null);

            Assert.Equal(Blue, image[10, 10]);
            Assert.Equal(new Rgba32(255, 255, 255, 255), image[25, 10]);
        }

        [Fact]
        public void Cut_Shadow_IsCastIntoItsArea_ALaterCutHasItsOwn()
        {
            // The cut is already cut away: transparent
            using var alone = new Image<Rgba32>(40, 20, new Rgba32(255, 255, 255, 255));
            CutDrawing.CutAway(alone, Band(10, 20));
            var shadow = (0.5f, 3, new Point(-1, -1));

            CutDrawing.Cut(alone, ImageValues.Srgb, new[] { Band(10, 20) }, 0, new PointF[0][], default, 0, default, shadow);

            // Next to what is left of the image, the shadow; in the middle of the area none
            Assert.True(alone[11, 10].A > 0, "shadow next to the image");
            Assert.True(alone[11, 10].R < 10, "the shadow is black");
            Assert.Equal(0, alone[20, 10].A);

            using var withLater = new Image<Rgba32>(40, 20, new Rgba32(255, 255, 255, 255));
            CutDrawing.CutAway(withLater, Band(10, 20));
            CutDrawing.Cut(withLater, ImageValues.Srgb, new[] { Band(10, 20), Band(10, 5) }, 0, new PointF[0][], default, 0, default, shadow);

            Assert.Equal(0, withLater[11, 10].A);
        }

        [Fact]
        public void MaskCursor_HasItsColor_InvertsOrLeavesThePixels()
        {
            var gray = new Rgba32(64, 64, 64, 255);
            using var image = new Image<Rgba32>(3, 1, gray);
            // Black mask: the color; white mask with white: inverted; white mask with black: unchanged
            using var color = new Image<Rgba32>(3, 1);
            color[0, 0] = Red;
            color[1, 0] = new Rgba32(255, 255, 255, 255);
            color[2, 0] = new Rgba32(0, 0, 0, 255);
            using var mask = new Image<Rgba32>(3, 1, new Rgba32(255, 255, 255, 255));
            mask[0, 0] = new Rgba32(0, 0, 0, 255);

            MouseCursors.Draw(image, ImageValues.Srgb, color, mask, new Rectangle(0, 0, 3, 1));

            Assert.Equal(Red, image[0, 0]);
            Assert.Equal(new Rgba32(191, 191, 191, 255), image[1, 0]);
            Assert.Equal(gray, image[2, 0]);
        }

        [Fact]
        public void MaskCursor_OnHdrPixels_InvertsTheSrgbColor_HighlightsBecomeBlack()
        {
            const float white = 3f;
            var values = ImageValues.Linear(white);
            float quarter = LinearColorEffects.Decode(0.25f) * white;
            using var image = new Image<RgbaVector>(2, 1);
            image[0, 0] = new RgbaVector(quarter, quarter, quarter, 1);
            image[1, 0] = new RgbaVector(2 * white, 2 * white, 2 * white, 1);
            using var color = new Image<Rgba32>(2, 1, new Rgba32(255, 255, 255, 255));
            using var mask = new Image<Rgba32>(2, 1, new Rgba32(255, 255, 255, 255));

            MouseCursors.Draw(image, values, color, mask, new Rectangle(0, 0, 2, 1));

            Assert.Equal(LinearColorEffects.Decode(0.75f) * white, image[0, 0].R, 3);
            Assert.Equal(0f, image[1, 0].R, 3);
        }
    }
}
