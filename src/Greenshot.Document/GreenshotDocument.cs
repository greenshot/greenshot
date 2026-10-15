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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
    /// Bytes an element references by id, in their original format (e.g. image/png, image/svg+xml). The bytes can't be
    /// changed, so documents share them.
    /// </summary>
    public sealed record Resource(Guid Id, string MediaType, ReadOnlyMemory<byte> Data)
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

        /// <summary>
        /// The bytes as a read-only stream, without a copy
        /// </summary>
        public Stream OpenRead()
        {
            var bytes = ArrayOf(Data);
            return new MemoryStream(bytes.Array, bytes.Offset, bytes.Count, false);
        }

        /// <summary>
        /// The array behind the bytes, a copy only when they aren't in an array
        /// </summary>
        public static ArraySegment<byte> ArrayOf(ReadOnlyMemory<byte> data) =>
            MemoryMarshal.TryGetArray(data, out var segment) ? segment : new ArraySegment<byte>(data.ToArray());
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
        /// What the document is and where its picture came from, null when it has none
        /// </summary>
        public DocumentMetadata Metadata { get; init; }

        /// <summary>
        /// Only what is drawn, e.g. to share the document: no metadata, analysis or extensions, no elements and groups of
        /// types this version doesn't know. A plugin element becomes an image of its fallback (without one it is left out).
        /// Only the resources the elements link to are kept, their bytes are shared.
        /// </summary>
        public GreenshotDocument Strict()
        {
            var strict = this with
            {
                Metadata = null,
                Analysis = new Dictionary<Guid, ImageAnalysis>(),
                Extensions = null,
                Layers = Layers.Select(layer => layer with { Extensions = null, Elements = layer.Elements.Select(StrictElement).Where(element => element != null).ToList() }).ToList(),
                Groups = Groups.Where(group => group.Type is GroupTypes.Steps or GroupTypes.Style or GroupTypes.Set)
                    .Select(group => group with { Extensions = null, Style = group.Style == null ? null : group.Style with { Extensions = null } }).ToList()
            };
            var linked = new HashSet<Guid>(strict.Layers.SelectMany(layer => layer.Elements).SelectMany(strict.Links));
            return strict with
            {
                Resources = Resources.Where(pair => linked.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value with { Extensions = null })
            };
        }

        private static Element StrictElement(Element element) =>
            (element switch
            {
                UnknownElement => null,
                PluginElement { Fallback: Guid fallback } plugin => new ImageElement(plugin.Id, plugin.Area, fallback) { Style = plugin.Style, Anchor = plugin.Anchor },
                PluginElement => null,
                _ => element
            }) is Element strict ? strict with { Extensions = null, Style = strict.Style == null ? null : strict.Style with { Extensions = null } } : null;

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
            var area = elements.Select(element => Canvas.Fit == CanvasFit.Content ? VisualBounds(element) : element.Bounds).Aggregate((RectD?)null, (union, bounds) => union is RectD known ? RectD.Union(known, bounds) : bounds) ?? default;
            double padding = Canvas.Padding;
            return new RectD(area.X - padding, area.Y - padding, area.Width + 2 * padding, area.Height + 2 * padding);
        }

        /// <summary>
        /// The groups the element is a member of
        /// </summary>
        public IEnumerable<Group> GroupsOf(Guid elementId) => Groups.Where(group => group.Members.Contains(elementId));

        /// <summary>
        /// The full style the element is drawn with: the cascade, a framed element has no shadow, its frame draws the
        /// shadow of the framed shape
        /// </summary>
        public Style StyleOf(Element element)
        {
            var style = CascadeOf(element);
            return FrameOf(element) == null ? style : style with { Shadow = (style.Shadow ?? new ShadowStyle()) with { Enabled = false } };
        }

        /// <summary>
        /// Value by value the element's own style, over the style of its group, over the default of its type. For a
        /// renderer which doesn't draw the frames of elements (today's editor).
        /// </summary>
        public Style CascadeOf(Element element) =>
            element.Style.Over(GroupsOf(element.Id).Select(group => group.Style).FirstOrDefault(style => style != null)).Over(StyleDefaults.For(element));

        /// <summary>
        /// The frame which is drawn for the element: the first frame with it as target in drawing order. Lines, arrows,
        /// freehand and frames have no area to frame. Other frames with the same target are kept, but not drawn.
        /// </summary>
        public FrameElement FrameOf(Element element) => element is LineElement or ArrowElement or FreehandElement or FrameElement
            ? null
            : Layers.SelectMany(layer => layer.Elements).OfType<FrameElement>().FirstOrDefault(frame => frame.Target == element.Id);

        /// <summary>
        /// Where the element draws: its Bounds (a frame's outer edge) with the part of the line, the shadow and the label
        /// outside of them. A content canvas uses it, the anchors use the Bounds. The size of a label is estimated from its
        /// text style, the model doesn't measure fonts.
        /// </summary>
        public RectD VisualBounds(Element element)
        {
            var style = StyleOf(element);
            var bounds = element is FrameElement frame ? frame.OuterEdge : element.Bounds;
            if (style.Stroke is { Enabled: not false, Width: > 0 and double width } stroke)
            {
                var position = element is LineElement or ArrowElement or FreehandElement ? StrokePosition.Center : stroke.Position ?? StrokePosition.Center;
                bounds = bounds.Inflate(position switch { StrokePosition.Outside => width, StrokePosition.Inside => 0, _ => width / 2 });
            }

            if (style.Shadow is { Enabled: true } shadow)
            {
                var offset = shadow.Offset ?? default;
                bounds = RectD.Union(bounds, bounds.Inflate(shadow.Size ?? 0).Offset(offset.X, offset.Y));
            }

            return LabelBounds(element, style.Text) is RectD label ? RectD.Union(bounds, label) : bounds;
        }

        /// <summary>
        /// The estimated box of the element's label: a line of text is its size times the line height high, a letter 0.6
        /// of the size wide
        /// </summary>
        private static RectD? LabelBounds(Element element, TextStyle text)
        {
            double size = text?.Size ?? 11, lineHeight = size * (text?.LineHeight ?? 1.2);
            string[] lines = LabelOf(element)?.Split('\n');
            return lines != null && LabelPlacement(element, lines.Max(line => line.Length) * size * 0.6, lines.Length * lineHeight) is var (box, rotation, pivot)
                ? box.Turned(rotation, pivot)
                : null;
        }

        /// <summary>
        /// The text of the element's label, null without one
        /// </summary>
        public static string LabelOf(Element element) => element switch
        {
            RectangleElement e => e.Label?.Text,
            EllipseElement e => e.Label?.Text,
            ImageElement e => e.Label?.Text,
            LineElement e => e.Label?.Text,
            ArrowElement e => e.Label?.Text,
            _ => null
        };

        /// <summary>
        /// Where the element's label of the given size is: its box before it turns, turned by Rotation around Pivot. A
        /// box label has the width of its box unless it has a Width, a label along a line is never upside down.
        /// </summary>
        public static (RectD Box, double Rotation, PointD Pivot)? LabelPlacement(Element element, double width, double height)
        {
            var (boxLabel, area) = element switch
            {
                RectangleElement e => (e.Label, e.Area),
                EllipseElement e => (e.Label, e.Area),
                ImageElement e => (e.Label, e.Area),
                _ => (null, default)
            };
            if (boxLabel != null)
            {
                return (boxLabel.AreaIn(area, height), element.Rotation, area.Center);
            }

            var (lineLabel, start, end) = element switch
            {
                LineElement e => (e.Label, e.Start, e.End),
                ArrowElement e => (e.Label, e.Start, e.End),
                _ => (null, default, default)
            };
            if (lineLabel == null)
            {
                return null;
            }

            double dx = end.X - start.X, dy = end.Y - start.Y, length = Math.Sqrt(dx * dx + dy * dy);
            // Above a line from left to right, the offset is from the line to the nearest edge of the label
            var normal = length == 0 ? new PointD(0, -1) : new PointD(dy / length, -dx / length);
            double distance = lineLabel.Offset == 0 ? 0 : lineLabel.Offset + Math.Sign(lineLabel.Offset) * height / 2;
            var center = new PointD(start.X + dx * lineLabel.At + normal.X * distance, start.Y + dy * lineLabel.At + normal.Y * distance);
            double angle = Math.Atan2(dy, dx) * 180 / Math.PI;
            angle = angle > 90 ? angle - 180 : angle <= -90 ? angle + 180 : angle;
            return (new RectD(center.X - width / 2, center.Y - height / 2, width, height), lineLabel.Orientation == LabelOrientation.Along ? angle : 0, center);
        }

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
