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
using System.Buffers;
using System.Drawing;
using System.Drawing.Imaging;
using Dapplo.Windows.Clipboard;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Converts between System.Drawing bitmaps and the top-down BGRA32 pixels which Dapplo.Windows.Clipboard reads and writes
    /// for CF_DIB / CF_DIBV5. Only spans over LockBits with checked sizes are used, no pointer arithmetic on clipboard data.
    /// </summary>
    public static class ClipboardBitmapConverter
    {
        /// <summary>
        /// Top-down BGRA32 pixels of an image, ready for ClipboardContents.AddDib or DibImage.CreateDib(V5): the locked bits of the bitmap,
        /// not a copy (an image of a 4K screen would be 33 MB on the large object heap). Dispose unlocks the bitmap.
        /// </summary>
        public sealed class Bgra32Pixels : MemoryManager<byte>
        {
            private readonly Bitmap _bitmap;
            private readonly bool _ownsBitmap;
            private BitmapData _bitmapData;

            internal Bgra32Pixels(Bitmap bitmap, bool ownsBitmap, BitmapData bitmapData, bool premultipliedAlpha)
            {
                _bitmap = bitmap;
                _ownsBitmap = ownsBitmap;
                _bitmapData = bitmapData;
                PremultipliedAlpha = premultipliedAlpha;
            }

            /// <summary>Width in pixels</summary>
            public int Width => _bitmapData.Width;

            /// <summary>Height in pixels</summary>
            public int Height => _bitmapData.Height;

            /// <summary>Bytes per row</summary>
            public int Stride => _bitmapData.Stride;

            /// <summary>True when the pixels have premultiplied alpha (source was Format32bppPArgb)</summary>
            public bool PremultipliedAlpha { get; }

            /// <summary>The pixels, top-down, B G R A</summary>
            public ReadOnlySpan<byte> Pixels => GetSpan();

            public override unsafe Span<byte> GetSpan()
            {
                var bitmapData = _bitmapData ?? throw new ObjectDisposedException(nameof(Bgra32Pixels));
                return new Span<byte>((void*)bitmapData.Scan0, checked(bitmapData.Stride * bitmapData.Height));
            }

            public override unsafe MemoryHandle Pin(int elementIndex = 0)
            {
                var span = GetSpan();
                if ((uint)elementIndex > (uint)span.Length) throw new ArgumentOutOfRangeException(nameof(elementIndex));
                // Locked bits don't move
                return new MemoryHandle((byte*)_bitmapData.Scan0 + elementIndex);
            }

            public override void Unpin()
            {
            }

            protected override void Dispose(bool disposing)
            {
                var bitmapData = _bitmapData;
                _bitmapData = null;
                if (bitmapData == null)
                {
                    return;
                }

                _bitmap.UnlockBits(bitmapData);
                if (_ownsBitmap)
                {
                    _bitmap.Dispose();
                }
            }
        }

        /// <summary>
        /// The pixels of the image as top-down BGRA32, without copying them when the image is a 32 bpp bitmap:
        /// Format32bppPArgb is used as is and reported as premultiplied, every other format is converted by GDI+ to straight Format32bppArgb.
        /// The image stays locked until the result is disposed.
        /// </summary>
        /// <param name="image">Image, not disposed</param>
        /// <returns>Bgra32Pixels, dispose it</returns>
        public static Bgra32Pixels ToBgra32(Image image)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            if (image is Bitmap bitmap)
            {
                bool premultiplied = bitmap.PixelFormat == PixelFormat.Format32bppPArgb;
                var lockFormat = premultiplied ? PixelFormat.Format32bppPArgb : PixelFormat.Format32bppArgb;
                var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, lockFormat);
                // Top-down rows without padding; else (a bitmap over bottom-up memory) a copy
                if (bitmapData.Stride == checked(bitmap.Width * 4))
                {
                    return new Bgra32Pixels(bitmap, false, bitmapData, premultiplied);
                }

                bitmap.UnlockBits(bitmapData);
            }

            // e.g. a Metafile: render it first
            return LockOwned(new Bitmap(image));
        }

        /// <summary>
        /// The pixels blended onto white and opaque, for CF_DIB which has no defined alpha channel
        /// </summary>
        /// <param name="image">Image, not disposed</param>
        /// <returns>Bgra32Pixels of a new bitmap, dispose it</returns>
        public static Bgra32Pixels FlattenOnWhite(Image image)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            var flattened = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
            try
            {
                using (var graphics = Graphics.FromImage(flattened))
                {
                    graphics.Clear(Color.White);
                    graphics.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel);
                }
            }
            catch
            {
                flattened.Dispose();
                throw;
            }

            return LockOwned(flattened);
        }

        private static Bgra32Pixels LockOwned(Bitmap bitmap)
        {
            try
            {
                var bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                return new Bgra32Pixels(bitmap, true, bitmapData, false);
            }
            catch
            {
                bitmap.Dispose();
                throw;
            }
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
