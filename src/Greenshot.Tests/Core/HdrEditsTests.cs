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
using System.Drawing.Drawing2D;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Effects;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// The edits of the HDR image for the changes of the editor: geometry like the 8-bit image, colors in linear light which look like the
    /// 8-bit effect in the SDR range and keep the highlights where the effect allows it
    /// </summary>
    public class HdrEditsTests
    {
        // SDR white of 240 nits in scRGB
        private const float White = 3f;

        private static readonly NativeSize Size = new NativeSize(2, 2);

        public HdrEditsTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static HdrImage CreateHdrImage(int width, int height, HalfVector4 pixel)
        {
            var hdrImage = new HdrImage(width, height, 240, 400);
            hdrImage.Pixels.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < height; y++)
                {
                    accessor.GetRowSpan(y).Fill(pixel);
                }
            });
            return hdrImage;
        }

        /// <summary>
        /// The pixel after the effect, of a 2x2 HDR image of it
        /// </summary>
        private static System.Numerics.Vector4 Apply(IEffect effect, HalfVector4 pixel)
        {
            using var hdrImage = CreateHdrImage(2, 2, pixel);
            using var result = HdrEdits.ForEffect(effect, Size, Size, new Matrix()).Apply(hdrImage);
            return result.Pixels[1, 1].ToVector4();
        }

        /// <summary>
        /// The 8-bit level of a linear value at the SDR white
        /// </summary>
        private static int Level(float linear)
        {
            float value = Math.Max(linear / White, 0);
            float encoded = value <= 0.0031308f ? value * 12.92f : 1.055f * (float)Math.Pow(value, 1 / 2.4) - 0.055f;
            return (int)Math.Round(encoded * 255);
        }

        /// <summary>
        /// The linear value of an 8-bit level at the SDR white
        /// </summary>
        private static float Linear(int level)
        {
            float encoded = level / 255f;
            return (encoded <= 0.04045f ? encoded / 12.92f : (float)Math.Pow((encoded + 0.055f) / 1.055f, 2.4)) * White;
        }

        private static HalfVector4 Gray(float linear) => new HalfVector4(linear, linear, linear, 1);

        [Fact]
        public void Place_PutsTheAreaIntoABiggerImage()
        {
            using var hdrImage = CreateHdrImage(4, 2, Gray(5));

            using var placed = HdrEdits.Place(new NativeRect(1, 0, 2, 2), new NativeSize(6, 6), new NativePoint(3, 2)).Apply(hdrImage);

            Assert.Equal(6, placed.Width);
            Assert.Equal(new NativeRect(3, 2, 2, 2), Assert.Single(placed.TileAreas));
        }

        [Fact]
        public void Join_PutsTwoAreasTogether()
        {
            using var hdrImage = CreateHdrImage(10, 2, Gray(5));

            using var joined = HdrEdits.Join(new NativeSize(6, 2), new NativeRect(0, 0, 3, 2), new NativePoint(0, 0), new NativeRect(7, 0, 3, 2), new NativePoint(3, 0))
                .Apply(hdrImage);

            Assert.Equal(6, joined.Width);
            Assert.Equal(new[] { new NativeRect(0, 0, 3, 2), new NativeRect(3, 0, 3, 2) }, joined.TileAreas);
        }

        [Fact]
        public void Effects_OfTheEditor_MapToEdits_MonochromeAndReduceColorsDont()
        {
            var size = new NativeSize(10, 10);
            Assert.NotNull(HdrEdits.ForEffect(new GrayscaleEffect(), size, size, new Matrix()));
            Assert.NotNull(HdrEdits.ForEffect(new InvertEffect(), size, size, new Matrix()));
            Assert.NotNull(HdrEdits.ForEffect(new AdjustEffect(), size, size, new Matrix()));
            Assert.NotNull(HdrEdits.ForEffect(new RemoveTransparencyEffect(), size, size, new Matrix()));
            Assert.NotNull(HdrEdits.ForEffect(new RotateEffect(90), size, size, new Matrix()));
            Assert.NotNull(HdrEdits.ForEffect(new ResizeEffect(5, 5, true), size, new NativeSize(5, 5), new Matrix()));
            Assert.NotNull(HdrEdits.ForEffect(new BorderEffect(), size, new NativeSize(14, 14), new Matrix()));
            Assert.NotNull(HdrEdits.ForEffect(new ResizeCanvasEffect(1, 1, 1, 1), size, new NativeSize(12, 12), new Matrix()));
            Assert.Null(HdrEdits.ForEffect(new MonochromeEffect(127), size, size, new Matrix()));
            Assert.Null(HdrEdits.ForEffect(new ReduceColorsEffect(), size, size, new Matrix()));
        }

        [Fact]
        public void Shadow_MovesTheImageByTheOffsetOfTheEffect()
        {
            var matrix = new Matrix();
            matrix.Translate(6, 6);
            using var hdrImage = CreateHdrImage(10, 10, Gray(5));

            using var shadowed = HdrEdits.ForEffect(new DropShadowEffect(), new NativeSize(10, 10), new NativeSize(24, 24), matrix).Apply(hdrImage);

            Assert.Equal(new NativeRect(6, 6, 10, 10), Assert.Single(shadowed.TileAreas));
        }

        [Fact]
        public void TornEdges_TheBandsOfTheTeethComeFromThe8BitImage()
        {
            var tornEdge = new TornEdgeEffect { ToothHeight = 3, GenerateShadow = false, Edges = new[] { true, false, true, false } };
            using var hdrImage = CreateHdrImage(20, 20, Gray(5));

            using var torn = HdrEdits.ForEffect(tornEdge, new NativeSize(20, 20), new NativeSize(20, 20), new Matrix()).Apply(hdrImage);

            // Top and bottom are torn: their bands are left out, left and right stay
            Assert.Equal(new NativeRect(0, 3, 20, 14), Assert.Single(torn.TileAreas));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(64)]
        [InlineData(128)]
        [InlineData(255)]
        public void Invert_InTheSdrRange_IsThe8BitNegative(int level)
        {
            var inverted = Apply(new InvertEffect(), Gray(Linear(level)));

            Assert.InRange(Level(inverted.X), 254 - level, 256 - level);
        }

        [Fact]
        public void Invert_OfAHighlight_IsBlack()
        {
            var inverted = Apply(new InvertEffect(), Gray(10));

            Assert.Equal(0f, inverted.X, 3);
        }

        [Fact]
        public void Adjust_WithoutChange_KeepsThePixels_AlsoTheHighlights()
        {
            var adjusted = Apply(new AdjustEffect(), new HalfVector4(1.5f, 10, 0.2f, 1));

            Assert.Equal(1.5f, adjusted.X, 1);
            Assert.Equal(10f, adjusted.Y, 0);
            Assert.Equal(0.2f, adjusted.Z, 1);
        }

        [Theory]
        [InlineData(1.2f, 1f, 1f)]
        [InlineData(1f, 1.5f, 1f)]
        [InlineData(1f, 1f, 2f)]
        public void Adjust_InTheSdrRange_IsWhatTheEffectDoesToThe8BitImage(float brightness, float contrast, float gamma)
        {
            using var gray = new Bitmap(1, 1, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            gray.SetPixel(0, 0, Color.FromArgb(100, 100, 100));
            using var adjusted8Bit = (Bitmap)ImageHelper.Adjust(gray, brightness, contrast, gamma);
            int expected = adjusted8Bit.GetPixel(0, 0).G;

            var adjusted = Apply(new AdjustEffect { Brightness = brightness, Contrast = contrast, Gamma = gamma }, Gray(Linear(100)));

            Assert.InRange(Level(adjusted.X), expected - 1, expected + 1);
        }

        [Fact]
        public void RemoveTransparency_PutsThePixelsOnTheColor_AtTheSdrWhite()
        {
            var transparent = Apply(new RemoveTransparencyEffect { Color = Color.White }, new HalfVector4(5, 5, 5, 0));
            var opaque = Apply(new RemoveTransparencyEffect { Color = Color.White }, Gray(5));

            Assert.Equal(White, transparent.X, 2);
            Assert.Equal(1f, transparent.W, 2);
            Assert.Equal(5f, opaque.X, 2);
        }

        [Fact]
        public void Grayscale_KeepsTheHighlights()
        {
            var gray = Apply(new GrayscaleEffect(), Gray(10));

            Assert.Equal(10f, gray.X, 1);
        }

        [Fact]
        public void Resize_OfTwoDisplays_ScalesTheirPlaces()
        {
            using var left = CreateHdrImage(4, 4, Gray(5));
            using var right = CreateHdrImage(4, 4, Gray(5));
            using var both = HdrImage.Combine(12, 4, new[] { (left, new NativePoint(0, 0)), (right, new NativePoint(8, 0)) });

            using var half = both.Resize(6, 2);

            Assert.Equal(new[] { new NativeRect(0, 0, 2, 2), new NativeRect(4, 0, 2, 2) }, half.TileAreas);
            Assert.True(half.HasHdrContent);
        }

        [Fact]
        public void Exclude_AHoleInTheMiddle_LeavesTheRectanglesAroundIt()
        {
            using var hdrImage = CreateHdrImage(10, 10, Gray(5));

            using var withHole = HdrEdits.Exclude(new[] { new NativeRect(3, 4, 2, 2) }).Apply(hdrImage);

            Assert.Equal(new[] { new NativeRect(0, 0, 10, 4), new NativeRect(0, 6, 10, 4), new NativeRect(0, 4, 3, 2), new NativeRect(5, 4, 5, 2) }, withHole.TileAreas);
            Assert.Null(HdrEdits.Exclude(new[] { new NativeRect(0, 0, 10, 10) }).Apply(hdrImage));
        }
    }
}
