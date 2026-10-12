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

namespace Greenshot.Document
{
    /// <summary>
    /// A line of an area along one axis: left, centre or right (top, centre or bottom)
    /// </summary>
    public enum AnchorLine
    {
        Start,
        Center,
        End
    }

    /// <summary>
    /// One axis of an anchor: the line Self of the element lies Offset pixels from the line Target of the target.
    /// For a point (the start or end of a line, the tail of a speech bubble) only Target and Offset count.
    /// </summary>
    public sealed record AxisAnchor(AnchorLine Target, AnchorLine Self = AnchorLine.Start, double Offset = 0);

    /// <summary>
    /// Keeps a part of an element at its place relative to the Bounds of the target element, the capture when Target is
    /// missing. An axis without an AxisAnchor isn't anchored.
    /// </summary>
    public sealed record Anchor
    {
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

        private sealed class Placer
        {
            private readonly Guid? _captureId;
            private readonly Dictionary<Guid, Element> _byId;
            private readonly Dictionary<Guid, Element> _placed = new();
            private readonly HashSet<Guid> _placing = new();

            public Placer(GreenshotDocument document)
            {
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

                placed = element;
                if (TargetBounds(element.Anchor, element.Id) is RectD target)
                {
                    // A speech bubble is placed by its text box, the tail follows
                    var box = element is SpeechBubbleElement bubble ? bubble.Area : element.Bounds;
                    placed = Move(element, Delta(element.Anchor.X, target.X, target.Width, box.X, box.Width), Delta(element.Anchor.Y, target.Y, target.Height, box.Y, box.Height));
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

            private RectD? TargetBounds(Anchor anchor, Guid elementId) =>
                anchor != null && TargetId(anchor, _captureId, elementId) is Guid id && _byId.TryGetValue(id, out var target) ? Place(target).Bounds : null;

            private PointD PlacePoint(Anchor anchor, Guid elementId, PointD point) =>
                TargetBounds(anchor, elementId) is RectD target
                    ? new PointD(anchor.X == null ? point.X : Line(target.X, target.Width, anchor.X.Target) + anchor.X.Offset,
                        anchor.Y == null ? point.Y : Line(target.Y, target.Height, anchor.Y.Target) + anchor.Y.Offset)
                    : point;

            private static double Delta(AxisAnchor axis, double targetStart, double targetLength, double start, double length) =>
                axis == null ? 0 : Line(targetStart, targetLength, axis.Target) + axis.Offset - Line(start, length, axis.Self);
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
}
