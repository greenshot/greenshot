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
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Greenshot.Base.UnmanagedHelpers;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Though Greenshot implements the specs for the DIB image format,
    /// it seems to cause a lot of issues when using the clipboard.
    /// There is some research done about the DIB on the clipboard, this code is based upon the information 
    /// <a href="https://stackoverflow.com/questions/44177115/copying-from-and-to-clipboard-loses-image-transparency">here</a>
    /// </summary>
    internal static class DibHelper
    {
        /// <summary>
        /// Converts the Bitmap to a Device Independent Bitmap format of type BITFIELDS.
        /// </summary>
        /// <param name="sourceBitmap">Bitmap to convert to DIB</param>
        /// <returns>The image converted to DIB, in bytes.</returns>
        public static byte[] ConvertToDib(this Bitmap sourceBitmap)
        {
            if (sourceBitmap == null) throw new ArgumentNullException(nameof(sourceBitmap));

            bool needsDisposal = false;
            if (sourceBitmap.PixelFormat != PixelFormat.Format32bppArgb)
            {
                needsDisposal = true;
                var clonedImage = ImageHelper.CreateEmptyLike(sourceBitmap, Color.Transparent, PixelFormat.Format32bppArgb);
                using (var graphics = Graphics.FromImage(clonedImage))
                {
                    graphics.DrawImage(sourceBitmap, new Rectangle(0, 0, clonedImage.Width, clonedImage.Height));
                }
                sourceBitmap = clonedImage;
            }

            var bitmapSize = 4 * sourceBitmap.Width * sourceBitmap.Height;
            var bitmapInfoHeaderSize = Marshal.SizeOf(typeof(BITMAPINFOHEADER));
            var bitmapInfoSize = bitmapInfoHeaderSize + 3 * Marshal.SizeOf(typeof(RGBQUAD));

            // Create a byte [] to contain the DIB
            var fullBmpBytes = new byte[bitmapInfoSize + bitmapSize];
            var fullBmpSpan = fullBmpBytes.AsSpan();
            // Cast the span to be of type BITMAPINFOHEADER so we can assign values
            // TODO: in .NET 6 we could do a AsRef
            var bitmapInfoHeader = MemoryMarshal.Cast<byte, BITMAPINFOHEADER>(fullBmpSpan);

            bitmapInfoHeader[0].biSize = (uint)bitmapInfoHeaderSize;
            bitmapInfoHeader[0].biWidth = sourceBitmap.Width;
            bitmapInfoHeader[0].biHeight = sourceBitmap.Height;
            bitmapInfoHeader[0].biPlanes = 1;
            bitmapInfoHeader[0].biBitCount = 32;
            bitmapInfoHeader[0].biCompression = BI_COMPRESSION.BI_BITFIELDS;
            bitmapInfoHeader[0].biSizeImage = (uint)bitmapSize;
            bitmapInfoHeader[0].biXPelsPerMeter = (int)(sourceBitmap.HorizontalResolution * 39.3701);
            bitmapInfoHeader[0].biYPelsPerMeter = (int)(sourceBitmap.VerticalResolution * 39.3701);

            // The aforementioned "BITFIELDS": color masks applied to the Int32 pixel value to get the R, G and B values.
            var rgbQuads = MemoryMarshal.Cast<byte, RGBQUAD>(fullBmpSpan.Slice(Marshal.SizeOf(typeof(BITMAPINFOHEADER))));
            rgbQuads[0].rgbRed = 255;
            rgbQuads[1].rgbGreen = 255;
            rgbQuads[2].rgbBlue = 255;

            // Now copy the lines, in reverse to the byte array
            var sourceBitmapData = sourceBitmap.LockBits(new Rectangle(0, 0, sourceBitmap.Width, sourceBitmap.Height), ImageLockMode.ReadOnly, sourceBitmap.PixelFormat);
            try
            {
                // Get a span for the real bitmap bytes, which starts after the header
                var bitmapSpan = fullBmpSpan.Slice(bitmapInfoSize);
                // Make sure we also have a span to copy from
                Span<byte> bitmapSourceSpan;
                unsafe
                {
                    bitmapSourceSpan = new Span<byte>(sourceBitmapData.Scan0.ToPointer(), sourceBitmapData.Stride * sourceBitmapData.Height);
                }

                // Loop over all the lines and copy the top line to the bottom (flipping the image)
                for (int y = 0; y < sourceBitmap.Height; y++)
                {
                    var sourceY = (sourceBitmap.Height - 1) - y;
                    var sourceLine = bitmapSourceSpan.Slice(sourceBitmapData.Stride * sourceY, 4 * sourceBitmap.Width);
                    var destinationLine = bitmapSpan.Slice(y * 4 * sourceBitmap.Width);
                    sourceLine.CopyTo(destinationLine);
                }
            }
            finally
            {
                sourceBitmap.UnlockBits(sourceBitmapData);
            }

            if (needsDisposal)
            {
                sourceBitmap.Dispose();
            }
            return fullBmpBytes;
        }
    }
}
