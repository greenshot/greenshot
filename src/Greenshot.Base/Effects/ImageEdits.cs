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
using System.Numerics;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;

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
    /// The edits for the changes of the editor, done on the HDR image. What isn't in the HDR image (the room around it, e.g. a border or a shadow,
    /// the torn teeth) comes from the 8-bit image when the HDR file is written (HdrImage.Compose), like an SDR display.
    /// </summary>
    public static class ImageEdits
    {
        /// <summary>
        /// An area of the HDR image at a place of a new image of the size, e.g. a crop (the area at 0,0 of an image of its size),
        /// a border (all of it, moved by the border width)
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
        /// Two areas of the HDR image put together in a new image of the size, e.g. a cut out (the parts before and after the band)
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
                if (joined != null)
                {
                    joined.IsRequested = hdrImage.IsRequested;
                }
                return joined;
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
                    // The teeth are cut into the image: those bands come from the 8-bit image, the shadow (offset) moves the rest
                    int tooth = Math.Max(tornEdge.ToothHeight, 0);
                    bool[] edges = tornEdge.Edges ?? new[] { true, true, true, true };
                    int top = edges.Length > 0 && edges[0] ? tooth : 0;
                    int right = edges.Length > 1 && edges[1] ? tooth : 0;
                    int bottom = edges.Length > 2 && edges[2] ? tooth : 0;
                    int left = edges.Length > 3 && edges[3] ? tooth : 0;
                    var inside = new NativeRect(left, top, oldSize.Width - left - right, oldSize.Height - top - bottom);
                    if (inside.Width <= 0 || inside.Height <= 0)
                    {
                        return new ImageEdit("Torn edges over all of it", _ => null);
                    }
                    var tornOffset = tornEdge.GenerateShadow ? offset : new NativePoint(0, 0);
                    return Place(inside, newSize, new NativePoint(tornOffset.X + left, tornOffset.Y + top));
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
                    return Colors($"Remove transparency on {color}", (context, white) => context.LinearBackground(white, background));
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
        internal static float[] AdjustCurve(float brightness, float contrast, float gamma)
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
