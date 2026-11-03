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

using System.Drawing.Drawing2D;
using System.Numerics;
using Greenshot.Base.ImageSharpDrawing;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Greenshot.Tests.ImageSharpDrawing
{
    /// <summary>
    /// Drawing with ImageSharp for any pixel type: the coverage of shapes, cutting them out of an image, the torn edge effect
    /// </summary>
    public class CutDrawingTests
    {
        private static IPath Square(float left, float top, float size) =>
            new Polygon(new[] { new PointF(left, top), new PointF(left + size, top), new PointF(left + size, top + size), new PointF(left, top + size) });

        [Fact]
        public void Coverage_IsOneInside_ZeroOutside_PartlyAtAHalfPixel()
        {
            var coverage = Coverage.Of(Square(2, 2, 4.5f), 10, 10);

            Assert.Equal(1f, coverage[3 * 10 + 3], 2);
            Assert.Equal(0f, coverage[8 * 10 + 8], 2);
            Assert.InRange(coverage[3 * 10 + 6], 0.3f, 0.7f);
        }

        [Fact]
        public void Coverage_EvenOdd_AFigureInsideAnotherIsAHole()
        {
            var shape = new ComplexPolygon(Square(0, 0, 10), Square(3, 3, 4));

            var coverage = Coverage.Of(shape, 10, 10);

            Assert.Equal(1f, coverage[1 * 10 + 1], 2);
            Assert.Equal(0f, coverage[5 * 10 + 5], 2);
        }

        [Fact]
        public void GdiPaths_KeepTheFiguresOfAGraphicsPath()
        {
            using var graphicsPath = new GraphicsPath(FillMode.Alternate);
            graphicsPath.AddRectangle(new System.Drawing.Rectangle(0, 0, 10, 10));
            graphicsPath.AddPolygon(new[] { new System.Drawing.PointF(3, 3), new System.Drawing.PointF(7, 3), new System.Drawing.PointF(7, 7), new System.Drawing.PointF(3, 7) });

            var coverage = Coverage.Of(GdiPaths.ToPath(graphicsPath), 10, 10);

            Assert.Equal(1f, coverage[1 * 10 + 1], 2);
            Assert.Equal(0f, coverage[5 * 10 + 5], 2);
        }

        [Fact]
        public void CutAway_WorksForAnyPixelType_TheColorStays()
        {
            using var bytes = new Image<Rgba32>(10, 10, new Rgba32(200, 100, 50, 255));
            using var halves = new Image<HalfVector4>(10, 10, new HalfVector4(5, 5, 5, 1));

            CutDrawing.CutAway(bytes, Square(0, 0, 5));
            CutDrawing.CutAway(halves, Square(0, 0, 5));

            Assert.Equal(0, bytes[2, 2].A);
            Assert.Equal(new Rgba32(200, 100, 50, 255), bytes[7, 7]);
            Assert.Equal(0f, halves[2, 2].ToVector4().W, 3);
            Assert.Equal(new Vector4(5, 5, 5, 1), halves[7, 7].ToVector4());
        }

        [Fact]
        public void TornEdge_CutsTheImage_OverItsShadowAndTheBackground()
        {
            using var source = new Image<HalfVector4>(20, 20, new HalfVector4(5, 5, 5, 1));
            var keep = Square(0, 0, 15);

            using var torn = CutDrawing.TornEdge(source, keep, new Size(30, 30), new Point(2, 2), (0.6f, 4), new Vector4(3, 3, 3, 1));

            Assert.Equal(30, torn.Width);
            // The image where it stays, the background (here white at the SDR white of 3.0) where it is torn off, a shadow between
            Assert.Equal(5f, torn[5, 5].ToVector4().X, 2);
            Assert.Equal(3f, torn[28, 28].ToVector4().X, 2);
            float shadow = torn[18, 10].ToVector4().X;
            Assert.InRange(shadow, 0.1f, 2.9f);
            Assert.Equal(1f, torn[18, 10].ToVector4().W, 2);
        }
    }
}
