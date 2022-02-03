/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Linq;
using System.Runtime.InteropServices;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.UnmanagedHelpers;

namespace Greenshot.Base.Core.FileFormatHandlers
{
    /// <summary>
    /// This handles creating a DIB (Device Independent Bitmap) on the clipboard
    /// </summary>
    public class DibFileFormatHandler : IFileFormatHandler
    {
        private const double DpiToPelsPerMeter = 39.3701;
        private static readonly string [] OurExtensions = { "dib" };

        /// <inheritdoc />
        public IEnumerable<string> SupportedExtensions(FileFormatHandlerActions fileFormatHandlerAction)
        {
            if (fileFormatHandlerAction == FileFormatHandlerActions.LoadDrawableFromStream)
            {
                return Enumerable.Empty<string>();
            }

            return OurExtensions;
        }

        /// <inheritdoc />
        public bool Supports(FileFormatHandlerActions fileFormatHandlerAction, string extension)
        {
            if (string.IsNullOrEmpty(extension) || fileFormatHandlerAction != FileFormatHandlerActions.SaveToStream)
            {
                return false;
            }
            
            return OurExtensions.Contains(extension.ToLowerInvariant());
        }

        /// <inheritdoc />
        public int PriorityFor(FileFormatHandlerActions fileFormatHandlerAction, string extension)
        {
            return int.MaxValue;
        }

        /// <inheritdoc />
        public bool TrySaveToStream(Bitmap bitmap, Stream destination, string extension)
        {
            var dibBytes = ConvertToDib(bitmap);
            destination.Write(dibBytes, 0, dibBytes.Length);
            return true;
        }

        /// <inheritdoc />
        public bool TryLoadFromStream(Stream stream, string extension, out Bitmap bitmap)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool TryLoadDrawableFromStream(Stream stream, string extension, out IDrawableContainer drawableContainer, ISurface surface)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Converts the Bitmap to a Device Independent Bitmap format of type BITFIELDS.
        /// </summary>
        /// <param name="sourceBitmap">Bitmap to convert to DIB</param>
        /// <returns>byte{} with the image converted to DIB</returns>
        private static byte[] ConvertToDib(Bitmap sourceBitmap)
        {
            if (sourceBitmap == null) throw new ArgumentNullException(nameof(sourceBitmap));

            var area = new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height);

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
            var bitmapInfoHeaderSize = Marshal.SizeOf(typeof(BITMAPINFOHEADER));
            // The bitmap info size is the header + 3 RGBQUADs
            var bitmapInfoSize = bitmapInfoHeaderSize + 3 * Marshal.SizeOf(typeof(RGBQUAD));

            // Create a byte [] to contain the complete DIB (with .NET 5 and upwards, we could write the pixels directly to a stream)
            var fullBmpBytes = new byte[bitmapInfoSize + bitmapSize];
            // Get a span for this, this simplifies the code a bit
            var fullBmpSpan = fullBmpBytes.AsSpan();
            // Cast the span to be of type BITMAPINFOHEADER so we can assign values
            // TODO: in .NET 6 we could do a AsRef, and even write to a stream directly
            var bitmapInfoHeader = MemoryMarshal.Cast<byte, BITMAPINFOHEADER>(fullBmpSpan);

            // Fill up the bitmap info header
            bitmapInfoHeader[0].biSize = (uint)bitmapInfoHeaderSize;
            bitmapInfoHeader[0].biWidth = sourceBitmap.Width;
            bitmapInfoHeader[0].biHeight = sourceBitmap.Height;
            bitmapInfoHeader[0].biPlanes = 1;
            bitmapInfoHeader[0].biBitCount = 32;
            bitmapInfoHeader[0].biCompression = BI_COMPRESSION.BI_BITFIELDS;
            bitmapInfoHeader[0].biSizeImage = (uint)bitmapSize;
            bitmapInfoHeader[0].biXPelsPerMeter = (int)(sourceBitmap.HorizontalResolution * DpiToPelsPerMeter);
            bitmapInfoHeader[0].biYPelsPerMeter = (int)(sourceBitmap.VerticalResolution * DpiToPelsPerMeter);

            // Specify the color masks applied to the Int32 pixel value to get the R, G and B values.
            var rgbQuads = MemoryMarshal.Cast<byte, RGBQUAD>(fullBmpSpan.Slice(Marshal.SizeOf(typeof(BITMAPINFOHEADER))));
            rgbQuads[0].rgbRed = 255;
            rgbQuads[1].rgbGreen = 255;
            rgbQuads[2].rgbBlue = 255;

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
