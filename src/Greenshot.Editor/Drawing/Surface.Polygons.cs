using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Greenshot.Base.Interfaces;
using Greenshot.Editor.Helpers;

namespace Greenshot.Editor.Drawing
{
    public sealed partial class Surface
    {
        private readonly List<Point> _polygonPoints = new List<Point>();
        private Point _polygonPointer;
        private PolygonContainer _vertexOwner;
        private Point[] _vertexBefore;
        private int _vertexAnchor;

        internal void CancelPolygonInteraction()
        {
            _polygonPoints.Clear();
            if (_vertexOwner != null) _vertexOwner.SetVertices(_vertexBefore);
            _vertexOwner = null; _vertexBefore = null;
            ClearSnapGuide(); Capture = false; Invalidate();
        }

        internal Point PolygonEndpoint(Point pointer, Point? anchor, PolygonContainer owner, Keys modifiers)
        {
            Point Constrain(Point p) => anchor.HasValue ? GetScaleEndpoint(anchor.Value, p, modifiers) : p;
            var baseline = Constrain(pointer);
            return Constrain(SnapGesture(baseline, anchor, owner, anchor.HasValue,
                p => ObjectSnapHelper.DistanceSquared(Constrain(p), p) <= 2));
        }

        private bool PolygonMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && (_polygonPoints.Count > 0 || _vertexOwner != null || DrawingMode == DrawingModes.Polygon))
            { CancelPolygonInteraction(); DrawingMode = DrawingModes.None; return true; }
            if (DrawingMode == DrawingModes.Polygon)
            {
                if (e.Button != MouseButtons.Left) return true;
                Focus();
                // Closing targets the first vertex directly, independently of object snapping.
                if (_polygonPoints.Count >= 3 && ObjectSnapHelper.DistanceSquared(e.Location, _polygonPoints[0]) <= Math.Pow(8 / (double)_zoomFactor, 2))
                {
                    var polygon = (PolygonContainer)_undrawnElement;
                    polygon.SetVertices(_polygonPoints.ToArray());
                    _undrawnElement = null;
                    DrawingMode = DrawingModes.None;
                    AddElement(polygon); SelectElement(polygon); polygon.Selected = true;
                    return true;
                }
                var point = PolygonEndpoint(e.Location, _polygonPoints.Count == 0 ? (Point?)null : _polygonPoints.Last(), null, ModifierKeys);
                if (_polygonPoints.Count == 0 || point != _polygonPoints.Last()) _polygonPoints.Add(point);
                _polygonPointer = point; Invalidate(); return true;
            }
            if (DrawingMode != DrawingModes.None || e.Button != MouseButtons.Left || selectedElements.Count != 1) return false;
            if (!(selectedElements.First() is PolygonContainer selected)) return false;
            var vertices = selected.ImageVertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                if (ObjectSnapHelper.DistanceSquared(e.Location, vertices[i]) > Math.Pow(7 / (double)_zoomFactor, 2)) continue;
                selected.SelectedVertex = i;
                _vertexOwner = selected; _vertexBefore = selected.ModelVertices;
                _vertexAnchor = selected.AnticlockwiseNeighbour(i);
                Focus(); Capture = true; Invalidate(); return true;
            }
            return false;
        }

        internal void MovePolygonVertex(Point pointer, Keys modifiers)
        {
            if (_vertexOwner != null) _vertexOwner.SetImageVertex(_vertexOwner.SelectedVertex,
                PolygonEndpoint(pointer, Point.Round(_vertexOwner.ModelToImage(_vertexBefore[_vertexAnchor])), _vertexOwner, modifiers));
        }

        private bool PolygonMouseMove(MouseEventArgs e)
        {
            if (_vertexOwner != null)
            {
                MovePolygonVertex(e.Location, ModifierKeys);
                Invalidate(); return true;
            }
            if (DrawingMode != DrawingModes.Polygon) return false;
            _polygonPointer = PolygonEndpoint(e.Location, _polygonPoints.Count == 0 ? (Point?)null : _polygonPoints.Last(), null, ModifierKeys);
            Cursor = Cursors.Cross; Invalidate(); return true;
        }

        private bool PolygonMouseUp(MouseEventArgs e)
        {
            if (_vertexOwner != null && e.Button == MouseButtons.Left)
            {
                PolygonMouseMove(e);
                var owner = _vertexOwner; var after = owner.ModelVertices;
                if (!after.SequenceEqual(_vertexBefore))
                {
                    owner.SetVertices(_vertexBefore); owner.MakeBoundsChangeUndoable(false);
                    owner.SetVertices(after); Modified = true;
                }
                _vertexOwner = null; _vertexBefore = null; Capture = false; ClearSnapGuide(); Invalidate(); return true;
            }
            return DrawingMode == DrawingModes.Polygon;
        }

        private void PaintPolygonInteraction(Graphics graphics)
        {
            var saved = graphics.Save();
            graphics.ScaleTransform(_zoomFactor, _zoomFactor);
            float size = (float)(6 / (double)_zoomFactor);
            if (_polygonPoints.Count > 0)
            {
                var points = _polygonPoints.Concat(new[] { _polygonPointer }).ToArray();
                using var pen = new Pen(Color.DeepSkyBlue, (float)(1 / (double)_zoomFactor)) { DashStyle = DashStyle.Dash };
                graphics.DrawLines(pen, points);
                graphics.DrawEllipse(pen, points[0].X - size, points[0].Y - size, size * 2, size * 2);
            }
            if (selectedElements.Count == 1 && selectedElements.First() is PolygonContainer polygon)
            {
                var vertices = polygon.ImageVertices;
                for (int i = 0; i < vertices.Length; i++)
                {
                    var p = vertices[i];
                    graphics.FillEllipse(i == polygon.SelectedVertex ? Brushes.DeepSkyBlue : Brushes.White, p.X - size, p.Y - size, size * 2, size * 2);
                    graphics.DrawEllipse(Pens.Black, p.X - size, p.Y - size, size * 2, size * 2);
                    graphics.DrawString((i + 1).ToString(), SystemFonts.DefaultFont, Brushes.Black, p.X + size, p.Y + size);
                }
            }
            graphics.Restore(saved);
        }
    }
}
