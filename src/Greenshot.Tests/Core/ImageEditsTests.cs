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
using System.Runtime.InteropServices;
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
    public class ImageEditsTests
    {
        // SDR white of 240 nits in scRGB
        private const float White = 3f;

        private static readonly NativeSize Size = new NativeSize(2, 2);

        public ImageEditsTests()
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
            using var result = ImageEdits.ForEffect(effect, Size, Size, new Matrix()).Apply(hdrImage);
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

            using var placed = ImageEdits.Place(new NativeRect(1, 0, 2, 2), new NativeSize(6, 6), new NativePoint(3, 2)).Apply(hdrImage);

            Assert.Equal(6, placed.Width);
            Assert.Equal(new NativeRect(3, 2, 2, 2), Assert.Single(placed.TileAreas));
        }

        [Fact]
        public void Join_PutsTwoAreasTogether()
        {
            using var hdrImage = CreateHdrImage(10, 2, Gray(5));

            using var joined = ImageEdits.Join(new NativeSize(6, 2), new NativeRect(0, 0, 3, 2), new NativePoint(0, 0), new NativeRect(7, 0, 3, 2), new NativePoint(3, 0))
                .Apply(hdrImage);

            Assert.Equal(6, joined.Width);
            Assert.Equal(new[] { new NativeRect(0, 0, 3, 2), new NativeRect(3, 0, 3, 2) }, joined.TileAreas);
        }

        [Fact]
        public void Effects_OfTheEditor_MapToEdits_MonochromeAndReduceColorsDont()
        {
            var size = new NativeSize(10, 10);
            Assert.NotNull(ImageEdits.ForEffect(new GrayscaleEffect(), size, size, new Matrix()));
            Assert.NotNull(ImageEdits.ForEffect(new InvertEffect(), size, size, new Matrix()));
            Assert.NotNull(ImageEdits.ForEffect(new AdjustEffect(), size, size, new Matrix()));
            Assert.NotNull(ImageEdits.ForEffect(new RemoveTransparencyEffect(), size, size, new Matrix()));
            Assert.NotNull(ImageEdits.ForEffect(new RotateEffect(90), size, size, new Matrix()));
            Assert.NotNull(ImageEdits.ForEffect(new ResizeEffect(5, 5, true), size, new NativeSize(5, 5), new Matrix()));
            Assert.NotNull(ImageEdits.ForEffect(new BorderEffect(), size, new NativeSize(14, 14), new Matrix()));
            Assert.NotNull(ImageEdits.ForEffect(new ResizeCanvasEffect(1, 1, 1, 1), size, new NativeSize(12, 12), new Matrix()));
            Assert.Null(ImageEdits.ForEffect(new MonochromeEffect(127), size, size, new Matrix()));
            Assert.Null(ImageEdits.ForEffect(new ReduceColorsEffect(), size, size, new Matrix()));
        }

        [Fact]
        public void Shadow_MovesTheImageByTheOffsetOfTheEffect()
        {
            var matrix = new Matrix();
            matrix.Translate(6, 6);
            using var hdrImage = CreateHdrImage(10, 10, Gray(5));

            using var shadowed = ImageEdits.ForEffect(new DropShadowEffect(), new NativeSize(10, 10), new NativeSize(24, 24), matrix).Apply(hdrImage);

            Assert.Equal(new NativeRect(6, 6, 10, 10), Assert.Single(shadowed.TileAreas));
        }

        [Fact]
        public void TornEdges_TheBandsOfTheTeethComeFromThe8BitImage()
        {
            var tornEdge = new TornEdgeEffect { ToothHeight = 3, GenerateShadow = false, Edges = new[] { true, false, true, false } };
            using var hdrImage = CreateHdrImage(20, 20, Gray(5));

            using var torn = ImageEdits.ForEffect(tornEdge, new NativeSize(20, 20), new NativeSize(20, 20), new Matrix()).Apply(hdrImage);

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

            using var withHole = ImageEdits.Exclude(new[] { new NativeRect(3, 4, 2, 2) }).Apply(hdrImage);

            Assert.Equal(new[] { new NativeRect(0, 0, 10, 4), new NativeRect(0, 6, 10, 4), new NativeRect(0, 4, 3, 2), new NativeRect(5, 4, 5, 2) }, withHole.TileAreas);
            Assert.Null(ImageEdits.Exclude(new[] { new NativeRect(0, 0, 10, 10) }).Apply(hdrImage));
        }

        [Fact]
        public void ImageFilters_WorkForAnyPixelType()
        {
            // The same filter on an 8-bit and on an HDR image: copy an area from a source and limit it to a marker color (white is 1 here)
            using var bytes = new SixLabors.ImageSharp.Image<Rgba32>(4, 2, new Rgba32(255, 255, 255, 255));
            using var halves = new SixLabors.ImageSharp.Image<HalfVector4>(4, 2, new HalfVector4(5, 5, 5, 1));
            using var black = new SixLabors.ImageSharp.Image<Rgba32>(2, 2, new Rgba32(0, 0, 0, 255));
            var highlighter = new ImageFilter("Highlight", (image, white) =>
                SixLabors.ImageSharp.Processing.ProcessingExtensions.Mutate(image, context => context.LinearHighlight(white, new System.Numerics.Vector3(1, 1, 0),
                    new SixLabors.ImageSharp.Rectangle(2, 0, 2, 2))));

            foreach (SixLabors.ImageSharp.Image image in new SixLabors.ImageSharp.Image[] { bytes, halves })
            {
                ImageFilter.CopyArea(image, black, new SixLabors.ImageSharp.Point(0, 0), new SixLabors.ImageSharp.Rectangle(0, 0, 2, 2));
                highlighter.Apply(image, 1f);
            }

            Assert.Equal(new Rgba32(0, 0, 0, 255), bytes[1, 1]);
            Assert.Equal(new Rgba32(255, 255, 0, 255), bytes[3, 1]);
            Assert.Equal(0f, halves[1, 1].ToVector4().X, 3);
            // The highlight is limited to the marker color: yellow at white
            Assert.Equal(new System.Numerics.Vector4(1, 1, 0, 1), halves[3, 1].ToVector4());
        }

        [Theory]
        [InlineData(255, 0, 0)]
        [InlineData(0, 255, 0)]
        [InlineData(0, 0, 255)]
        public void Grayscale_OfASaturatedColor_IsTheSameGrayInThe8BitAndTheHdrImage(int red, int green, int blue)
        {
            using var bitmap = new Bitmap(2, 2, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.FromArgb(red, green, blue));
            }
            using var gray8Bit = (Bitmap)ImageHelper.CreateGrayscale(bitmap);
            int expected = gray8Bit.GetPixel(1, 1).R;

            var grayHdr = Apply(new GrayscaleEffect(), new HalfVector4(Linear(red), Linear(green), Linear(blue), 1));

            Assert.Equal(expected, gray8Bit.GetPixel(1, 1).G);
            Assert.InRange(Level(grayHdr.X), expected - 1, expected + 1);
        }

        [Fact]
        public void Resize_OfTransparentPixels_KeepsThemTransparent_TheHighlightsStay()
        {
            // The left half a highlight of 5, the right half transparent (e.g. torn off pixels)
            using var hdrImage = new HdrImage(16, 4, 240, 400);
            hdrImage.Pixels.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < 4; y++)
                {
                    accessor.GetRowSpan(y).Slice(0, 8).Fill(Gray(5));
                }
            });

            using var half = hdrImage.Resize(8, 2);

            for (int x = 0; x < 8; x++)
            {
                var pixel = half.Pixels[x, 1].ToVector4();
                Assert.False(float.IsNaN(pixel.X) || float.IsInfinity(pixel.X), $"pixel {x}");
                Assert.InRange(pixel.W, 0f, 1f);
                Assert.InRange(pixel.X, -0.5f, 5.5f);
            }
            Assert.Equal(5f, half.Pixels[1, 1].ToVector4().X, 1);
            Assert.Equal(0f, half.Pixels[7, 1].ToVector4().W, 2);
        }

        [Fact]
        public void Adjust_OfABrokenPixel_DoesNotFail()
        {
            var adjusted = Apply(new AdjustEffect { Brightness = 1.2f }, new HalfVector4(float.NaN, 1, 1, 1));

            Assert.True(adjusted.Y > 1f);
        }

        /// <summary>
        /// The same HDR image as one display and as two displays side by side (left and right half), with a highlight which changes over it
        /// </summary>
        private static (HdrImage Whole, HdrImage Halves) CreateWholeAndHalves(int width, int height)
        {
            static HalfVector4 PixelAt(int x, int y) => new HalfVector4(2 + x * 0.1f, 3 + y * 0.1f, 5, 1);
            var whole = new HdrImage(width, height, 240, 400);
            using var left = new HdrImage(width / 2, height, 240, 400);
            using var right = new HdrImage(width - width / 2, height, 240, 400);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    whole.Pixels[x, y] = PixelAt(x, y);
                    if (x < width / 2)
                    {
                        left.Pixels[x, y] = PixelAt(x, y);
                    }
                    else
                    {
                        right.Pixels[x - width / 2, y] = PixelAt(x, y);
                    }
                }
            }
            var halves = HdrImage.Combine(width, height, new[] { (left, new NativePoint(0, 0)), (right, new NativePoint(width / 2, 0)) });
            return (whole, halves);
        }

        /// <summary>
        /// The pixels of the tiles of the HDR image are the ones of the complete HDR image at the same place
        /// </summary>
        private static void AssertTilesAreLike(HdrImage complete, HdrImage tiled)
        {
            Assert.True(complete.IsComplete);
            using var sdr = new Bitmap(tiled.Width, tiled.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using var composed = tiled.Compose(sdr);
            foreach (var area in tiled.TileAreas)
            {
                for (int y = area.Top; y < area.Bottom; y++)
                {
                    for (int x = area.Left; x < area.Right; x++)
                    {
                        var expected = complete.Pixels[x, y].ToVector4();
                        var actual = composed.Pixels[x, y].ToVector4();
                        Assert.True(Math.Abs(expected.W - actual.W) < 0.01f, $"alpha at {x},{y}: {actual.W} instead of {expected.W}");
                        if (expected.W > 0.01f)
                        {
                            Assert.True(System.Numerics.Vector3.Distance(new System.Numerics.Vector3(expected.X, expected.Y, expected.Z),
                                new System.Numerics.Vector3(actual.X, actual.Y, actual.Z)) < 0.02f, $"color at {x},{y}: {actual} instead of {expected}");
                        }
                    }
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void TornEdgeEffect_OverTwoDisplays_IsDrawnOnEachOfThem_LikeOverOne(bool withShadow)
        {
            var tornEdge = new TornEdgeEffect { ToothHeight = 6, HorizontalToothRange = 8, VerticalToothRange = 8, Seed = 3, GenerateShadow = withShadow, ShadowSize = 5 };
            var (whole, halves) = CreateWholeAndHalves(40, 30);
            using (whole)
            using (halves)
            {
                int room = withShadow ? 12 : 0;
                using var oldImage = new Bitmap(40, 30);
                using var newImage = new Bitmap(40 + room, 30 + room);
                using var matrix = new Matrix();
                matrix.Translate(room / 2, room / 2);
                var edit = ImageEdits.ForEffect(tornEdge, oldImage, newImage, matrix);

                using var tornWhole = edit.Apply(whole);
                using var tornHalves = edit.Apply(halves);

                // Each display's part where it is moved to, nothing of it is left out or cleared
                var offset = withShadow ? room / 2 : 0;
                Assert.Equal(new[] { new NativeRect(offset, offset, 20, 30), new NativeRect(20 + offset, offset, 20, 30) }, tornHalves.TileAreas);
                AssertTilesAreLike(tornWhole, tornHalves);
                // The teeth are drawn: some pixels of the band are torn off
                int tornOff = 0;
                for (int x = offset; x < 40 + offset; x++)
                {
                    if (tornWhole.Pixels[x, offset + 1].ToVector4().X < 1.9f || tornWhole.Pixels[x, offset + 1].ToVector4().W < 0.99f)
                    {
                        tornOff++;
                    }
                }
                Assert.True(tornOff > 0, "torn off teeth");
            }
        }

        [Fact]
        public void Draw_OverTwoDisplays_IsLikeTheElementOverOne_NothingComesFromThe8BitImage()
        {
            var (whole, halves) = CreateWholeAndHalves(40, 30);
            using (whole)
            using (halves)
            {
                // A cut across both displays, then something drawn over the cut and the two of them
                var cut = new SixLabors.ImageSharp.Drawing.EllipsePolygon(20, 15, 12, 8);
                var cutAway = new ImageFilter("Cut", (image, _) => Greenshot.Base.ImageSharpDrawing.CutDrawing.CutAway(image, cut),
                    (part, origin, _) => Greenshot.Base.ImageSharpDrawing.CutDrawing.CutAway(part, SixLabors.ImageSharp.Drawing.PathExtensions.Translate(cut, -origin.X, -origin.Y)));
                var area = new NativeRect(10, 5, 20, 20);
                Action<SixLabors.ImageSharp.Image, Greenshot.Base.ImageSharpDrawing.ImageValues> draw = (image, values) =>
                    SixLabors.ImageSharp.Processing.ProcessingExtensions.Mutate(image, context =>
                        SixLabors.ImageSharp.Drawing.Processing.DrawPathExtensions.Draw(context, values.ColorOf(new Rgba32(255, 0, 0, 255)), 3f,
                            new SixLabors.ImageSharp.Drawing.RectangularPolygon(2, 2, 16, 16)));
                var steps = new[] { HdrOverlayStep.Of(cutAway), HdrOverlayStep.Of(area, draw) };
                using var sdr = new Bitmap(40, 30, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using var overlay = new HdrOverlay(steps, null, null);
                // As it is saved while the element is on the image
                using var saved = whole.Compose(sdr, overlay);

                using var drawn = ImageEdits.Draw("test", steps).Apply(halves);

                Assert.Equal(halves.TileAreas, drawn.TileAreas);
                AssertTilesAreLike(saved, drawn);
                // Cut away in the middle
                using var composed = drawn.Compose(sdr);
                Assert.Equal(0f, composed.Pixels[20, 15].ToVector4().W, 2);
            }
        }

        [Fact]
        public void Draw_OnlyWhatTouchesADisplay_ChangesIt()
        {
            var (whole, halves) = CreateWholeAndHalves(40, 30);
            using (whole)
            using (halves)
            {
                var area = new NativeRect(2, 2, 10, 10);
                Action<SixLabors.ImageSharp.Image, Greenshot.Base.ImageSharpDrawing.ImageValues> draw = (image, values) =>
                    SixLabors.ImageSharp.Processing.ProcessingExtensions.Mutate(image, context =>
                        SixLabors.ImageSharp.Drawing.Processing.FillRectangleExtensions.Fill(context, values.ColorOf(new Rgba32(0, 0, 255, 255)),
                            new SixLabors.ImageSharp.RectangleF(0, 0, 10, 10)));

                using var drawn = ImageEdits.Draw("test", new[] { HdrOverlayStep.Of(area, draw) }).Apply(halves);

                using var sdr = new Bitmap(40, 30, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using var composed = drawn.Compose(sdr);
                Assert.True(composed.Pixels[5, 5].ToVector4().X < 0.01f, "drawn on the left display");
                // The right display is as it was
                using var before = halves.Compose(sdr);
                Assert.Equal(before.Pixels[30, 5].ToVector4(), composed.Pixels[30, 5].ToVector4());
            }
        }

        [Fact]
        public void Draw_WithAFilterWhichMixesPixels_IsRefused()
        {
            using var hdrImage = CreateHdrImage(4, 4, Gray(5));
            var blur = new ImageFilter("Blur", (_, _) => { });

            Assert.Throws<ArgumentException>(() => ImageEdits.Draw("blur", new[] { HdrOverlayStep.Of(blur) }).Apply(hdrImage));
        }

        [Fact]
        public void Grayscale_OfABitmapWithGapsBetweenItsRows_IsTheSame()
        {
            using var packed = new Bitmap(3, 2, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var memory = Marshal.AllocHGlobal(2 * 16);
            try
            {
                using (var padded = new Bitmap(3, 2, 16, System.Drawing.Imaging.PixelFormat.Format32bppArgb, memory))
                {
                    foreach (var bitmap in new[] { packed, padded })
                    {
                        bitmap.SetPixel(0, 0, Color.Red);
                        bitmap.SetPixel(2, 1, Color.Blue);
                        ImageHelper.ApplyGrayscale(bitmap);
                    }
                    Assert.Equal(packed.GetPixel(0, 0), padded.GetPixel(0, 0));
                    Assert.Equal(packed.GetPixel(2, 1), padded.GetPixel(2, 1));
                    Assert.Equal(packed.GetPixel(0, 0).R, packed.GetPixel(0, 0).G);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }
        }
    }
}
