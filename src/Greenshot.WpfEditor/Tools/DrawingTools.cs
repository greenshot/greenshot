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
    /// Draws a new element by dragging: a box from the point pressed to the point released, or a line between them. A click
    /// without dragging places an element of the default size where it makes sense (text, step label, emoji, speech bubble).
    /// Shift draws a square or a line at a multiple of 15 degrees. The new element is selected; a text starts typing.
    /// </summary>
    public sealed class CreateTool : IEditorTool
    {
        private readonly IToolHost _host;
        private GreenshotDocument _before;
        private PointD _from;
        private Guid _id;
        private bool _dragging;

        public CreateTool(IToolHost host, ToolKind kind)
        {
            _host = host;
            Kind = kind;
        }

        public ToolKind Kind { get; }

        public ToolCursor Cursor => ToolCursor.Cross;

        public IReadOnlyList<Adorner> Adorners => Array.Empty<Adorner>();

        public void Down(ToolPointer pointer)
        {
            _before = _host.Document;
            _from = Rounded(pointer.Point);
            _id = Uuid7.CreateVersion7();
            _dragging = true;
            _host.Select(Array.Empty<Guid>());
        }

        public void Move(ToolPointer pointer)
        {
            if (!_dragging)
            {
                return;
            }

            var element = Create(_from, Rounded(pointer.Point), pointer.Shift, false);
            _host.Preview(element == null ? _before : DocumentEdits.Add(_before, new[] { element }));
        }

        public void Up(ToolPointer pointer)
        {
            if (!_dragging)
            {
                return;
            }

            _dragging = false;
            var to = Rounded(pointer.Point);
            bool click = Math.Abs(to.X - _from.X) < 3 * pointer.PixelSize && Math.Abs(to.Y - _from.Y) < 3 * pointer.PixelSize;
            var element = Create(_from, to, pointer.Shift, click);
            if (element == null)
            {
                _host.CancelPreview();
                return;
            }

            _host.Commit(DocumentEdits.Add(_before, new[] { element }), EditorTexts.Wpf.UndoAdd);
            _host.Select(new[] { element.Id });
            if (element is TextElement or SpeechBubbleElement)
            {
                _host.EditText(element.Id);
            }
        }

        public void Hover(ToolPointer pointer)
        {
        }

        public bool Key(ToolKey key, bool shift, bool control)
        {
            if (key == ToolKey.Escape && _dragging)
            {
                Cancel();
                return true;
            }

            return false;
        }

        public void Cancel()
        {
            if (_dragging)
            {
                _dragging = false;
                _host.CancelPreview();
            }
        }

        private static PointD Rounded(PointD point) => new(Math.Round(point.X), Math.Round(point.Y));

        /// <summary>
        /// The new element from the point pressed to the point now, null when it would be too small to see
        /// </summary>
        private Element Create(PointD from, PointD to, bool constrain, bool click)
        {
            var style = _host.StyleFor(Kind);
            if (Kind is ToolKind.Line or ToolKind.Arrow)
            {
                if (click)
                {
                    return null;
                }

                var end = constrain ? SelectTool.Snapped(from, to) : to;
                return Kind == ToolKind.Line
                    ? new LineElement(_id, from, end) { Style = style }
                    : new ArrowElement(_id, from, end, _host.Options.ArrowHeads) { Style = style };
            }

            var area = click ? DefaultArea(from) : Box(from, to, constrain);
            if (area == null)
            {
                return null;
            }

            var box = area.Value;
            var options = _host.Options;
            return Kind switch
            {
                ToolKind.Rectangle => new RectangleElement(_id, box) { Style = style },
                ToolKind.Ellipse => new EllipseElement(_id, box) { Style = style },
                ToolKind.Text => new TextElement(_id, box, string.Empty) { Style = style },
                ToolKind.SpeechBubble => new SpeechBubbleElement(_id, box, string.Empty, new PointD(box.X + box.Width * 0.25, box.Bottom + Math.Max(20, box.Height * 0.5))) { Style = style },
                ToolKind.StepLabel => new StepLabelElement(_id, box) { Style = style },
                ToolKind.Emoji => new EmojiElement(_id, box, options.Emoji) { Style = style },
                ToolKind.Highlight => new HighlightElement(_id, box, options.HighlightMode) { Style = style, Brightness = options.Brightness, BlurRadius = options.BlurRadius, Magnification = options.Magnification },
                ToolKind.Obfuscate => new ObfuscateElement(_id, box, options.ObfuscateMode, options.ObfuscateMode == ObfuscateMode.Pixelate ? options.PixelSize : options.BlurRadius) { Style = style },
                ToolKind.CutMark => new CutMarkElement(_id, box, box.Width >= box.Height, new Random().Next()) { Style = style, Shape = new EdgeShape(options.CutMarkShape) },
                _ => null
            };
        }

        /// <summary>
        /// The box between the points, a square with constrain; null when it is less than 2 pixels in one direction
        /// </summary>
        private static RectD? Box(PointD from, PointD to, bool constrain)
        {
            if (constrain)
            {
                double size = Math.Max(Math.Abs(to.X - from.X), Math.Abs(to.Y - from.Y));
                to = new PointD(from.X + Math.Sign(to.X - from.X) * size, from.Y + Math.Sign(to.Y - from.Y) * size);
            }

            var box = RectD.FromPoints(from, to);
            return box.Width < 2 || box.Height < 2 ? null : box;
        }

        /// <summary>
        /// The size of an element placed with a click, null for elements which need a size
        /// </summary>
        private RectD? DefaultArea(PointD at) => Kind switch
        {
            ToolKind.Text => new RectD(at.X, at.Y, 200, 40),
            ToolKind.SpeechBubble => new RectD(at.X, at.Y, 200, 80),
            ToolKind.StepLabel => new RectD(at.X - 15, at.Y - 15, 30, 30),
            ToolKind.Emoji => new RectD(at.X - 32, at.Y - 32, 64, 64),
            _ => null
        };
    }

    /// <summary>
    /// Draws a freehand line: the points the mouse went through, close points are left out
    /// </summary>
    public sealed class FreehandTool : IEditorTool
    {
        private readonly IToolHost _host;
        private GreenshotDocument _before;
        private List<PointD> _points;
        private Guid _id;

        public FreehandTool(IToolHost host)
        {
            _host = host;
        }

        public ToolKind Kind => ToolKind.Freehand;

        public ToolCursor Cursor => ToolCursor.Pen;

        public IReadOnlyList<Adorner> Adorners => Array.Empty<Adorner>();

        public void Down(ToolPointer pointer)
        {
            _before = _host.Document;
            _points = new List<PointD> { pointer.Point };
            _id = Uuid7.CreateVersion7();
            _host.Select(Array.Empty<Guid>());
        }

        public void Move(ToolPointer pointer)
        {
            if (_points == null)
            {
                return;
            }

            // Points closer than a screen pixel add nothing
            if (HitTesting.Distance(_points[_points.Count - 1], pointer.Point) < pointer.PixelSize)
            {
                return;
            }

            _points.Add(pointer.Point);
            _host.Preview(DocumentEdits.Add(_before, new[] { Element() }));
        }

        public void Up(ToolPointer pointer)
        {
            if (_points == null)
            {
                return;
            }

            if (_points.Count < 2)
            {
                _points = null;
                _host.CancelPreview();
                return;
            }

            _host.Commit(DocumentEdits.Add(_before, new[] { Element() }), EditorTexts.Wpf.UndoAdd);
            _points = null;
        }

        private Element Element() => new FreehandElement(_id, _points.ToList()) { Style = _host.StyleFor(ToolKind.Freehand) };

        public void Hover(ToolPointer pointer)
        {
        }

        public bool Key(ToolKey key, bool shift, bool control) => false;

        public void Cancel()
        {
            if (_points != null)
            {
                _points = null;
                _host.CancelPreview();
            }
        }
    }

    /// <summary>
    /// Chooses the area to keep: drag it, change it with its handles, Enter (or Confirm) crops, Escape cancels
    /// </summary>
    public sealed class CropTool : IEditorTool
    {
        private readonly IToolHost _host;
        private RectD? _area;
        private PointD _from;
        private int _handle = -1;
        private bool _dragging;
        private RectD _startArea;

        public CropTool(IToolHost host)
        {
            _host = host;
        }

        public ToolKind Kind => ToolKind.Crop;

        public ToolCursor Cursor { get; private set; } = ToolCursor.Cross;

        /// <summary>
        /// The area which stays, null while none is chosen
        /// </summary>
        public RectD? Area => _area;

        public IReadOnlyList<Adorner> Adorners
        {
            get
            {
                if (_area is not RectD area)
                {
                    return Array.Empty<Adorner>();
                }

                var adorners = new List<Adorner> { new(AdornerKind.CropArea, area) };
                for (int handle = 0; handle < 8; handle++)
                {
                    adorners.Add(new Adorner(AdornerKind.Handle, Point: SelectionHandles.Point(area, 0, handle)));
                }

                return adorners;
            }
        }

        public void Down(ToolPointer pointer)
        {
            _from = pointer.Point;
            _dragging = true;
            _handle = -1;
            if (_area is RectD area)
            {
                _startArea = area;
                for (int handle = 0; handle < 8; handle++)
                {
                    if (HitTesting.Distance(SelectionHandles.Point(area, 0, handle), pointer.Point) <= SelectionHandles.GrabRadius * pointer.PixelSize)
                    {
                        _handle = handle;
                        return;
                    }
                }

                if (HitTesting.Contains(area, pointer.Point))
                {
                    // Moves the area
                    _handle = 8;
                    return;
                }
            }

            _area = null;
        }

        public void Move(ToolPointer pointer)
        {
            if (!_dragging)
            {
                return;
            }

            var canvas = _host.Document.CanvasArea();
            var point = new PointD(Math.Max(canvas.X, Math.Min(canvas.Right, Math.Round(pointer.Point.X))), Math.Max(canvas.Y, Math.Min(canvas.Bottom, Math.Round(pointer.Point.Y))));
            if (_handle == 8)
            {
                double dx = Math.Round(pointer.Point.X - _from.X), dy = Math.Round(pointer.Point.Y - _from.Y);
                dx = Math.Max(canvas.X - _startArea.X, Math.Min(canvas.Right - _startArea.Right, dx));
                dy = Math.Max(canvas.Y - _startArea.Y, Math.Min(canvas.Bottom - _startArea.Bottom, dy));
                _area = _startArea.Offset(dx, dy);
            }
            else if (_handle >= 0)
            {
                _area = SelectTool.ResizedArea(_startArea, 0, _handle, point, pointer.Shift);
            }
            else
            {
                var from = new PointD(Math.Max(canvas.X, Math.Min(canvas.Right, Math.Round(_from.X))), Math.Max(canvas.Y, Math.Min(canvas.Bottom, Math.Round(_from.Y))));
                var box = RectD.FromPoints(from, point);
                _area = box.Width >= 1 && box.Height >= 1 ? box : null;
            }

            _host.ToolChanged();
        }

        public void Up(ToolPointer pointer)
        {
            _dragging = false;
            if (pointer.ClickCount >= 2 && _area != null)
            {
                Apply();
            }
        }

        public void Hover(ToolPointer pointer)
        {
            var cursor = ToolCursor.Cross;
            if (_area is RectD area)
            {
                for (int handle = 0; handle < 8; handle++)
                {
                    if (HitTesting.Distance(SelectionHandles.Point(area, 0, handle), pointer.Point) <= SelectionHandles.GrabRadius * pointer.PixelSize)
                    {
                        cursor = SelectionHandles.CursorOf(handle, 0);
                    }
                }

                if (cursor == ToolCursor.Cross && HitTesting.Contains(area, pointer.Point))
                {
                    cursor = ToolCursor.Move;
                }
            }

            if (cursor != Cursor)
            {
                Cursor = cursor;
                _host.ToolChanged();
            }
        }

        /// <summary>
        /// Crops the document to the chosen area
        /// </summary>
        public bool Apply()
        {
            if (_area is not RectD area)
            {
                return false;
            }

            _area = null;
            _host.Commit(DocumentEdits.Crop(_host.Document, area), EditorTexts.Wpf.UndoCrop);
            _host.ToolChanged();
            return true;
        }

        public bool Key(ToolKey key, bool shift, bool control)
        {
            switch (key)
            {
                case ToolKey.Enter:
                    return Apply();
                case ToolKey.Escape when _area != null:
                    Cancel();
                    return true;
                default:
                    return false;
            }
        }

        public void Cancel()
        {
            _dragging = false;
            _area = null;
            _host.ToolChanged();
        }
    }
}
