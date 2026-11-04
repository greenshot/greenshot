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
using Greenshot.Base.Effects;
using Greenshot.Base.ImageSharpDrawing;
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
            bool requested = false;
            bool cleared = false;
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
                requested |= part.IsRequested;
                cleared |= part.HasClearedPixels;
            }
            return tiles.Count == 0 ? null : new HdrImage(width, height, tiles.ToArray(), peak) { IsRequested = requested, HasClearedPixels = cleared };
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
        /// The image it belongs to still exists (e.g. kept by the undo of the editor): otherwise nothing can show this HDR image anymore
        /// </summary>
        public bool IsLinked => _belongsTo != null && _belongsTo.TryGetTarget(out _);

        /// <summary>
        /// HDR was asked for explicitly (a Source step with Hdr true, an opened HDR file), not only by the setting: Save as suggests JPEG XR.
        /// Copied to the HDR images made of it (crop, rotation, copies).
        /// </summary>
        public bool IsRequested { get; set; }

        /// <summary>
        /// Pixels were cleared (see <see cref="ClearPixels"/>, e.g. the torn off teeth of torn edges): when it is composed, its transparent pixels
        /// are taken from the 8-bit image. Copied to the HDR images made of it.
        /// </summary>
        public bool HasClearedPixels { get; private set; }

        /// <summary>
        /// The HDR image with the pixels which the 8-bit image changed cleared (transparent black), as a new HDR image: e.g. the teeth of torn
        /// edges, which follow no rectangle. When it is composed, those pixels are taken from the 8-bit image. Tiles without such a pixel are shared.
        /// </summary>
        /// <param name="changes">Areas of the image and which of their pixels changed (row by row)</param>
        public HdrImage ClearPixels(IReadOnlyList<(NativeRect Area, System.Collections.BitArray Changed)> changes)
        {
            CheckNotDisposed();
            var tiles = _tiles.Select(tile =>
            {
                var touching = changes.Where(change => change.Area.IntersectsWith(tile.Area)).ToList();
                if (touching.Count == 0)
                {
                    return tile.Share();
                }
                var pixels = tile.Shared.Pixels.Clone();
                pixels.ProcessPixelRows(accessor =>
                {
                    foreach (var (area, changed) in touching)
                    {
                        var inTile = area.Intersect(tile.Area);
                        for (int y = inTile.Top; y < inTile.Bottom; y++)
                        {
                            var row = accessor.GetRowSpan(y - tile.Area.Top);
                            for (int x = inTile.Left; x < inTile.Right; x++)
                            {
                                if (changed[(y - area.Top) * area.Width + x - area.Left])
                                {
                                    row[x - tile.Area.Left] = default;
                                }
                            }
                        }
                    }
                });
                return new Tile(tile.Area, new SharedPixels(pixels), tile.SdrWhiteLevelInNits);
            }).ToArray();
            return new HdrImage(Width, Height, tiles, DisplayPeakInNits) { IsRequested = IsRequested, HasClearedPixels = true };
        }

        // 0: not measured yet, 1: with HDR content, 2: without
        private int _hasHdrContent;

        /// <summary>
        /// Something an 8-bit sRGB file can't show: a highlight (brighter than SDR white) or a color outside sRGB. Without it (e.g. a normal window
        /// on an HDR display) an HDR file would only look like the normal one, so the capture counts as one without HDR content: no HDR file,
        /// no HDR badge, payload.hdr is None. Measured once, when it is first asked for (the pixels are complete then).
        /// </summary>
        public bool HasHdrContent
        {
            get
            {
                CheckNotDisposed();
                int known = Volatile.Read(ref _hasHdrContent);
                if (known == 0)
                {
                    known = _tiles.Any(tile => Native.HdrContent.HasHdrContent(tile.Shared.Pixels, tile.SdrWhiteLevelInNits)) ? 1 : 2;
                    Volatile.Write(ref _hasHdrContent, known);
                }
                return known == 1;
            }
        }

        /// <summary>
        /// The HDR image rotated like the editor rotates the capture (RotateEffect), as a new HDR image; the tiles are rotated with their places
        /// </summary>
        /// <param name="angle">90 (clockwise) or 270 / -90 (counterclockwise)</param>
        /// <returns>The new HDR image, null for another angle</returns>
        public HdrImage Rotate(int angle)
        {
            CheckNotDisposed();
            bool clockwise = angle == 90;
            if (!clockwise && angle != 270 && angle != -90)
            {
                return null;
            }
            var tiles = _tiles.Select(tile =>
            {
                var area = tile.Area;
                // Clockwise the left column becomes the top row: x' = height - y - h, y' = x; counterclockwise x' = y, y' = width - x - w
                var rotatedArea = clockwise
                    ? new NativeRect(Height - area.Bottom, area.Left, area.Height, area.Width)
                    : new NativeRect(area.Top, Width - area.Right, area.Height, area.Width);
                var rotated = tile.Shared.Pixels.Clone(context => context.Rotate(clockwise ? RotateMode.Rotate90 : RotateMode.Rotate270));
                return new Tile(rotatedArea, new SharedPixels(rotated), tile.SdrWhiteLevelInNits);
            }).ToArray();
            return new HdrImage(Height, Width, tiles, DisplayPeakInNits) { IsRequested = IsRequested, HasClearedPixels = HasClearedPixels };
        }

        /// <summary>
        /// The HDR image resized like the editor resizes the capture (ResizeEffect: bicubic, the whole image to the new size), as a new HDR image;
        /// each tile is resized to its scaled place
        /// </summary>
        /// <returns>The new HDR image, null when no tile is left (a size so small that a tile has no pixel)</returns>
        public HdrImage Resize(int width, int height)
        {
            CheckNotDisposed();
            double scaleX = CheckSize(width) / (double)Width;
            double scaleY = CheckSize(height) / (double)Height;
            var tiles = _tiles.Select(tile =>
            {
                var area = tile.Area;
                int left = (int)Math.Round(area.Left * scaleX);
                int top = (int)Math.Round(area.Top * scaleY);
                var scaledArea = new NativeRect(left, top, (int)Math.Round(area.Right * scaleX) - left, (int)Math.Round(area.Bottom * scaleY) - top);
                if (scaledArea.Width <= 0 || scaledArea.Height <= 0)
                {
                    return null;
                }
                // On float pixels: ImageSharp would mix HalfVector4 on its scaled values (see FloatPixels)
                var resized = FloatPixels.Clone(tile.Shared.Pixels, context => context.Resize(scaledArea.Width, scaledArea.Height, KnownResamplers.Bicubic));
                return new Tile(scaledArea, new SharedPixels(resized), tile.SdrWhiteLevelInNits);
            }).Where(tile => tile != null).ToArray();
            return tiles.Length == 0 ? null : new HdrImage(width, height, tiles, DisplayPeakInNits) { IsRequested = IsRequested, HasClearedPixels = HasClearedPixels };
        }

        /// <summary>
        /// The HDR image without the areas, as a new HDR image: e.g. what an element drawn into the image covers. Those areas come from the
        /// 8-bit image when it is composed, like an SDR display. A tile with an area in it is split into the rectangles around it (copied).
        /// </summary>
        /// <returns>The new HDR image, null when nothing is left</returns>
        public HdrImage Exclude(IEnumerable<NativeRect> areas)
        {
            CheckNotDisposed();
            var pieces = _tiles.Select(tile => (Tile: tile, Area: tile.Area)).ToList();
            foreach (var area in areas ?? Enumerable.Empty<NativeRect>())
            {
                pieces = pieces.SelectMany(piece => Subtract(piece.Area, area).Select(rest => (piece.Tile, Area: rest))).ToList();
            }
            var tiles = pieces.Select(piece =>
            {
                if (piece.Area.Equals(piece.Tile.Area))
                {
                    return piece.Tile.Share();
                }
                var crop = new Rectangle(piece.Area.X - piece.Tile.Area.X, piece.Area.Y - piece.Tile.Area.Y, piece.Area.Width, piece.Area.Height);
                return new Tile(piece.Area, new SharedPixels(piece.Tile.Shared.Pixels.Clone(context => context.Crop(crop))), piece.Tile.SdrWhiteLevelInNits);
            }).ToArray();
            return tiles.Length == 0 ? null : new HdrImage(Width, Height, tiles, DisplayPeakInNits) { IsRequested = IsRequested, HasClearedPixels = HasClearedPixels };
        }

        /// <summary>
        /// The rectangles of the area which are outside of all the holes, e.g. where an inverted filter of elements works
        /// </summary>
        public static IReadOnlyList<NativeRect> Outside(NativeRect area, IEnumerable<NativeRect> holes)
        {
            var rest = new List<NativeRect> { area };
            foreach (var hole in holes ?? Enumerable.Empty<NativeRect>())
            {
                rest = rest.SelectMany(piece => Subtract(piece, hole)).ToList();
            }
            return rest;
        }

        /// <summary>
        /// The rectangles of the area which are outside of the hole: above, below, left and right of it
        /// </summary>
        private static IEnumerable<NativeRect> Subtract(NativeRect area, NativeRect hole)
        {
            var inside = area.Intersect(hole);
            if (inside.Width <= 0 || inside.Height <= 0)
            {
                return new[] { area };
            }
            return new[]
            {
                new NativeRect(area.Left, area.Top, area.Width, inside.Top - area.Top),
                new NativeRect(area.Left, inside.Bottom, area.Width, area.Bottom - inside.Bottom),
                new NativeRect(area.Left, inside.Top, inside.Left - area.Left, inside.Height),
                new NativeRect(inside.Right, inside.Top, area.Right - inside.Right, inside.Height)
            }.Where(rest => rest.Width > 0 && rest.Height > 0);
        }

        /// <summary>
        /// A complete HDR image drawn anew from this one (e.g. the torn edge effect, see ImageSharpDrawing.CutDrawing), as a new complete HDR image
        /// </summary>
        /// <param name="draw">Makes the new pixels from the pixels of this one and the SDR white as scRGB value; it must use their configuration</param>
        /// <returns>The new HDR image, null when this one isn't complete (the parts of SDR displays are only in the 8-bit image)</returns>
        public HdrImage Redraw(Func<Image<HalfVector4>, float, Image<HalfVector4>> draw)
        {
            if (draw == null) throw new ArgumentNullException(nameof(draw));
            CheckNotDisposed();
            if (!IsComplete)
            {
                return null;
            }
            var pixels = draw(Pixels, SdrWhiteLevelInNits / 80f);
            var tile = new Tile(new NativeRect(0, 0, pixels.Width, pixels.Height), new SharedPixels(pixels), SdrWhiteLevelInNits);
            return new HdrImage(pixels.Width, pixels.Height, new[] { tile }, DisplayPeakInNits) { IsRequested = IsRequested, HasClearedPixels = HasClearedPixels };
        }

        /// <summary>
        /// The HDR image with the pixels of each tile changed (e.g. a color effect, see ImageEdits), as a new HDR image; this one stays as it is
        /// </summary>
        /// <param name="process">Changes a copy of the pixels of a tile; it gets the SDR white of the tile's display as scRGB value (1.0 = 80 nits)</param>
        public HdrImage Process(Action<IImageProcessingContext, float> process)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));
            CheckNotDisposed();
            var tiles = _tiles.Select(tile =>
            {
                var processed = tile.Shared.Pixels.Clone(context => process(context, tile.SdrWhiteLevelInNits / 80f));
                return new Tile(tile.Area, new SharedPixels(processed), tile.SdrWhiteLevelInNits);
            }).ToArray();
            return new HdrImage(Width, Height, tiles, DisplayPeakInNits) { IsRequested = IsRequested, HasClearedPixels = HasClearedPixels };
        }

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
            return tiles.Length == 0 ? null : new HdrImage(clipped.Width, clipped.Height, tiles, DisplayPeakInNits) { IsRequested = IsRequested, HasClearedPixels = HasClearedPixels };
        }

        /// <summary>
        /// The same HDR image for another owner, e.g. a copy of the capture or the surface: the pixels are shared, not copied.
        /// It doesn't belong to an image yet (see <see cref="BelongTo"/>), and it is disposed by its owner like any other.
        /// </summary>
        public HdrImage Share()
        {
            CheckNotDisposed();
            return new HdrImage(Width, Height, _tiles.Select(tile => tile.Share()).ToArray(), DisplayPeakInNits) { IsRequested = IsRequested, HasClearedPixels = HasClearedPixels };
        }

        /// <summary>
        /// The complete HDR image, e.g. to encode it: the tiles of the HDR displays and, around them, the pixels of the SDR displays from the
        /// capture's 8-bit image at the SDR white level, so they look as they did on the screen. A complete HDR image is shared, not copied.
        /// </summary>
        /// <param name="sdrImage">The 8-bit image this HDR image belongs to (see <see cref="BelongsTo"/>), only read where no tile is</param>
        /// <param name="overlay">What is drawn on the capture (see ISurface.CreateHdrOverlay), put over it with white halfway between SDR white and the white of the capture as the editor shows it; null for nothing</param>
        /// <returns>A complete HDR image, the caller disposes it</returns>
        public HdrImage Compose(Bitmap sdrImage, HdrOverlay overlay = null)
        {
            CheckNotDisposed();
            if (IsComplete && overlay == null && !HasClearedPixels)
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
                if (HasClearedPixels && sdrImage != null)
                {
                    // The cleared pixels (e.g. the torn off teeth) are the 8-bit image
                    composed.FillTransparentFromSdr(sdrImage, _tiles.Select(tile => tile.Area).ToArray());
                }
                if (overlay != null)
                {
                    // The white of the elements: halfway (geometric) between SDR white and the white of the capture as the editor shows it (the white
                    // level of its tone mapping, HdrImageToneMapping). At SDR white a white element looks gray next to bright HDR content, at the
                    // white of the tone mapping (up to the display's peak) it is glaring.
                    var (peakInNits, highlightShare) = HdrContent.Measure(composed, SdrWhiteLevelInNits);
                    float toneMappingWhite = HdrContent.WhiteLevelInNits(SdrWhiteLevelInNits, DisplayPeakInNits, peakInNits, highlightShare);
                    float elementWhite = (float)Math.Sqrt(SdrWhiteLevelInNits * toneMappingWhite) / 80f;
                    // The elements in their order: a filter works on what is drawn before it
                    foreach (var step in overlay.Steps)
                    {
                        if (step.Draw != null)
                        {
                            composed.DrawOver(new NativeRect(step.Location, step.Size), step.Draw, elementWhite);
                        }
                        else if (step.Layer != null)
                        {
                            composed.BlendSdrOver(step.Layer, step.Location, elementWhite);
                        }
                        else
                        {
                            step.Filter.Apply(composed.Pixels, composed.SdrWhiteLevelInNits / 80f);
                        }
                    }
                    // The replaced areas are the export as it is, with everything which is drawn over them
                    foreach (var area in overlay.ReplacedAreas)
                    {
                        composed.ReplaceWithSdr(overlay.Rendered, area);
                    }
                    foreach (var area in overlay.ChangedAreas)
                    {
                        // Only the pixels the element changed (e.g. the teeth of torn edges), the others keep their HDR content
                        if (sdrImage != null)
                        {
                            composed.ReplaceChangedWithSdr(overlay.Rendered, sdrImage, area);
                        }
                        else
                        {
                            composed.ReplaceWithSdr(overlay.Rendered, area);
                        }
                    }
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
        /// Fill a new HDR image while it is made: its pixels as one block of memory (row after row, <see cref="Stride"/> bytes each),
        /// pinned while the action writes them; e.g. for a decoder
        /// </summary>
        /// <param name="action">Gets the address of the first pixel</param>
        public unsafe void FillPixelMemory(Action<IntPtr> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (!Pixels.DangerousTryGetSinglePixelMemory(out Memory<HalfVector4> memory))
            {
                // Every HdrImage is made with ContiguousConfiguration
                throw new InvalidOperationException("The pixels of the HDR image are not one block of memory.");
            }
            using var handle = memory.Pin();
            action((IntPtr)handle.Pointer);
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
        /// <param name="unpremultiply">The source has premultiplied alpha (a Direct3D texture of Windows.Graphics.Capture): the HDR image has straight alpha</param>
        internal unsafe void CopyFrom(byte* source, int sourceRowPitch, NativeSize size, NativePoint location, bool unpremultiply = false)
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
                    var targetRow = accessor.GetRowSpan(target.Y + y).Slice(target.X, target.Width);
                    sourceRow.CopyTo(targetRow);
                    if (unpremultiply)
                    {
                        Unpremultiply(targetRow);
                    }
                }
            });
        }

        // 1.0 as half-float: the alpha of nearly every pixel of a capture
        private const ushort HalfOne = 0x3C00;

        /// <summary>
        /// Premultiplied to straight alpha: only the partly transparent pixels (e.g. the rounded corners of a window) change
        /// </summary>
        private static void Unpremultiply(Span<HalfVector4> row)
        {
            var packed = MemoryMarshal.Cast<HalfVector4, ulong>(row);
            for (int x = 0; x < packed.Length; x++)
            {
                if ((ushort)(packed[x] >> 48) == HalfOne)
                {
                    continue;
                }
                var pixel = row[x].ToVector4();
                row[x] = pixel.W > 0.0001f
                    ? new HalfVector4(pixel.X / pixel.W, pixel.Y / pixel.W, pixel.Z / pixel.W, pixel.W)
                    : new HalfVector4(0, 0, 0, 0);
            }
        }

        /// <summary>
        /// The pixels with premultiplied alpha, e.g. for Direct2D which takes an FP16 texture only premultiplied; null when every pixel is opaque
        /// (then they are the same). The caller disposes it.
        /// </summary>
        internal Image<HalfVector4> ClonePremultiplied()
        {
            bool opaque = true;
            Pixels.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height && opaque; y++)
                {
                    var packed = MemoryMarshal.Cast<HalfVector4, ulong>(accessor.GetRowSpan(y));
                    for (int x = 0; x < packed.Length; x++)
                    {
                        if ((ushort)(packed[x] >> 48) != HalfOne)
                        {
                            opaque = false;
                            break;
                        }
                    }
                }
            });
            if (opaque)
            {
                return null;
            }
            var premultiplied = Pixels.Clone();
            premultiplied.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < row.Length; x++)
                    {
                        var pixel = row[x].ToVector4();
                        row[x] = new HalfVector4(pixel.X * pixel.W, pixel.Y * pixel.W, pixel.Z * pixel.W, pixel.W);
                    }
                }
            });
            return premultiplied;
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
                                covered.Add((Math.Min(Math.Max(area.Left, 0), width), Math.Min(area.Right, width)));
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
        /// Replace the pixels of an area of this complete HDR image while it is made where the rendered 8-bit image differs from the capture's
        /// 8-bit image (e.g. the teeth of torn edges, a cut mark), at the SDR white level: the unchanged pixels keep their HDR content
        /// </summary>
        private unsafe void ReplaceChangedWithSdr(Bitmap rendered, Bitmap original, NativeRect area)
        {
            if (rendered == null)
            {
                return;
            }
            var target = area.Intersect(new NativeRect(0, 0, Math.Min(Width, Math.Min(rendered.Width, original.Width)), Math.Min(Height, Math.Min(rendered.Height, original.Height))));
            if (target.Width <= 0 || target.Height <= 0)
            {
                return;
            }
            ushort[] table = GetSrgbTable(SdrWhiteLevelInNits);
            var renderedData = rendered.LockBits(target, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                var originalData = original.LockBits(target, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                try
                {
                    Pixels.ProcessPixelRows(accessor =>
                    {
                        for (int y = 0; y < target.Height; y++)
                        {
                            uint* renderedRow = (uint*)((byte*)renderedData.Scan0 + (long)y * renderedData.Stride);
                            uint* originalRow = (uint*)((byte*)originalData.Scan0 + (long)y * originalData.Stride);
                            var targetRow = MemoryMarshal.Cast<HalfVector4, ushort>(accessor.GetRowSpan(target.Y + y).Slice(target.X, target.Width));
                            for (int x = 0; x < target.Width; x++)
                            {
                                if (renderedRow[x] == originalRow[x])
                                {
                                    continue;
                                }
                                // BGRA in memory
                                byte* pixel = (byte*)(renderedRow + x);
                                targetRow[x * 4] = table[pixel[2]];
                                targetRow[x * 4 + 1] = table[pixel[1]];
                                targetRow[x * 4 + 2] = table[pixel[0]];
                                targetRow[x * 4 + 3] = HalfHelper.FloatToHalf(pixel[3] / 255f);
                            }
                        }
                    });
                }
                finally
                {
                    original.UnlockBits(originalData);
                }
            }
            finally
            {
                rendered.UnlockBits(renderedData);
            }
        }

        /// <summary>
        /// Fill the transparent pixels of the tiles of this complete HDR image while it is made from the 8-bit image of the same size, at the SDR
        /// white level: the cleared pixels (see <see cref="ClearPixels"/>), partly cleared ones (a resize mixed them) go over the 8-bit pixel. A pixel
        /// which is transparent in the capture is transparent in it too.
        /// </summary>
        private unsafe void FillTransparentFromSdr(Bitmap source, NativeRect[] tileAreas)
        {
            ushort[] table = GetSrgbTable(SdrWhiteLevelInNits);
            var bounds = new NativeRect(0, 0, Math.Min(Width, source.Width), Math.Min(Height, source.Height));
            var bitmapData = source.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                Pixels.ProcessPixelRows(accessor =>
                {
                    foreach (var area in tileAreas.Select(tileArea => tileArea.Intersect(bounds)).Where(tileArea => tileArea.Width > 0 && tileArea.Height > 0))
                    {
                        for (int y = area.Top; y < area.Bottom; y++)
                        {
                            byte* sourceRow = (byte*)bitmapData.Scan0 + (long)y * bitmapData.Stride;
                            var targetRow = MemoryMarshal.Cast<HalfVector4, ushort>(accessor.GetRowSpan(y));
                            for (int x = area.Left; x < area.Right; x++)
                            {
                                byte* pixel = sourceRow + x * 4;
                                // A half of 0 (or -0) is a cleared alpha: the 8-bit pixel
                                if ((targetRow[x * 4 + 3] & 0x7FFF) == 0)
                                {
                                    targetRow[x * 4] = table[pixel[2]];
                                    targetRow[x * 4 + 1] = table[pixel[1]];
                                    targetRow[x * 4 + 2] = table[pixel[0]];
                                    targetRow[x * 4 + 3] = HalfHelper.FloatToHalf(pixel[3] / 255f);
                                    continue;
                                }
                                // Partly cleared (e.g. a resize mixed a cleared pixel in): over the 8-bit pixel, in linear light
                                float alpha = HalfHelper.HalfToFloat(targetRow[x * 4 + 3]);
                                if (!(alpha < 1f))
                                {
                                    continue;
                                }
                                float sdrAlpha = pixel[3] / 255f * (1f - alpha);
                                float outAlpha = alpha + sdrAlpha;
                                for (int channel = 0; channel < 3; channel++)
                                {
                                    float hdr = HalfHelper.HalfToFloat(targetRow[x * 4 + channel]);
                                    float sdr = HalfHelper.HalfToFloat(table[pixel[2 - channel]]);
                                    targetRow[x * 4 + channel] = HalfHelper.FloatToHalf(outAlpha > 0f ? (hdr * alpha + sdr * sdrAlpha) / outAlpha : 0f);
                                }
                                targetRow[x * 4 + 3] = HalfHelper.FloatToHalf(outAlpha);
                            }
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
        /// Draw an element with ImageSharp on the area of this complete HDR image while it is made: on float pixels (ImageSharp mixes HalfVector4
        /// wrongly, see FloatPixels), its white at the value of white (scRGB)
        /// </summary>
        private void DrawOver(NativeRect area, Action<Image, ImageValues> draw, float white)
        {
            if (area.Width <= 0 || area.Height <= 0 || area.X < 0 || area.Y < 0 || area.Right > Width || area.Bottom > Height)
            {
                return;
            }
            var rectangle = new Rectangle(area.X, area.Y, area.Width, area.Height);
            using var canvas = FloatPixels.Of(Pixels, rectangle);
            draw(canvas, ImageValues.Linear(white));
            Pixels.ProcessPixelRows(canvas, (target, drawn) =>
            {
                for (int y = 0; y < drawn.Height; y++)
                {
                    var targetRow = target.GetRowSpan(area.Y + y).Slice(area.X, area.Width);
                    var drawnRow = drawn.GetRowSpan(y);
                    for (int x = 0; x < targetRow.Length; x++)
                    {
                        targetRow[x].FromVector4(drawnRow[x].ToVector4());
                    }
                }
            });
        }

        /// <summary>
        /// Draw an 8-bit layer with alpha over this complete HDR image while it is made: its white at the value of white (scRGB), blended in linear light
        /// (scRGB is linear), so its shadow darkens the HDR pixels under it
        /// </summary>
        private unsafe void BlendSdrOver(Bitmap layer, NativePoint location, float white)
        {
            var target = new NativeRect(location, new NativeSize(layer.Width, layer.Height)).Intersect(new NativeRect(0, 0, Width, Height));
            if (target.Width <= 0 || target.Height <= 0)
            {
                return;
            }
            float scale = white;
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
                            // BGRA in memory, straight alpha: "over" with the alpha of both (under is opaque on a capture, not in a torn edge)
                            float underWeight = under.W * (1 - a);
                            float outAlpha = a + underWeight;
                            targetRow[x] = new HalfVector4(
                                (linear[sourceRow[2]] * a + under.X * underWeight) / outAlpha,
                                (linear[sourceRow[1]] * a + under.Y * underWeight) / outAlpha,
                                (linear[sourceRow[0]] * a + under.Z * underWeight) / outAlpha,
                                outAlpha);
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
