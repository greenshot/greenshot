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
using System.IO;
using System.Runtime.InteropServices;
using Dapplo.Windows.Imaging;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Editor.FileFormatHandlers;
using Xunit;

namespace Greenshot.Tests.Editor
{
    public class WicFileFormatHandlerTests
    {
        private const int Width = 19, Height = 7;

        // GUID_WICPixelFormat8bppGray
        private static readonly Guid Gray8 = new Guid("6fddc324-4e03-4bfe-b185-3d77768dc908");

        public WicFileFormatHandlerTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        /// <summary>
        /// WIC decodes these in a pixel format which isn't 32 bit, the handler lets WIC convert them to BGRA
        /// </summary>
        [Theory]
        [InlineData(PixelFormat.Format8bppIndexed, ".png")]
        [InlineData(PixelFormat.Format8bppIndexed, ".tif")]
        [InlineData(PixelFormat.Format1bppIndexed, ".png")]
        [InlineData(PixelFormat.Format1bppIndexed, ".tif")]
        public void TryLoadFromStream_ConvertsIndexedImages(PixelFormat pixelFormat, string extension)
        {
            // The WIC encoding of Dapplo.Windows.Imaging has no palette, GDI+ writes the indexed samples
            using var source = CreateIndexed(pixelFormat, out var expected);
            using var stream = new MemoryStream();
            source.Save(stream, extension == ".png" ? ImageFormat.Png : ImageFormat.Tiff);
            stream.Position = 0;

            AssertLoads(stream, extension, Width, Height, expected);
        }

        [Fact]
        public void TryLoadFromStream_ConvertsGray()
        {
            var pixels = new byte[Width * Height];
            var expected = new int[Width * Height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = (byte)(i * 2 % 256);
                expected[i] = Color.FromArgb(255, pixels[i], pixels[i], pixels[i]).ToArgb();
            }

            using var stream = new MemoryStream();
            var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
            try
            {
                WicImage.Encode(stream, new WicEncodeOptions { ContainerFormat = WicContainerFormats.Png }, handle.AddrOfPinnedObject(), Width, Height, Width, Gray8);
            }
            finally
            {
                handle.Free();
            }
            stream.Position = 0;

            AssertLoads(stream, ".png", Width, Height, expected);
        }

        /// <summary>
        /// A photo with the EXIF orientation 6 (rotate 90° clockwise) is loaded upright, ProcessEXIFOrientation is on by default
        /// </summary>
        [Fact]
        public void TryLoadFromStream_AppliesTheExifOrientation()
        {
            const int width = 40, height = 20;
            using var source = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (var graphics = Graphics.FromImage(source))
            {
                graphics.Clear(Color.White);
                // Red in the top left corner, after the rotation it is in the top right corner
                graphics.FillRectangle(Brushes.Red, 0, 0, 10, 10);
            }

            using var stream = new MemoryStream();
            var data = source.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                var options = new WicEncodeOptions
                {
                    ContainerFormat = WicContainerFormats.Jpeg,
                    ImageQuality = 1f,
                    Metadata = writer => writer.Set(WicMetadataPaths.Orientation(WicContainerFormats.Jpeg), (ushort)6)
                };
                var result = WicImage.Encode(stream, options, data.Scan0, width, height, data.Stride, WicPixelFormats.Format32bppBgra);
                Assert.Empty(result.MetadataIssues);
            }
            finally
            {
                source.UnlockBits(data);
            }
            stream.Position = 0;

            Assert.True(new WicFileFormatHandler().TryLoadFromStream(stream, ".jpg", out var loaded));
            using (loaded)
            {
                Assert.Equal(height, loaded.Width);
                Assert.Equal(width, loaded.Height);
                var corner = loaded.GetPixel(loaded.Width - 5, 5);
                Assert.True(corner.R > 200 && corner.G < 60 && corner.B < 60, $"Expected red in the top right corner, got {corner}");
                var other = loaded.GetPixel(5, 5);
                Assert.True(other.R > 200 && other.G > 200 && other.B > 200, $"Expected white in the top left corner, got {other}");
            }
        }

        /// <summary>
        /// JPEG XR at quality 100 written by the handler and read back without a difference
        /// </summary>
        [Fact]
        public void JpegXr_SaveAndLoad_RoundTrip()
        {
            const int width = 33, height = 21;
            using var source = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var expected = new int[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var color = Color.FromArgb(255, x * 7, y * 11, (x + y) * 3);
                    source.SetPixel(x, y, color);
                    expected[x + y * width] = color.ToArgb();
                }
            }

            var handler = new WicFileFormatHandler();
            using var stream = new MemoryStream();
            Assert.True(handler.TrySaveToStream(source, stream, ".jxr", null, new SurfaceOutputSettings(WellKnownFileFormats.Jxr, 100)));
            Assert.True(stream.Length > 0);
            stream.Position = 0;

            // ImageQuality 1.0 is lossless for JPEG XR
            AssertLoads(stream, ".jxr", width, height, expected);
        }

        private static void AssertLoads(Stream stream, string extension, int width, int height, int[] expected, int tolerance = 0)
        {
            Assert.True(new WicFileFormatHandler().TryLoadFromStream(stream, extension, out var loaded));
            using (loaded)
            {
                Assert.Equal(width, loaded.Width);
                Assert.Equal(height, loaded.Height);
                int maxDelta = 0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int actual = loaded.GetPixel(x, y).ToArgb();
                        int wanted = expected[x + y * width];
                        for (int shift = 0; shift < 32; shift += 8)
                        {
                            maxDelta = Math.Max(maxDelta, Math.Abs(((actual >> shift) & 255) - ((wanted >> shift) & 255)));
                        }
                    }
                }
                Assert.True(maxDelta <= tolerance, $"The largest difference of a channel is {maxDelta}, allowed is {tolerance}");
            }
        }

        /// <summary>
        /// An indexed bitmap with an opaque palette and the ARGB value each pixel must load as
        /// </summary>
        private static Bitmap CreateIndexed(PixelFormat pixelFormat, out int[] expected)
        {
            var bitmap = new Bitmap(Width, Height, pixelFormat);
            var palette = bitmap.Palette;
            var colors = pixelFormat == PixelFormat.Format1bppIndexed
                ? new[] { Color.Black, Color.White }
                : new[] { Color.Black, Color.White, Color.Red, Color.FromArgb(10, 200, 30) };
            for (int i = 0; i < palette.Entries.Length; i++)
            {
                palette.Entries[i] = i < colors.Length ? colors[i] : Color.Black;
            }
            bitmap.Palette = palette;

            expected = new int[Width * Height];
            var data = bitmap.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.WriteOnly, pixelFormat);
            try
            {
                var row = new byte[data.Stride];
                for (int y = 0; y < Height; y++)
                {
                    Array.Clear(row, 0, row.Length);
                    for (int x = 0; x < Width; x++)
                    {
                        int index = (x + y) % colors.Length;
                        if (pixelFormat == PixelFormat.Format1bppIndexed)
                        {
                            if (index == 1)
                            {
                                row[x / 8] |= (byte)(0x80 >> (x % 8));
                            }
                        }
                        else
                        {
                            row[x] = (byte)index;
                        }
                        expected[x + y * Width] = colors[index].ToArgb();
                    }
                    Marshal.Copy(row, 0, data.Scan0 + y * data.Stride, row.Length);
                }
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
            return bitmap;
        }
    }
}
