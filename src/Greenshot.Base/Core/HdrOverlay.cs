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
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Effects;
using Greenshot.Base.ImageSharpDrawing;
using SixLabors.ImageSharp;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// What is drawn on a capture (the elements of the surface), for an HDR file: it goes over the HDR image with white halfway between SDR white and the white of the capture as the editor shows it
    /// (see HdrImage.Compose, ISurface.CreateHdrOverlay), so annotations look as on the screen and the HDR content around them keeps its highlights.
    /// Every element is drawn with ImageSharp on the HDR pixels (see Greenshot.Base.ImageSharpDrawing), nothing comes from the 8-bit image.
    /// </summary>
    public sealed class HdrOverlay : IDisposable
    {
        /// <param name="steps">The elements drawn on the HDR pixels and the filters done on them, in the order of the elements</param>
        public HdrOverlay(IReadOnlyList<HdrOverlayStep> steps)
        {
            Steps = steps ?? throw new ArgumentNullException(nameof(steps));
        }

        /// <summary>
        /// In the order of the elements: the elements drawn with ImageSharp on the HDR pixels (arrows, text, shapes, the mouse cursor, what torn
        /// edges draw, ...) and the filters of elements done on the HDR pixels (e.g. a highlighter, a magnifier, the cut of torn edges): each step
        /// changes what is before it, like in the editor
        /// </summary>
        public IReadOnlyList<HdrOverlayStep> Steps { get; }

        /// <summary>
        /// Nothing to release: the steps hold only managed data
        /// </summary>
        public void Dispose()
        {
        }
    }

    /// <summary>
    /// A step of an HdrOverlay: an element drawn on the HDR pixels, or a filter of an element done on the HDR pixels
    /// </summary>
    public sealed class HdrOverlayStep
    {
        private HdrOverlayStep(NativePoint location, ImageFilter filter, NativeSize size = default, Action<Image, ImageValues> draw = null)
        {
            Location = location;
            Filter = filter;
            Size = size;
            Draw = draw;
        }

        /// <summary>
        /// An element drawn with ImageSharp on the HDR pixels of the area
        /// </summary>
        /// <param name="area">Where the element draws, inside the capture</param>
        /// <param name="draw">Draws the element on an image of the area (its top left corner is the area's), in the values given</param>
        public static HdrOverlayStep Of(NativeRect area, Action<Image, ImageValues> draw) =>
            new(area.Location, null, area.Size, draw ?? throw new ArgumentNullException(nameof(draw)));

        public static HdrOverlayStep Of(ImageFilter filter) => new(new NativePoint(0, 0), filter ?? throw new ArgumentNullException(nameof(filter)));

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
