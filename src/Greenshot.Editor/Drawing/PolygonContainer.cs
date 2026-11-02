using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.Serialization;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing.Fields;

namespace Greenshot.Editor.Drawing
{
    /// <summary>Closed, indexed vertices. Relative coordinates follow ordinary bounds translation and resizing.</summary>
    [Serializable]
    public sealed class PolygonContainer : DrawableContainer
    {
        private Point[] _vertices = Array.Empty<Point>();
        private int _referenceWidth = 1, _referenceHeight = 1;
        [NonSerialized] private int _selectedVertex;
        public int SelectedVertex
        {
            get => _selectedVertex;
            set { if (value < 0 || value >= _vertices.Length) return; _selectedVertex = value; NotifyGeometryChanged(); }
        }
        public Point[] Vertices => ImageVertices.Select(Point.Round).ToArray();
        internal PointF[] ImageVertices => ModelVertices.Select(p => ModelToImage(p)).ToArray();
        internal Point[] ModelVertices => _vertices.Select(p => new Point(
            Left + (int)Math.Round((double)p.X * Width / _referenceWidth), Top + (int)Math.Round((double)p.Y * Height / _referenceHeight))).ToArray();

        public PolygonContainer(ISurface parent) : base(parent) => CreateDefaultAdorners();
        protected override void OnDeserialized(StreamingContext context) => CreateDefaultAdorners();
        protected override void InitializeFields()
        {
            AddField(GetType(), FieldType.LINE_THICKNESS, 2);
            AddField(GetType(), FieldType.LINE_COLOR, Color.Red);
            AddField(GetType(), FieldType.FILL_COLOR, Color.Transparent);
        }
        internal void SetVertices(Point[] points)
        {
            if (points == null || points.Length < 3) throw new ArgumentException("A polygon needs at least three vertices.");
            Invalidate();
            int x = points.Min(p => p.X), y = points.Min(p => p.Y);
            int w = Math.Max(1, points.Max(p => p.X) - x), h = Math.Max(1, points.Max(p => p.Y) - y);
            _vertices = points.Select(p => new Point(p.X - x, p.Y - y)).ToArray();
            _referenceWidth = w; _referenceHeight = h;
            Left = x; Top = y; Width = w; Height = h;
            NotifyGeometryChanged(); Invalidate();
        }
        internal void SetVertex(int index, Point point)
        {
            var vertices = ModelVertices;
            if (vertices[index] == point) return;
            vertices[index] = point; SetVertices(vertices);
        }
        internal void SetImageVertex(int index, PointF point) => SetVertex(index, LocalPixel(ImageToModel(point)));
        internal double SignedArea
        {
            get
            {
                var v = ImageVertices; double area = 0;
                for (int i = 0; i < v.Length; i++) { var next = v[(i + 1) % v.Length]; area += (double)v[i].X * next.Y - (double)next.X * v[i].Y; }
                return area / 2;
            }
        }
        // Positive area is clockwise on a screen whose Y axis points down. Zero-area
        // and self-crossing cancellation use the previous stable index as the fallback.
        internal int AnticlockwiseNeighbour(int index) => (index + (SignedArea < 0 ? 1 : _vertices.Length - 1)) % _vertices.Length;
        public override void Transform(Matrix matrix)
        {
            if (HasObjectTransform) { ComposeObjectTransform(matrix); return; }
            var points = ModelVertices; matrix.TransformPoints(points); SetVertices(points);
        }
        private GraphicsPath Path()
        {
            var path = new GraphicsPath();
            if (_vertices.Length >= 3) path.AddPolygon(ModelVertices);
            return path;
        }
        public override void Draw(Graphics graphics, RenderMode renderMode)
        {
            if (_vertices.Length < 3) return;
            using var path = Path();
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var fill = new SolidBrush(GetFieldValueAsColor(FieldType.FILL_COLOR));
            graphics.FillPath(fill, path);
            int thickness = GetFieldValueAsInt(FieldType.LINE_THICKNESS);
            if (thickness <= 0) return;
            using var pen = new Pen(GetFieldValueAsColor(FieldType.LINE_COLOR), thickness) { LineJoin = LineJoin.Round };
            graphics.DrawPath(pen, path);
        }
        protected override bool ClickableAtModel(int x, int y)
        {
            if (_vertices.Length < 3) return false;
            using var path = Path();
            using var pen = new Pen(Color.Black, Math.Max(1, GetFieldValueAsInt(FieldType.LINE_THICKNESS)) + 8);
            return path.IsVisible(x, y) || path.IsOutlineVisible(x, y, pen);
        }
    }
}
