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
using System.Linq;
using Greenshot.Document;
using Greenshot.Document.Editing;

namespace Greenshot.WpfEditor.Tools
{
    /// <summary>
    /// The handles around a selection: where they are and what dragging them does
    /// </summary>
    internal static class SelectionHandles
    {
        /// <summary>
        /// The handle positions on an area, clockwise from the top left corner: NW, N, NE, E, SE, S, SW, W
        /// </summary>
        public static readonly (double X, double Y)[] Places =
        {
            (0, 0), (0.5, 0), (1, 0), (1, 0.5), (1, 1), (0.5, 1), (0, 1), (0, 0.5)
        };

        /// <summary>
        /// The distance of the turn handle above the top edge, in screen pixels
        /// </summary>
        public const double RotateDistance = 24;

        /// <summary>
        /// Half the size of a handle you can grab, in screen pixels
        /// </summary>
        public const double GrabRadius = 6;

        public static PointD Point(RectD area, double rotation, int handle)
        {
            var place = Places[handle];
            var point = new PointD(area.X + place.X * area.Width, area.Y + place.Y * area.Height);
            return rotation % 360 == 0 ? point : point.Turned(rotation, area.Center);
        }

        public static PointD RotatePoint(RectD area, double rotation, double pixelSize)
        {
            var point = new PointD(area.Center.X, area.Y - RotateDistance * pixelSize);
            return rotation % 360 == 0 ? point : point.Turned(rotation, area.Center);
        }

        /// <summary>
        /// The cursor for a handle, chosen by the direction it points to after the turn
        /// </summary>
        public static ToolCursor CursorOf(int handle, double rotation)
        {
            // 0 = NW .. 7 = W, every step is 45 degrees
            int direction = ((int)Math.Round(handle + rotation / 45) % 8 + 8) % 8;
            return direction switch
            {
                0 or 4 => ToolCursor.SizeNorthWestSouthEast,
                2 or 6 => ToolCursor.SizeNorthEastSouthWest,
                1 or 5 => ToolCursor.SizeNorthSouth,
                _ => ToolCursor.SizeWestEast
            };
        }
    }

    /// <summary>
    /// Selects, moves, resizes and turns elements; selects with a rubber band; double click edits a text
    /// </summary>
    public sealed class SelectTool : IEditorTool
    {
        private enum Drag
        {
            None,
            Move,
            Resize,
            Rotate,
            Start,
            End,
            Tail,
            RubberBand
        }

        private readonly IToolHost _host;
        private Drag _drag;
        private int _handle;
        private PointD _from;
        private GreenshotDocument _before;
        private List<Element> _dragged = new();
        private RectD _startArea;
        private RectD _rubberBand;
        private bool _moved;
        private ToolCursor _cursor = ToolCursor.Arrow;

        public SelectTool(IToolHost host)
        {
            _host = host;
        }

        public ToolKind Kind => ToolKind.Select;

        public ToolCursor Cursor => _cursor;

        /// <summary>
        /// Used by the view to draw the handles at the right distance
        /// </summary>
        public double PixelSize { get; private set; } = 1;

        public IReadOnlyList<Adorner> Adorners
        {
            get
            {
                var adorners = new List<Adorner>();
                if (_drag == Drag.RubberBand)
                {
                    adorners.Add(new Adorner(AdornerKind.RubberBand, _rubberBand));
                }

                var selected = SelectedElements();
                if (selected.Count == 1)
                {
                    AddSingle(adorners, selected[0]);
                }
                else if (selected.Count > 1)
                {
                    foreach (var element in selected)
                    {
                        adorners.Add(OutlineOf(element));
                    }

                    var union = Union(selected);
                    adorners.Add(new Adorner(AdornerKind.Outline, union));
                    for (int handle = 0; handle < 8; handle++)
                    {
                        adorners.Add(new Adorner(AdornerKind.Handle, Point: SelectionHandles.Point(union, 0, handle)));
                    }
                }

                return adorners;
            }
        }

        private void AddSingle(List<Adorner> adorners, Element element)
        {
            if (ElementShapes.EndsOf(element) is var (start, end))
            {
                adorners.Add(new Adorner(AdornerKind.PointHandle, Point: start));
                adorners.Add(new Adorner(AdornerKind.PointHandle, Point: end));
                return;
            }

            var (area, rotation) = FrameOf(element);
            adorners.Add(new Adorner(AdornerKind.Outline, area, rotation));
            for (int handle = 0; handle < 8; handle++)
            {
                adorners.Add(new Adorner(AdornerKind.Handle, Point: SelectionHandles.Point(area, rotation, handle)));
            }

            if (ElementShapes.CanTurn(element))
            {
                var top = SelectionHandles.Point(area, rotation, 1);
                adorners.Add(new Adorner(AdornerKind.RotateHandle, Point: SelectionHandles.RotatePoint(area, rotation, PixelSize), To: top));
            }

            if (element is SpeechBubbleElement bubble)
            {
                adorners.Add(new Adorner(AdornerKind.PointHandle, Point: bubble.Tail));
            }
        }

        private static Adorner OutlineOf(Element element)
        {
            var (area, rotation) = FrameOf(element);
            return new Adorner(AdornerKind.Outline, area, rotation);
        }

        /// <summary>
        /// The area the handles of an element sit on, and its turn
        /// </summary>
        private static (RectD Area, double Rotation) FrameOf(Element element) =>
            ElementShapes.AreaOf(element) is RectD area ? (area, ElementShapes.CanTurn(element) ? element.Rotation : 0) : (element.Bounds, 0);

        private static RectD Union(IEnumerable<Element> elements) => elements.Select(element => element.Bounds).Aggregate(RectD.Union);

        private List<Element> SelectedElements()
        {
            var document = _host.Document;
            return _host.Selection.Select(id => DocumentEdits.Find(document, id)).Where(element => element != null).ToList();
        }

        public void Down(ToolPointer pointer)
        {
            PixelSize = pointer.PixelSize;
            _before = _host.Document;
            _from = pointer.Point;
            _moved = false;
            var selected = SelectedElements();
            if (FindHandle(selected, pointer) is var (drag, handle))
            {
                _drag = drag;
                _handle = handle;
                _dragged = selected;
                _startArea = selected.Count == 1 ? FrameOf(selected[0]).Area : Union(selected);
                return;
            }

            var hit = HitTesting.ElementAt(_before, pointer.Point, pointer.Tolerance);
            if (hit == null)
            {
                if (!pointer.Shift && !pointer.Control)
                {
                    _host.Select(Array.Empty<Guid>());
                }

                _drag = Drag.RubberBand;
                _rubberBand = new RectD(pointer.Point.X, pointer.Point.Y, 0, 0);
                _host.ToolChanged();
                return;
            }

            if (pointer.ClickCount >= 2 && CanEditText(hit))
            {
                _drag = Drag.None;
                _host.Select(new[] { hit.Id });
                _host.EditText(hit.Id);
                return;
            }

            var ids = DocumentEdits.WithSets(_before, new[] { hit.Id });
            if (pointer.Shift || pointer.Control)
            {
                var selection = new HashSet<Guid>(_host.Selection);
                if (selection.Contains(hit.Id))
                {
                    selection.ExceptWith(ids);
                    _host.Select(selection);
                    _drag = Drag.None;
                    return;
                }

                selection.UnionWith(ids);
                _host.Select(selection);
            }
            else if (!_host.Selection.Contains(hit.Id))
            {
                _host.Select(ids);
            }

            _drag = Drag.Move;
            _dragged = SelectedElements();
        }

        private static bool CanEditText(Element element) => element is TextElement or SpeechBubbleElement or RectangleElement or EllipseElement;

        private (Drag Drag, int Handle)? FindHandle(List<Element> selected, ToolPointer pointer)
        {
            double grab = SelectionHandles.GrabRadius * pointer.PixelSize;
            bool Near(PointD point) => HitTesting.Distance(point, pointer.Point) <= grab;
            if (selected.Count == 1)
            {
                var element = selected[0];
                if (ElementShapes.EndsOf(element) is var (start, end))
                {
                    return Near(end) ? (Drag.End, 0) : Near(start) ? (Drag.Start, 0) : null;
                }

                if (element is SpeechBubbleElement bubble && Near(bubble.Tail))
                {
                    return (Drag.Tail, 0);
                }

                var (area, rotation) = FrameOf(element);
                if (ElementShapes.CanTurn(element) && Near(SelectionHandles.RotatePoint(area, rotation, pointer.PixelSize)))
                {
                    return (Drag.Rotate, 0);
                }

                for (int handle = 0; handle < 8; handle++)
                {
                    if (Near(SelectionHandles.Point(area, rotation, handle)))
                    {
                        return (Drag.Resize, handle);
                    }
                }
            }
            else if (selected.Count > 1)
            {
                var union = Union(selected);
                for (int handle = 0; handle < 8; handle++)
                {
                    if (Near(SelectionHandles.Point(union, 0, handle)))
                    {
                        return (Drag.Resize, handle);
                    }
                }
            }

            return null;
        }

        public void Move(ToolPointer pointer)
        {
            PixelSize = pointer.PixelSize;
            if (_drag == Drag.None)
            {
                return;
            }

            double dx = pointer.Point.X - _from.X, dy = pointer.Point.Y - _from.Y;
            if (!_moved && Math.Abs(dx) < 2 * pointer.PixelSize && Math.Abs(dy) < 2 * pointer.PixelSize)
            {
                return;
            }

            _moved = true;
            switch (_drag)
            {
                case Drag.RubberBand:
                    _rubberBand = RectD.FromPoints(_from, pointer.Point);
                    _host.ToolChanged();
                    return;
                case Drag.Move:
                    if (pointer.Shift)
                    {
                        // Only along the axis it moved most
                        if (Math.Abs(dx) > Math.Abs(dy))
                        {
                            dy = 0;
                        }
                        else
                        {
                            dx = 0;
                        }
                    }

                    _host.Preview(DocumentEdits.Replace(_before, _dragged.Select(element => ElementShapes.MovedByHand(element, Math.Round(dx), Math.Round(dy)))));
                    return;
                case Drag.Start:
                case Drag.End:
                {
                    var element = _dragged[0];
                    var (start, end) = ElementShapes.EndsOf(element).Value;
                    var point = pointer.Shift ? Snapped(_drag == Drag.End ? start : end, pointer.Point) : pointer.Point;
                    _host.Preview(DocumentEdits.Replace(_before, _drag == Drag.End ? ElementShapes.WithEnds(element, start, point) : ElementShapes.WithEnds(element, point, end)));
                    return;
                }
                case Drag.Tail:
                    _host.Preview(DocumentEdits.Replace(_before, ((SpeechBubbleElement)_dragged[0]) with { Tail = pointer.Point, TailAnchor = null }));
                    return;
                case Drag.Rotate:
                {
                    var element = _dragged[0];
                    var center = FrameOf(element).Area.Center;
                    double angle = Math.Atan2(pointer.Point.Y - center.Y, pointer.Point.X - center.X) * 180 / Math.PI + 90;
                    if (pointer.Shift)
                    {
                        angle = Math.Round(angle / 15) * 15;
                    }

                    _host.Preview(DocumentEdits.Replace(_before, ElementShapes.Unanchored(ElementShapes.WithRotation(element, angle))));
                    return;
                }
                case Drag.Resize:
                    _host.Preview(DocumentEdits.Replace(_before, Resized(pointer)));
                    return;
            }
        }

        private IEnumerable<Element> Resized(ToolPointer pointer)
        {
            if (_dragged.Count == 1)
            {
                var element = _dragged[0];
                var (area, rotation) = FrameOf(element);
                var resized = ResizedArea(area, rotation, _handle, pointer.Point, pointer.Shift);
                return new[] { ElementShapes.Unanchored(ElementShapes.WithArea(element, resized)) };
            }

            var target = ResizedArea(_startArea, 0, _handle, pointer.Point, pointer.Shift);
            return _dragged.Select(element => ElementShapes.Unanchored(ScaledInto(element, _startArea, target)));
        }

        /// <summary>
        /// The element scaled from one area into another, like the selection it is in
        /// </summary>
        private static Element ScaledInto(Element element, RectD from, RectD to)
        {
            double fx = from.Width > 0 ? to.Width / from.Width : 1, fy = from.Height > 0 ? to.Height / from.Height : 1;
            PointD Point(PointD point) => new(to.X + (point.X - from.X) * fx, to.Y + (point.Y - from.Y) * fy);
            if (ElementShapes.EndsOf(element) is var (start, end))
            {
                return ElementShapes.WithEnds(element, Point(start), Point(end));
            }

            if (ElementShapes.AreaOf(element) is RectD area)
            {
                var center = Point(area.Center);
                double width = area.Width * fx, height = area.Height * fy;
                var scaled = ElementShapes.WithArea(element, new RectD(center.X - width / 2, center.Y - height / 2, width, height));
                return scaled is SpeechBubbleElement bubble ? bubble with { Tail = Point(bubble.Tail) } : scaled;
            }

            var bounds = element.Bounds;
            return ElementShapes.WithArea(element, RectD.FromPoints(Point(new PointD(bounds.X, bounds.Y)), Point(new PointD(bounds.Right, bounds.Bottom))));
        }

        /// <summary>
        /// The area after its handle was dragged to the point: the opposite side stays where it is, also when the area is
        /// turned. With keepRatio a corner keeps the ratio of width and height.
        /// </summary>
        internal static RectD ResizedArea(RectD area, double rotation, int handle, PointD point, bool keepRatio)
        {
            // In the unturned area: the point turned back around the centre
            var local = HitTesting.Unturned(point, area, rotation);
            double left = area.X, top = area.Y, right = area.Right, bottom = area.Bottom;
            var place = SelectionHandles.Places[handle];
            if (place.X == 0)
            {
                left = local.X;
            }
            else if (place.X == 1)
            {
                right = local.X;
            }

            if (place.Y == 0)
            {
                top = local.Y;
            }
            else if (place.Y == 1)
            {
                bottom = local.Y;
            }

            if (keepRatio && place.X != 0.5 && place.Y != 0.5 && area.Width > 0 && area.Height > 0)
            {
                double ratio = area.Width / area.Height;
                double width = Math.Abs(right - left), height = Math.Abs(bottom - top);
                if (width / ratio > height)
                {
                    height = width / ratio;
                }
                else
                {
                    width = height * ratio;
                }

                if (place.X == 0)
                {
                    left = right - width * Math.Sign(right - left == 0 ? 1 : right - left);
                }
                else
                {
                    right = left + width * Math.Sign(right - left == 0 ? 1 : right - left);
                }

                if (place.Y == 0)
                {
                    top = bottom - height * Math.Sign(bottom - top == 0 ? 1 : bottom - top);
                }
                else
                {
                    bottom = top + height * Math.Sign(bottom - top == 0 ? 1 : bottom - top);
                }
            }

            var resized = RectD.FromPoints(new PointD(Math.Round(left), Math.Round(top)), new PointD(Math.Round(right), Math.Round(bottom)));
            resized = resized with { Width = Math.Max(1, resized.Width), Height = Math.Max(1, resized.Height) };
            if (rotation % 360 == 0)
            {
                return resized;
            }

            // The new area turns around its own centre: move it so the side which wasn't dragged stays in place
            var center = resized.Center.Turned(rotation, area.Center);
            return resized with { X = center.X - resized.Width / 2, Y = center.Y - resized.Height / 2 };
        }

        /// <summary>
        /// The point moved so the line from the start is at a multiple of 15 degrees
        /// </summary>
        internal static PointD Snapped(PointD start, PointD point)
        {
            double dx = point.X - start.X, dy = point.Y - start.Y, length = Math.Sqrt(dx * dx + dy * dy);
            double angle = Math.Round(Math.Atan2(dy, dx) / (Math.PI / 12)) * (Math.PI / 12);
            return new PointD(start.X + length * Math.Cos(angle), start.Y + length * Math.Sin(angle));
        }

        public void Up(ToolPointer pointer)
        {
            var drag = _drag;
            _drag = Drag.None;
            switch (drag)
            {
                case Drag.RubberBand:
                {
                    var found = HitTesting.ElementsIn(_host.Document, _rubberBand, pointer.Alt).Select(element => element.Id);
                    var selection = new HashSet<Guid>(pointer.Shift || pointer.Control ? _host.Selection : Array.Empty<Guid>());
                    selection.UnionWith(DocumentEdits.WithSets(_host.Document, found));
                    if (_moved)
                    {
                        _host.Select(selection);
                    }

                    _host.ToolChanged();
                    return;
                }
                case Drag.None:
                    return;
            }

            if (!_moved)
            {
                _host.CancelPreview();
                return;
            }

            string description = drag switch
            {
                Drag.Move => EditorTexts.Wpf.UndoMove,
                Drag.Rotate => EditorTexts.Wpf.UndoTurn,
                _ => EditorTexts.Wpf.UndoResize
            };
            _host.Commit(_host.Document, description);
        }

        public void Hover(ToolPointer pointer)
        {
            PixelSize = pointer.PixelSize;
            var cursor = ToolCursor.Arrow;
            var selected = SelectedElements();
            if (FindHandle(selected, pointer) is var (drag, handle))
            {
                cursor = drag switch
                {
                    Drag.Rotate => ToolCursor.Rotate,
                    Drag.Resize => SelectionHandles.CursorOf(handle, selected.Count == 1 ? FrameOf(selected[0]).Rotation : 0),
                    _ => ToolCursor.Cross
                };
            }
            else if (HitTesting.ElementAt(_host.Document, pointer.Point, pointer.Tolerance) != null)
            {
                cursor = ToolCursor.Move;
            }

            if (cursor != _cursor)
            {
                _cursor = cursor;
                _host.ToolChanged();
            }
        }

        public bool Key(ToolKey key, bool shift, bool control)
        {
            var selected = SelectedElements();
            switch (key)
            {
                case ToolKey.Escape when _drag != Drag.None:
                    Cancel();
                    return true;
                case ToolKey.Escape when selected.Count > 0:
                    _host.Select(Array.Empty<Guid>());
                    return true;
                case ToolKey.Left or ToolKey.Right or ToolKey.Up or ToolKey.Down when selected.Count > 0 && _drag == Drag.None:
                    double step = shift ? 10 : 1;
                    double dx = key == ToolKey.Left ? -step : key == ToolKey.Right ? step : 0;
                    double dy = key == ToolKey.Up ? -step : key == ToolKey.Down ? step : 0;
                    _host.Commit(DocumentEdits.Replace(_host.Document, selected.Select(element => ElementShapes.MovedByHand(element, dx, dy))), EditorTexts.Wpf.UndoMove);
                    return true;
                default:
                    return false;
            }
        }

        public void Cancel()
        {
            if (_drag is not Drag.None and not Drag.RubberBand)
            {
                _host.CancelPreview();
            }

            _drag = Drag.None;
            _host.ToolChanged();
        }
    }
}
