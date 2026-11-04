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
using System.Linq;
using Greenshot.Base.Core;
using Greenshot.Base.Hdr;
using Greenshot.Base.ImageSharpDrawing;
using Greenshot.Base.Threading;
using Greenshot.Editor.Drawing;
using Greenshot.Plugin.Zxing.Drawing;
using Greenshot.Plugin.Zxing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;
using ZXing;

namespace Greenshot.Tests.Editor
{
    /// <summary>
    /// The barcode of the Zxing plugin is drawn with ImageSharp from the same layout as its GDI+ bitmap in the editor, e.g. in an HDR file
    /// </summary>
    public class BarcodeHdrTests
    {
        public BarcodeHdrTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        /// <summary>
        /// How many pixels of the ImageSharp drawing differ from the GDI+ bitmap of the same layout by more than the tolerance
        /// </summary>
        private static int CountDifferences(BarcodeLayout layout, int tolerance) => CountDifferences(layout, tolerance, out _);

        private static int CountDifferences(BarcodeLayout layout, int tolerance, out string first)
        {
            first = null;
            using var gdi = ZxingBarcodeGenerator.Render(layout);
            using var drawn = new SixLabors.ImageSharp.Image<Rgba32>(layout.Width, layout.Height);
            ZxingBarcodeGenerator.Draw(drawn, ImageValues.Srgb, layout, new SixLabors.ImageSharp.Rectangle(0, 0, layout.Width, layout.Height));
            int differences = 0;
            for (int y = 0; y < layout.Height; y++)
            {
                for (int x = 0; x < layout.Width; x++)
                {
                    var expected = gdi.GetPixel(x, y);
                    var actual = drawn[x, y];
                    if (Math.Abs(expected.R - actual.R) > tolerance || Math.Abs(expected.G - actual.G) > tolerance || Math.Abs(expected.B - actual.B) > tolerance)
                    {
                        first ??= $"{x},{y}: GDI+ {expected.R}/{expected.A}, ImageSharp {actual.R}/{actual.A}; row GDI+ {string.Join(" ", System.Linq.Enumerable.Range(0, 12).Select(i => gdi.GetPixel(i, y).R))}, ImageSharp {string.Join(" ", System.Linq.Enumerable.Range(0, 12).Select(i => drawn[i, y].R))}";
                        differences++;
                    }
                }
            }
            return differences;
        }

        [Fact]
        public void SquareModules_AreLikeTheGdiBitmap()
        {
            // 4 pixels per module: every edge on a whole pixel (GDI+ anti-aliases them, its pixels have their middle there); where two modules
            // meet, the two blend their edges a bit differently
            var matrix = new MultiFormatWriter().encode("https://getgreenshot.org", BarcodeFormat.QR_CODE, 0, 0,
                new System.Collections.Generic.Dictionary<EncodeHintType, object> { { EncodeHintType.MARGIN, 1 } });
            var layout = ZxingBarcodeGenerator.Layout(matrix, Color.Black, Color.White, false, BarcodeFormat.QR_CODE, matrix.Width * 4, matrix.Height * 4, 1);

            Assert.NotEmpty(layout.Modules);
            int differences = CountDifferences(layout, 16, out string first);
            Assert.True(differences == 0, $"{differences} pixels differ, first {first}");
        }

        [Fact]
        public void RoundedDotsAndUnevenModules_AreLikeTheGdiBitmap_UpToTheAntiAliasing()
        {
            var layout = ZxingBarcodeGenerator.CreateLayout("https://getgreenshot.org", BarcodeFormat.QR_CODE, Color.Black, Color.White, true, 157, 157, 1);

            // Only edge pixels of the dots may differ a bit
            int differences = CountDifferences(layout, 64, out string first);
            Assert.True(differences < layout.Width * layout.Height / 50, $"{differences} pixels differ, first {first}");
        }

        [Fact]
        public void Barcode_IsDrawnOnTheHdrPixels_FromItsLayout()
        {
            var capture = new Capture(new Bitmap(200, 200, System.Drawing.Imaging.PixelFormat.Format32bppArgb));
            var hdrImage = new HdrImage(200, 200, 240, 400);
            hdrImage.Pixels.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < 200; y++)
                {
                    accessor.GetRowSpan(y).Fill(new HalfVector4(5, 5, 5, 1));
                }
            });
            hdrImage.BelongTo(capture.Image);
            capture.HdrImage = hdrImage;
            using var surface = WinFormsContextGuard.CreateWithoutContext(() => new Surface(capture));
            var model = new ZxingModel { FormatIndex = 0, RawText = "https://getgreenshot.org" };
            var barcode = new BarcodeContainer(surface, model, 0) { Left = 50, Top = 50, Width = 100, Height = 100 };
            barcode.RegenerateBarcode();
            surface.AddElement(barcode, false, false);

            Assert.Equal(HdrState.Available, surface.HdrState);
            var steps = surface.CreateImageSharpSteps();
            using var composed = surface.HdrImage.Compose(steps);

            // The barcode is black and white at the white of the elements, the highlights around it stay
            int black = 0, white = 0;
            for (int y = 50; y < 150; y++)
            {
                for (int x = 50; x < 150; x++)
                {
                    float value = composed.Pixels[x, y].ToVector4().X;
                    if (value < 0.05f)
                    {
                        black++;
                    }
                    else if (Math.Abs(value - (float)Math.Sqrt(3f * 5f)) < 0.05f)
                    {
                        white++;
                    }
                }
            }
            Assert.True(black > 1000, $"black modules: {black}");
            Assert.True(white > 1000, $"white: {white}");
            Assert.Equal(5f, composed.Pixels[20, 20].ToVector4().X, 1);
        }
    }
}
