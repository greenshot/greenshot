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
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Greenshot.Editor.FileFormatHandlers;
using Xunit;

namespace Greenshot.Tests.Editor
{
    public class WpfFileFormatHandlerTests
    {
        private const int Width = 19, Height = 7;

        public WpfFileFormatHandlerTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        /// <summary>
        /// WIC decodes these with a pixel format ImageHelper.ToBitmap can't map, the handler converts them to Bgra32
        /// </summary>
        [Theory]
        [InlineData("Indexed8", ".png")]
        [InlineData("Indexed8", ".tif")]
        [InlineData("BlackWhite", ".png")]
        [InlineData("BlackWhite", ".tif")]
        [InlineData("Gray8", ".png")]
        public void TryLoadFromStream_ConvertsUnmappedPixelFormats(string pixelFormatName, string extension)
        {
            var (source, expected) = CreateSource(pixelFormatName);
            BitmapEncoder encoder = extension == ".png" ? new PngBitmapEncoder() : new TiffBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));
            using var stream = new MemoryStream();
            encoder.Save(stream);
            stream.Position = 0;

            Assert.True(new WpfFileFormatHandler().TryLoadFromStream(stream, extension, out var loaded));
            using (loaded)
            {
                Assert.Equal(Width, loaded.Width);
                Assert.Equal(Height, loaded.Height);
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        Assert.Equal(expected[x + y * Width], loaded.GetPixel(x, y).ToArgb());
                    }
                }
            }
        }

        /// <summary>
        /// One byte per pixel (BlackWhite: one bit) and the opaque ARGB value each pixel must load as
        /// </summary>
        private static (BitmapSource Source, int[] Expected) CreateSource(string pixelFormatName)
        {
            var expected = new int[Width * Height];
            switch (pixelFormatName)
            {
                case "Indexed8":
                {
                    var colors = new List<Color> { Colors.Black, Colors.White, Colors.Red, Color.FromRgb(10, 200, 30) };
                    var pixels = new byte[Width * Height];
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        pixels[i] = (byte)(i % colors.Count);
                        var color = colors[pixels[i]];
                        expected[i] = System.Drawing.Color.FromArgb(255, color.R, color.G, color.B).ToArgb();
                    }
                    return (BitmapSource.Create(Width, Height, 96, 96, PixelFormats.Indexed8, new BitmapPalette(colors), pixels, Width), expected);
                }
                case "BlackWhite":
                {
                    int stride = (Width + 7) / 8;
                    var pixels = new byte[stride * Height];
                    for (int y = 0; y < Height; y++)
                    {
                        for (int x = 0; x < Width; x++)
                        {
                            bool white = (x + y) % 3 == 0;
                            if (white)
                            {
                                pixels[y * stride + x / 8] |= (byte)(0x80 >> (x % 8));
                            }
                            expected[x + y * Width] = (white ? System.Drawing.Color.White : System.Drawing.Color.Black).ToArgb();
                        }
                    }
                    return (BitmapSource.Create(Width, Height, 96, 96, PixelFormats.BlackWhite, null, pixels, stride), expected);
                }
                default:
                {
                    var pixels = new byte[Width * Height];
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        pixels[i] = (byte)(i * 2 % 256);
                        expected[i] = System.Drawing.Color.FromArgb(255, pixels[i], pixels[i], pixels[i]).ToArgb();
                    }
                    return (BitmapSource.Create(Width, Height, 96, 96, PixelFormats.Gray8, null, pixels, Width), expected);
                }
            }
        }
    }
}
