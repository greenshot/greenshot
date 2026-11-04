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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Numerics;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.ImageSharpDrawing;
using SixLabors.ImageSharp.PixelFormats;

namespace Greenshot.Base.Effects
{
    /// <summary>
    /// A change of an image (a crop, an effect) as a description which can be done on another image of the same capture, see <see cref="ImageEdits"/>:
    /// the editor keeps the edits of its images and does them on the HDR image only when it is needed (an HDR file is written), see ISurface.HdrImage.
    /// Inside they are ImageSharp operations, which work for any pixel type: when the 8-bit image is an ImageSharp image too, it can get an Apply of its own.
    /// </summary>
    public interface IImageEdit
    {
        /// <summary>
        /// What it does, for the log
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The HDR image after the edit, as a new HDR image (the given one stays the caller's); null when no HDR content is left
        /// </summary>
        HdrImage Apply(HdrImage hdrImage);
    }

    /// <summary>
    /// The edits for the changes of the editor, done on the HDR image. The editor's HDR image is complete (see HdrImage.Complete): what an edit
    /// adds around it which isn't in the HDR image (the room of a border, a shadow, a bigger canvas) is taken from the new 8-bit image at the SDR
    /// white level when the edit is made (see <see cref="Room"/>), so it stays complete.
    /// </summary>
    public static class ImageEdits
    {
        /// <summary>
        /// An area of the HDR image at a place of a new image of the size, e.g. a crop (the area at 0,0 of an image of its size),
        /// a border (all of it, moved by the border width). Around it the new image has no HDR pixels, see <see cref="Room"/>.
        /// </summary>
        public static IImageEdit Place(NativeRect area, NativeSize size, NativePoint location) =>
            new ImageEdit($"Place {area} at {location} of {size.Width}x{size.Height}", hdrImage =>
            {
                var part = hdrImage.Crop(area);
                if (part == null || (location.X == 0 && location.Y == 0 && part.Width == size.Width && part.Height == size.Height))
                {
                    return part;
                }
                using (part)
                {
                    return HdrImage.Combine(size.Width, size.Height, new[] { (part, location) });
                }
            });

        /// <summary>
        /// Like <see cref="Place"/>, around the area the pixels of the new 8-bit image (e.g. the border, the shadow, the background of the room for
        /// torn edges): the HDR image stays complete
        /// </summary>
        /// <param name="area">The area of the HDR image</param>
        /// <param name="newImage">The new 8-bit image, read now</param>
        /// <param name="location">Where the area is in the new image</param>
        public static IImageEdit PlaceInRoom(NativeRect area, Image newImage, NativePoint location)
        {
            var size = new NativeSize(newImage.Width, newImage.Height);
            var placed = new NativeRect(location, area.Size);
            return Place(area, size, location).Then(Room(newImage, HdrImage.Outside(new NativeRect(0, 0, size.Width, size.Height), new[] { placed })));
        }

        /// <summary>
        /// Where the HDR image has no pixels, the pixels of these areas of the 8-bit image at the SDR white level: e.g. the room which a border adds.
        /// The pixels are read now and kept with the edit, which can be done again later (e.g. after an undo) on another thread.
        /// </summary>
        /// <param name="image">The 8-bit image, of the size of the HDR image</param>
        /// <param name="areas">The areas, clipped to the image</param>
        public static IImageEdit Room(Image image, IEnumerable<NativeRect> areas)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            var bounds = new NativeRect(0, 0, image.Width, image.Height);
            var parts = new List<(NativeRect, Bgra32[])>();
            // Read where they are: a copy only of an image which isn't a bitmap (e.g. a metafile)
            using var copy = image is Bitmap ? null : new Bitmap(image);
            var bitmap = copy ?? (Bitmap)image;
            foreach (var area in areas.Select(area => area.Intersect(bounds)).Where(area => area.Width > 0 && area.Height > 0))
            {
                var pixels = new Bgra32[area.Width * area.Height];
                BitmapPixels.ProcessPixelRows<Bgra32>(bitmap, new Rectangle(area.X, area.Y, area.Width, area.Height), rows =>
                {
                    for (int y = 0; y < rows.Height; y++)
                    {
                        rows.GetRowSpan(y).CopyTo(pixels.AsSpan(y * area.Width, area.Width));
                    }
                }, ImageLockMode.ReadOnly);
                parts.Add((area, pixels));
            }
            return new ImageEdit($"Room {string.Join(", ", parts.Select(part => part.Item1))} from the 8-bit image", hdrImage => hdrImage.Complete(parts));
        }

        /// <summary>
        /// Two areas of the HDR image put together in a new image of the size, e.g. a cut out (the parts before and after the band). When they
        /// cover all of it, the new HDR image is complete.
        /// </summary>
        public static IImageEdit Join(NativeSize size, NativeRect first, NativePoint firstLocation, NativeRect second, NativePoint secondLocation) =>
            new ImageEdit($"Join {first} and {second} into {size.Width}x{size.Height}", hdrImage =>
            {
                using var firstPart = first.Width > 0 && first.Height > 0 ? hdrImage.Crop(first) : null;
                using var secondPart = second.Width > 0 && second.Height > 0 ? hdrImage.Crop(second) : null;
                var parts = new List<(HdrImage, NativePoint)>();
                if (firstPart != null)
                {
                    parts.Add((firstPart, firstLocation));
                }
                if (secondPart != null)
                {
                    parts.Add((secondPart, secondLocation));
                }
                var joined = HdrImage.Combine(size.Width, size.Height, parts);
                if (joined == null)
                {
                    return null;
                }
                joined.IsRequested = hdrImage.IsRequested;
                if (joined.IsComplete || joined.TileAreas.Sum(area => (long)area.Width * area.Height) != (long)size.Width * size.Height)
                {
                    return joined;
                }
                using (joined)
                {
                    return joined.Complete(Array.Empty<(NativeRect, Bgra32[])>());
                }
            });

        /// <summary>
        /// The areas are taken from the 8-bit image, e.g. an element drawn into it (torn edges, cut marks)
        /// </summary>
        public static IImageEdit Exclude(IReadOnlyList<NativeRect> areas) =>
            new ImageEdit($"Exclude {string.Join(", ", areas)}", hdrImage => hdrImage.Exclude(areas));

        /// <summary>
        /// The HDR content is gone, e.g. the image was cleared: nothing is left to make, the image has no HDR content (not lost, there is none)
        /// </summary>
        public static readonly IImageEdit Remove = new ImageEdit("Remove the HDR content", _ => null);

        public static IImageEdit Rotate(int angle) => new ImageEdit($"Rotate {angle}", hdrImage => hdrImage.Rotate(angle));

        public static IImageEdit Resize(NativeSize size) => new ImageEdit($"Resize to {size.Width}x{size.Height}", hdrImage => hdrImage.Resize(size.Width, size.Height));

        /// <summary>
        /// An element drawn into the image for good (torn edges, a cut mark which is applied to the image): its steps (see ISurface.CreateHdrOverlay)
        /// are drawn on the HDR pixels, as when the HDR file is written with the element (see HdrImage.Draw)
        /// </summary>
        /// <param name="name">What is drawn, for the log</param>
        /// <param name="steps">What the element does: the cut of its filter, then its drawing</param>
        public static IImageEdit Draw(string name, IReadOnlyList<HdrOverlayStep> steps)
        {
            if (steps == null) throw new ArgumentNullException(nameof(steps));
            return new ImageEdit($"Draw {name}", hdrImage => hdrImage.Draw(steps));
        }

        /// <summary>
        /// One edit after the other
        /// </summary>
        public static IImageEdit Then(this IImageEdit first, IImageEdit second) =>
            new ImageEdit($"{first.Name}, {second.Name}", hdrImage =>
            {
                using var between = first.Apply(hdrImage);
                return between == null ? null : second.Apply(between);
            });

        /// <summary>
        /// The edit of the HDR image for an effect of the editor which made the new image of the old one: like the one for the sizes, but the
        /// torn edge effect is drawn on the HDR pixels (its teeth follow no rectangle), and the room which a border, a shadow or a bigger canvas
        /// adds is taken from the new image
        /// </summary>
        public static IImageEdit ForEffect(IEffect effect, Image oldImage, Image newImage, Matrix matrix)
        {
            var oldSize = new NativeSize(oldImage.Width, oldImage.Height);
            var newSize = new NativeSize(newImage.Width, newImage.Height);
            // TornEdgeEffect is a DropShadowEffect: it draws itself, see below
            if (effect is not TornEdgeEffect && effect is BorderEffect or DropShadowEffect or ResizeCanvasEffect)
            {
                var offset = matrix == null ? new NativePoint(0, 0) : new NativePoint((int)Math.Round(matrix.OffsetX), (int)Math.Round(matrix.OffsetY));
                return PlaceInRoom(new NativeRect(0, 0, oldSize.Width, oldSize.Height), newImage, offset);
            }
            if (effect is not TornEdgeEffect tornEdge)
            {
                return ForEffect(effect, oldSize, newSize, matrix);
            }
            var tornOffset = tornEdge.GenerateShadow && matrix != null ? new NativePoint((int)Math.Round(matrix.OffsetX), (int)Math.Round(matrix.OffsetY)) : new NativePoint(0, 0);

            // The HDR image is drawn for real (ImageSharp): cut by the same teeth, over its shadow and the background
            var outline = ImageHelper.CreateTornEdgeOutline(oldSize.Width, oldSize.Height, tornEdge.ToothHeight, tornEdge.HorizontalToothRange, tornEdge.VerticalToothRange,
                tornEdge.Edges ?? new[] { true, true, true, true }, tornEdge.Seed);
            var shape = new SixLabors.ImageSharp.Drawing.Polygon(outline.Select(point => new SixLabors.ImageSharp.PointF(point.X, point.Y)).ToArray());
            // Like ImageHelper.CreateShadow: with the blur of GDI+ (odd size) the shadow is 0.1 darker
            int oddShadowSize = (tornEdge.ShadowSize & 1) == 0 ? tornEdge.ShadowSize + 1 : tornEdge.ShadowSize;
            float darkness = Dapplo.Windows.Gdi32.GdiPlusApi.IsBlurPossible(oddShadowSize) ? tornEdge.Darkness + 0.1f : tornEdge.Darkness;
            (float, int)? shadow = tornEdge.GenerateShadow ? (darkness, tornEdge.ShadowSize) : null;
            var backgroundColor = tornEdge.BackgroundColor;

            // Only a complete HDR image, as the editor has it: of one with only the parts of HDR displays nothing is left
            return new ImageEdit($"Torn edges (seed {tornEdge.Seed})", hdrImage => hdrImage.Redraw((pixels, white) =>
            {
                var background = backgroundColor.A == 0
                    ? Vector4.Zero
                    : new Vector4(LinearColorEffects.Decode(backgroundColor.R / 255f) * white, LinearColorEffects.Decode(backgroundColor.G / 255f) * white,
                        LinearColorEffects.Decode(backgroundColor.B / 255f) * white, backgroundColor.A / 255f);
                return CutDrawing.TornEdge(pixels, shape, new SixLabors.ImageSharp.Size(newSize.Width, newSize.Height), new SixLabors.ImageSharp.Point(tornOffset.X, tornOffset.Y),
                    shadow, background);
            }));
        }

        /// <summary>
        /// The part of the image which the teeth of the torn edge effect don't reach
        /// </summary>
        private static NativeRect TornEdgeInside(TornEdgeEffect tornEdge, NativeSize size)
        {
            int tooth = Math.Max(tornEdge.ToothHeight, 0);
            bool[] edges = tornEdge.Edges ?? new[] { true, true, true, true };
            int top = edges.Length > 0 && edges[0] ? tooth : 0;
            int right = edges.Length > 1 && edges[1] ? tooth : 0;
            int bottom = edges.Length > 2 && edges[2] ? tooth : 0;
            int left = edges.Length > 3 && edges[3] ? tooth : 0;
            return new NativeRect(left, top, Math.Max(size.Width - left - right, 0), Math.Max(size.Height - top - bottom, 0));
        }

        /// <summary>
        /// The edit of the HDR image for an effect of the editor, null when the effect can't be done on the HDR image (monochrome, reduce colors:
        /// their result is 8-bit by nature, the capture loses its HDR content then)
        /// </summary>
        /// <param name="effect">The effect</param>
        /// <param name="oldSize">The size of the image before</param>
        /// <param name="newSize">The size of the image the effect made</param>
        /// <param name="matrix">How the effect moved the image (its offset in the new one), as the elements are moved</param>
        public static IImageEdit ForEffect(IEffect effect, NativeSize oldSize, NativeSize newSize, Matrix matrix)
        {
            var all = new NativeRect(0, 0, oldSize.Width, oldSize.Height);
            var offset = matrix == null ? new NativePoint(0, 0) : new NativePoint((int)Math.Round(matrix.OffsetX), (int)Math.Round(matrix.OffsetY));
            switch (effect)
            {
                case RotateEffect rotate:
                    return Rotate(rotate.Angle);
                case ResizeEffect:
                    return Resize(newSize);
                case TornEdgeEffect tornEdge:
                    // Without the images (see the other ForEffect): the bands of the teeth come from the 8-bit image, the shadow (offset) moves the rest
                    var inside = TornEdgeInside(tornEdge, oldSize);
                    if (inside.Width <= 0 || inside.Height <= 0)
                    {
                        return new ImageEdit("Torn edges over all of it", _ => null);
                    }
                    var tornOffset = tornEdge.GenerateShadow ? offset : new NativePoint(0, 0);
                    return Place(inside, newSize, new NativePoint(tornOffset.X + inside.X, tornOffset.Y + inside.Y));
                case BorderEffect:
                case DropShadowEffect:
                case ResizeCanvasEffect:
                    // The image as it is, moved into the bigger one; the border, the shadow, the canvas come from the 8-bit image
                    return Place(all, newSize, offset);
                case GrayscaleEffect:
                    return Colors("Grayscale", (context, _) => context.LinearGrayscale());
                case InvertEffect:
                    return Colors("Invert", (context, white) => context.LinearInvert(white));
                case AdjustEffect adjust:
                    var curve = AdjustCurve(adjust.Brightness, adjust.Contrast, adjust.Gamma);
                    return Colors($"Adjust brightness {adjust.Brightness}, contrast {adjust.Contrast}, gamma {adjust.Gamma}", (context, white) => context.LinearCurve(white, curve));
                case RemoveTransparencyEffect removeTransparency:
                    var color = removeTransparency.Color;
                    var background = new Vector3(color.R / 255f, color.G / 255f, color.B / 255f);
                    return new ImageEdit($"Remove transparency on {color}", hdrImage => hdrImage.Process((context, white) =>
                        context.LinearBackground(white, background)));
                default:
                    return null;
            }
        }

        private static IImageEdit Colors(string name, Action<SixLabors.ImageSharp.Processing.IImageProcessingContext, float> process) =>
            new ImageEdit(name, hdrImage => hdrImage.Process(process));

        /// <summary>
        /// What the adjust effect does to each gray level of the 8-bit image (GDI+ color matrix and gamma), as a curve: the HDR image follows
        /// the 8-bit image exactly in the SDR range
        /// </summary>
        public static float[] AdjustCurve(float brightness, float contrast, float gamma)
        {
            using var gradient = new Bitmap(256, 1, PixelFormat.Format24bppRgb);
            for (int level = 0; level < 256; level++)
            {
                gradient.SetPixel(level, 0, Color.FromArgb(level, level, level));
            }
            using var adjusted = (Bitmap)ImageHelper.Adjust(gradient, brightness, contrast, gamma);
            return LinearColorEffects.CurveOf(level => adjusted.GetPixel(level, 0).G);
        }

        private sealed class ImageEdit : IImageEdit
        {
            private readonly Func<HdrImage, HdrImage> _apply;

            public ImageEdit(string name, Func<HdrImage, HdrImage> apply)
            {
                Name = name;
                _apply = apply;
            }

            public string Name { get; }

            public HdrImage Apply(HdrImage hdrImage) => _apply(hdrImage ?? throw new ArgumentNullException(nameof(hdrImage)));

            public override string ToString() => Name;
        }
    }
}
