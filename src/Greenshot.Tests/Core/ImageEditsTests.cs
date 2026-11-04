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
            // The area at its place, around it transparent
            Assert.Equal(5f, placed.Pixels[3, 2].ToVector4().X, 1);
            Assert.Equal(0f, placed.Pixels[0, 0].ToVector4().W, 2);
        }

        [Fact]
        public void Join_PutsTwoAreasTogether()
        {
            using var hdrImage = CreateHdrImage(10, 2, Gray(5));

            using var joined = ImageEdits.Join(new NativeSize(6, 2), new NativeRect(0, 0, 3, 2), new NativePoint(0, 0), new NativeRect(7, 0, 3, 2), new NativePoint(3, 0))
                .Apply(hdrImage);

            Assert.Equal(6, joined.Width);
            Assert.Equal(5f, joined.Pixels[4, 1].ToVector4().X, 1);
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

            Assert.Equal(5f, shadowed.Pixels[6, 6].ToVector4().X, 1);
            Assert.Equal(0f, shadowed.Pixels[5, 5].ToVector4().W, 2);
        }

        [Fact]
        public void TornEdges_WithoutTheImages_LeavesTheBandsOfTheTeethOut()
        {
            var tornEdge = new TornEdgeEffect { ToothHeight = 3, GenerateShadow = false, Edges = new[] { true, false, true, false } };
            using var hdrImage = CreateHdrImage(20, 20, Gray(5));

            using var torn = ImageEdits.ForEffect(tornEdge, new NativeSize(20, 20), new NativeSize(20, 20), new Matrix()).Apply(hdrImage);

            // Top and bottom are torn: their bands are left out (transparent), left and right stay
            Assert.Equal(0f, torn.Pixels[10, 1].ToVector4().W, 2);
            Assert.Equal(5f, torn.Pixels[10, 10].ToVector4().X, 1);
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
        public void Resize_OfTwoHdrDisplaysAndAnSdrDisplayBetweenThem_KeepsTheHighlights()
        {
            using var left = CreateHdrImage(4, 4, Gray(5));
            using var right = CreateHdrImage(4, 4, Gray(5));
            using var sdr = CreateBitmap(12, 4, Color.Black);
            using var both = HdrImage.Combine(sdr, new[] { (left, new NativePoint(0, 0)), (right, new NativePoint(8, 0)) });

            using var half = both.Resize(6, 2);

            // Bicubic mixes across the edge of the displays now (one image, like the 8-bit resize), with its small overshoot
            Assert.InRange(half.Pixels[0, 1].ToVector4().X, 4.8f, 5.3f);
            Assert.InRange(half.Pixels[3, 1].ToVector4().X, -0.3f, 0.3f);
            Assert.True(half.HasHdrContent);
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
        /// An 8-bit image of one color
        /// </summary>
        private static Bitmap CreateBitmap(int width, int height, Color color)
        {
            var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(color);
            return bitmap;
        }

        [Fact]
        public void Combine_PutsTheSdrDisplaysInAtTheSdrWhite_TheHdrDisplaysOverThem()
        {
            using var left = CreateHdrImage(4, 2, Gray(5));
            using var sdr = CreateBitmap(8, 2, Color.White);

            using var complete = HdrImage.Combine(sdr, new[] { (left, new NativePoint(0, 0)) });

            Assert.Equal(5f, complete.Pixels[1, 1].ToVector4().X, 1);
            Assert.Equal(White, complete.Pixels[6, 1].ToVector4().X, 2);
        }

        [Fact]
        public void Room_OfABorder_IsTheBorderOfThe8BitImage()
        {
            using var hdrImage = CreateHdrImage(10, 10, Gray(5));
            using var oldImage = CreateBitmap(10, 10, Color.Gray);
            var border = new BorderEffect { Width = 2, Color = Color.White };
            using var matrix = new Matrix();
            using var newImage = border.Apply(oldImage, matrix);

            using var bordered = ImageEdits.ForEffect(border, oldImage, newImage, matrix).Apply(hdrImage);
            Assert.Equal(14, bordered.Width);
            // The border is white at SDR white, the capture keeps its highlight
            Assert.Equal(White, bordered.Pixels[0, 7].ToVector4().X, 2);
            Assert.Equal(1f, bordered.Pixels[0, 7].ToVector4().W, 2);
            Assert.Equal(5f, bordered.Pixels[7, 7].ToVector4().X, 1);
        }

        [Fact]
        public void Room_OfADropShadow_IsTheShadowOfThe8BitImage()
        {
            using var hdrImage = CreateHdrImage(10, 10, Gray(5));
            using var oldImage = CreateBitmap(10, 10, Color.Gray);
            var dropShadow = new DropShadowEffect();
            using var matrix = new Matrix();
            using var newImage = (Bitmap)dropShadow.Apply(oldImage, matrix);

            using var shadowed = ImageEdits.ForEffect(dropShadow, oldImage, newImage, matrix).Apply(hdrImage);
            Assert.Equal(newImage.Width, shadowed.Width);
            // The room is drawn with ImageSharp: about the alpha of the 8-bit shadow (GDI+ blurs a bit differently), black
            int offset = (int)matrix.OffsetX;
            for (int y = 0; y < newImage.Height; y++)
            {
                for (int x = 0; x < newImage.Width; x++)
                {
                    if (x >= offset && x < offset + 10 && y >= offset && y < offset + 10)
                    {
                        continue;
                    }
                    var pixel = shadowed.Pixels[x, y].ToVector4();
                    Assert.InRange(pixel.W - newImage.GetPixel(x, y).A / 255f, -0.1f, 0.1f);
                    Assert.Equal(0f, pixel.X, 2);
                }
            }
            // The capture keeps its highlight
            Assert.Equal(5f, shadowed.Pixels[offset + 5, offset + 5].ToVector4().X, 1);
        }

        [Fact]
        public void Room_OfABiggerCanvas_IsItsColorAtTheSdrWhite()
        {
            using var hdrImage = CreateHdrImage(10, 10, Gray(5));
            using var oldImage = CreateBitmap(10, 10, Color.Gray);
            var canvas = new ResizeCanvasEffect(2, 3, 4, 5) { BackgroundColor = Color.White };
            using var matrix = new Matrix();
            using var newImage = (Bitmap)canvas.Apply(oldImage, matrix);

            using var bigger = ImageEdits.ForEffect(canvas, oldImage, newImage, matrix).Apply(hdrImage);

            Assert.Equal(newImage.Width, bigger.Width);
            Assert.Equal(newImage.Height, bigger.Height);
            Assert.Equal(White, bigger.Pixels[0, 0].ToVector4().X, 2);
            Assert.Equal(1f, bigger.Pixels[0, 0].ToVector4().W, 2);
            Assert.Equal(5f, bigger.Pixels[2, 4].ToVector4().X, 1);
            Assert.Equal(White, bigger.Pixels[13, 18].ToVector4().X, 2);
        }

        [Fact]
        public void PlaceOnBackground_IsTheBackground_OrTransparentForAColorWhichIsntOpaque()
        {
            using var hdrImage = CreateHdrImage(4, 4, Gray(5));

            using var onWhite = ImageEdits.PlaceOnBackground(new NativeRect(0, 0, 4, 4), new NativeSize(8, 8), new NativePoint(2, 2), Color.White).Apply(hdrImage);
            using var onNothing = ImageEdits.PlaceOnBackground(new NativeRect(0, 0, 4, 4), new NativeSize(8, 8), new NativePoint(2, 2), Color.FromArgb(128, 255, 255, 255)).Apply(hdrImage);

            Assert.Equal(White, onWhite.Pixels[0, 0].ToVector4().X, 2);
            Assert.Equal(5f, onWhite.Pixels[3, 3].ToVector4().X, 1);
            // Like Surface.ExpandImage: a color which isn't opaque leaves the room transparent
            Assert.Equal(0f, onNothing.Pixels[0, 0].ToVector4().W, 2);
        }

        [Fact]
        public void Join_OfTwoPartsWhichCoverAllOfIt_HasThePixelsOfBoth()
        {
            using var hdrImage = CreateHdrImage(10, 4, Gray(5));

            using var joined = ImageEdits.Join(new NativeSize(6, 4), new NativeRect(0, 0, 3, 4), new NativePoint(0, 0), new NativeRect(7, 0, 3, 4), new NativePoint(3, 0)).Apply(hdrImage);
            Assert.Equal(5f, joined.Pixels[4, 2].ToVector4().X, 1);
        }

        [Fact]
        public void Draw_IsWhatComposeDoesWithTheSameSteps()
        {
            using var hdrImage = CreateHdrImage(40, 30, Gray(5));
            // A cut, then something drawn over the cut
            var cut = new SixLabors.ImageSharp.Drawing.EllipsePolygon(20, 15, 12, 8);
            var cutAway = new ImageFilter("Cut", (image, _) => Greenshot.Base.ImageSharpDrawing.CutDrawing.CutAway(image, cut));
            var area = new NativeRect(10, 5, 20, 20);
            Action<SixLabors.ImageSharp.Image, Greenshot.Base.ImageSharpDrawing.ImageValues> draw = (image, values) =>
                SixLabors.ImageSharp.Processing.ProcessingExtensions.Mutate(image, context =>
                    SixLabors.ImageSharp.Drawing.Processing.DrawPathExtensions.Draw(context, values.ColorOf(new Rgba32(255, 0, 0, 255)), 3f,
                        new SixLabors.ImageSharp.Drawing.RectangularPolygon(2, 2, 16, 16)));
            var steps = new[] { HdrOverlayStep.Of(cutAway), HdrOverlayStep.Of(area, draw) };
            using var overlay = new HdrOverlay(steps);
            using var saved = hdrImage.Compose(overlay);

            using var drawn = ImageEdits.Draw("test", steps).Apply(hdrImage);
            for (int y = 0; y < 30; y++)
            {
                for (int x = 0; x < 40; x++)
                {
                    Assert.Equal(saved.Pixels[x, y], drawn.Pixels[x, y]);
                }
            }
            Assert.Equal(0f, drawn.Pixels[20, 15].ToVector4().W, 2);
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
