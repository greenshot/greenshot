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
using System.Drawing.Imaging;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Greenshot.Tests.Core
{
    /// <summary>
    /// BitmapPixels and the code which uses it for pixel access (auto crop, monochrome, color reduction)
    /// </summary>
    public class BitmapPixelsTests
    {
        [Fact]
        public void ProcessPixelRows_Bgra32_ReadsAndWritesTheRows()
        {
            using var bitmap = new Bitmap(5, 3, PixelFormat.Format32bppArgb);
            bitmap.SetPixel(4, 2, Color.FromArgb(10, 20, 30, 40));

            BitmapPixels.ProcessPixelRows<Bgra32>(bitmap, pixels =>
            {
                Assert.Equal(5, pixels.Width);
                Assert.Equal(3, pixels.Height);
                Assert.Equal(5, pixels.GetRowSpan(2).Length);
                Assert.Equal(new Bgra32(20, 30, 40, 10), pixels.GetRowSpan(2)[4]);
                pixels.GetRowSpan(0)[1] = new Bgra32(1, 2, 3, 4);
            });

            Assert.Equal(Color.FromArgb(4, 1, 2, 3).ToArgb(), bitmap.GetPixel(1, 0).ToArgb());
        }

        /// <summary>
        /// The accessor is a ref struct, it can't be used in the lambda of Assert.Throws
        /// </summary>
        [Fact]
        public void ProcessPixelRows_RowOutsideTheBitmap_Throws()
        {
            using var bitmap = new Bitmap(4, 2, PixelFormat.Format32bppArgb);
            bool thrown = false;
            BitmapPixels.ProcessPixelRows<Bgra32>(bitmap, pixels =>
            {
                try
                {
                    pixels.GetRowSpan(2);
                }
                catch (ArgumentOutOfRangeException)
                {
                    thrown = true;
                }
            });
            Assert.True(thrown);
        }

        [Fact]
        public void ProcessPixelRows_Bgra32On24Bpp_IsConvertedWithOpaqueAlpha_AndWrittenBack()
        {
            using var bitmap = new Bitmap(3, 2, PixelFormat.Format24bppRgb);
            bitmap.SetPixel(2, 1, Color.FromArgb(50, 60, 70));

            BitmapPixels.ProcessPixelRows<Bgra32>(bitmap, pixels =>
            {
                Assert.Equal(new Bgra32(50, 60, 70, 255), pixels.GetRowSpan(1)[2]);
                pixels.GetRowSpan(0)[0] = new Bgra32(7, 8, 9, 255);
            });

            Assert.Equal(PixelFormat.Format24bppRgb, bitmap.PixelFormat);
            Assert.Equal(Color.FromArgb(7, 8, 9).ToArgb(), bitmap.GetPixel(0, 0).ToArgb());
        }

        [Fact]
        public void ProcessPixelRows_Area_RowZeroIsTheTopOfTheArea()
        {
            using var bitmap = new Bitmap(6, 6, PixelFormat.Format24bppRgb);
            bitmap.SetPixel(3, 4, Color.FromArgb(1, 2, 3));

            BitmapPixels.ProcessPixelRows<Bgr24>(bitmap, new Rectangle(2, 3, 3, 2), pixels =>
            {
                Assert.Equal(3, pixels.Width);
                Assert.Equal(2, pixels.Height);
                Assert.Equal(new Bgr24(1, 2, 3), pixels.GetRowSpan(1)[1]);
            }, ImageLockMode.ReadOnly);
        }

        [Fact]
        public void ProcessPixelRows_ByteNeedsAnIndexedBitmap()
        {
            using var bitmap = new Bitmap(2, 2, PixelFormat.Format24bppRgb);
            Assert.Throws<NotSupportedException>(() => BitmapPixels.ProcessPixelRows<byte>(bitmap, _ => { }));
        }

        [Fact]
        public void FindAutoCropRectangle_FindsTheContent()
        {
            using var bitmap = new Bitmap(40, 30, PixelFormat.Format24bppRgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.White);
                graphics.FillRectangle(Brushes.Red, 10, 5, 8, 6);
            }

            var cropRectangle = ImageHelper.FindAutoCropRectangle(bitmap, 10);

            Assert.Equal(new NativeRect(10, 5, 8, 6), cropRectangle);
        }

        [Fact]
        public void CreateMonochrome_UsesTheThreshold_AndKeepsAlpha()
        {
            using var bitmap = new Bitmap(2, 1, PixelFormat.Format32bppArgb);
            bitmap.SetPixel(0, 0, Color.FromArgb(100, 200, 200, 200));
            bitmap.SetPixel(1, 0, Color.FromArgb(255, 10, 10, 10));

            using var monochrome = ImageHelper.CreateMonochrome(bitmap, 128);

            Assert.Equal(Color.FromArgb(100, 255, 255, 255).ToArgb(), monochrome.GetPixel(0, 0).ToArgb());
            Assert.Equal(Color.FromArgb(255, 0, 0, 0).ToArgb(), monochrome.GetPixel(1, 0).ToArgb());
        }

        [Fact]
        public void WuQuantizer_FewColors_KeepsThemExactly_AndBlendsAlphaOnWhite()
        {
            using var bitmap = new Bitmap(4, 4, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Blue);
            }
            bitmap.SetPixel(1, 1, Color.Yellow);
            bitmap.SetPixel(2, 3, Color.FromArgb(128, 255, 0, 0));

            using var quantizer = new WuQuantizer(bitmap);
            Assert.Equal(3, quantizer.GetColorCount());
            using var reduced = quantizer.GetQuantizedImage(256);

            Assert.Equal(PixelFormat.Format8bppIndexed, reduced.PixelFormat);
            Assert.Equal(Color.Blue.ToArgb(), reduced.GetPixel(0, 0).ToArgb());
            Assert.Equal(Color.Yellow.ToArgb(), reduced.GetPixel(1, 1).ToArgb());
            // Red at alpha 128 on white: (255 * 128 + 255 * 127) / 255 = 255, (0 * 128 + 255 * 127) / 255 = 127
            Assert.Equal(Color.FromArgb(255, 127, 127).ToArgb(), reduced.GetPixel(2, 3).ToArgb());
        }

        [Fact]
        public void WuQuantizer_ManyColors_ReducesToTheAllowedCount()
        {
            using var bitmap = new Bitmap(64, 64, PixelFormat.Format24bppRgb);
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    bitmap.SetPixel(x, y, Color.FromArgb(x * 4, y * 4, (x + y) * 2));
                }
            }

            using var quantizer = new WuQuantizer(bitmap);
            using var reduced = quantizer.GetQuantizedImage(16);

            Assert.Equal(PixelFormat.Format8bppIndexed, reduced.PixelFormat);
            for (int y = 0; y < 64; y += 7)
            {
                for (int x = 0; x < 64; x += 7)
                {
                    Color original = bitmap.GetPixel(x, y);
                    Color result = reduced.GetPixel(x, y);
                    Assert.True(Math.Abs(original.R - result.R) < 64 && Math.Abs(original.G - result.G) < 64 && Math.Abs(original.B - result.B) < 64,
                        $"Pixel {x},{y}: {original} became {result}");
                }
            }
        }
    }
}
