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
using System.Numerics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Greenshot.Base.Effects
{
    /// <summary>
    /// The filter of an element (e.g. a highlighter, a magnifier) as ImageSharp processing on an area of an image, for any pixel type; used for
    /// the HDR image of an HDR file (HdrOverlay.Filters, see HdrImage.Compose)
    /// </summary>
    public sealed class ImageFilter
    {
        /// <param name="name">What it does, for the log</param>
        /// <param name="apply">Changes the pixels of the image; gets the value of white (for an HDR image the SDR white in scRGB, 1.0 = 80 nits)</param>
        public ImageFilter(string name, Action<Image, float> apply)
        {
            Name = name;
            Apply = apply ?? throw new ArgumentNullException(nameof(apply));
        }

        public string Name { get; }

        public Action<Image, float> Apply { get; }

        public override string ToString() => Name;

        /// <summary>
        /// Copy an area of the image from the source (e.g. a blurred copy of the part around it), for any pixel type of both
        /// </summary>
        /// <param name="target">The image</param>
        /// <param name="source">The pixels, the source shows the image from sourceOffset on</param>
        /// <param name="sourceOffset">Where the source is in the image</param>
        /// <param name="area">The area of the image to copy, inside the source</param>
        public static void CopyArea(Image target, Image source, Point sourceOffset, Rectangle area)
        {
            if (area.Width <= 0 || area.Height <= 0)
            {
                return;
            }
            // The pixels of the area as Vector4, then into the image: works for every pixel type, lossless for HalfVector4
            var pixels = new Vector4[area.Width * area.Height];
            var sourceArea = new Rectangle(area.X - sourceOffset.X, area.Y - sourceOffset.Y, area.Width, area.Height);
            source.Mutate(context => context.ProcessPixelRowsAsVector4((row, start) =>
                row.CopyTo(pixels.AsSpan((start.Y - sourceArea.Y) * area.Width, area.Width)), sourceArea));
            target.Mutate(context => context.ProcessPixelRowsAsVector4((row, start) =>
                pixels.AsSpan((start.Y - area.Y) * area.Width, area.Width).CopyTo(row), area));
        }
    }
}
