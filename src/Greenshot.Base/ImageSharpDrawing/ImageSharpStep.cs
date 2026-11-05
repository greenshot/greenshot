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
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Effects;
using SixLabors.ImageSharp;

namespace Greenshot.Base.ImageSharpDrawing
{
    /// <summary>
    /// One step of what the elements of the editor do to an image, as ImageSharp operations (see ISurface.CreateImageSharpSteps): an element
    /// drawn with ImageSharp, or a filter of an element done on the pixels. The steps are made from the elements on the UI thread and take all
    /// they need from them then, so they can run later on another thread, in their order: each changes what is before it, like in the editor.
    /// They work on any pixel type, in the values of the image (see ImageValues), e.g. on the HDR pixels of an HDR file (HdrImage.Compose,
    /// HdrImage.Draw).
    /// </summary>
    public sealed class ImageSharpStep
    {
        private ImageSharpStep(NativePoint location, ImageFilter filter, NativeSize size = default, Action<Image, ImageValues> draw = null)
        {
            Location = location;
            Filter = filter;
            Size = size;
            Draw = draw;
        }

        /// <summary>
        /// An element drawn with ImageSharp on the pixels of the area
        /// </summary>
        /// <param name="area">Where the element draws, inside the capture</param>
        /// <param name="draw">Draws the element on an image of the area (its top left corner is the area's), in the values given</param>
        public static ImageSharpStep Of(NativeRect area, Action<Image, ImageValues> draw) =>
            new(area.Location, null, area.Size, draw ?? throw new ArgumentNullException(nameof(draw)));

        /// <summary>
        /// A filter of an element, done on all of the image (it knows its areas)
        /// </summary>
        public static ImageSharpStep Of(ImageFilter filter) => new(new NativePoint(0, 0), filter ?? throw new ArgumentNullException(nameof(filter)));

        /// <summary>
        /// Where the area of a drawn element is in the capture
        /// </summary>
        public NativePoint Location { get; }

        /// <summary>
        /// A filter of an element; null for a drawn element
        /// </summary>
        public ImageFilter Filter { get; }

        /// <summary>
        /// The size of the area of a drawn element
        /// </summary>
        public NativeSize Size { get; }

        /// <summary>
        /// Draws the element on an image of its area (see <see cref="Of(NativeRect, Action{Image, ImageValues})"/>); null for a filter
        /// </summary>
        public Action<Image, ImageValues> Draw { get; }
    }
}
