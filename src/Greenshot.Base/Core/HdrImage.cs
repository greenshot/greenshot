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
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Native;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Bitmap = System.Drawing.Bitmap;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// The HDR image of a capture, as Windows.Graphics.Capture delivers it on an HDR display: RGBA half-floats (HalfVector4, 8 bytes per pixel,
    /// bit for bit DXGI_FORMAT_R16G16B16A16_FLOAT) in scRGB, which is linear light with the sRGB primaries where 1.0 is 80 nits and values
    /// above the SDR white level are highlights. Straight (not premultiplied) alpha.
    /// It belongs to the capture next to its tone-mapped 8-bit image (ICapture.HdrImage), the surface takes it over with the image
    /// (ISurface.HdrImage); the owner disposes it. So the capture can be saved as an HDR file, see IHdrImageEncoder.
    /// The pixels are filled while the HDR image is made (CopyFrom, CopyFromSdr) and not changed after it was handed on: copies of a capture or
    /// a surface share them (<see cref="Share"/>), each with its own HdrImage; the pixels are released with the last one.
    /// </summary>
    public sealed class HdrImage : IDisposable
    {
        /// <summary>
        /// The bytes of one pixel: R, G, B and A as half-floats
        /// </summary>
        public const int BytesPerPixel = 8;

        // One block of memory per image: the WIC encoder takes the pixels as they are, without a copy
        private static readonly Configuration ContiguousConfiguration = CreateConfiguration();

        private readonly SharedPixels _shared;
        private WeakReference<System.Drawing.Image> _belongsTo;
        private int _disposed;

        /// <summary>
        /// A new (transparent black) HDR image
        /// </summary>
        public HdrImage(int width, int height, float sdrWhiteLevelInNits, float displayPeakInNits)
            : this(new SharedPixels(new Image<HalfVector4>(ContiguousConfiguration, CheckSize(width), CheckSize(height))), sdrWhiteLevelInNits, displayPeakInNits)
        {
        }

        private HdrImage(SharedPixels shared, float sdrWhiteLevelInNits, float displayPeakInNits)
        {
            _shared = shared;
            SdrWhiteLevelInNits = sdrWhiteLevelInNits > 0 ? sdrWhiteLevelInNits : 80f;
            DisplayPeakInNits = displayPeakInNits;
        }

        /// <summary>
        /// The pixels, shared with the copies of this HDR image (see <see cref="Share"/>): only read them once it was handed on
        /// </summary>
        public Image<HalfVector4> Pixels => Volatile.Read(ref _disposed) == 0 ? _shared.Pixels : throw new ObjectDisposedException(nameof(HdrImage));

        public int Width => Pixels.Width;

        public int Height => Pixels.Height;

        /// <summary>
        /// The bytes of a row
        /// </summary>
        public int Stride => Width * BytesPerPixel;

        /// <summary>
        /// The SDR white level of the display the pixels come from: SDR content (normal windows) has this brightness
        /// </summary>
        public float SdrWhiteLevelInNits { get; }

        /// <summary>
        /// The brightest the display can show, 0 when unknown
        /// </summary>
        public float DisplayPeakInNits { get; }

        /// <summary>
        /// Mark the tone-mapped image this HDR image belongs to. When the image is replaced (an effect, a crop in the editor) the HDR image no longer shows it.
        /// The link to System.Drawing stays until the capture itself is an ImageSharp image.
        /// </summary>
        /// <param name="image">The image of the capture, taken together with this HDR image</param>
        public void BelongTo(System.Drawing.Image image) => _belongsTo = image == null ? null : new WeakReference<System.Drawing.Image>(image);

        /// <summary>
        /// True when this HDR image belongs to exactly this image (see <see cref="BelongTo"/>)
        /// </summary>
        public bool BelongsTo(System.Drawing.Image image) => image != null && _belongsTo != null && _belongsTo.TryGetTarget(out var own) && ReferenceEquals(own, image);

        /// <summary>
        /// An area of the HDR image as a new HDR image (the pixels are copied)
        /// </summary>
        /// <param name="area">The area in pixel coordinates, it is clipped to the image</param>
        /// <returns>The new HDR image, null when the area is outside</returns>
        public HdrImage Crop(NativeRect area)
        {
            var clipped = area.Intersect(new NativeRect(0, 0, Width, Height));
            if (clipped.Width <= 0 || clipped.Height <= 0)
            {
                return null;
            }
            if (clipped.X == 0 && clipped.Y == 0 && clipped.Width == Width && clipped.Height == Height)
            {
                // Nothing to cut off: no copy
                return Share();
            }
            // Only the area is copied
            var cropped = Pixels.Clone(context => context.Crop(new Rectangle(clipped.X, clipped.Y, clipped.Width, clipped.Height)));
            return new HdrImage(new SharedPixels(cropped), SdrWhiteLevelInNits, DisplayPeakInNits);
        }

        /// <summary>
        /// The same HDR image for another owner, e.g. a copy of the capture or the surface: the pixels are shared, not copied.
        /// It doesn't belong to an image yet (see <see cref="BelongTo"/>), and it is disposed by its owner like any other.
        /// </summary>
        public HdrImage Share()
        {
            _ = Pixels; // not disposed
            return new HdrImage(_shared.AddReference(), SdrWhiteLevelInNits, DisplayPeakInNits);
        }

        /// <summary>
        /// The pixels as one block of memory (row after row, <see cref="Stride"/> bytes each), pinned while the action runs; e.g. for an encoder
        /// </summary>
        /// <param name="action">Gets the address of the first pixel</param>
        public unsafe void UsePixelMemory(Action<IntPtr> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (Pixels.DangerousTryGetSinglePixelMemory(out Memory<HalfVector4> memory))
            {
                using var handle = memory.Pin();
                action((IntPtr)handle.Pointer);
                return;
            }

            // Not one block (only when the image wasn't made with ContiguousConfiguration): copy the rows
            var copy = new HalfVector4[(long)Width * Height];
            Pixels.CopyPixelDataTo(copy);
            fixed (HalfVector4* first = copy)
            {
                action((IntPtr)first);
            }
        }

        /// <summary>
        /// Copy half-float RGBA rows into the HDR image, e.g. from a mapped FP16 texture
        /// </summary>
        /// <param name="source">The first pixel of the source rectangle</param>
        /// <param name="sourceRowPitch">The bytes from one source row to the next</param>
        /// <param name="size">The size of the area to copy</param>
        /// <param name="location">Where the area goes in this image</param>
        internal unsafe void CopyFrom(byte* source, int sourceRowPitch, NativeSize size, NativePoint location)
        {
            var target = new NativeRect(location, size).Intersect(new NativeRect(0, 0, Width, Height));
            if (target.Width <= 0 || target.Height <= 0)
            {
                return;
            }
            byte* first = source + (long)(target.Y - location.Y) * sourceRowPitch + (long)(target.X - location.X) * BytesPerPixel;
            Pixels.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < target.Height; y++)
                {
                    var sourceRow = new ReadOnlySpan<HalfVector4>(first + (long)y * sourceRowPitch, target.Width);
                    sourceRow.CopyTo(accessor.GetRowSpan(target.Y + y).Slice(target.X, target.Width));
                }
            });
        }

        /// <summary>
        /// Copy another HDR image (e.g. of one monitor) into this one, clipped to this one. The pixels are copied as they are, not blended.
        /// </summary>
        /// <param name="source">The HDR image to copy</param>
        /// <param name="location">Where it goes in this image</param>
        public void CopyFrom(HdrImage source, NativePoint location)
        {
            if (source == null)
            {
                return;
            }
            var target = new NativeRect(location, new NativeSize(source.Width, source.Height)).Intersect(new NativeRect(0, 0, Width, Height));
            if (target.Width <= 0 || target.Height <= 0)
            {
                return;
            }
            Pixels.ProcessPixelRows(source.Pixels, (targetAccessor, sourceAccessor) =>
            {
                for (int y = 0; y < target.Height; y++)
                {
                    var sourceRow = sourceAccessor.GetRowSpan(target.Y - location.Y + y).Slice(target.X - location.X, target.Width);
                    sourceRow.CopyTo(targetAccessor.GetRowSpan(target.Y + y).Slice(target.X, target.Width));
                }
            });
        }

        /// <summary>
        /// Put SDR pixels (e.g. of a monitor without HDR) into the HDR image, at the SDR white level: they look as they did on the screen
        /// </summary>
        /// <param name="source">A bitmap, read as Format32bppArgb</param>
        /// <param name="sourceRect">The area of the bitmap</param>
        /// <param name="location">Where the area goes in this image</param>
        public unsafe void CopyFromSdr(Bitmap source, NativeRect sourceRect, NativePoint location)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var target = new NativeRect(location, sourceRect.Size).Intersect(new NativeRect(0, 0, Width, Height));
            sourceRect = new NativeRect(sourceRect.X + target.X - location.X, sourceRect.Y + target.Y - location.Y, target.Width, target.Height)
                .Intersect(new NativeRect(0, 0, source.Width, source.Height));
            if (sourceRect.Width <= 0 || sourceRect.Height <= 0)
            {
                return;
            }

            ushort[] table = GetSrgbTable(SdrWhiteLevelInNits);
            ushort one = HalfHelper.FloatToHalf(1f);
            var bitmapData = source.LockBits(sourceRect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                Pixels.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < sourceRect.Height; y++)
                    {
                        byte* sourceRow = (byte*)bitmapData.Scan0 + (long)y * bitmapData.Stride;
                        // HalfVector4 is R, G, B, A as half-floats in memory
                        var targetRow = MemoryMarshal.Cast<HalfVector4, ushort>(accessor.GetRowSpan(target.Y + y).Slice(target.X, sourceRect.Width));
                        for (int x = 0; x < sourceRect.Width; x++, sourceRow += 4)
                        {
                            // BGRA in memory
                            targetRow[x * 4] = table[sourceRow[2]];
                            targetRow[x * 4 + 1] = table[sourceRow[1]];
                            targetRow[x * 4 + 2] = table[sourceRow[0]];
                            targetRow[x * 4 + 3] = sourceRow[3] == 255 ? one : HalfHelper.FloatToHalf(sourceRow[3] / 255f);
                        }
                    }
                });
            }
            finally
            {
                source.UnlockBits(bitmapData);
            }
        }

        /// <summary>
        /// Done with this HDR image: the pixels are released when no other copy (see <see cref="Share"/>) uses them
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                _shared.Release();
            }
        }

        /// <summary>
        /// The pixels with the number of HdrImages which use them
        /// </summary>
        private sealed class SharedPixels
        {
            private int _references = 1;

            public SharedPixels(Image<HalfVector4> pixels)
            {
                Pixels = pixels;
            }

            public Image<HalfVector4> Pixels { get; }

            public SharedPixels AddReference()
            {
                Interlocked.Increment(ref _references);
                return this;
            }

            public void Release()
            {
                if (Interlocked.Decrement(ref _references) == 0)
                {
                    Pixels.Dispose();
                }
            }
        }

        private static int CheckSize(int size) => size > 0 ? size : throw new ArgumentOutOfRangeException(nameof(size));

        private static Configuration CreateConfiguration()
        {
            var configuration = Configuration.Default.Clone();
            configuration.PreferContiguousImageBuffers = true;
            return configuration;
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
        }
    }
}
