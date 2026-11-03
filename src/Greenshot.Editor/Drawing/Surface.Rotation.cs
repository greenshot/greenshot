using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using Dapplo.Windows.Common.Structs;
using Greenshot.Editor.Helpers;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Memento;

namespace Greenshot.Editor.Drawing
{
    public sealed partial class Surface
    {
        internal static bool SupportsObjectRotation(IDrawableContainer element)
        {
            if (!(element is DrawableContainer shape) || shape.HasFilters) return false;
            var type = shape.GetType();
            return type == typeof(RectangleContainer) || type == typeof(EllipseContainer) || type == typeof(LineContainer)
                || type == typeof(ArrowContainer) || type == typeof(PolygonContainer) || type == typeof(TextContainer)
                || type == typeof(ImageContainer) || type == typeof(SvgContainer) || type == typeof(MetafileContainer);
        }
        internal bool CanRotateObjects => selectedElements.Count > 0 && selectedElements.All(SupportsObjectRotation);
        internal static double NormalizeRotation(double angle) { double result = angle % 360; return result < 0 ? result + 360 : result; }
        internal bool TryGetSelectedRotation(out double angle)
        {
            angle = 0;
            if (selectedElements.Count == 0) return false;
            var members = selectedElements.OfType<DrawableContainer>().ToArray();
            if (members.Length != selectedElements.Count) return false;
            if (members.Length == 1 && members[0].GroupId == Guid.Empty) { angle = NormalizeRotation(members[0].ObjectAngle); return true; }
            if (members[0].GroupId == Guid.Empty || members.Any(m => m.GroupId != members[0].GroupId)) return false;
            angle = members[0].GroupReferenceAngle; return true;
        }
        internal bool CanSetSelectedRotation => CanRotateObjects && TryGetSelectedRotation(out _);
        internal const string MixedRotationLimit = "Mixed selection: group these objects to set a shared absolute rotation.";
        public bool SetSelectedRotation(double angle)
        {
            if (!CanRotateObjects || !TryGetSelectedRotation(out double current) || double.IsNaN(angle) || double.IsInfinity(angle)) return false;
            double target = NormalizeRotation(angle), delta = NormalizeRotation(target - current + 180) - 180;
            // atan2/composition can differ by a few floating-point ulps after a turn.
            if (Math.Abs(delta) < 1e-12) return true;
            return RotateSelectedObjects(delta);
        }
        internal const string RotationLimit = "Rotation supports rectangles, ellipses, lines/arrows, polygons, text, images and SVG/metafile objects without filters. Ungroup unsupported members first.";
        public bool RotateSelectedObjects(double degrees)
        {
            if (!CanRotateObjects || double.IsNaN(degrees) || double.IsInfinity(degrees)) return false;
            if (degrees % 360 == 0) return true;
            CancelPolygonInteraction(); ClearSnapGuide();
            var members = selectedElements.Cast<DrawableContainer>().ToArray();
            foreach (var text in members.OfType<TextContainer>()) text.EndTextEditingForRotation();
            double x = members.Average(m => m.ImageCentre.X), y = members.Average(m => m.ImageCentre.Y);
            MakeUndoable(new DrawableContainerBoundsChangeMemento(new DrawableContainerList(members), mergeable: false), false);
            foreach (var member in members)
            {
                member.RotateObject(degrees, x, y);
                if (member.GroupId != Guid.Empty) member.GroupReferenceAngle += degrees;
            }
            Modified = true; Invalidate();
            _movingElementChanged?.Invoke(this, new Greenshot.Base.Interfaces.SurfaceElementEventArgs { Elements = selectedElements });
            return true;
        }

        private NativeRectFloat SnapRotatedHandle(DrawableContainer owner, NativeRect original, Positions position, Point pointer, ScaleOptions? options)
        {
            bool line = ObjectSnapHelper.IsLine(owner);
            var opts = options ?? ScaleHelper.GetScaleOptions();
            if (line) opts &= ~ScaleOptions.Rational;
            var anchor = Point.Round(owner.ModelToImage(position == Positions.TopLeft ? new PointF(original.Right, original.Bottom) : new PointF(original.X, original.Y)));
            Point Constrain(Point p) => line ? GetScaleEndpoint(anchor, p, ModifierKeys) : p;
            NativeRectFloat Scale(Point p) => ScaleHelper.Scale(original, position, owner.ImageToModel(Constrain(p)), opts);
            Point End(NativeRectFloat b) => Point.Round(owner.ModelToImage(new PointF(
                position == Positions.TopLeft || position == Positions.MiddleLeft || position == Positions.BottomLeft ? b.X : position == Positions.TopCenter || position == Positions.BottomCenter ? b.X + b.Width / 2 : b.X + b.Width,
                position == Positions.TopLeft || position == Positions.TopCenter || position == Positions.TopRight ? b.Y : position == Positions.MiddleLeft || position == Positions.MiddleRight ? b.Y + b.Height / 2 : b.Y + b.Height)));
            var baseline = Scale(pointer);
            var end = End(baseline);
            var snapped = SnapGesture(end, line ? anchor : (Point?)null, owner, line,
                p => ObjectSnapHelper.DistanceSquared(End(Scale(p)), p) <= 2);
            return snapped == end ? baseline : Scale(snapped);
        }
    }
}
