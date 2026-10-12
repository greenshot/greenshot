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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Greenshot.Document
{
    /// <summary>
    /// A line of an area along one axis: left, centre or right (top, centre or bottom). Point is the AnchorPoint of the
    /// target on that axis, e.g. the tip of a cursor.
    /// </summary>
    public enum AnchorLine
    {
        Start,
        Center,
        End,
        Point
    }

    /// <summary>
    /// One axis of an anchor: the line Self of the element lies Offset pixels from the line Target of the target.
    /// For a point (the start or end of a line, the tail of a speech bubble) only Target and Offset count. Self can't be
    /// Point, it counts as Start.
    /// </summary>
    public sealed record AxisAnchor(AnchorLine Target, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] AnchorLine Self = AnchorLine.Start, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)] double Offset = 0);

    /// <summary>
    /// Keeps a part of an element at its place relative to the Bounds or AnchorPoint of the target element, the capture
    /// when Target is missing, the canvas when Target is Anchors.Canvas ("canvas"). An axis without an AxisAnchor isn't
    /// anchored, but an anchor of a point without any axis puts the point on the AnchorPoint of the target.
    /// </summary>
    public sealed record Anchor
    {
        [JsonConverter(typeof(TargetConverter))]
        public Guid? Target { get; init; }

        public AxisAnchor X { get; init; }

        public AxisAnchor Y { get; init; }
    }

    /// <summary>
    /// Places anchored elements: the target first (up the chain to an element without anchor), then the element.
    /// Every element is placed once, a target which doesn't exist leaves the part where it is.
    /// </summary>
    public static class Anchors
    {
        /// <summary>
        /// The target of an anchor to the canvas, written as "canvas". Only a Fixed canvas can be a target, the others
        /// depend on the elements.
        /// </summary>
        public static readonly Guid Canvas = Guid.Empty;

        /// <summary>
        /// The id of the capture, the target of an anchor without one: the first image of a capture layer
        /// </summary>
        public static Guid? CaptureId(GreenshotDocument document) =>
            document.Layers.Where(layer => layer.Kind == LayerKind.Capture).SelectMany(layer => layer.Elements).OfType<ImageElement>().FirstOrDefault()?.Id;

        /// <summary>
        /// The anchors of all parts of the element
        /// </summary>
        public static IEnumerable<Anchor> Of(Element element) =>
            new[]
            {
                element.Anchor,
                (element as LineElement)?.StartAnchor, (element as LineElement)?.EndAnchor,
                (element as ArrowElement)?.StartAnchor, (element as ArrowElement)?.EndAnchor,
                (element as SpeechBubbleElement)?.TailAnchor
            }.Where(anchor => anchor != null);

        /// <summary>
        /// true when anchoring the element to the target (null: the capture) would make a loop, this is checked when an anchor is set
        /// </summary>
        public static bool MakesLoop(GreenshotDocument document, Guid elementId, Guid? targetId)
        {
            var captureId = CaptureId(document);
            var byId = ById(document);
            var seen = new HashSet<Guid>();
            var open = new Stack<Guid>();
            if ((targetId ?? captureId) is Guid first)
            {
                open.Push(first);
            }

            while (open.Count > 0)
            {
                var id = open.Pop();
                if (id == elementId)
                {
                    return true;
                }

                if (seen.Add(id) && byId.TryGetValue(id, out var element))
                {
                    foreach (var next in Of(element).Select(anchor => TargetId(anchor, captureId, id)).OfType<Guid>())
                    {
                        open.Push(next);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// The document with every anchored part placed again, e.g. after a target moved or for elements a recipe adds
        /// </summary>
        /// <exception cref="InvalidDataException">When anchors make a loop</exception>
        public static GreenshotDocument Apply(GreenshotDocument document)
        {
            var placer = new Placer(document);
            return document with
            {
                Layers = document.Layers.Select(layer => layer with { Elements = layer.Elements.Select(placer.Place).ToList() }).ToList()
            };
        }

        /// <summary>
        /// Refuses a document whose anchors make a loop, it is checked again when a document is written
        /// </summary>
        /// <exception cref="InvalidDataException">When anchors make a loop</exception>
        public static void CheckLoops(GreenshotDocument document)
        {
            var placer = new Placer(document);
            foreach (var element in document.Layers.SelectMany(layer => layer.Elements))
            {
                placer.Place(element);
            }
        }

        // The first element with an id, ids should be unique but a document from elsewhere may repeat one
        private static Dictionary<Guid, Element> ById(GreenshotDocument document) =>
            document.Layers.SelectMany(layer => layer.Elements).GroupBy(element => element.Id).ToDictionary(group => group.Key, group => group.First());

        private static Guid? TargetId(Anchor anchor, Guid? captureId, Guid elementId) =>
            (anchor.Target ?? captureId) is Guid target && target != elementId ? target : null;

        private static double Line(double start, double length, AnchorLine line) => line switch
        {
            AnchorLine.Center => start + length / 2,
            AnchorLine.End => start + length,
            _ => start
        };

        private static double TargetLine(Element target, bool x, AnchorLine line) =>
            line == AnchorLine.Point
                ? x ? target.AnchorPoint.X : target.AnchorPoint.Y
                : x ? Line(target.Bounds.X, target.Bounds.Width, line) : Line(target.Bounds.Y, target.Bounds.Height, line);

        private static readonly AxisAnchor OnPoint = new(AnchorLine.Point);

        private sealed class Placer
        {
            private readonly Guid? _captureId;
            private readonly Dictionary<Guid, Element> _byId;
            private readonly Dictionary<Guid, Element> _placed = new();
            private readonly HashSet<Guid> _placing = new();

            private readonly GreenshotDocument _document;

            public Placer(GreenshotDocument document)
            {
                _document = document;
                _captureId = CaptureId(document);
                _byId = ById(document);
            }

            public Element Place(Element element)
            {
                if (_placed.TryGetValue(element.Id, out var placed))
                {
                    return placed;
                }

                if (!_placing.Add(element.Id))
                {
                    throw new InvalidDataException($"The anchors of element {element.Id} make a loop");
                }

                placed = Scaled(element);
                if (Target(element.Anchor, element.Id) is Element target)
                {
                    // A speech bubble is placed by its text box, the tail follows
                    var box = placed is SpeechBubbleElement bubble ? bubble.Area : placed.Bounds;
                    placed = Move(placed, Delta(element.Anchor.X, target, true, box.X, box.Width), Delta(element.Anchor.Y, target, false, box.Y, box.Height));
                }

                placed = placed switch
                {
                    LineElement line => line with { Start = PlacePoint(line.StartAnchor, line.Id, line.Start), End = PlacePoint(line.EndAnchor, line.Id, line.End) },
                    ArrowElement arrow => arrow with { Start = PlacePoint(arrow.StartAnchor, arrow.Id, arrow.Start), End = PlacePoint(arrow.EndAnchor, arrow.Id, arrow.End) },
                    SpeechBubbleElement bubble => bubble with { Tail = PlacePoint(bubble.TailAnchor, bubble.Id, bubble.Tail) },
                    _ => placed
                };

                _placing.Remove(element.Id);
                _placed[element.Id] = placed;
                return placed;
            }

            private Element Target(Anchor anchor, Guid elementId)
            {
                if (anchor?.Target == Canvas)
                {
                    return _document.Canvas.Fit == CanvasFit.Fixed ? new RectangleElement(Canvas, _document.CanvasArea()) : null;
                }

                return anchor != null && TargetId(anchor, _captureId, elementId) is Guid id && _byId.TryGetValue(id, out var target) ? Place(target) : null;
            }

            // An image with a Scale gets the size of the part of its resource it shows times Scale, when the size of the resource is known
            private Element Scaled(Element element) =>
                element is ImageElement { Scale: double scale } image && _document.Resources.TryGetValue(image.Resource, out var resource) && GreenshotPackage.WithSize(resource) is { Width: int width, Height: int height }
                    ? image with { Area = image.Area with { Width = width * (image.Source?.Width ?? 1) * scale, Height = height * (image.Source?.Height ?? 1) * scale } }
                    : element;

            private PointD PlacePoint(Anchor anchor, Guid elementId, PointD point)
            {
                if (Target(anchor, elementId) is not Element target)
                {
                    return point;
                }

                var (x, y) = anchor.X == null && anchor.Y == null ? (OnPoint, OnPoint) : (anchor.X, anchor.Y);
                return new PointD(x == null ? point.X : TargetLine(target, true, x.Target) + x.Offset,
                    y == null ? point.Y : TargetLine(target, false, y.Target) + y.Offset);
            }

            private static double Delta(AxisAnchor axis, Element target, bool x, double start, double length) =>
                axis == null ? 0 : TargetLine(target, x, axis.Target) + axis.Offset - Line(start, length, axis.Self);
        }

        /// <summary>
        /// The element moved by dx, dy: its Area, or its points. An element of an unknown type can't be moved.
        /// </summary>
        public static Element Move(Element element, double dx, double dy)
        {
            if (dx == 0 && dy == 0)
            {
                return element;
            }

            PointD Point(PointD point) => new(point.X + dx, point.Y + dy);
            return element switch
            {
                RectangleElement e => e with { Area = e.Area.Offset(dx, dy) },
                EllipseElement e => e with { Area = e.Area.Offset(dx, dy) },
                LineElement e => e with { Start = Point(e.Start), End = Point(e.End) },
                ArrowElement e => e with { Start = Point(e.Start), End = Point(e.End) },
                FreehandElement e => e with { Points = e.Points.Select(Point).ToList() },
                TextElement e => e with { Area = e.Area.Offset(dx, dy) },
                SpeechBubbleElement e => e with { Area = e.Area.Offset(dx, dy), Tail = Point(e.Tail) },
                StepLabelElement e => e with { Area = e.Area.Offset(dx, dy) },
                HighlightElement e => e with { Area = e.Area.Offset(dx, dy) },
                ObfuscateElement e => e with { Area = e.Area.Offset(dx, dy) },
                ImageElement e => e with { Area = e.Area.Offset(dx, dy) },
                EmojiElement e => e with { Area = e.Area.Offset(dx, dy) },
                CursorElement e => e with { Area = e.Area.Offset(dx, dy) },
                CutMarkElement e => e with { Area = e.Area.Offset(dx, dy) },
                TornEdgeElement e => e with { Area = e.Area.Offset(dx, dy) },
                PluginElement e => e with { Area = e.Area.Offset(dx, dy) },
                _ => element
            };
        }
    }

    /// <summary>
    /// Writes the target of an anchor as its id, or "canvas"
    /// </summary>
    internal sealed class TargetConverter : JsonConverter<Guid?>
    {
        private const string CanvasName = "canvas";

        public override Guid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType == JsonTokenType.Null ? null
            : reader.GetString() is CanvasName ? Anchors.Canvas
            : reader.GetGuid();

        public override void Write(Utf8JsonWriter writer, Guid? value, JsonSerializerOptions options)
        {
            if (value == Anchors.Canvas)
            {
                writer.WriteStringValue(CanvasName);
            }
            else
            {
                writer.WriteStringValue(value.Value);
            }
        }
    }
}
