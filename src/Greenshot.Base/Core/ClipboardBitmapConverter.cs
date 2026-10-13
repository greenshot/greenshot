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
    /// for CF_DIB / CF_DIBV5. Only managed arrays and spans over LockBits with checked sizes are used, no pointer arithmetic on clipboard data.
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
        /// Blend the pixels onto white and make them opaque, for CF_DIB which has no defined alpha channel
        /// </summary>
        /// <param name="pixels">Bgra32Pixels from ToBgra32</param>
        /// <returns>byte array with opaque pixels, straight alpha, the same stride</returns>
        public static byte[] FlattenOnWhite(Bgra32Pixels pixels)
        {
            var result = (byte[])pixels.Pixels.Clone();
            for (int i = 0; i < result.Length; i += 4)
            {
                int alpha = result[i + 3];
                if (alpha == 255)
                {
                    continue;
                }

                for (int channel = i; channel < i + 3; channel++)
                {
                    // Premultiplied: color + white * (1 - alpha), straight: color * alpha + white * (1 - alpha)
                    result[channel] = pixels.PremultipliedAlpha
                        ? (byte)Math.Min(255, result[channel] + 255 - alpha)
                        : (byte)((result[channel] * alpha + 255 * (255 - alpha) + 127) / 255);
                }

                result[i + 3] = 255;
            }

            return result;
        }

        /// <summary>
        /// Decode CF_DIB / CF_DIBV5 data (or the content of a .dib file with BITMAPFILEHEADER) straight into a new Bitmap:
        /// Format32bppArgb when the DIB has alpha, otherwise Format32bppRgb. DibImage validates the header against the data
        /// and checks the pixel count (DibImage.DefaultMaxPixelCount, 64 megapixels) before the bitmap is created.
        /// </summary>
        /// <param name="dib">the data, e.g. the buffer of a clipboard snapshot stream</param>
        /// <param name="bitmap">Bitmap or null</param>
        /// <returns>true when the data could be decoded</returns>
        public static bool TryDecodeDib(ReadOnlySpan<byte> dib, out Bitmap bitmap)
        {
            bitmap = null;
            // A .dib / .bmp file starts with a 14 byte BITMAPFILEHEADER ("BM"), the clipboard formats don't
            if (dib.Length > 14 && dib[0] == (byte)'B' && dib[1] == (byte)'M')
            {
                dib = dib.Slice(14);
            }

            if (!DibImage.TryReadInfo(dib, DibImage.DefaultMaxPixelCount, out int width, out int height, out bool hasAlpha))
            {
                return false;
            }

            var pixelFormat = hasAlpha ? PixelFormat.Format32bppArgb : PixelFormat.Format32bppRgb;
            var result = new Bitmap(width, height, pixelFormat);
            try
            {
                bool decoded;
                var bitmapData = result.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, pixelFormat);
                try
                {
                    // The span covers exactly the locked bits, the decoder checks every write against it
                    unsafe
                    {
                        var destination = new Span<byte>((void*)bitmapData.Scan0, bitmapData.Stride * height);
                        decoded = DibImage.TryDecode(dib, DibImage.DefaultMaxPixelCount, destination, bitmapData.Stride);
                    }
                }
                finally
                {
                    result.UnlockBits(bitmapData);
                }

                if (decoded)
                {
                    bitmap = result;
                    return true;
                }
            }
            catch
            {
                result.Dispose();
                throw;
            }

            result.Dispose();
            return false;
        }
    }
}
