using System;
using System.Collections.Generic;
using System.Drawing;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;

namespace Greenshot.Editor.Helpers
{
    internal static class ObjectSnapHelper
    {
        internal static bool IsLine(IDrawableContainer shape) => shape?.GetType() == typeof(LineContainer) || shape?.GetType() == typeof(ArrowContainer);
        internal static bool Supported(IDrawableContainer shape) => IsLine(shape) || shape?.GetType() == typeof(RectangleContainer) || shape?.GetType() == typeof(EllipseContainer);

        internal static IEnumerable<PointF> Targets(IDrawableContainer shape)
        {
            if (!Supported(shape)) yield break;
            float x = shape.Left, y = shape.Top, r = x + shape.Width, b = y + shape.Height;
            float cx = (x + r) / 2, cy = (y + b) / 2;
            if (IsLine(shape))
            {
                yield return new PointF(x, y); yield return new PointF(r, b); yield return new PointF(cx, cy); yield break;
            }
            float l = Math.Min(x, r), t = Math.Min(y, b);
            r = Math.Max(x, r); b = Math.Max(y, b);
            if (shape.GetType() == typeof(RectangleContainer))
            {
                yield return new PointF(l, t); yield return new PointF(r, t);
                yield return new PointF(r, b); yield return new PointF(l, b);
            }
            yield return new PointF(cx, t); yield return new PointF(r, cy);
            yield return new PointF(cx, b); yield return new PointF(l, cy); yield return new PointF(cx, cy);
        }

        // Stable enumeration order breaks equal-distance ties. All distances are in image coordinates.
        internal static Point Snap(Point pointer, Point? anchor, IEnumerable<PointF> targets, bool points, bool through,
            double zoom, Func<Point, bool> compatible, out PointF? target, out bool ray)
        {
            target = null; ray = false;
            double best = Math.Pow(8 / Math.Max(0.001, zoom), 2);
            Point result = pointer;
            if (points)
                foreach (var candidate in targets)
                {
                    var end = Point.Round(candidate);
                    double distance = DistanceSquared(pointer, candidate);
                    if (distance <= best && (target == null || distance < best) && (compatible == null || compatible(end)))
                    { best = distance; result = end; target = candidate; }
                }
            if (target != null || !through || anchor == null) return result;
            foreach (var candidate in targets)
            {
                double dx = candidate.X - anchor.Value.X, dy = candidate.Y - anchor.Value.Y;
                double length2 = dx * dx + dy * dy;
                if (length2 == 0) continue;
                double projection = ((pointer.X - anchor.Value.X) * dx + (pointer.Y - anchor.Value.Y) * dy) / length2;
                if (projection < 1) continue; // Target must lie on the forward segment, not beyond its endpoint.
                var projected = new PointF((float)(anchor.Value.X + projection * dx), (float)(anchor.Value.Y + projection * dy));
                var end = Point.Round(projected);
                double distance = DistanceSquared(pointer, projected);
                if (distance <= best && (target == null || distance < best) && (compatible == null || compatible(end)))
                { best = distance; result = end; target = candidate; ray = true; }
            }
            return result;
        }

        internal static double DistanceSquared(PointF a, PointF b) => (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);
    }
}
