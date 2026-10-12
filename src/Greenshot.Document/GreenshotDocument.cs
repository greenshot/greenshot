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
using System.Text.Json.Serialization;

namespace Greenshot.Document
{
    /// <summary>
    /// What the canvas is fitted to
    /// </summary>
    public enum CanvasFit
    {
        /// <summary>The capture image, plus padding</summary>
        Capture,

        /// <summary>All elements of the visible layers, plus padding</summary>
        Content,

        /// <summary>Width and Height, from 0, 0</summary>
        Fixed
    }

    /// <summary>
    /// The area which is drawn and exported, as a rule: GreenshotDocument.CanvasArea() works it out. The elements keep
    /// their own coordinates (the capture normally at 0, 0, elements left of it at a negative x), nothing moves when
    /// the canvas changes.
    /// </summary>
    public sealed record Canvas
    {
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public CanvasFit Fit { get; init; }

        /// <summary>
        /// Room around the capture or the elements in canvas pixels, not used by Fixed
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public double Padding { get; init; }

        /// <summary>
        /// The size of a Fixed canvas
        /// </summary>
        public int? Width { get; init; }

        public int? Height { get; init; }

        public Rgba Background { get; init; } = Rgba.Transparent;

        public static Canvas Fixed(int width, int height) => new() { Fit = CanvasFit.Fixed, Width = width, Height = height };
    }

    /// <summary>
    /// Bytes an element references by id, in their original format (e.g. image/png, image/svg+xml).
    /// </summary>
    public sealed record Resource(Guid Id, string MediaType, byte[] Data)
    {
        /// <summary>
        /// The size of the image in pixels, when it's known
        /// </summary>
        public int? Width { get; init; }

        public int? Height { get; init; }

        /// <summary>
        /// Values of plugins (keyed by plugin id), written back unchanged
        /// </summary>
        public RawJson Extensions { get; init; }
    }

    /// <summary>
    /// A Greenshot document: a canvas with layers of elements and the resources they reference.
    /// The first layer is drawn first (bottom). Documents are immutable, a change creates a new document.
    /// </summary>
    public sealed record GreenshotDocument(Guid Id, Canvas Canvas, IReadOnlyList<Layer> Layers, IReadOnlyDictionary<Guid, Resource> Resources)
    {
        /// <summary>
        /// The groups, e.g. steps which number step labels, styles elements share and sets which move together
        /// </summary>
        public IReadOnlyList<Group> Groups { get; init; } = Array.Empty<Group>();

        /// <summary>
        /// What was found in the resources (OCR, barcodes, areas), keyed by resource id
        /// </summary>
        public IReadOnlyDictionary<Guid, ImageAnalysis> Analysis { get; init; } = new Dictionary<Guid, ImageAnalysis>();

        /// <summary>
        /// Values of plugins (keyed by plugin id), written back unchanged
        /// </summary>
        public RawJson Extensions { get; init; }

        /// <summary>
        /// The area of the canvas in the coordinates of the elements, after Anchors.Apply it includes anchored elements
        /// where they belong
        /// </summary>
        public RectD CanvasArea()
        {
            if (Canvas.Fit == CanvasFit.Fixed)
            {
                return new RectD(0, 0, Canvas.Width ?? 0, Canvas.Height ?? 0);
            }

            var elements = Canvas.Fit == CanvasFit.Content
                ? Layers.Where(layer => layer.Visible).SelectMany(layer => layer.Elements)
                : Layers.Where(layer => layer.Kind == LayerKind.Capture).SelectMany(layer => layer.Elements).OfType<ImageElement>().Take(1);
            var area = elements.Select(element => element.Bounds).Aggregate((RectD?)null, (union, bounds) => union is RectD known ? RectD.Union(known, bounds) : bounds) ?? default;
            double padding = Canvas.Padding;
            return new RectD(area.X - padding, area.Y - padding, area.Width + 2 * padding, area.Height + 2 * padding);
        }

        /// <summary>
        /// The groups the element is a member of
        /// </summary>
        public IEnumerable<Group> GroupsOf(Guid elementId) => Groups.Where(group => group.Members.Contains(elementId));

        /// <summary>
        /// The full style the element is drawn with: value by value its own style, over the style of its group, over
        /// the default of its type
        /// </summary>
        public Style StyleOf(Element element) =>
            element.Style.Over(GroupsOf(element.Id).Select(group => group.Style).FirstOrDefault(style => style != null)).Over(StyleDefaults.For(element));

        /// <summary>
        /// The text of the step label, e.g. "3" or "c": from its steps group, otherwise 1, 2, 3 in drawing order of
        /// the step labels in no steps group
        /// </summary>
        public string LabelOf(StepLabelElement stepLabel)
        {
            var steps = GroupsOf(stepLabel.Id).FirstOrDefault(group => group.Type == GroupTypes.Steps);
            if (steps != null)
            {
                return StepNumbering.Of(steps).Label(steps.Members.ToList().IndexOf(stepLabel.Id));
            }

            var ungrouped = Layers.SelectMany(layer => layer.Elements).OfType<StepLabelElement>()
                .Where(label => !GroupsOf(label.Id).Any(group => group.Type == GroupTypes.Steps)).Select(label => label.Id).ToList();
            return (ungrouped.IndexOf(stepLabel.Id) + 1).ToString();
        }

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
