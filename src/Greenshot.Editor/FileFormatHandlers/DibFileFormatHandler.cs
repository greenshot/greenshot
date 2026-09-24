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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Gdi32.Enums;
using Dapplo.Windows.Gdi32.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using log4net;

namespace Greenshot.Editor.FileFormatHandlers
{
    /// <summary>
    /// This handles creating a DIB (Device Independent Bitmap) on the clipboard
    /// </summary>
    public class DibFileFormatHandler : AbstractFileFormatHandler, IFileFormatHandler
    {
        private const double DpiToPelsPerMeter = 39.3701;
        private static readonly ILog Log = LogManager.GetLogger(typeof(DibFileFormatHandler));

        private readonly IReadOnlyCollection<string> _ourExtensions = new[] { ".dib", ".format17", ".deviceindependentbitmap" };

        public DibFileFormatHandler()
        {
            SupportedExtensions[FileFormatHandlerActions.LoadDrawableFromStream] = _ourExtensions;
            SupportedExtensions[FileFormatHandlerActions.LoadFromStream] = _ourExtensions;
            SupportedExtensions[FileFormatHandlerActions.SaveToStream] = _ourExtensions;
        }

        /// <inheritdoc />
        public override bool TrySaveToStream(Bitmap bitmap, Stream destination, string extension, ISurface surface = null, SurfaceOutputSettings surfaceOutputSettings = null)
        {
            var dibBytes = ConvertToDib(bitmap);
            destination.Write(dibBytes, 0, dibBytes.Length);
            return true;
        }

        /// <inheritdoc />
        public override bool TryLoadFromStream(Stream stream, string extension, out Bitmap bitmap)
        {
            bitmap = null;
            if (stream == null)
            {
                return false;
            }

            // Zero-copy access if stream is a MemoryStream with an exposed buffer; otherwise read into a buffer
            byte[] rentedBuffer = null;
            ReadOnlySpan<byte> dibSpan;

            if (stream is MemoryStream ms && ms.TryGetBuffer(out ArraySegment<byte> segment))
            {
                dibSpan = segment.AsSpan();
            }
            else if (stream.CanSeek)
            {
                rentedBuffer = new byte[stream.Length];
                long originalPosition = stream.Position;
                stream.Position = 0;
                int totalRead = 0;
                while (totalRead < rentedBuffer.Length)
                {
                    int read = stream.Read(rentedBuffer, totalRead, rentedBuffer.Length - totalRead);
                    if (read == 0) break;
                    totalRead += read;
                }
                stream.Position = originalPosition;
                dibSpan = rentedBuffer.AsSpan(0, totalRead);
            }
            else
            {
                using var tempStream = RecyclableMemoryStreamFactory.GetStream("DibFileFormatHandler.LoadFromStream");
                stream.CopyTo(tempStream);
                rentedBuffer = tempStream.ToArray();
                dibSpan = rentedBuffer;
            }

            int minHeaderSize = Marshal.SizeOf(typeof(BitmapInfoHeader));
            if (dibSpan.Length < minHeaderSize)
            {
                Log.WarnFormat("DIB data too short: {0} bytes (minimum {1} bytes required)", dibSpan.Length, minHeaderSize);
                return false;
            }

            var infoHeader = BinaryStructHelper.FromByteArray<BitmapInfoHeader>(rentedBuffer ?? dibSpan.ToArray());
            if (!infoHeader.IsDibV5)
            {
                Log.InfoFormat("Using special DIB <v5 format reader with biCompression {0}", infoHeader.Compression);
                var fileHeader = BitmapFileHeader.Create(infoHeader);

                byte[] fileHeaderBytes = BinaryStructHelper.ToByteArray(fileHeader);

                using var bitmapStream = RecyclableMemoryStreamFactory.GetStream("DibFileFormatHandler.LoadFromStream");
                bitmapStream.Write(fileHeaderBytes, 0, fileHeaderBytes.Length);
                if (rentedBuffer != null)
                {
                    bitmapStream.Write(rentedBuffer, 0, dibSpan.Length);
                }
                else
                {
                    var bytes = dibSpan.ToArray();
                    bitmapStream.Write(bytes, 0, bytes.Length);
                }
                bitmapStream.Seek(0, SeekOrigin.Begin);
                try
                {
                    using var beforeCloneImage = Image.FromStream(bitmapStream);
                    bitmap = ImageHelper.Clone(beforeCloneImage) as Bitmap;
                    return bitmap != null;
                }
                catch (Exception ex)
                {
                    Log.Error("Problem retrieving Format17 from clipboard.", ex);
                    bitmap = null;
                    return false;
                }
            }

            Log.Info("Using special DIBV5 / Format17 format reader");
            // CF_DIBV5
            // Validate header and bounds to prevent out-of-bounds reads and native crashes
            int width = infoHeader.Width;
            int rawHeight = infoHeader.Height;
            int bitCount = infoHeader.BitCount;

            // Dimensions check: Width must be positive, Height cannot be 0, within GDI+ limits (65535)
            if (width <= 0 || width > 65535 || rawHeight == 0 || Math.Abs((long)rawHeight) > 65535)
            {
                Log.WarnFormat("Invalid DIB V5 dimensions: width={0}, height={1}", width, rawHeight);
                return false;
            }

            // Only 32bpp and 24bpp formats are supported by this direct reader
            if (bitCount != 32 && bitCount != 24)
            {
                Log.WarnFormat("Unsupported DIB V5 bitCount: {0}", bitCount);
                return false;
            }

            int absHeight = Math.Abs(rawHeight);

            // Calculate standard DWORD-aligned scanline stride
            int stride;
            try
            {
                stride = checked(((width * bitCount + 31) / 32) * 4);
            }
            catch (OverflowException)
            {
                Log.Warn("Overflow calculating DIB V5 stride");
                return false;
            }

            long requiredPixelBytes = (long)stride * absHeight;
            long offsetToPixels = infoHeader.OffsetToPixels;

            // BITMAPV5HEADER is at least 124 bytes. OffsetToPixels must be at least 124 bytes
            // and the entire pixel buffer [offsetToPixels, offsetToPixels + requiredPixelBytes)
            // must fall within dibSpan.
            if (offsetToPixels < 124 || offsetToPixels >= dibSpan.Length ||
                offsetToPixels + requiredPixelBytes > dibSpan.Length)
            {
                Log.WarnFormat("DIB V5 buffer out of bounds: offsetToPixels={0}, requiredPixelBytes={1}, bufferLength={2}", offsetToPixels, requiredPixelBytes, dibSpan.Length);
                return false;
            }

            var pixelFormat = bitCount == 32 ? PixelFormat.Format32bppArgb : PixelFormat.Format24bppRgb;
            int bytesPerPixel = bitCount / 8;
            int rowBytesToCopy = width * bytesPerPixel;
            bool isBottomUp = rawHeight > 0;

            Bitmap createdBitmap = null;
            try
            {
                createdBitmap = new Bitmap(width, absHeight, pixelFormat);
                var rect = new Rectangle(0, 0, width, absHeight);
                var bitmapData = createdBitmap.LockBits(rect, ImageLockMode.WriteOnly, pixelFormat);
                try
                {
                    unsafe
                    {
                        var destSpan = new Span<byte>(bitmapData.Scan0.ToPointer(), bitmapData.Stride * absHeight);
                        for (int y = 0; y < absHeight; y++)
                        {
                            int srcY = isBottomUp ? (absHeight - 1 - y) : y;
                            int srcOffset = checked((int)(offsetToPixels + (long)srcY * stride));
                            var srcRow = dibSpan.Slice(srcOffset, rowBytesToCopy);
                            var destRow = destSpan.Slice(y * bitmapData.Stride, rowBytesToCopy);
                            srcRow.CopyTo(destRow);
                        }
                    }
                }
                finally
                {
                    createdBitmap.UnlockBits(bitmapData);
                }

                bitmap = createdBitmap;
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("Problem retrieving Format17 from clipboard.", ex);
                createdBitmap?.Dispose();
                bitmap = null;
                return false;
            }
        }

        /// <summary>
        /// Converts the Bitmap to a Device Independent Bitmap format of type BITFIELDS.
        /// </summary>
        /// <param name="sourceBitmap">Bitmap to convert to DIB</param>
        /// <returns>byte{} with the image converted to DIB</returns>
        private static byte[] ConvertToDib(Bitmap sourceBitmap)
        {
            if (sourceBitmap == null) throw new ArgumentNullException(nameof(sourceBitmap));

            var area = new NativeRect(0, 0, sourceBitmap.Width, sourceBitmap.Height);

            // If the supplied format doesn't match 32bpp, we need to convert it first, and dispose the new bitmap afterwards
            bool needsDisposal = false;
            if (sourceBitmap.PixelFormat != PixelFormat.Format32bppArgb)
            {
                needsDisposal = true;
                var clonedImage = ImageHelper.CreateEmptyLike(sourceBitmap, Color.Transparent, PixelFormat.Format32bppArgb);
                using (var graphics = Graphics.FromImage(clonedImage))
                {
                    graphics.DrawImage(sourceBitmap, area);
                }
                sourceBitmap = clonedImage;
            }

            // All the pixels take this many bytes:
            var bitmapSize = 4 * sourceBitmap.Width * sourceBitmap.Height;
            // The bitmap info hear takes this many bytes:
            var bitmapInfoHeaderSize = Marshal.SizeOf(typeof(BitmapInfoHeader));
            // The bitmap info size is the header + 3 RGBQUADs
            var bitmapInfoSize = bitmapInfoHeaderSize + 3 * Marshal.SizeOf(typeof(RgbQuad));

            // Create a byte [] to contain the complete DIB (with .NET 5 and upwards, we could write the pixels directly to a stream)
            var fullBmpBytes = new byte[bitmapInfoSize + bitmapSize];
            // Get a span for this, this simplifies the code a bit
            var fullBmpSpan = fullBmpBytes.AsSpan();
            // Cast the span to be of type BITMAPINFOHEADER so we can assign values
            // TODO: in .NET 6 we could do a AsRef, and even write to a stream directly
            var bitmapInfoHeader = MemoryMarshal.Cast<byte, BitmapInfoHeader>(fullBmpSpan);

            // Fill up the bitmap info header
            bitmapInfoHeader[0].Size = (uint)bitmapInfoHeaderSize;
            bitmapInfoHeader[0].Width = sourceBitmap.Width;
            bitmapInfoHeader[0].Height = sourceBitmap.Height;
            bitmapInfoHeader[0].Planes = 1;
            bitmapInfoHeader[0].BitCount = 32;
            bitmapInfoHeader[0].Compression = BitmapCompressionMethods.BI_BITFIELDS;
            bitmapInfoHeader[0].SizeImage = (uint)bitmapSize;
            bitmapInfoHeader[0].XPelsPerMeter = (int)(sourceBitmap.HorizontalResolution * DpiToPelsPerMeter);
            bitmapInfoHeader[0].YPelsPerMeter = (int)(sourceBitmap.VerticalResolution * DpiToPelsPerMeter);

            // Specify the color masks applied to the Int32 pixel value to get the R, G and B values.
            var rgbQuads = MemoryMarshal.Cast<byte, RgbQuad>(fullBmpSpan.Slice(Marshal.SizeOf(typeof(BitmapInfoHeader))));
            rgbQuads[0].Red = 255;
            rgbQuads[1].Green = 255;
            rgbQuads[2].Blue = 255;

            // Now copy the lines, in reverse (bmp is upside down) to the byte array
            var sourceBitmapData = sourceBitmap.LockBits(area, ImageLockMode.ReadOnly, sourceBitmap.PixelFormat);
            try
            {
                // Get a span for the real bitmap bytes, which starts after the bitmapinfo (header + 3xRGBQuad)
                var bitmapSpan = fullBmpSpan.Slice(bitmapInfoSize);
                // Make sure we also have a span to copy from, by taking the pointer from the locked bitmap
                Span<byte> bitmapSourceSpan;
                unsafe
                {
                    bitmapSourceSpan = new Span<byte>(sourceBitmapData.Scan0.ToPointer(), sourceBitmapData.Stride * sourceBitmapData.Height);
                }

                // Loop over all the bitmap lines
                for (int destinationY = 0; destinationY < sourceBitmap.Height; destinationY++)
                {
                    // Calculate the y coordinate for the bottom up. (flipping the image)
                    var sourceY = (sourceBitmap.Height - 1) - destinationY;
                    // Make a Span for the source bitmap pixels
                    var sourceLine = bitmapSourceSpan.Slice(sourceBitmapData.Stride * sourceY, 4 * sourceBitmap.Width);
                    // Make a Span for the destination dib pixels
                    var destinationLine = bitmapSpan.Slice(destinationY * 4 * sourceBitmap.Width);
                    sourceLine.CopyTo(destinationLine);
                }
            }
            finally
            {
                sourceBitmap.UnlockBits(sourceBitmapData);
            }

            // If we created a new bitmap, we need to dispose this
            if (needsDisposal)
            {
                sourceBitmap.Dispose();
            }
            return fullBmpBytes;
        }
    }
}
