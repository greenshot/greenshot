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
using System.Linq;

namespace Greenshot.Document
{
    /// <summary>
    /// The size of the document and settings for all of it.
    /// </summary>
    public sealed record Canvas(int Width, int Height)
    {
        public Rgba Background { get; init; } = Rgba.Transparent;
    }

    /// <summary>
    /// Bytes an element references by id, in their original format (e.g. image/png, image/svg+xml).
    /// </summary>
    public sealed record Resource(Guid Id, string MediaType, byte[] Data);

    /// <summary>
    /// A Greenshot document: a canvas with layers of elements and the resources they reference.
    /// The first layer is drawn first (bottom). Documents are immutable, a change creates a new document.
    /// </summary>
    public sealed record GreenshotDocument(Guid Id, Canvas Canvas, IReadOnlyList<Layer> Layers, IReadOnlyDictionary<Guid, Resource> Resources)
    {
        /// <summary>
        /// The styles elements share
        /// </summary>
        public IReadOnlyList<SharedStyle> Styles { get; init; } = Array.Empty<SharedStyle>();

        /// <summary>
        /// The series which number the step labels
        /// </summary>
        public IReadOnlyList<StepSeries> Series { get; init; } = Array.Empty<StepSeries>();

        /// <summary>
        /// The style the element is drawn with: its own parts over those of its shared style.
        /// A step label without a shared style of its own uses the one of its series. An unknown style id is ignored.
        /// </summary>
        public ElementStyle StyleOf(Element element)
        {
            var styleId = element.StyleId ?? (element is StepLabelElement stepLabel ? SeriesOf(stepLabel)?.StyleId : null);
            var shared = Styles.FirstOrDefault(style => style.Id == styleId);
            return shared == null ? element.Style : element.Style.Over(shared.Style);
        }

        /// <summary>
        /// The series the step label is in, null when it is in none
        /// </summary>
        public StepSeries SeriesOf(StepLabelElement stepLabel) => Series.FirstOrDefault(series => series.Steps.Contains(stepLabel.Id));

        /// <summary>
        /// The text of the step label, e.g. "3" or "c", null when it is in no series
        /// </summary>
        public string LabelOf(StepLabelElement stepLabel) => SeriesOf(stepLabel) is { } series ? series.Label(stepLabel.Id) : null;

        /// <summary>
        /// The ids of the resources the element links to
        /// </summary>
        public IEnumerable<Guid> Links(Element element) => element switch
        {
            ImageElement image => new[] { image.Resource },
            CursorElement cursor => cursor.MaskResource.HasValue ? new[] { cursor.Resource, cursor.MaskResource.Value } : new[] { cursor.Resource },
            PluginElement plugin => plugin.Fallback.HasValue ? plugin.Resources.Append(plugin.Fallback.Value) : plugin.Resources,
            // The ids are unique, so a resource linked by an element of an unknown type is found in its JSON
            UnknownElement unknown => Resources.Keys.Where(id => unknown.Json.IndexOf(id.ToString("D"), StringComparison.OrdinalIgnoreCase) >= 0),
            _ => Array.Empty<Guid>()
        };
    }
}
