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
using System.Drawing;
using System.Drawing.Imaging;
using Greenshot.Base.Native;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// The HDR pixels of a capture, as Windows.Graphics.Capture delivers them on an HDR display: RGBA half-floats (8 bytes per pixel)
    /// in scRGB, which is linear light with the sRGB primaries where 1.0 is 80 nits and values above the SDR white level are highlights.
    /// Kept next to the tone-mapped 8-bit image, so the capture can be saved as an HDR file (see IHdrImageEncoder). Not changed after it is created.
    /// </summary>
    public sealed class HdrPixels
    {
        /// <summary>
        /// The bytes of one pixel: R, G, B and A as half-floats
        /// </summary>
        public const int BytesPerPixel = 8;


        public HdrPixels(int width, int height, float sdrWhiteLevelInNits, float displayPeakInNits)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            Width = width;
            Height = height;
            SdrWhiteLevelInNits = sdrWhiteLevelInNits > 0 ? sdrWhiteLevelInNits : 80f;
            DisplayPeakInNits = displayPeakInNits;
            Data = new ushort[(long)width * height * 4];
        }

        public int Width { get; }
        public int Height { get; }

        /// <summary>
        /// R, G, B, A half-floats per pixel, row after row without padding, straight (not premultiplied) alpha
        /// </summary>
        public ushort[] Data { get; }

        /// <summary>
        /// The SDR white level of the display the pixels come from: SDR content (normal windows) has this brightness
        /// </summary>
        public float SdrWhiteLevelInNits { get; }

        /// <summary>
        /// The brightest the display can show, 0 when unknown
        /// </summary>
        public float DisplayPeakInNits { get; }

        /// <summary>
        /// The bytes of a row
        /// </summary>
        public int Stride => Width * BytesPerPixel;

        // The tone-mapped image these pixels belong to, weak: the capture or the surface owns the image
        private WeakReference<Image> _image;

        /// <summary>
        /// Mark the tone-mapped image these pixels belong to. When the image is replaced (an effect, a crop in the editor) the pixels no longer show it.
        /// </summary>
        /// <param name="image">The image of the capture, taken together with these pixels</param>
        public void BelongTo(Image image) => _image = image == null ? null : new WeakReference<Image>(image);

        /// <summary>
        /// True when these pixels belong to exactly this image (see <see cref="BelongTo"/>)
        /// </summary>
        public bool BelongsTo(Image image) => image != null && _image != null && _image.TryGetTarget(out var own) && ReferenceEquals(own, image);

        /// <summary>
        /// The area of the pixels, a new instance (the data is copied)
        /// </summary>
        /// <param name="area">The area in pixel coordinates, it is clipped to the pixels</param>
        /// <returns>The cropped pixels, null when the area is outside</returns>
        public HdrPixels Crop(Rectangle area)
        {
            var clipped = Rectangle.Intersect(area, new Rectangle(0, 0, Width, Height));
            if (clipped.Width <= 0 || clipped.Height <= 0)
            {
                return null;
            }
            if (clipped.X == 0 && clipped.Y == 0 && clipped.Width == Width && clipped.Height == Height)
            {
                return this;
            }

            var result = new HdrPixels(clipped.Width, clipped.Height, SdrWhiteLevelInNits, DisplayPeakInNits);
            for (int y = 0; y < clipped.Height; y++)
            {
                Array.Copy(Data, ((long)(clipped.Y + y) * Width + clipped.X) * 4, result.Data, (long)y * clipped.Width * 4, (long)clipped.Width * 4);
            }
            return result;
        }

        /// <summary>
        /// Copy half-float RGBA rows into the pixels, e.g. from a mapped FP16 texture
        /// </summary>
        /// <param name="source">The first pixel of the source rectangle</param>
        /// <param name="sourceRowPitch">The bytes from one source row to the next</param>
        /// <param name="size">The size of the area to copy</param>
        /// <param name="location">Where the area goes in these pixels</param>
        internal unsafe void CopyFrom(byte* source, int sourceRowPitch, Size size, Point location)
        {
            var target = Rectangle.Intersect(new Rectangle(location, size), new Rectangle(0, 0, Width, Height));
            if (target.Width <= 0 || target.Height <= 0)
            {
                return;
            }
            source += (long)(target.Y - location.Y) * sourceRowPitch + (long)(target.X - location.X) * BytesPerPixel;
            long bytesPerRow = (long)target.Width * BytesPerPixel;
            fixed (ushort* data = Data)
            {
                for (int y = 0; y < target.Height; y++)
                {
                    byte* targetRow = (byte*)data + ((long)(target.Y + y) * Width + target.X) * BytesPerPixel;
                    Buffer.MemoryCopy(source + (long)y * sourceRowPitch, targetRow, bytesPerRow, bytesPerRow);
                }
            }
        }

        /// <summary>
        /// Copy other HDR pixels (e.g. of one monitor) into these, clipped to these
        /// </summary>
        /// <param name="source">The pixels to copy</param>
        /// <param name="location">Where they go in these pixels</param>
        public void CopyFrom(HdrPixels source, Point location)
        {
            if (source == null)
            {
                return;
            }
            var target = Rectangle.Intersect(new Rectangle(location, new Size(source.Width, source.Height)), new Rectangle(0, 0, Width, Height));
            for (int y = 0; y < target.Height; y++)
            {
                long sourceIndex = ((long)(target.Y - location.Y + y) * source.Width + (target.X - location.X)) * 4;
                long targetIndex = ((long)(target.Y + y) * Width + target.X) * 4;
                Array.Copy(source.Data, sourceIndex, Data, targetIndex, (long)target.Width * 4);
            }
        }

        /// <summary>
        /// Put SDR pixels (e.g. of a monitor without HDR) into the HDR pixels, at the SDR white level: they look as they did on the screen
        /// </summary>
        /// <param name="source">A bitmap in Format32bppArgb</param>
        /// <param name="sourceRect">The area of the bitmap</param>
        /// <param name="location">Where the area goes in these pixels</param>
        public unsafe void CopyFromSdr(Bitmap source, Rectangle sourceRect, Point location)
        {
            var target = Rectangle.Intersect(new Rectangle(location, sourceRect.Size), new Rectangle(0, 0, Width, Height));
            sourceRect = new Rectangle(sourceRect.X + target.X - location.X, sourceRect.Y + target.Y - location.Y, target.Width, target.Height);
            sourceRect = Rectangle.Intersect(sourceRect, new Rectangle(0, 0, source.Width, source.Height));
            if (sourceRect.Width <= 0 || sourceRect.Height <= 0)
            {
                return;
            }

            ushort[] table = GetSrgbTable(SdrWhiteLevelInNits);
            ushort one = HalfHelper.FloatToHalf(1f);
            var bitmapData = source.LockBits(sourceRect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                fixed (ushort* data = Data)
                {
                    for (int y = 0; y < sourceRect.Height; y++)
                    {
                        byte* sourceRow = (byte*)bitmapData.Scan0 + (long)y * bitmapData.Stride;
                        ushort* targetRow = data + ((long)(target.Y + y) * Width + target.X) * 4;
                        for (int x = 0; x < sourceRect.Width; x++, sourceRow += 4, targetRow += 4)
                        {
                            // BGRA in memory
                            targetRow[0] = table[sourceRow[2]];
                            targetRow[1] = table[sourceRow[1]];
                            targetRow[2] = table[sourceRow[0]];
                            targetRow[3] = sourceRow[3] == 255 ? one : HalfHelper.FloatToHalf(sourceRow[3] / 255f);
                        }
                    }
                }
            }
            finally
            {
                source.UnlockBits(bitmapData);
            }
        }

        /// <summary>
        /// The scRGB half-float of every 8-bit sRGB value, at the SDR white level
        /// </summary>
        private static ushort[] GetSrgbTable(float sdrWhiteLevelInNits)
        {
            var table = new ushort[256];
            float scale = sdrWhiteLevelInNits / 80f;
            for (int i = 0; i < 256; i++)
            {
                double srgb = i / 255.0;
                double linear = srgb <= 0.04045 ? srgb / 12.92 : Math.Pow((srgb + 0.055) / 1.055, 2.4);
                table[i] = HalfHelper.FloatToHalf((float)(linear * scale));
            }
            return table;
        }    }
}
