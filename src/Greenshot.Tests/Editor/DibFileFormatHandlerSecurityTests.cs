/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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
using System.IO;
using Greenshot.Editor.FileFormatHandlers;
using Xunit;

namespace Greenshot.Tests.Editor
{
    public class DibFileFormatHandlerSecurityTests
    {
        public DibFileFormatHandlerSecurityTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static byte[] CreateDibV5Buffer(
            int width,
            int height,
            ushort bitCount,
            uint headerSize = 124,
            uint? sizeImage = null,
            int? pixelDataLength = null)
        {
            int absHeight = Math.Abs(height);
            int absWidth = Math.Max(1, Math.Abs(width));
            int stride = ((absWidth * bitCount + 31) / 32) * 4;
            int actualPixelBytes = pixelDataLength ?? (stride * absHeight);
            uint imageSize = sizeImage ?? (uint)(stride * absHeight);

            byte[] buffer = new byte[headerSize + actualPixelBytes];
            using var ms = new MemoryStream(buffer);
            using var writer = new BinaryWriter(ms);

            writer.Write(headerSize);               // bV5Size
            writer.Write(width);                    // bV5Width
            writer.Write(height);                   // bV5Height
            writer.Write((ushort)1);                // bV5Planes
            writer.Write(bitCount);                 // bV5BitCount
            writer.Write((uint)0);                  // bV5Compression (BI_RGB)
            writer.Write(imageSize);                // bV5SizeImage
            writer.Write(0);                        // bV5XPelsPerMeter
            writer.Write(0);                        // bV5YPelsPerMeter
            writer.Write((uint)0);                  // bV5ClrUsed
            writer.Write((uint)0);                  // bV5ClrImportant

            // Pad the rest of the V5 header up to headerSize
            while (ms.Position < headerSize)
            {
                writer.Write((byte)0);
            }

            // Fill pixel data
            for (int i = 0; i < actualPixelBytes; i++)
            {
                writer.Write((byte)0x80);
            }

            return buffer;
        }

        [Fact]
        public void TryLoadFromStream_ValidDibV5_LoadsSuccessfully()
        {
            var handler = new DibFileFormatHandler();
            byte[] dib = CreateDibV5Buffer(10, 10, 32);

            using var ms = new MemoryStream(dib);
            bool success = handler.TryLoadFromStream(ms, ".dib", out Bitmap bitmap);

            Assert.True(success);
            Assert.NotNull(bitmap);
            Assert.Equal(10, bitmap.Width);
            Assert.Equal(10, bitmap.Height);
            bitmap.Dispose();
        }

        [Fact]
        public void TryLoadFromStream_ValidTopDownDibV5_LoadsSuccessfully()
        {
            var handler = new DibFileFormatHandler();
            byte[] dib = CreateDibV5Buffer(10, -10, 32);

            using var ms = new MemoryStream(dib);
            bool success = handler.TryLoadFromStream(ms, ".dib", out Bitmap bitmap);

            Assert.True(success);
            Assert.NotNull(bitmap);
            Assert.Equal(10, bitmap.Width);
            Assert.Equal(10, bitmap.Height);
            bitmap.Dispose();
        }

        [Fact]
        public void TryLoadFromStream_OversizedHeaderSize_RejectedSafely()
        {
            var handler = new DibFileFormatHandler();
            // Header claims to be 0x10000000 bytes, which would cause pointer arithmetic overflow
            byte[] dib = CreateDibV5Buffer(10, 10, 32, headerSize: 124);
            // Overwrite header size with 0x10000000
            dib[0] = 0x00;
            dib[1] = 0x00;
            dib[2] = 0x00;
            dib[3] = 0x10;

            using var ms = new MemoryStream(dib);
            bool success = handler.TryLoadFromStream(ms, ".dib", out Bitmap bitmap);

            Assert.False(success);
            Assert.Null(bitmap);
        }

        [Fact]
        public void TryLoadFromStream_TruncatedPixelBuffer_RejectedSafely()
        {
            var handler = new DibFileFormatHandler();
            // Provide only 10 bytes of pixel data when 100 * 4 = 400 bytes are expected
            byte[] dib = CreateDibV5Buffer(10, 10, 32, pixelDataLength: 10);

            using var ms = new MemoryStream(dib);
            bool success = handler.TryLoadFromStream(ms, ".dib", out Bitmap bitmap);

            Assert.False(success);
            Assert.Null(bitmap);
        }

        [Fact]
        public void TryLoadFromStream_ZeroHeight_RejectedSafely()
        {
            var handler = new DibFileFormatHandler();
            byte[] dib = CreateDibV5Buffer(10, 0, 32);

            using var ms = new MemoryStream(dib);
            bool success = handler.TryLoadFromStream(ms, ".dib", out Bitmap bitmap);

            Assert.False(success);
            Assert.Null(bitmap);
        }

        [Fact]
        public void TryLoadFromStream_NegativeWidth_RejectedSafely()
        {
            var handler = new DibFileFormatHandler();
            byte[] dib = CreateDibV5Buffer(-5, 10, 32);

            using var ms = new MemoryStream(dib);
            bool success = handler.TryLoadFromStream(ms, ".dib", out Bitmap bitmap);

            Assert.False(success);
            Assert.Null(bitmap);
        }

        [Fact]
        public void TryLoadFromStream_ManipulatedSizeImage_DoesNotCauseOutOfBounds()
        {
            var handler = new DibFileFormatHandler();
            // SizeImage set to 0 or huge value should not affect calculated stride
            byte[] dib = CreateDibV5Buffer(10, 10, 32, sizeImage: 0);

            using var ms = new MemoryStream(dib);
            bool success = handler.TryLoadFromStream(ms, ".dib", out Bitmap bitmap);

            Assert.True(success);
            Assert.NotNull(bitmap);
            bitmap.Dispose();
        }

        [Fact]
        public void TryLoadFromStream_DataTooShortForHeader_RejectedSafely()
        {
            var handler = new DibFileFormatHandler();
            byte[] dib = new byte[20]; // Less than 40-byte BITMAPINFOHEADER

            using var ms = new MemoryStream(dib);
            bool success = handler.TryLoadFromStream(ms, ".dib", out Bitmap bitmap);

            Assert.False(success);
            Assert.Null(bitmap);
        }
    }
}
