using System;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;

namespace Greenshot.Editor.Measurements
{
    public static class MeasurementCalculator
    {
        public static double LineLength(double width, double height) => Math.Sqrt(width * width + height * height);

        public static (string Label, double Pixels)[] GetDimensions(IDrawableContainer container)
        {
            // Exact types deliberately exclude arrows, crop tools and other specialized descendants.
            var type = container?.GetType();
            if (type == typeof(LineContainer))
                return new[] { ("L1", LineLength(container.Width, container.Height)) };
            if (type == typeof(RectangleContainer))
                return new[] { ("L1", Math.Abs((double)container.Width)), ("L2", Math.Abs((double)container.Height)) };
            if (type == typeof(EllipseContainer))
                return new[] { ("D1", Math.Abs((double)container.Width)), ("D2", Math.Abs((double)container.Height)) };
            return Array.Empty<(string, double)>();
        }
    }
}
