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
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Greenshot.Helpers.Ipc;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Greenshot.Tests.Ipc
{
    /// <summary>
    /// IMPORT_CAPTURE: only PNG/JPEG, dimension limits checked before decoding, and an acknowledgement for the extension.
    /// </summary>
    public class ImportCaptureTests
    {
        private static string EncodeImage(int width, int height, ImageFormat format)
        {
            using (var bitmap = new Bitmap(width, height))
            using (var stream = new MemoryStream())
            {
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.Clear(Color.CornflowerBlue);
                }
                bitmap.Save(stream, format);
                return Convert.ToBase64String(stream.ToArray());
            }
        }

        [Theory]
        [InlineData("png")]
        [InlineData("jpeg")]
        public void TryDecode_PngAndJpeg_AreAccepted(string formatName)
        {
            var format = formatName == "png" ? ImageFormat.Png : ImageFormat.Jpeg;
            Assert.True(ImportCaptureDecoder.TryDecode(EncodeImage(40, 30, format), out var bitmap, out var error), error);
            using (bitmap)
            {
                Assert.Null(error);
                Assert.Equal(40, bitmap.Width);
                Assert.Equal(30, bitmap.Height);
            }
        }

        [Fact]
        public void TryDecode_OtherFormats_AreRejectedWithoutDecoding()
        {
            Assert.False(ImportCaptureDecoder.TryDecode(EncodeImage(10, 10, ImageFormat.Bmp), out var bitmap, out var error));
            Assert.Null(bitmap);
            Assert.Contains("PNG and JPEG", error);

            Assert.False(ImportCaptureDecoder.TryDecode(EncodeImage(10, 10, ImageFormat.Gif), out _, out error));
            Assert.Contains("PNG and JPEG", error);
        }

        [Theory]
        [InlineData(null, "missing")]
        [InlineData("", "missing")]
        [InlineData("not base64!", "base64")]
        public void TryDecode_InvalidPayload_IsRejected(string payload, string expectedError)
        {
            Assert.False(ImportCaptureDecoder.TryDecode(payload, out var bitmap, out var error));
            Assert.Null(bitmap);
            Assert.Contains(expectedError, error);
        }

        [Fact]
        public void TryDecode_TruncatedPng_IsRejected()
        {
            byte[] png = Convert.FromBase64String(EncodeImage(50, 50, ImageFormat.Png));
            byte[] truncated = new byte[20];
            Array.Copy(png, truncated, truncated.Length);

            Assert.False(ImportCaptureDecoder.TryDecode(Convert.ToBase64String(truncated), out var bitmap, out var error));
            Assert.Null(bitmap);
            Assert.NotNull(error);
        }

        [Theory]
        [InlineData(40000, 16)]   // wider than the maximum side
        [InlineData(20000, 20000)] // 400 megapixels
        public void TryDecode_HugeDimensionsInHeader_AreRejectedBeforeDecoding(int width, int height)
        {
            // A few hundred bytes claiming a gigantic image: decoding it would allocate gigabytes
            string payload = Convert.ToBase64String(CreatePngHeaderOnly(width, height));

            Assert.False(ImportCaptureDecoder.TryDecode(payload, out var bitmap, out var error));
            Assert.Null(bitmap);
            Assert.Contains("too large", error);
        }

        [Theory]
        [InlineData(1, 1, true)]
        [InlineData(32767, 1, true)]
        [InlineData(32768, 1, false)]
        [InlineData(1, 32768, false)]
        [InlineData(10000, 10000, true)]
        [InlineData(10001, 10000, false)]
        [InlineData(0, 10, false)]
        [InlineData(10, -1, false)]
        public void IsAcceptableSize_EnforcesSideAndPixelLimits(int width, int height, bool expected)
        {
            Assert.Equal(expected, ImportCaptureDecoder.IsAcceptableSize(width, height, out var error));
            Assert.Equal(expected, error == null);
        }

        [Fact]
        public async Task Dispatcher_InvalidImage_RepliesWithError()
        {
            var reply = await DispatchImportAsync(Convert.ToBase64String(Encoding.ASCII.GetBytes("GIF89a....")));
            Assert.Equal("error", reply.Value<string>("status"));
            Assert.Equal("IMPORT_CAPTURE", reply.Value<string>("reply_to"));
            Assert.Equal(1, reply.Value<int>("exit_code"));
            Assert.Contains("PNG and JPEG", reply.Value<string>("stderr"));
        }

        [Fact]
        public async Task Dispatcher_HugeImage_RepliesWithError()
        {
            var reply = await DispatchImportAsync(Convert.ToBase64String(CreatePngHeaderOnly(50000, 50000)));
            Assert.Equal("error", reply.Value<string>("status"));
            Assert.Equal("IMPORT_CAPTURE", reply.Value<string>("reply_to"));
            Assert.Contains("too large", reply.Value<string>("stderr"));
        }

        [Fact]
        public async Task Dispatcher_ValidImageWithoutUi_RepliesNotReady()
        {
            var reply = await DispatchImportAsync(EncodeImage(20, 20, ImageFormat.Png));
            Assert.Equal("error", reply.Value<string>("status"));
            Assert.Equal("IMPORT_CAPTURE", reply.Value<string>("reply_to"));
            Assert.Contains("not ready", reply.Value<string>("stderr"));
        }

        private static async Task<JObject> DispatchImportAsync(string payload)
        {
            var envelope = new IpcEnvelope
            {
                Source = IpcSources.NativeMessaging,
                Command = "IMPORT_CAPTURE",
                Data = new IpcCaptureData { MimeType = "image/png", Encoding = "base64", Payload = payload }
            };
            using (var ms = new MemoryStream())
            {
                var context = new IpcRequestContext(envelope, ms);
                await IpcSecurityDispatcher.DispatchAsync(context, null, () => { }, () => { }, () => { }, f => { });
                Assert.True(ms.Length > 4, "IMPORT_CAPTURE must always be acknowledged");
                ms.Position = 0;
                byte[] lengthBytes = new byte[4];
                ms.Read(lengthBytes, 0, 4);
                byte[] payloadBytes = new byte[BitConverter.ToUInt32(lengthBytes, 0)];
                ms.Read(payloadBytes, 0, payloadBytes.Length);
                return JObject.Parse(Encoding.UTF8.GetString(payloadBytes));
            }
        }

        /// <summary>
        /// A PNG with a valid IHDR (1-bit grayscale) for the given dimensions, a tiny IDAT and IEND, but nowhere near
        /// the pixel data the header announces.
        /// </summary>
        private static byte[] CreatePngHeaderOnly(int width, int height)
        {
            using (var stream = new MemoryStream())
            {
                stream.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, 0, 8);

                var ihdr = new byte[13];
                WriteBigEndian(ihdr, 0, (uint)width);
                WriteBigEndian(ihdr, 4, (uint)height);
                ihdr[8] = 1;  // bit depth
                ihdr[9] = 0;  // grayscale
                WriteChunk(stream, "IHDR", ihdr);
                // zlib stream of a few zero bytes (stored block)
                WriteChunk(stream, "IDAT", new byte[] { 0x78, 0x01, 0x01, 0x04, 0x00, 0xFB, 0xFF, 0, 0, 0, 0, 0x00, 0x00, 0x00, 0x01 });
                WriteChunk(stream, "IEND", new byte[0]);
                return stream.ToArray();
            }
        }

        private static void WriteChunk(Stream stream, string type, byte[] data)
        {
            var lengthBytes = new byte[4];
            WriteBigEndian(lengthBytes, 0, (uint)data.Length);
            stream.Write(lengthBytes, 0, 4);

            var typeAndData = new byte[4 + data.Length];
            Encoding.ASCII.GetBytes(type, 0, 4, typeAndData, 0);
            Array.Copy(data, 0, typeAndData, 4, data.Length);
            stream.Write(typeAndData, 0, typeAndData.Length);

            var crcBytes = new byte[4];
            WriteBigEndian(crcBytes, 0, Crc32(typeAndData));
            stream.Write(crcBytes, 0, 4);
        }

        private static void WriteBigEndian(byte[] buffer, int offset, uint value)
        {
            // Masked, because Debug builds check arithmetic overflow (CheckForOverflowUnderflow)
            buffer[offset] = (byte)((value >> 24) & 0xFF);
            buffer[offset + 1] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 3] = (byte)(value & 0xFF);
        }

        private static uint Crc32(byte[] data)
        {
            uint crc = 0xFFFFFFFF;
            foreach (byte b in data)
            {
                crc ^= b;
                for (int bit = 0; bit < 8; bit++)
                {
                    crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320u : crc >> 1;
                }
            }
            return ~crc;
        }
    }
}
