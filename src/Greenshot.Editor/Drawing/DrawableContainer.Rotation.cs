using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.Serialization;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Common.Extensions;

namespace Greenshot.Editor.Drawing
{
    public abstract partial class DrawableContainer
    {
        // Null is the legacy identity. Double precision composition preserves original
        // geometry and bitmap/vector content through repeated object rotations.
        [OptionalField] private double[] _objectTransform;
        public bool HasObjectTransform => _objectTransform != null;
        public double ObjectAngle => _objectTransform == null ? 0 : Math.Atan2(_objectTransform[1], _objectTransform[0]) * 180 / Math.PI;
        internal double[] ObjectTransformState => _objectTransform == null ? null : (double[])_objectTransform.Clone();
        internal void RestoreObjectTransform(double[] state) { _objectTransform = state == null ? null : (double[])state.Clone(); NotifyGeometryChanged(); }
        internal NativeRect ModelBounds => new NativeRect(Left, Top, Width, Height).Normalize();
        public NativeRect VisualBounds => TransformBounds(ModelBounds);
        internal (double X, double Y) ImageCentre => Map(Left + Width / 2d, Top + Height / 2d);
        private (double X, double Y) Map(double x, double y)
        {
            var m = _objectTransform;
            return m == null ? (x, y) : (x * m[0] + y * m[2] + m[4], x * m[1] + y * m[3] + m[5]);
        }
        public PointF ModelToImage(PointF point)
        { var p = Map(point.X, point.Y); return new PointF((float)p.X, (float)p.Y); }
        public PointF ImageToModel(PointF point)
        {
            var m = _objectTransform; if (m == null) return point;
            double det = m[0] * m[3] - m[1] * m[2];
            return new PointF((float)(((point.X - m[4]) * m[3] - (point.Y - m[5]) * m[2]) / det),
                (float)(((point.Y - m[5]) * m[0] - (point.X - m[4]) * m[1]) / det));
        }
        internal static Point LocalPixel(PointF point)
        {
            if (float.IsNaN(point.X) || float.IsNaN(point.Y) || Math.Abs((double)point.X) > int.MaxValue / 4d || Math.Abs((double)point.Y) > int.MaxValue / 4d)
                throw new ArgumentException("The value is outside the supported local image geometry range.");
            return new Point((int)Math.Round(point.X, MidpointRounding.AwayFromZero), (int)Math.Round(point.Y, MidpointRounding.AwayFromZero));
        }
        internal NativeRect TransformBounds(NativeRect bounds)
        {
            if (!HasObjectTransform) return bounds;
            var corners = new[] { new PointF(bounds.Left, bounds.Top), new PointF(bounds.Right, bounds.Top), new PointF(bounds.Right, bounds.Bottom), new PointF(bounds.Left, bounds.Bottom) }.Select(ModelToImage).ToArray();
            int x = (int)Math.Floor(corners.Min(p => p.X)), y = (int)Math.Floor(corners.Min(p => p.Y));
            return new NativeRect(x, y, (int)Math.Ceiling(corners.Max(p => p.X)) - x, (int)Math.Ceiling(corners.Max(p => p.Y)) - y);
        }
        internal bool ClickableAtImage(int x, int y)
        { return ClickableAt(x, y); }
        internal void RotateObject(double degrees, double pivotX, double pivotY)
        {
            double radians = degrees % 360 * Math.PI / 180, c = Math.Cos(radians), s = Math.Sin(radians);
            ComposeObjectTransform(new[] { c, s, -s, c, pivotX - c * pivotX + s * pivotY, pivotY - s * pivotX - c * pivotY });
        }
        internal void ComposeObjectTransform(Matrix matrix) => ComposeObjectTransform(matrix.Elements.Select(v => (double)v).ToArray());
        private void ComposeObjectTransform(double[] b)
        {
            var a = _objectTransform ?? new double[] { 1, 0, 0, 1, 0, 0 };
            _objectTransform = new[] { b[0]*a[0]+b[2]*a[1], b[1]*a[0]+b[3]*a[1], b[0]*a[2]+b[2]*a[3], b[1]*a[2]+b[3]*a[3], b[0]*a[4]+b[2]*a[5]+b[4], b[1]*a[4]+b[3]*a[5]+b[5] };
            NotifyGeometryChanged();
        }
        internal void DrawObjectContent(Graphics graphics, Bitmap bitmap, Greenshot.Base.Interfaces.Drawing.RenderMode mode, NativeRect clip, bool skipInvertedFilters)
        {
            if (!HasObjectTransform) { DrawModelContent(graphics, bitmap, mode, clip, skipInvertedFilters); return; }
            var saved = graphics.Save();
            try
            {
                var m = _objectTransform;
                using var transform = new Matrix((float)m[0], (float)m[1], (float)m[2], (float)m[3], (float)m[4], (float)m[5]);
                graphics.MultiplyTransform(transform, MatrixOrder.Prepend);
                DrawModelContent(graphics, bitmap, mode, clip, skipInvertedFilters);
            }
            finally { graphics.Restore(saved); }
        }
    }
}
