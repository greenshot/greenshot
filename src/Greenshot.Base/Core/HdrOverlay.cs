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
using Bitmap = System.Drawing.Bitmap;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// What is drawn on a capture (the elements of the surface), for an HDR file: it goes over the HDR image at the SDR white level
    /// (see HdrImage.Compose, ISurface.CreateHdrOverlay), so annotations look as on the screen and the HDR content around them keeps its highlights.
    /// Elements are drawn with ImageSharp on the HDR pixels where they can be (see Greenshot.Base.ImageSharpDrawing), others are 8-bit layers.
    /// </summary>
    public sealed class HdrOverlay : IDisposable
    {
        /// <param name="steps">The layers of drawn elements and the filters done on the HDR pixels, in the order of the elements</param>
        /// <param name="rendered">The capture as it is exported (with its elements), for the replaced areas; null without them</param>
        /// <param name="replacedAreas">The areas taken from the rendered capture</param>
        /// <param name="changedAreas">The areas where only the pixels which the elements changed are taken from the rendered capture; null for none</param>
        public HdrOverlay(IReadOnlyList<HdrOverlayStep> steps, Bitmap rendered, IReadOnlyList<NativeRect> replacedAreas, IReadOnlyList<NativeRect> changedAreas = null)
        {
            Steps = steps ?? throw new ArgumentNullException(nameof(steps));
            ChangedAreas = changedAreas ?? Array.Empty<NativeRect>();
            Rendered = rendered;
            ReplacedAreas = replacedAreas ?? Array.Empty<NativeRect>();
        }

        /// <summary>
        /// In the order of the elements: layers of drawn elements (arrows, text, shapes, the mouse cursor, ...) with their alpha, blended over
        /// the HDR pixels, and between them the filters of elements done on the HDR pixels (e.g. a highlighter, a magnifier, the cut of torn
        /// edges): a filter changes what is drawn before it, like in the editor. The layers are transparent in the replaced and changed areas.
        /// </summary>
        public IReadOnlyList<HdrOverlayStep> Steps { get; }

        /// <summary>
        /// The capture as exported, with its elements: where the replaced areas come from
        /// </summary>
        public Bitmap Rendered { get; }

        /// <summary>
        /// The areas of elements which change the pixels of the capture (blur, pixelate): they can't be drawn
        /// over the HDR pixels, these areas of the HDR file are the rendered 8-bit capture, at the SDR white level. What is blurred stays blurred.
        /// </summary>
        public IReadOnlyList<NativeRect> ReplacedAreas { get; }

        /// <summary>
        /// The areas of elements which change only some pixels of the capture (the teeth of torn edges, cut marks): there only the pixels which
        /// the rendered capture has different from the capture's image are taken from it, the others keep their HDR content. Transparent in the layer.
        /// </summary>
        public IReadOnlyList<NativeRect> ChangedAreas { get; }

        public void Dispose()
        {
            foreach (var step in Steps)
            {
                step.Layer?.Dispose();
            }
            Rendered?.Dispose();
        }
    }

    /// <summary>
    /// A step of an HdrOverlay: an element drawn on the HDR pixels, a layer of 8-bit drawn elements, or a filter of an element done on the HDR pixels
    /// </summary>
    public sealed class HdrOverlayStep
    {
        private HdrOverlayStep(Bitmap layer, NativePoint location, ImageFilter filter, NativeSize size = default, Action<Image, ImageValues> draw = null)
        {
            Layer = layer;
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
            new(null, area.Location, null, area.Size, draw ?? throw new ArgumentNullException(nameof(draw)));

        /// <param name="layer">Drawn elements with their alpha</param>
        /// <param name="location">Where the layer is in the capture: a layer is only as big as what is drawn into it</param>
        public static HdrOverlayStep Of(Bitmap layer, NativePoint location) => new(layer ?? throw new ArgumentNullException(nameof(layer)), location, null);

        public static HdrOverlayStep Of(ImageFilter filter) => new(null, new NativePoint(0, 0), filter ?? throw new ArgumentNullException(nameof(filter)));

        /// <summary>
        /// Where the layer is in the capture
        /// </summary>
        public NativePoint Location { get; }

        /// <summary>
        /// Drawn elements with their alpha, at <see cref="Location"/>; null for a filter or a drawn element
        /// </summary>
        public Bitmap Layer { get; }

        /// <summary>
        /// A filter of an element; null for a layer or a drawn element
        /// </summary>
        public ImageFilter Filter { get; }

        /// <summary>
        /// The size of the area of a drawn element
        /// </summary>
        public NativeSize Size { get; }

        /// <summary>
        /// Draws the element on an image of its area (see <see cref="Of(NativeRect, Action{Image, ImageValues})"/>); null for a layer or a filter
        /// </summary>
        public Action<Image, ImageValues> Draw { get; }
    }
}
