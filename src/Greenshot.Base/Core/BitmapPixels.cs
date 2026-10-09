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
using SixLabors.ImageSharp.PixelFormats;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Processes the rows of a <see cref="BitmapPixelAccessor{TPixel}"/>
    /// </summary>
    public delegate void BitmapPixelRowsAction<TPixel>(BitmapPixelAccessor<TPixel> pixels) where TPixel : unmanaged;

    /// <summary>
    /// Processes the rows of two bitmaps at the same time
    /// </summary>
    public delegate void BitmapPixelRowsAction<TPixel1, TPixel2>(BitmapPixelAccessor<TPixel1> pixels1, BitmapPixelAccessor<TPixel2> pixels2)
        where TPixel1 : unmanaged where TPixel2 : unmanaged;

    /// <summary>
    /// The rows of a locked bitmap, like the PixelAccessor of ImageSharp: only valid inside the action.
    /// </summary>
    public readonly ref struct BitmapPixelAccessor<TPixel> where TPixel : unmanaged
    {
        private readonly Span<byte> _pixels;
        private readonly int _stride;
        private readonly int _rowBytes;

        internal BitmapPixelAccessor(Span<byte> pixels, int stride, int width, int height, int rowBytes)
        {
            _pixels = pixels;
            _stride = stride;
            _rowBytes = rowBytes;
            Width = width;
            Height = height;
        }

        /// <summary>Width in pixels</summary>
        public int Width { get; }

        /// <summary>Height in pixels</summary>
        public int Height { get; }

        /// <summary>
        /// The pixels of a row, every access is bounds checked
        /// </summary>
        /// <param name="rowIndex">0 for the top row</param>
        /// <returns>Span with Width pixels</returns>
        public Span<TPixel> GetRowSpan(int rowIndex) => MemoryMarshal.Cast<byte, TPixel>(_pixels.Slice(rowIndex * _stride, _rowBytes));
    }

    /// <summary>
    /// Row access to the pixels of a System.Drawing Bitmap, shaped like ImageSharp's Image.ProcessPixelRows, so the code can move to ImageSharp later.
    /// The pixel types are those of ImageSharp: <see cref="Bgra32"/> for 32 bpp (Format32bppArgb and Format32bppPArgb as they are, other formats
    /// are converted by GDI+ while locked, with alpha 255), <see cref="Bgr24"/> for 24 bpp and byte for the palette indices of Format8bppIndexed.
    /// </summary>
    public static class BitmapPixels
    {
        /// <summary>
        /// Lock the bitmap and process its pixel rows
        /// </summary>
        public static void ProcessPixelRows<TPixel>(Bitmap bitmap, BitmapPixelRowsAction<TPixel> processPixels, ImageLockMode lockMode = ImageLockMode.ReadWrite)
            where TPixel : unmanaged =>
            ProcessPixelRows(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height), processPixels, lockMode);

        /// <summary>
        /// Lock an area of the bitmap and process its pixel rows, row 0 is the top of the area
        /// </summary>
        public static void ProcessPixelRows<TPixel>(Bitmap bitmap, Rectangle area, BitmapPixelRowsAction<TPixel> processPixels, ImageLockMode lockMode = ImageLockMode.ReadWrite)
            where TPixel : unmanaged
        {
            area.Intersect(new Rectangle(0, 0, bitmap.Width, bitmap.Height));
            if (area.Width <= 0 || area.Height <= 0)
            {
                return;
            }

            var bitmapData = Lock<TPixel>(bitmap, area, lockMode, out int rowBytes);
            try
            {
                processPixels(CreateAccessor<TPixel>(bitmapData, rowBytes));
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
        }

        /// <summary>
        /// Lock two bitmaps and process their pixel rows together, e.g. a source and a destination
        /// </summary>
        public static void ProcessPixelRows<TPixel1, TPixel2>(Bitmap bitmap1, ImageLockMode lockMode1, Bitmap bitmap2, ImageLockMode lockMode2,
            BitmapPixelRowsAction<TPixel1, TPixel2> processPixels) where TPixel1 : unmanaged where TPixel2 : unmanaged
        {
            var bitmapData1 = Lock<TPixel1>(bitmap1, new Rectangle(0, 0, bitmap1.Width, bitmap1.Height), lockMode1, out int rowBytes1);
            try
            {
                var bitmapData2 = Lock<TPixel2>(bitmap2, new Rectangle(0, 0, bitmap2.Width, bitmap2.Height), lockMode2, out int rowBytes2);
                try
                {
                    processPixels(CreateAccessor<TPixel1>(bitmapData1, rowBytes1), CreateAccessor<TPixel2>(bitmapData2, rowBytes2));
                }
                finally
                {
                    bitmap2.UnlockBits(bitmapData2);
                }
            }
            finally
            {
                bitmap1.UnlockBits(bitmapData1);
            }
        }

        private static BitmapData Lock<TPixel>(Bitmap bitmap, Rectangle area, ImageLockMode lockMode, out int rowBytes) where TPixel : unmanaged
        {
            PixelFormat lockFormat;
            int bytesPerPixel;
            if (typeof(TPixel) == typeof(Bgra32))
            {
                lockFormat = bitmap.PixelFormat is PixelFormat.Format32bppArgb or PixelFormat.Format32bppPArgb ? bitmap.PixelFormat : PixelFormat.Format32bppArgb;
                bytesPerPixel = 4;
            }
            else if (typeof(TPixel) == typeof(Bgr24))
            {
                lockFormat = PixelFormat.Format24bppRgb;
                bytesPerPixel = 3;
            }
            else if (typeof(TPixel) == typeof(byte) && bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
            {
                lockFormat = PixelFormat.Format8bppIndexed;
                bytesPerPixel = 1;
            }
            else
            {
                throw new NotSupportedException($"Pixel type {typeof(TPixel).Name} isn't supported for {bitmap.PixelFormat}");
            }

            rowBytes = area.Width * bytesPerPixel;
            return bitmap.LockBits(area, lockMode, lockFormat);
        }

        private static unsafe BitmapPixelAccessor<TPixel> CreateAccessor<TPixel>(BitmapData bitmapData, int rowBytes) where TPixel : unmanaged
        {
            if (bitmapData.Stride < rowBytes)
            {
                throw new NotSupportedException($"Unexpected stride {bitmapData.Stride} for rows of {rowBytes} bytes");
            }

            // The span covers exactly the locked rows, so no access can go outside them
            var pixels = new Span<byte>((void*)bitmapData.Scan0, bitmapData.Stride * (bitmapData.Height - 1) + rowBytes);
            return new BitmapPixelAccessor<TPixel>(pixels, bitmapData.Stride, bitmapData.Width, bitmapData.Height, rowBytes);
        }
    }
}
