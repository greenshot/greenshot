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

using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Greenshot.Base.Core;
using Greenshot.Base.Core.FileFormatHandlers;
using Greenshot.Base.Interfaces;
using Xunit;

namespace Greenshot.Tests.Editor
{
    public class TiffLoadingTests
    {
        public TiffLoadingTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        /// <summary>
        /// ImageSharp doesn't load TIFF (#1472) and WIC can't map 1 and 8 bit TIFF to a bitmap, so the registered handlers must load them with System.Drawing
        /// </summary>
        [Theory]
        [InlineData(".tif", PixelFormat.Format24bppRgb)]
        [InlineData(".tiff", PixelFormat.Format8bppIndexed)]
        [InlineData(".tif", PixelFormat.Format1bppIndexed)]
        public void RegisteredHandlers_LoadTiff(string extension, PixelFormat pixelFormat)
        {
            using var source = CreateBitmap(pixelFormat);
            using var stream = new MemoryStream();
            source.Save(stream, ImageFormat.Tiff);
            stream.Position = 0;

            var handlers = SimpleServiceProvider.Current.GetAllInstances<IFileFormatHandler>();
            Assert.True(handlers.TryLoadFromStream(stream, extension, out var loaded));
            using (loaded)
            {
                Assert.Equal(source.Size, loaded.Size);
                for (int y = 0; y < source.Height; y += 7)
                {
                    for (int x = 0; x < source.Width; x += 7)
                    {
                        Assert.Equal(source.GetPixel(x, y).ToArgb(), loaded.GetPixel(x, y).ToArgb());
                    }
                }
            }
        }

        private static Bitmap CreateBitmap(PixelFormat pixelFormat)
        {
            const int width = 33, height = 21;
            var bitmap = new Bitmap(width, height, pixelFormat);
            if (pixelFormat == PixelFormat.Format24bppRgb)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        bitmap.SetPixel(x, y, Color.FromArgb(x * 7, y * 11, 77));
                    }
                }
                return bitmap;
            }

            // Indexed: write palette indexes, the default palette has black and white at 0 and 1 (1 bit) or 0 and 255 (8 bit)
            var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, pixelFormat);
            try
            {
                var row = new byte[data.Stride];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        bool set = (x + y) % 3 == 0;
                        if (pixelFormat == PixelFormat.Format1bppIndexed)
                        {
                            if (set) row[x / 8] |= (byte)(0x80 >> (x % 8)); else row[x / 8] &= (byte)~(0x80 >> (x % 8));
                        }
                        else
                        {
                            row[x] = (byte)(set ? 255 : x * 3 + y);
                        }
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
