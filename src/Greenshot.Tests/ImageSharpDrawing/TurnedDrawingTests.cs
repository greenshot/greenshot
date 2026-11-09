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
using System.Numerics;
using Greenshot.ImageSharpDrawing;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Greenshot.Tests.ImageSharpDrawing
{
    /// <summary>
    /// The painters turned: a quarter turn draws what the turned element covers, the shadow still falls right and down
    /// </summary>
    public class TurnedDrawingTests
    {
        private static readonly Rgba32 Red = new Rgba32(220, 30, 40, 255);
        private static readonly Rgba32 Blue = new Rgba32(30, 60, 200, 255);

        // A quarter turn clockwise around the middle of a 60 x 60 image
        private static readonly Matrix3x2 QuarterTurn = Matrix3x2.CreateRotation((float)(Math.PI / 2), new Vector2(30, 30));

        private static void AssertSame(Image<Rgba32> expected, Image<Rgba32> actual)
        {
            for (int y = 0; y < expected.Height; y++)
            {
                for (int x = 0; x < expected.Width; x++)
                {
                    Rgba32 e = expected[x, y], a = actual[x, y];
                    // The turn is calculated in float, anti-aliased edges may differ by a step
                    Assert.True(Math.Abs(e.R - a.R) <= 2 && Math.Abs(e.G - a.G) <= 2 && Math.Abs(e.B - a.B) <= 2 && Math.Abs(e.A - a.A) <= 2,
                        $"Pixel {x},{y}: expected {e}, got {a}");
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Rectangle_TurnedAQuarter_IsTheSwappedRectangle(bool ellipse)
        {
            using var turned = new Image<Rgba32>(60, 60);
            using var swapped = new Image<Rgba32>(60, 60);
            // 40 x 10 around the middle, turned it is 10 x 40
            if (ellipse)
            {
                Shapes.Ellipse(turned, ImageValues.Srgb, new Rectangle(10, 25, 40, 10), 3, Red, Blue, false, QuarterTurn);
                Shapes.Ellipse(swapped, ImageValues.Srgb, new Rectangle(25, 10, 10, 40), 3, Red, Blue, false);
            }
            else
            {
                Shapes.Rectangle(turned, ImageValues.Srgb, new Rectangle(10, 25, 40, 10), 3, Red, Blue, false, QuarterTurn);
                Shapes.Rectangle(swapped, ImageValues.Srgb, new Rectangle(25, 10, 10, 40), 3, Red, Blue, false);
            }

            AssertSame(swapped, turned);
        }

        [Fact]
        public void Line_TurnedAQuarter_IsTheTurnedLine()
        {
            using var turned = new Image<Rgba32>(60, 60);
            using var swapped = new Image<Rgba32>(60, 60);

            Lines.Line(turned, ImageValues.Srgb, new PointF(10, 30), new PointF(50, 30), 3, Red, ArrowHeads.End, false, QuarterTurn);
            Lines.Line(swapped, ImageValues.Srgb, new PointF(30, 10), new PointF(30, 50), 3, Red, ArrowHeads.End, false);

            AssertSame(swapped, turned);
        }

        [Fact]
        public void Shadow_OfATurnedRectangle_StillFallsRightAndDown()
        {
            using var turned = new Image<Rgba32>(60, 60);
            using var swapped = new Image<Rgba32>(60, 60);

            Shapes.Rectangle(turned, ImageValues.Srgb, new Rectangle(10, 25, 40, 10), 2, Red, Blue, true, QuarterTurn);
            Shapes.Rectangle(swapped, ImageValues.Srgb, new Rectangle(25, 10, 10, 40), 2, Red, Blue, true);

            AssertSame(swapped, turned);
            // Right of and below the turned rectangle, not left of it
            Assert.True(turned[37, 30].A > 0);
            Assert.True(turned[30, 52].A > 0);
            Assert.Equal(0, turned[22, 30].A);
        }

        [Fact]
        public void Text_TurnedAQuarter_StaysInItsTurnedBox()
        {
            if (!SystemFonts.Families.GetEnumerator().MoveNext())
            {
                // No font to draw with
                return;
            }

            using var image = new Image<Rgba32>(60, 60);
            Texts.Draw(image, ImageValues.Srgb, new Rectangle(5, 22, 50, 16), "MMMMMMMM", new TextFont("Arial", 12), Red, TextPlacement.Center,
                TextPlacement.Center, turn: QuarterTurn);

            int inside = 0;
            for (int y = 0; y < 60; y++)
            {
                for (int x = 0; x < 60; x++)
                {
                    if (image[x, y].A == 0)
                    {
                        continue;
                    }

                    // The box turned around the middle: x 22 - 38, y 5 - 55
                    Assert.True(x >= 22 && x < 38 && y >= 5 && y < 55, $"Text pixel {x},{y} outside of the turned box");
                    inside++;
                }
            }

            Assert.True(inside > 0);
        }
    }
}
