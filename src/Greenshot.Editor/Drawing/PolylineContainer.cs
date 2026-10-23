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
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Forms;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing.Adorners;
using Greenshot.Editor.Drawing.Fields;

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// A sequence of straight segments, drawn with a click per corner and stored independently of its editable bounds.
    /// Clicking the start again closes it: the last corner connects to the first.
    /// </summary>
    [Serializable]
    public class PolylineContainer : DrawableContainer
    {
        private const int CloseSnapDistance = 8;

        private List<Point> _points = new List<Point>();
        private Rectangle _pointBounds;
        private bool _closed;
        [NonSerialized] private Point? _previewPoint;

        public PolylineContainer(ISurface parent) : base(parent)
        {
        }

        protected override void InitializeFields()
        {
            AddField(GetType(), FieldType.LINE_THICKNESS, 2);
            AddField(GetType(), FieldType.LINE_COLOR, Color.Red);
            AddField(GetType(), FieldType.SHADOW, true);
        }

        protected override void OnDeserialized(StreamingContext context)
        {
            UpdateVertexAdorners();
        }

        /// <summary>
        /// A snapshot of the committed vertices in image coordinates. Moving and resizing
        /// use the usual bounds mementos without changing the underlying vertices.
        /// </summary>
        public Point[] Points => Enumerable.Range(0, _points.Count).Select(GetVertexPosition).ToArray();

        internal Point GetVertexPosition(int index)
        {
            Point point = _points[index];
            return new Point(
                Left + (_pointBounds.Width == 0 ? 0 : (int)Math.Round((double)(point.X - _pointBounds.Left) * Width / _pointBounds.Width)),
                Top + (_pointBounds.Height == 0 ? 0 : (int)Math.Round((double)(point.Y - _pointBounds.Top) * Height / _pointBounds.Height)));
        }

        private void UpdateVertexAdorners()
        {
            // Keep active handles while dragging; their positions follow the vertices.
            if (Adorners.Count == _points.Count)
            {
                return;
            }

            Adorners.Clear();
            for (int index = 0; index < _points.Count; index++)
            {
                Adorners.Add(new PolylineVertexAdorner(this, index));
            }
        }

        public override bool InitContent() => _points.Count >= 2;

        /// <summary>
        /// The last vertex connects back to the first one.
        /// </summary>
        internal bool IsClosed => _closed;

        /// <summary>
        /// Every click adds a corner, until the polyline is closed.
        /// </summary>
        public override bool AcceptsMoreClicks => !_closed;

        // Styling the draft is part of its creation, not a separate undoable edit.
        public override bool IsUndoable => Status is not (EditStatus.UNDRAWN or EditStatus.DRAWING);

        public override bool HandleMouseDown(int x, int y)
        {
            AddPoint(new Point(x, y), (Control.ModifierKeys & Keys.Shift) != 0);
            return true;
        }

        internal void AddPoint(Point point, bool snapToAxes)
        {
            Invalidate();
            point = ConstrainNewPoint(point, snapToAxes);
            if (_points.Count >= 3 && point == GetVertexPosition(0))
            {
                // After two segments, a click at the start closes the polyline instead of adding the start again.
                _closed = true;
            }
            else if (_points.Count == 0 || GetVertexPosition(_points.Count - 1) != point)
            {
                SetPoints(new List<Point>(Points) { point });
            }

            _previewPoint = null;
            Invalidate();
        }

        public override bool HandleMouseMove(int x, int y)
        {
            UpdatePreview(new Point(x, y), (Control.ModifierKeys & Keys.Shift) != 0);
            return true;
        }

        internal void UpdatePreview(Point point, bool snapToAxes)
        {
            Invalidate();
            _previewPoint = ConstrainNewPoint(point, snapToAxes);
            Invalidate();
        }

        private Point ConstrainNewPoint(Point point, bool snapToAxes)
        {
            // Give closing the polyline priority over Shift's axis constraint.
            if (_points.Count >= 3 && IsNearStart(point))
            {
                return GetVertexPosition(0);
            }

            return snapToAxes && _points.Count > 0
                ? SnapToAxes(GetVertexPosition(_points.Count - 1), point)
                : point;
        }

        private bool IsNearStart(Point point)
        {
            // Keep the magnet the same size on screen at every zoom and DPI.
            Point start = GetVertexPosition(0);
            double zoom = Parent == null ? 1 : (double)Parent.ZoomFactor;
            double radius = CloseSnapDistance * (Parent?.CurrentDpi ?? 96) / 96.0;
            double dx = ((double)point.X - start.X) * zoom;
            double dy = ((double)point.Y - start.Y) * zoom;
            return dx * dx + dy * dy <= radius * radius;
        }

        private void SetPoints(List<Point> points)
        {
            _points = points;
            _pointBounds = Rectangle.FromLTRB(points.Min(point => point.X), points.Min(point => point.Y),
                points.Max(point => point.X), points.Max(point => point.Y));
            Bounds = _pointBounds;
            UpdateVertexAdorners();
        }

        private static Point SnapToAxes(Point anchor, Point point)
        {
            return Math.Abs((long)point.X - anchor.X) >= Math.Abs((long)point.Y - anchor.Y)
                ? new Point(point.X, anchor.Y)
                : new Point(anchor.X, point.Y);
        }

        internal Point ConstrainVertex(int index, Point point, bool snapToAxes)
        {
            if (!snapToAxes || _points.Count < 2)
            {
                return point;
            }

            // Snap to the previous vertex; the first vertex of an open polyline only has a following one.
            int neighbor = index > 0 ? index - 1 : _closed ? _points.Count - 1 : 1;
            return SnapToAxes(GetVertexPosition(neighbor), point);
        }

        internal void MoveVertex(int index, Point point)
        {
            Invalidate();
            var points = new List<Point>(Points);
            points[index] = point;
            SetPoints(points);
            if (Parent != null)
            {
                Parent.Modified = true;
            }

            Invalidate();
        }

        internal IMemento CreateVertexChangeMemento() => new VertexChangeMemento(this);

        public override void Transform(Matrix matrix)
        {
            if (matrix == null || _points.Count == 0)
            {
                return;
            }

            var points = Points;
            matrix.TransformPoints(points);
            SetPoints(new List<Point>(points));
            if (_previewPoint.HasValue)
            {
                var preview = new[] { _previewPoint.Value };
                matrix.TransformPoints(preview);
                _previewPoint = preview[0];
            }
        }

        /// <summary>
        /// The segment to the pointer is only shown while the polyline is drawn.
        /// </summary>
        private bool ShowsPreview => Status == EditStatus.DRAWING && _previewPoint.HasValue && _points.Count > 0;

        public override NativeRect DrawingBounds
        {
            get
            {
                if (_points.Count == 0)
                {
                    return NativeRect.Empty;
                }

                NativeRect bounds = base.DrawingBounds;
                if (ShowsPreview)
                {
                    int margin = GetFieldValueAsInt(FieldType.LINE_THICKNESS) + 15;
                    Point preview = _previewPoint.Value;
                    bounds = bounds.Union(new NativeRect(preview.X - margin, preview.Y - margin, margin * 2, margin * 2));
                }

                return bounds;
            }
        }

        public override void Draw(Graphics graphics, RenderMode renderMode)
        {
            int thickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS);
            if (thickness <= 0)
            {
                return;
            }

            var points = new List<Point>(Points);
            // The rubber band follows the pointer only in the editor, never in an export.
            if (renderMode == RenderMode.EDIT && ShowsPreview)
            {
                points.Add(_previewPoint.Value);
            }

            if (points.Count < 2)
            {
                return;
            }

            graphics.SmoothingMode = SmoothingMode.HighQuality;
            Point[] vertices = points.ToArray();
            if (GetFieldValueAsBool(FieldType.SHADOW))
            {
                DrawShadow(thickness, (alpha, step, shadowPen, brush) =>
                {
                    shadowPen.LineJoin = LineJoin.Round;
                    DrawSegments(graphics, shadowPen, vertices.Select(point => new Point(point.X + step, point.Y + step)).ToArray());
                });
            }

            using var pen = new Pen(GetFieldValueAsColor(FieldType.LINE_COLOR), thickness)
            {
                LineJoin = LineJoin.Round,
                StartCap = LineCap.Round,
                EndCap = LineCap.Round
            };
            DrawSegments(graphics, pen, vertices);
        }

        /// <summary>
        /// A closed polyline is one outline, joined at the start like at every other corner.
        /// </summary>
        private void DrawSegments(Graphics graphics, Pen pen, Point[] vertices)
        {
            if (_closed)
            {
                graphics.DrawPolygon(pen, vertices);
            }
            else
            {
                graphics.DrawLines(pen, vertices);
            }
        }

        public override bool ClickableAt(int x, int y)
        {
            if (_points.Count < 2)
            {
                return false;
            }

            using var path = new GraphicsPath();
            if (_closed)
            {
                path.AddPolygon(Points);
            }
            else
            {
                path.AddLines(Points);
            }

            using var pen = new Pen(Color.White, Math.Max(1, GetFieldValueAsInt(FieldType.LINE_THICKNESS)) + 5)
            {
                LineJoin = LineJoin.Round
            };
            return path.IsOutlineVisible(x, y, pen);
        }

        /// <summary>
        /// Store the original geometry as well as the current bounds so undo remains exact
        /// after a resize, including a collapsed or flipped dimension.
        /// </summary>
        private sealed class VertexChangeMemento : IMemento
        {
            private PolylineContainer _container;
            private readonly Point[] _points;
            private readonly Rectangle _pointBounds;
            private readonly Rectangle _bounds;

            public VertexChangeMemento(PolylineContainer container)
            {
                _container = container;
                _points = container._points.ToArray();
                _pointBounds = container._pointBounds;
                _bounds = new Rectangle(container.Left, container.Top, container.Width, container.Height);
            }

            public bool Merge(IMemento other) => false;

            public IMemento Restore()
            {
                var inverse = new VertexChangeMemento(_container);
                _container.Invalidate();
                _container._points = new List<Point>(_points);
                _container._pointBounds = _pointBounds;
                _container.Left = _bounds.X;
                _container.Top = _bounds.Y;
                _container.Width = _bounds.Width;
                _container.Height = _bounds.Height;
                _container.UpdateVertexAdorners();
                if (_container.Parent != null)
                {
                    _container.Parent.Modified = true;
                }

                _container.Invalidate();
                return inverse;
            }

            public void Dispose()
            {
                _container = null;
            }
        }
    }
}
