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
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Icons;
using Greenshot.Base.Native;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Bitmap = System.Drawing.Bitmap;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// The HDR content of a capture, as Windows.Graphics.Capture delivers it on an HDR display: RGBA half-floats (HalfVector4, 8 bytes per pixel,
    /// bit for bit DXGI_FORMAT_R16G16B16A16_FLOAT) in scRGB, which is linear light with the sRGB primaries where 1.0 is 80 nits and values
    /// above the SDR white level are highlights. Straight (not premultiplied) alpha.
    /// </summary>
    /// <remarks>
    /// It holds only what the HDR displays showed: one part (tile) per HDR display at its place in the capture. The parts of SDR displays are
    /// the same as in the capture's 8-bit image, so they aren't kept twice: <see cref="Compose"/> puts them in, at the SDR white level, when an
    /// HDR file is written. A capture of one HDR display is one tile which covers it (<see cref="IsComplete"/>), nothing is composed then.
    /// It belongs to the capture next to its tone-mapped 8-bit image (ICapture.HdrImage), the surface takes it over with the image
    /// (ISurface.HdrImage); the owner disposes it. The pixels are filled while the HDR image is made and not changed after it was handed on:
    /// copies of a capture or a surface share them (<see cref="Share"/>), each with its own HdrImage; the pixels are released with the last one.
    /// </remarks>
    public sealed class HdrImage : IDisposable
    {
        /// <summary>
        /// The bytes of one pixel: R, G, B and A as half-floats
        /// </summary>
        public const int BytesPerPixel = 8;

        // One block of memory per image: the WIC encoder takes the pixels as they are, without a copy
        private static readonly Configuration ContiguousConfiguration = CreateConfiguration();

        private readonly Tile[] _tiles;
        private WeakReference<System.Drawing.Image> _belongsTo;
        private int _disposed;

        /// <summary>
        /// A new (transparent black) HDR image, complete: one tile which covers it
        /// </summary>
        public HdrImage(int width, int height, float sdrWhiteLevelInNits, float displayPeakInNits)
            : this(CheckSize(width), CheckSize(height),
                new[] { new Tile(new NativeRect(0, 0, width, height), new SharedPixels(new Image<HalfVector4>(ContiguousConfiguration, width, height)), sdrWhiteLevelInNits) },
                displayPeakInNits)
        {
        }

        private HdrImage(int width, int height, Tile[] tiles, float displayPeakInNits)
        {
            Width = width;
            Height = height;
            _tiles = tiles;
            DisplayPeakInNits = displayPeakInNits;
        }

        /// <summary>
        /// The HDR image of an area from the HDR images of its HDR displays, without copying them (they are shared, see <see cref="Share"/>).
        /// The parts stay the caller's to dispose.
        /// </summary>
        /// <param name="width">The width of the whole area</param>
        /// <param name="height">The height of the whole area</param>
        /// <param name="parts">The complete HDR image of each HDR display in the area, with its place in the area</param>
        /// <returns>The HDR image, null without a part</returns>
        public static HdrImage Combine(int width, int height, IEnumerable<(HdrImage Part, NativePoint Location)> parts)
        {
            var bounds = new NativeRect(0, 0, CheckSize(width), CheckSize(height));
            var tiles = new List<Tile>();
            float peak = 0;
            foreach (var (part, location) in parts ?? Enumerable.Empty<(HdrImage, NativePoint)>())
            {
                if (part == null)
                {
                    continue;
                }
                part.CheckNotDisposed();
                foreach (var tile in part._tiles)
                {
                    var placed = tile.Place(location, bounds);
                    if (placed != null)
                    {
                        tiles.Add(placed);
                    }
                }
                peak = Math.Max(peak, part.DisplayPeakInNits);
            }
            return tiles.Count == 0 ? null : new HdrImage(width, height, tiles.ToArray(), peak);
        }

        public int Width { get; }

        public int Height { get; }

        /// <summary>
        /// The bytes of a row of a complete HDR image
        /// </summary>
        public int Stride => Width * BytesPerPixel;

        /// <summary>
        /// The SDR white level of the (first) HDR display: SDR content (normal windows, SDR displays) has this brightness
        /// </summary>
        public float SdrWhiteLevelInNits => _tiles[0].SdrWhiteLevelInNits;

        /// <summary>
        /// The brightest the displays can show, 0 when unknown
        /// </summary>
        public float DisplayPeakInNits { get; }

        /// <summary>
        /// One tile covers the whole HDR image: it can be encoded as it is, without composing it
        /// </summary>
        public bool IsComplete => _tiles.Length == 1 && _tiles[0].Area.Equals(new NativeRect(0, 0, Width, Height));

        /// <summary>
        /// The places of the parts of the HDR displays
        /// </summary>
        public IReadOnlyList<NativeRect> TileAreas => _tiles.Select(tile => tile.Area).ToList();

        /// <summary>
        /// The pixels of a complete HDR image (see <see cref="IsComplete"/>, otherwise <see cref="Compose"/> it), shared with its copies:
        /// only read them once it was handed on
        /// </summary>
        public Image<HalfVector4> Pixels
        {
            get
            {
                CheckNotDisposed();
                if (!IsComplete) throw new InvalidOperationException("The HDR image has only the parts of the HDR displays, compose it first.");
                return _tiles[0].Shared.Pixels;
            }
        }

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
        /// An area of the HDR image as a new HDR image. Only the tiles which are cut are copied (only their part), the others are shared.
        /// </summary>
        /// <param name="area">The area in pixel coordinates, it is clipped to the image</param>
        /// <returns>The new HDR image, null when no HDR display shows anything in the area</returns>
        public HdrImage Crop(NativeRect area)
        {
            CheckNotDisposed();
            var clipped = area.Intersect(new NativeRect(0, 0, Width, Height));
            if (clipped.Width <= 0 || clipped.Height <= 0)
            {
                return null;
            }
            if (clipped.Equals(new NativeRect(0, 0, Width, Height)))
            {
                // Nothing to cut off: no copy
                return Share();
            }
            var tiles = _tiles
                .Select(tile => tile.Place(new NativePoint(-clipped.X, -clipped.Y), new NativeRect(0, 0, clipped.Width, clipped.Height)))
                .Where(tile => tile != null)
                .ToArray();
            return tiles.Length == 0 ? null : new HdrImage(clipped.Width, clipped.Height, tiles, DisplayPeakInNits);
        }

        /// <summary>
        /// The same HDR image for another owner, e.g. a copy of the capture or the surface: the pixels are shared, not copied.
        /// It doesn't belong to an image yet (see <see cref="BelongTo"/>), and it is disposed by its owner like any other.
        /// </summary>
        public HdrImage Share()
        {
            CheckNotDisposed();
            return new HdrImage(Width, Height, _tiles.Select(tile => tile.Share()).ToArray(), DisplayPeakInNits);
        }

        /// <summary>
        /// The complete HDR image, e.g. to encode it: the tiles of the HDR displays and, around them, the pixels of the SDR displays from the
        /// capture's 8-bit image at the SDR white level, so they look as they did on the screen. A complete HDR image is shared, not copied.
        /// </summary>
        /// <param name="sdrImage">The 8-bit image this HDR image belongs to (see <see cref="BelongsTo"/>), only read where no tile is</param>
        /// <param name="overlay">What is drawn on the capture (see ISurface.CreateHdrOverlay), put over it at the SDR white level; null for nothing</param>
        /// <returns>A complete HDR image, the caller disposes it</returns>
        public HdrImage Compose(Bitmap sdrImage, HdrOverlay overlay = null)
        {
            CheckNotDisposed();
            if (IsComplete && overlay == null)
            {
                return Share();
            }
            if (sdrImage == null && !IsComplete) throw new ArgumentNullException(nameof(sdrImage));

            var composed = new HdrImage(Width, Height, SdrWhiteLevelInNits, DisplayPeakInNits);
            try
            {
                if (!IsComplete)
                {
                    composed.CopyFromSdrAround(sdrImage, _tiles.Select(tile => tile.Area).ToArray());
                }
                foreach (var tile in _tiles)
                {
                    composed.CopyFrom(tile.Shared.Pixels, tile.Area.Location);
                }
                if (overlay != null)
                {
                    foreach (var area in overlay.ReplacedAreas)
                    {
                        composed.ReplaceWithSdr(overlay.Rendered, area);
                    }
                    composed.BlendSdrOver(overlay.Layer, new NativePoint(0, 0));
                }
                return composed;
            }
            catch
            {
                composed.Dispose();
                throw;
            }
        }

        /// <summary>
        /// The mouse cursor as an 8-bit layer with alpha, to draw it over the HDR image (in the HdrOverlay of the surface) like the editor draws it
        /// over the capture. An alpha cursor is drawn alone, so its soft edges and shadow are blended with the HDR pixels; a mask cursor (which inverts the
        /// screen, e.g. the I-beam) is drawn over the 8-bit image and only its changed pixels are taken.
        /// </summary>
        /// <param name="sdrImage">The capture's 8-bit image, for a mask cursor</param>
        /// <param name="cursor">The cursor</param>
        /// <param name="bounds">Where the cursor is drawn on the capture, and how big</param>
        /// <returns>The layer at bounds.Location, the caller disposes it; null when nothing of it is on the capture</returns>
        public static unsafe Bitmap CreateCursorLayer(Bitmap sdrImage, CapturedCursor cursor, NativeRect bounds)
        {
            if (cursor == null || bounds.Width <= 0 || bounds.Height <= 0)
            {
                return null;
            }
            var layer = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
            try
            {
                if (cursor.MaskLayer == null || sdrImage == null)
                {
                    CursorHelper.DrawCursorOnBitmap(layer, cursor, new NativePoint(0, 0), bounds.Size);
                    return layer;
                }

                // The 8-bit pixels under the cursor, with the cursor drawn on them: what changed is the cursor
                var visible = bounds.Intersect(new NativeRect(0, 0, sdrImage.Width, sdrImage.Height));
                if (visible.Width <= 0 || visible.Height <= 0)
                {
                    layer.Dispose();
                    return null;
                }
                using (var graphics = System.Drawing.Graphics.FromImage(layer))
                {
                    graphics.DrawImage(sdrImage, new System.Drawing.Rectangle(visible.X - bounds.X, visible.Y - bounds.Y, visible.Width, visible.Height),
                        new System.Drawing.Rectangle(visible.X, visible.Y, visible.Width, visible.Height), System.Drawing.GraphicsUnit.Pixel);
                }
                using var drawn = (Bitmap)layer.Clone();
                CursorHelper.DrawCursorOnBitmap(drawn, cursor, new NativePoint(0, 0), bounds.Size);
                var area = new NativeRect(0, 0, bounds.Width, bounds.Height);
                var layerData = layer.LockBits(area, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                var drawnData = drawn.LockBits(area, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    for (int y = 0; y < bounds.Height; y++)
                    {
                        uint* under = (uint*)((byte*)layerData.Scan0 + (long)y * layerData.Stride);
                        uint* withCursor = (uint*)((byte*)drawnData.Scan0 + (long)y * drawnData.Stride);
                        for (int x = 0; x < bounds.Width; x++)
                        {
                            under[x] = under[x] == withCursor[x] ? 0u : withCursor[x] | 0xFF000000u;
                        }
                    }
                }
                finally
                {
                    drawn.UnlockBits(drawnData);
                    layer.UnlockBits(layerData);
                }
                return layer;
            }
            catch
            {
                layer.Dispose();
                throw;
            }
        }

        /// <summary>
        /// The pixels of a complete HDR image as one block of memory (row after row, <see cref="Stride"/> bytes each), pinned while the action
        /// runs; e.g. for an encoder
        /// </summary>
        /// <param name="action">Gets the address of the first pixel</param>
        public unsafe void UsePixelMemory(Action<IntPtr> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            var pixels = Pixels;
            if (pixels.DangerousTryGetSinglePixelMemory(out Memory<HalfVector4> memory))
            {
                using var handle = memory.Pin();
                action((IntPtr)handle.Pointer);
                return;
            }

            // Not one block (only when the image wasn't made with ContiguousConfiguration): copy the rows
            var copy = new HalfVector4[(long)Width * Height];
            pixels.CopyPixelDataTo(copy);
            fixed (HalfVector4* first = copy)
            {
                action((IntPtr)first);
            }
        }

        /// <summary>
        /// Copy half-float RGBA rows into a complete HDR image while it is made, e.g. from a mapped FP16 texture
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
        /// Copy pixels into a complete HDR image while it is made, clipped to it. The pixels are copied as they are, not blended.
        /// </summary>
        private void CopyFrom(Image<HalfVector4> source, NativePoint location)
        {
            var target = new NativeRect(location, new NativeSize(source.Width, source.Height)).Intersect(new NativeRect(0, 0, Width, Height));
            if (target.Width <= 0 || target.Height <= 0)
            {
                return;
            }
            Pixels.ProcessPixelRows(source, (targetAccessor, sourceAccessor) =>
            {
                for (int y = 0; y < target.Height; y++)
                {
                    var sourceRow = sourceAccessor.GetRowSpan(target.Y - location.Y + y).Slice(target.X - location.X, target.Width);
                    sourceRow.CopyTo(targetAccessor.GetRowSpan(target.Y + y).Slice(target.X, target.Width));
                }
            });
        }

        /// <summary>
        /// Put the pixels of an 8-bit image of the same size into this complete HDR image at the SDR white level, except where the tiles are:
        /// they come from the HDR displays, their 8-bit pixels aren't converted for nothing
        /// </summary>
        private unsafe void CopyFromSdrAround(Bitmap source, NativeRect[] tileAreas)
        {
            int width = Math.Min(Width, source.Width);
            int height = Math.Min(Height, source.Height);
            ushort[] table = GetSrgbTable(SdrWhiteLevelInNits);
            ushort one = HalfHelper.FloatToHalf(1f);
            var bitmapData = source.LockBits(new NativeRect(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                Pixels.ProcessPixelRows(accessor =>
                {
                    var covered = new List<(int Start, int End)>();
                    for (int y = 0; y < height; y++)
                    {
                        // The columns of this row which a tile covers, sorted
                        covered.Clear();
                        foreach (var area in tileAreas)
                        {
                            if (y >= area.Top && y < area.Bottom)
                            {
                                covered.Add((Math.Max(area.Left, 0), Math.Min(area.Right, width)));
                            }
                        }
                        covered.Sort((a, b) => a.Start.CompareTo(b.Start));

                        byte* sourceRow = (byte*)bitmapData.Scan0 + (long)y * bitmapData.Stride;
                        // HalfVector4 is R, G, B, A as half-floats in memory
                        var targetRow = MemoryMarshal.Cast<HalfVector4, ushort>(accessor.GetRowSpan(y));
                        int x = 0;
                        foreach (var (start, end) in covered.Append((width, width)))
                        {
                            for (; x < start; x++)
                            {
                                // BGRA in memory
                                byte* pixel = sourceRow + x * 4;
                                targetRow[x * 4] = table[pixel[2]];
                                targetRow[x * 4 + 1] = table[pixel[1]];
                                targetRow[x * 4 + 2] = table[pixel[0]];
                                targetRow[x * 4 + 3] = pixel[3] == 255 ? one : HalfHelper.FloatToHalf(pixel[3] / 255f);
                            }
                            x = Math.Max(x, end);
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
        /// Replace an area of this complete HDR image while it is made with the pixels of an 8-bit image of the same size, at the SDR white level,
        /// alpha included (e.g. the transparent part of a torn edge)
        /// </summary>
        private unsafe void ReplaceWithSdr(Bitmap source, NativeRect area)
        {
            if (source == null)
            {
                return;
            }
            var target = area.Intersect(new NativeRect(0, 0, Math.Min(Width, source.Width), Math.Min(Height, source.Height)));
            if (target.Width <= 0 || target.Height <= 0)
            {
                return;
            }
            ushort[] table = GetSrgbTable(SdrWhiteLevelInNits);
            var bitmapData = source.LockBits(target, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                Pixels.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < target.Height; y++)
                    {
                        byte* sourceRow = (byte*)bitmapData.Scan0 + (long)y * bitmapData.Stride;
                        var targetRow = MemoryMarshal.Cast<HalfVector4, ushort>(accessor.GetRowSpan(target.Y + y).Slice(target.X, target.Width));
                        for (int x = 0; x < target.Width; x++, sourceRow += 4)
                        {
                            // BGRA in memory
                            targetRow[x * 4] = table[sourceRow[2]];
                            targetRow[x * 4 + 1] = table[sourceRow[1]];
                            targetRow[x * 4 + 2] = table[sourceRow[0]];
                            targetRow[x * 4 + 3] = HalfHelper.FloatToHalf(sourceRow[3] / 255f);
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
        /// Draw an 8-bit layer with alpha over this complete HDR image while it is made: its colors at the SDR white level, blended in linear light
        /// (scRGB is linear), so a cursor looks as it did on the screen and its shadow darkens the HDR pixels under it
        /// </summary>
        private unsafe void BlendSdrOver(Bitmap layer, NativePoint location)
        {
            var target = new NativeRect(location, new NativeSize(layer.Width, layer.Height)).Intersect(new NativeRect(0, 0, Width, Height));
            if (target.Width <= 0 || target.Height <= 0)
            {
                return;
            }
            float scale = SdrWhiteLevelInNits / 80f;
            var linear = new float[256];
            for (int i = 0; i < 256; i++)
            {
                double srgb = i / 255.0;
                linear[i] = (float)((srgb <= 0.04045 ? srgb / 12.92 : Math.Pow((srgb + 0.055) / 1.055, 2.4)) * scale);
            }
            var layerData = layer.LockBits(new NativeRect(target.X - location.X, target.Y - location.Y, target.Width, target.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                Pixels.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < target.Height; y++)
                    {
                        byte* sourceRow = (byte*)layerData.Scan0 + (long)y * layerData.Stride;
                        var targetRow = accessor.GetRowSpan(target.Y + y).Slice(target.X, target.Width);
                        for (int x = 0; x < target.Width; x++, sourceRow += 4)
                        {
                            byte alpha = sourceRow[3];
                            if (alpha == 0)
                            {
                                continue;
                            }
                            float a = alpha / 255f;
                            var under = targetRow[x].ToVector4();
                            // BGRA in memory, straight alpha
                            targetRow[x] = new HalfVector4(
                                linear[sourceRow[2]] * a + under.X * (1 - a),
                                linear[sourceRow[1]] * a + under.Y * (1 - a),
                                linear[sourceRow[0]] * a + under.Z * (1 - a),
                                a + under.W * (1 - a));
                        }
                    }
                });
            }
            finally
            {
                layer.UnlockBits(layerData);
            }
        }

        /// <summary>
        /// Done with this HDR image: the pixels are released when no other copy (see <see cref="Share"/>) uses them
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                foreach (var tile in _tiles)
                {
                    tile.Shared.Release();
                }
            }
        }

        private void CheckNotDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0) throw new ObjectDisposedException(nameof(HdrImage));
        }

        /// <summary>
        /// The part of one HDR display: its place in the HDR image, its pixels (shared) and its SDR white level
        /// </summary>
        private sealed class Tile
        {
            public Tile(NativeRect area, SharedPixels shared, float sdrWhiteLevelInNits)
            {
                Area = area;
                Shared = shared;
                SdrWhiteLevelInNits = sdrWhiteLevelInNits > 0 ? sdrWhiteLevelInNits : 80f;
            }

            public NativeRect Area { get; }

            public SharedPixels Shared { get; }

            public float SdrWhiteLevelInNits { get; }

            /// <summary>
            /// The same pixels for another HdrImage
            /// </summary>
            public Tile Share() => new Tile(Area, Shared.AddReference(), SdrWhiteLevelInNits);

            /// <summary>
            /// The tile moved by the offset and clipped to the bounds, for another HdrImage: the pixels are shared when all of it is inside,
            /// otherwise only the part inside is copied. Null when nothing of it is inside.
            /// </summary>
            public Tile Place(NativePoint offset, NativeRect bounds)
            {
                var moved = Area.Offset(offset.X, offset.Y);
                var part = moved.Intersect(bounds);
                if (part.Width <= 0 || part.Height <= 0)
                {
                    return null;
                }
                if (part.Equals(moved))
                {
                    return new Tile(moved, Shared.AddReference(), SdrWhiteLevelInNits);
                }
                var crop = new Rectangle(part.X - moved.X, part.Y - moved.Y, part.Width, part.Height);
                return new Tile(part, new SharedPixels(Shared.Pixels.Clone(context => context.Crop(crop))), SdrWhiteLevelInNits);
            }
        }

        /// <summary>
        /// The pixels with the number of tiles which use them
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
