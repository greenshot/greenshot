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
    /// One image of the whole capture, like its 8-bit image: the parts of SDR displays are put in from the 8-bit image at the SDR white level
    /// when the capture is made (<see cref="Combine(Bitmap, IEnumerable{ValueTuple{HdrImage, NativePoint}})"/>). It belongs to the capture next
    /// to its tone-mapped 8-bit image (ICapture.HdrImage), the surface takes it over with the image (ISurface.HdrImage); the owner disposes it.
    /// The pixels are filled while the HDR image is made and not changed after it was handed on: copies of a capture or a surface share them
    /// (<see cref="Share"/>), each with its own HdrImage; the pixels are released with the last one.
    /// </remarks>
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
        /// The HDR image of a capture: its 8-bit image at the SDR white level (the parts of SDR displays) with the HDR image of each HDR display
        /// at its place over it, as one image of the size of the 8-bit image. One part which covers all of it is shared, not copied. The parts
        /// stay the caller's to dispose.
        /// </summary>
        /// <param name="sdrImage">The 8-bit image of the capture</param>
        /// <param name="parts">The HDR image of each HDR display in the capture, with its place in it</param>
        /// <returns>The HDR image, null without a part</returns>
        public static HdrImage Combine(Bitmap sdrImage, IEnumerable<(HdrImage Part, NativePoint Location)> parts)
        {
            if (sdrImage == null) throw new ArgumentNullException(nameof(sdrImage));
            var list = (parts ?? Enumerable.Empty<(HdrImage, NativePoint)>()).Where(part => part.Item1 != null).ToList();
            if (list.Count == 1 && list[0].Item2.X == 0 && list[0].Item2.Y == 0 && list[0].Item1.Width == sdrImage.Width && list[0].Item1.Height == sdrImage.Height)
            {
                return list[0].Item1.Share();
            }
            return Combine(sdrImage.Width, sdrImage.Height, list, combined => combined.CopyFromSdr(sdrImage));
        }

        /// <summary>
        /// Parts of HDR images at their places in a new HDR image of the size, e.g. an edit of the editor which moves the image into a bigger one
        /// (see ImageEdits.Place): where no part is, the 8-bit pixels of the areas at the SDR white level (e.g. the room which a border adds),
        /// what neither covers is transparent. The parts stay the caller's to dispose.
        /// </summary>
        /// <param name="size">The size of the new image</param>
        /// <param name="sdrAreas">Areas of the new 8-bit image and their pixels (row by row, straight alpha)</param>
        /// <param name="parts">The HDR images with their places in the new image</param>
        /// <returns>The HDR image, null without a part</returns>
        public static HdrImage Combine(NativeSize size, IReadOnlyList<(NativeRect Area, Bgra32[] Pixels)> sdrAreas, IEnumerable<(HdrImage Part, NativePoint Location)> parts)
        {
            var list = (parts ?? Enumerable.Empty<(HdrImage, NativePoint)>()).Where(part => part.Item1 != null).ToList();
            return Combine(CheckSize(size.Width), CheckSize(size.Height), list, combined => combined.CopyFromSdr(sdrAreas ?? Array.Empty<(NativeRect, Bgra32[])>()));
        }

        private static HdrImage Combine(int width, int height, IReadOnlyList<(HdrImage Part, NativePoint Location)> parts, Action<HdrImage> fillFromSdr)
        {
            if (parts.Count == 0)
            {
                return null;
            }
            var first = parts[0].Part;
            var combined = new HdrImage(width, height, first.SdrWhiteLevelInNits, parts.Max(part => part.Part.DisplayPeakInNits))
            {
                IsRequested = parts.Any(part => part.Part.IsRequested)
            };
            try
            {
                fillFromSdr(combined);
                foreach (var (part, location) in parts)
                {
                    combined.CopyFrom(part.Pixels, location);
                }
                return combined;
            }
            catch
            {
                combined.Dispose();
                throw;
            }
        }

        public int Width => _shared.Pixels.Width;

        public int Height => _shared.Pixels.Height;

        /// <summary>
        /// The bytes of a row of the HDR image
        /// </summary>
        public int Stride => Width * BytesPerPixel;

        /// <summary>
        /// The SDR white level of the (first) HDR display: SDR content (normal windows, SDR displays) has this brightness
        /// </summary>
        public float SdrWhiteLevelInNits { get; }

        /// <summary>
        /// The brightest the displays can show, 0 when unknown
        /// </summary>
        public float DisplayPeakInNits { get; }

        /// <summary>
        /// The pixels, shared with its copies: only read them once it was handed on
        /// </summary>
        public Image<HalfVector4> Pixels
        {
            get
            {
                CheckNotDisposed();
                return _shared.Pixels;
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
        /// A new HDR image of these pixels, with the levels of this one
        /// </summary>
        private HdrImage With(Image<HalfVector4> pixels) => new HdrImage(new SharedPixels(pixels), SdrWhiteLevelInNits, DisplayPeakInNits) { IsRequested = IsRequested };

        /// <summary>
        /// Steps of an overlay (see HdrOverlay.Steps) drawn into the pixels for good, as a new HDR image: e.g. torn edges or a cut mark which is
        /// applied to the image in the editor. The same as <see cref="Compose"/> does with them, the white of the elements too, so the HDR file
        /// stays the same.
        /// </summary>
        /// <param name="steps">The steps, in their order</param>
        public HdrImage Draw(IReadOnlyList<HdrOverlayStep> steps)
        {
            if (steps == null) throw new ArgumentNullException(nameof(steps));
            var drawn = With(Pixels.Clone());
            try
            {
                drawn.DrawSteps(steps);
                return drawn;
            }
            catch
            {
                drawn.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Do the steps of an overlay on this HDR image while it is made, in their order: a filter works on what is drawn before it
        /// </summary>
        private void DrawSteps(IEnumerable<HdrOverlayStep> steps)
        {
            var (peakInNits, highlightShare) = HdrContent.Measure(this, SdrWhiteLevelInNits);
            float elementWhite = ElementWhite(peakInNits, highlightShare);
            foreach (var step in steps)
            {
                if (step.Draw != null)
                {
                    DrawOver(new NativeRect(step.Location, step.Size), step.Draw, elementWhite);
                }
                else
                {
                    step.Filter.Apply(Pixels, SdrWhiteLevelInNits / 80f);
                }
            }
        }

        /// <summary>
        /// The white of the elements: halfway (geometric) between SDR white and the white of the capture as the editor shows it (the white level of
        /// its tone mapping, HdrImageToneMapping). At SDR white a white element looks gray next to bright HDR content, at the white of the tone
        /// mapping (up to the display's peak) it is glaring.
        /// </summary>
        /// <returns>The white as scRGB value (1.0 = 80 nits)</returns>
        private float ElementWhite(float peakInNits, float highlightShare)
        {
            float toneMappingWhite = HdrContent.WhiteLevelInNits(SdrWhiteLevelInNits, DisplayPeakInNits, peakInNits, highlightShare);
            return (float)Math.Sqrt(SdrWhiteLevelInNits * toneMappingWhite) / 80f;
        }

        // 0: not measured yet, 1: with HDR content, 2: without
        private int _hasHdrContent;

        /// <summary>
        /// Something an 8-bit sRGB file can't show: a highlight (brighter than SDR white) or a color outside sRGB. Without it (e.g. a normal window
        /// on an HDR display) an HDR file would only look like the normal one, so the capture counts as one without HDR content: no HDR file,
        /// no HDR badge, payload.hdr is None. Measured once, when it is first asked for.
        /// </summary>
        public bool HasHdrContent
        {
            get
            {
                CheckNotDisposed();
                int known = Volatile.Read(ref _hasHdrContent);
                if (known == 0)
                {
                    known = Native.HdrContent.HasHdrContent(_shared.Pixels, SdrWhiteLevelInNits) ? 1 : 2;
                    Volatile.Write(ref _hasHdrContent, known);
                }
                return known == 1;
            }
        }

        /// <summary>
        /// The HDR image rotated like the editor rotates the capture (RotateEffect), as a new HDR image
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
            return With(Pixels.Clone(context => context.Rotate(clockwise ? RotateMode.Rotate90 : RotateMode.Rotate270)));
        }

        /// <summary>
        /// The HDR image resized like the editor resizes the capture (ResizeEffect: bicubic, the whole image to the new size), as a new HDR image
        /// </summary>
        public HdrImage Resize(int width, int height)
        {
            CheckNotDisposed();
            // On float pixels: ImageSharp would mix HalfVector4 on its scaled values (see FloatPixels)
            return With(FloatPixels.Clone(Pixels, context => context.Resize(CheckSize(width), CheckSize(height), KnownResamplers.Bicubic)));
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
        /// The HDR image drawn anew from this one (e.g. the torn edge effect, see ImageSharpDrawing.CutDrawing), as a new HDR image
        /// </summary>
        /// <param name="draw">Makes the new pixels from the pixels of this one and the SDR white as scRGB value; it must use their configuration</param>
        public HdrImage Redraw(Func<Image<HalfVector4>, float, Image<HalfVector4>> draw)
        {
            if (draw == null) throw new ArgumentNullException(nameof(draw));
            CheckNotDisposed();
            return With(draw(Pixels, SdrWhiteLevelInNits / 80f));
        }

        /// <summary>
        /// The HDR image with its pixels changed (e.g. a color effect, see ImageEdits), as a new HDR image; this one stays as it is
        /// </summary>
        /// <param name="process">Changes a copy of the pixels; it gets the SDR white as scRGB value (1.0 = 80 nits)</param>
        public HdrImage Process(Action<IImageProcessingContext, float> process)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));
            CheckNotDisposed();
            float white = SdrWhiteLevelInNits / 80f;
            return With(Pixels.Clone(context => process(context, white)));
        }

        /// <summary>
        /// An area of the HDR image as a new HDR image
        /// </summary>
        /// <param name="area">The area in pixel coordinates, it is clipped to the image</param>
        /// <returns>The new HDR image, null when nothing of the area is in the image</returns>
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
            return With(Pixels.Clone(context => context.Crop(new Rectangle(clipped.X, clipped.Y, clipped.Width, clipped.Height))));
        }

        /// <summary>
        /// The same HDR image for another owner, e.g. a copy of the capture or the surface: the pixels are shared, not copied.
        /// It doesn't belong to an image yet (see <see cref="BelongTo"/>), and it is disposed by its owner like any other.
        /// </summary>
        public HdrImage Share()
        {
            CheckNotDisposed();
            return new HdrImage(_shared.AddReference(), SdrWhiteLevelInNits, DisplayPeakInNits) { IsRequested = IsRequested };
        }

        /// <summary>
        /// The HDR image to encode it: with the elements drawn on the capture, or this one shared when there are none
        /// </summary>
        /// <param name="overlay">What is drawn on the capture (see ISurface.CreateHdrOverlay), put over it with white halfway between SDR white and the white of the capture as the editor shows it; null for nothing</param>
        /// <returns>The HDR image, the caller disposes it</returns>
        public HdrImage Compose(HdrOverlay overlay = null)
        {
            CheckNotDisposed();
            if (overlay == null)
            {
                return Share();
            }
            var composed = With(Pixels.Clone());
            composed.IsRequested = false;
            try
            {
                composed.DrawSteps(overlay.Steps);
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
        /// Put all pixels of an 8-bit image into this HDR image while it is made, at the SDR white level, clipped to both
        /// </summary>
        private unsafe void CopyFromSdr(Bitmap source)
        {
            int width = Math.Min(Width, source.Width);
            int height = Math.Min(Height, source.Height);
            ushort[] table = GetSrgbTable(SdrWhiteLevelInNits);
            var bitmapData = source.LockBits(new NativeRect(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                Pixels.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < height; y++)
                    {
                        // BGRA in memory, like Bgra32
                        var sourceRow = new ReadOnlySpan<Bgra32>((byte*)bitmapData.Scan0 + (long)y * bitmapData.Stride, width);
                        ConvertSdr(sourceRow, accessor.GetRowSpan(y).Slice(0, width), table);
                    }
                });
            }
            finally
            {
                source.UnlockBits(bitmapData);
            }
        }

        /// <summary>
        /// Put the 8-bit pixels of the areas into this HDR image while it is made, at the SDR white level, clipped to it
        /// </summary>
        private void CopyFromSdr(IReadOnlyList<(NativeRect Area, Bgra32[] Pixels)> sdrAreas)
        {
            ushort[] table = GetSrgbTable(SdrWhiteLevelInNits);
            var bounds = new NativeRect(0, 0, Width, Height);
            Pixels.ProcessPixelRows(accessor =>
            {
                foreach (var (area, pixels) in sdrAreas)
                {
                    if (pixels == null || pixels.Length < (long)area.Width * area.Height) throw new ArgumentException($"The pixels of {area} are missing.", nameof(sdrAreas));
                    var inside = area.Intersect(bounds);
                    for (int y = inside.Top; y < inside.Bottom; y++)
                    {
                        var sourceRow = new ReadOnlySpan<Bgra32>(pixels, (y - area.Top) * area.Width + inside.Left - area.Left, inside.Width);
                        ConvertSdr(sourceRow, accessor.GetRowSpan(y).Slice(inside.Left, inside.Width), table);
                    }
                }
            });
        }

        /// <summary>
        /// 8-bit sRGB pixels as scRGB half-floats by the table (see <see cref="GetSrgbTable"/>), the alpha as it is
        /// </summary>
        private static void ConvertSdr(ReadOnlySpan<Bgra32> source, Span<HalfVector4> target, ushort[] table)
        {
            ushort one = HalfHelper.FloatToHalf(1f);
            // HalfVector4 is R, G, B, A as half-floats in memory
            var halves = MemoryMarshal.Cast<HalfVector4, ushort>(target);
            for (int x = 0; x < source.Length; x++)
            {
                var pixel = source[x];
                halves[x * 4] = table[pixel.R];
                halves[x * 4 + 1] = table[pixel.G];
                halves[x * 4 + 2] = table[pixel.B];
                halves[x * 4 + 3] = pixel.A == 255 ? one : HalfHelper.FloatToHalf(pixel.A / 255f);
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
        /// Done with this HDR image: the pixels are released when no other copy (see <see cref="Share"/>) uses them
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                _shared.Release();
            }
        }

        private void CheckNotDisposed()
        {
            if (Volatile.Read(ref _disposed) != 0) throw new ObjectDisposedException(nameof(HdrImage));
        }

        /// <summary>
        /// The pixels with the number of HDR images which use them
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
