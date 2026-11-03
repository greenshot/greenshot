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
using Bitmap = System.Drawing.Bitmap;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// What is drawn on a capture (the elements of the surface), for an HDR file: it goes over the HDR image at the SDR white level
    /// (see HdrImage.Compose, ISurface.CreateHdrOverlay), so annotations look as on the screen and the HDR content around them keeps its highlights.
    /// </summary>
    public sealed class HdrOverlay : IDisposable
    {
        /// <param name="layer">The drawn elements with their alpha, the size of the capture</param>
        /// <param name="rendered">The capture as it is exported (with its elements), for the replaced areas; null without them</param>
        /// <param name="replacedAreas">The areas taken from the rendered capture</param>
        /// <param name="filters">The filters of elements done on the HDR pixels, in the order of the elements; null for none</param>
        public HdrOverlay(Bitmap layer, Bitmap rendered, IReadOnlyList<NativeRect> replacedAreas, IReadOnlyList<ImageFilter> filters = null)
        {
            Filters = filters ?? Array.Empty<ImageFilter>();
            Layer = layer ?? throw new ArgumentNullException(nameof(layer));
            Rendered = rendered;
            ReplacedAreas = replacedAreas ?? Array.Empty<NativeRect>();
        }

        /// <summary>
        /// The drawn elements (arrows, text, shapes, the mouse cursor, ...) with their alpha, blended over the HDR pixels.
        /// Transparent in the replaced areas.
        /// </summary>
        public Bitmap Layer { get; }

        /// <summary>
        /// The capture as exported, with its elements: where the replaced areas come from
        /// </summary>
        public Bitmap Rendered { get; }

        /// <summary>
        /// The areas of elements which change the pixels of the capture (blur, pixelate, highlight, cut marks, torn edges): they can't be drawn
        /// over the HDR pixels, these areas of the HDR file are the rendered 8-bit capture, at the SDR white level. What is blurred stays blurred.
        /// </summary>
        public IReadOnlyList<NativeRect> ReplacedAreas { get; }

        /// <summary>
        /// The filters of elements which can be done on the HDR pixels (e.g. a highlighter, a magnifier), in the order of the elements: they are
        /// done first, then the replaced areas and the drawn elements go over them. Their elements are drawn in the layer without the filter.
        /// </summary>
        public IReadOnlyList<ImageFilter> Filters { get; }

        public void Dispose()
        {
            Layer.Dispose();
            Rendered?.Dispose();
        }
    }
}
