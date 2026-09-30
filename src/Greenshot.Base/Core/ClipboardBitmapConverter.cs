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
using System.Runtime.InteropServices;
using Dapplo.Windows.Clipboard;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Converts between System.Drawing bitmaps and the top-down BGRA32 pixels which Dapplo.Windows.Clipboard reads and writes
    /// for CF_DIB / CF_DIBV5. Only managed arrays and LockBits with checked sizes are used, no pointer arithmetic on clipboard data.
    /// </summary>
    public static class ClipboardBitmapConverter
    {
        /// <summary>
        /// Top-down BGRA32 pixels of an image, ready for ClipboardContents.AddDib or DibImage.CreateDib(V5)
        /// </summary>
        public sealed class Bgra32Pixels
        {
            internal Bgra32Pixels(byte[] pixels, int width, int height, int stride, bool premultipliedAlpha)
            {
                Pixels = pixels;
                Width = width;
                Height = height;
                Stride = stride;
                PremultipliedAlpha = premultipliedAlpha;
            }

            /// <summary>The pixels, top-down, B G R A</summary>
            public byte[] Pixels { get; }

            /// <summary>Width in pixels</summary>
            public int Width { get; }

            /// <summary>Height in pixels</summary>
            public int Height { get; }

            /// <summary>Bytes per row</summary>
            public int Stride { get; }

            /// <summary>True when the pixels have premultiplied alpha (source was Format32bppPArgb)</summary>
            public bool PremultipliedAlpha { get; }
        }

        /// <summary>
        /// Copy the pixels of the image into a managed top-down BGRA32 array.
        /// Format32bppPArgb is copied as is and reported as premultiplied, every other format is converted by GDI+ to straight Format32bppArgb.
        /// </summary>
        /// <param name="image">Image, not disposed</param>
        /// <returns>Bgra32Pixels</returns>
        public static Bgra32Pixels ToBgra32(Image image)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            Bitmap bitmap = image as Bitmap;
            bool disposeBitmap = false;
            if (bitmap == null)
            {
                // e.g. a Metafile: render it first
                bitmap = new Bitmap(image);
                disposeBitmap = true;
            }

            try
            {
                bool premultiplied = bitmap.PixelFormat == PixelFormat.Format32bppPArgb;
                var lockFormat = premultiplied ? PixelFormat.Format32bppPArgb : PixelFormat.Format32bppArgb;
                int width = bitmap.Width;
                int height = bitmap.Height;
                int stride = checked(width * 4);
                byte[] pixels = new byte[checked(stride * height)];
                var bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, lockFormat);
                try
                {
                    for (int y = 0; y < height; y++)
                    {
                        // Stride can be negative for bottom-up bitmaps, use it per row
                        var row = IntPtr.Add(bitmapData.Scan0, y * bitmapData.Stride);
                        Marshal.Copy(row, pixels, y * stride, stride);
                    }
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                }

                return new Bgra32Pixels(pixels, width, height, stride, premultiplied);
            }
            finally
            {
                if (disposeBitmap)
                {
                    bitmap.Dispose();
                }
            }
        }

        /// <summary>
        /// Create a Bitmap from a decoded DIB. With alpha the bitmap is Format32bppArgb (the DibImage has straight alpha),
        /// otherwise Format32bppRgb.
        /// </summary>
        /// <param name="dibImage">DibImage, e.g. from DibImage.TryDecode or TryGetAsDib</param>
        /// <returns>Bitmap, the caller disposes it</returns>
        public static Bitmap ToBitmap(DibImage dibImage)
        {
            if (dibImage == null)
            {
                throw new ArgumentNullException(nameof(dibImage));
            }

            var pixelFormat = dibImage.HasAlpha ? PixelFormat.Format32bppArgb : PixelFormat.Format32bppRgb;
            int width = dibImage.Width;
            int height = dibImage.Height;
            int stride = dibImage.Stride;
            byte[] pixels = dibImage.Pixels;
            if (width <= 0 || height <= 0 || stride != width * 4 || pixels == null || pixels.LongLength < (long)stride * height)
            {
                throw new ArgumentException("The DibImage has inconsistent dimensions.", nameof(dibImage));
            }

            var bitmap = new Bitmap(width, height, pixelFormat);
            try
            {
                var bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, pixelFormat);
                try
                {
                    for (int y = 0; y < height; y++)
                    {
                        var row = IntPtr.Add(bitmapData.Scan0, y * bitmapData.Stride);
                        Marshal.Copy(pixels, y * stride, row, stride);
                    }
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                }
                return bitmap;
            }
            catch
            {
                bitmap.Dispose();
                throw;
            }
        }

        /// <summary>
        /// The largest image which is decoded from DIB data: 64 megapixels (256 MiB of BGRA32 pixels)
        /// </summary>
        public const long MaxDibPixels = 16384L * 16384L / 4;

        private static bool HasAcceptableDimensions(byte[] dib)
        {
            if (dib.Length < 12)
            {
                return false;
            }

            uint headerSize = BitConverter.ToUInt32(dib, 0);
            long width, height;
            if (headerSize == 12)
            {
                // BITMAPCOREHEADER
                width = BitConverter.ToUInt16(dib, 4);
                height = BitConverter.ToUInt16(dib, 6);
            }
            else
            {
                if (dib.Length < 16)
                {
                    return false;
                }
                width = BitConverter.ToInt32(dib, 4);
                height = Math.Abs((long)BitConverter.ToInt32(dib, 8));
            }

            return width > 0 && height > 0 && width * height <= MaxDibPixels;
        }

        /// <summary>
        /// Decode CF_DIB / CF_DIBV5 bytes (or the content of a .dib file without BITMAPFILEHEADER) into a Bitmap.
        /// The decoding is done by DibImage.TryDecode which validates the header against the data.
        /// </summary>
        /// <param name="dib">bytes</param>
        /// <param name="bitmap">Bitmap or null</param>
        /// <returns>true when the data could be decoded</returns>
        public static bool TryDecodeDib(byte[] dib, out Bitmap bitmap)
        {
            bitmap = null;
            if (dib == null || dib.Length == 0)
            {
                return false;
            }

            // A .dib / .bmp file starts with a 14 byte BITMAPFILEHEADER ("BM"), the clipboard formats don't
            if (dib.Length > 14 && dib[0] == (byte)'B' && dib[1] == (byte)'M')
            {
                var withoutFileHeader = new byte[dib.Length - 14];
                Buffer.BlockCopy(dib, 14, withoutFileHeader, 0, withoutFileHeader.Length);
                dib = withoutFileHeader;
            }

            // The decoder allocates width * height * 4 bytes: reject headers which claim an image Greenshot can't handle anyway
            if (!HasAcceptableDimensions(dib))
            {
                return false;
            }

            if (!DibImage.TryDecode(dib, out var dibImage))
            {
                return false;
            }

            bitmap = ToBitmap(dibImage);
            return true;
        }
    }
}
