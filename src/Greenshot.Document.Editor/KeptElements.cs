/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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
using System.Runtime.CompilerServices;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Interfaces.Plugin;

namespace Greenshot.Document.Editor
{
    /// <summary>
    /// What the editor doesn't hold itself: the element each container was made from, so its id and anchors survive a
    /// trip through the editor; the ids of the capture and the layers and the groups of the document; and the elements
    /// the editor can't edit, kept with the container which shows them (a plugin element without its plugin) or with the
    /// surface (an element of an unknown type), so they are written back unchanged
    /// </summary>
    internal static class KeptElements
    {
        internal sealed class Kept
        {
            public Kept(Element element, IReadOnlyList<Resource> resources)
            {
                Element = element;
                Resources = resources;
            }

            public Element Element { get; }

            public IReadOnlyList<Resource> Resources { get; }
        }

        internal sealed class KeptDocument
        {
            public Guid CaptureId { get; set; } = Uuid7.CreateVersion7();

            public Guid CaptureLayerId { get; set; } = Uuid7.CreateVersion7();

            public Guid AnnotationsLayerId { get; set; } = Uuid7.CreateVersion7();

            public IReadOnlyList<Group> Groups { get; set; } = Array.Empty<Group>();

            public RawJson Extensions { get; set; }

            public IReadOnlyDictionary<Guid, ImageAnalysis> Analysis { get; set; } = new Dictionary<Guid, ImageAnalysis>();

            /// <summary>
            /// The analysis of the capture and the features the editor got from it: while the editor still has exactly
            /// these features, the analysis comes back unchanged (with engine, language and corner points)
            /// </summary>
            public ImageAnalysis CaptureAnalysis { get; set; }

            public List<IDetectedFeature> RestoredFeatures { get; } = new();

            /// <summary>
            /// The image the editor got when the capture doesn't simply fill the canvas: the capture layers drawn onto the
            /// canvas. While the editor still has it, the capture layers, the canvas and the coordinates come back unchanged.
            /// </summary>
            public Image StandIn { get; set; }

            public IReadOnlyList<Layer> CaptureLayers { get; set; }

            public IReadOnlyList<Resource> CaptureResources { get; set; }

            public Canvas Canvas { get; set; }

            /// <summary>
            /// Where the canvas starts in the coordinates of the document, the editor's 0, 0
            /// </summary>
            public PointD Origin { get; set; }

            public List<Kept> Elements { get; } = new();
        }

        internal static readonly ConditionalWeakTable<IDrawableContainer, Element> Originals = new();

        internal static readonly ConditionalWeakTable<IDrawableContainer, Kept> ByContainer = new();

        private static readonly ConditionalWeakTable<ISurface, KeptDocument> Documents = new();

        /// <summary>
        /// What is kept for the surface, new ids for a surface which didn't come from a document, which stay for all of its documents
        /// </summary>
        internal static KeptDocument DocumentOf(ISurface surface) => Documents.GetValue(surface, _ => new KeptDocument());
    }
}
