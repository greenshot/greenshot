using System;
using System.Collections.Generic;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Editor.Drawing;

namespace Greenshot.Editor.Measurements
{
    internal sealed class MeasurementProperty
    {
        public string Key { get; }
        public string Label { get; }
        public double Pixels { get; }
        public MeasurementProperty(string key, string label, double pixels)
        {
            Key = key; Label = label; Pixels = pixels;
        }
    }

    /// <summary>Reads and edits ordinary container geometry; no parallel parametric model or drawing fields.</summary>
    internal static class MeasurementGeometry
    {
        internal static bool IsLine(IDrawableContainer shape) => shape?.GetType() == typeof(LineContainer) || shape?.GetType() == typeof(ArrowContainer);

        internal static IReadOnlyList<MeasurementProperty> GetProperties(IDrawableContainer shape)
        {
            var result = new List<MeasurementProperty>();
            foreach (var dimension in MeasurementCalculator.GetDimensions(shape))
                result.Add(new MeasurementProperty(dimension.Label, dimension.Label, dimension.Pixels));
            if (result.Count == 0) return result;
            double left = Math.Min(shape.Left, (double)shape.Left + shape.Width);
            double top = Math.Min(shape.Top, (double)shape.Top + shape.Height);
            double right = left + Math.Abs((double)shape.Width), bottom = top + Math.Abs((double)shape.Height);
            void Point(string suffix, string label, double x, double y)
            {
                result.Add(new MeasurementProperty("X" + suffix, label + " X", x));
                result.Add(new MeasurementProperty("Y" + suffix, label + " Y", y));
            }
            if (IsLine(shape))
            {
                Point("1", "Start", shape.Left, shape.Top);
                Point("2", "End", (double)shape.Left + shape.Width, (double)shape.Top + shape.Height);
            }
            else if (shape.GetType() == typeof(RectangleContainer))
            {
                Point("1", "Top-left", left, top);
                Point("2", "Top-right", right, top);
                Point("3", "Bottom-right", right, bottom);
                Point("4", "Bottom-left", left, bottom);
            }
            else Point("C", "Centre", (left + right) / 2, (top + bottom) / 2);
            return result;
        }

        internal static bool TryEdit(DrawableContainer shape, MeasurementState state, string key, double value, out string error)
        {
            error = null;
            try
            {
                bool known = false;
                foreach (var property in GetProperties(shape)) if (property.Key == key) { known = true; break; }
                if (!known) throw new ArgumentException("This property is not available for the selected object.");
                double pixels = state.ToPixels(value);
                if (double.IsNaN(pixels) || double.IsInfinity(pixels)) throw new ArgumentException("Enter a finite number.");
                bool length = key.StartsWith("L", StringComparison.Ordinal) || key.StartsWith("D", StringComparison.Ordinal);
                if (length && pixels <= 0) throw new ArgumentException("Dimensions must be greater than zero.");

                int x = shape.Left, y = shape.Top, width = shape.Width, height = shape.Height;
                if (IsLine(shape))
                {
                    double endX = (double)x + width, endY = (double)y + height;
                    if (key == "L1")
                    {
                        double oldLength = MeasurementCalculator.LineLength(width, height);
                        width = Coordinate(oldLength == 0 ? pixels : width * pixels / oldLength);
                        height = Coordinate(oldLength == 0 ? 0 : height * pixels / oldLength);
                        if (width == 0 && height == 0) throw new ArgumentException("This length is smaller than the image's pixel geometry supports.");
                    }
                    else
                    {
                        if (key == "X1") x = Coordinate(pixels);
                        if (key == "Y1") y = Coordinate(pixels);
                        if (key == "X2") endX = pixels;
                        if (key == "Y2") endY = pixels;
                        width = Coordinate(Coordinate(endX) - (double)x);
                        height = Coordinate(Coordinate(endY) - (double)y);
                    }
                }
                else
                {
                    double left = Math.Min(x, (double)x + width), top = Math.Min(y, (double)y + height);
                    double right = left + Math.Abs((double)width), bottom = top + Math.Abs((double)height);
                    bool ellipse = shape.GetType() == typeof(EllipseContainer);
                    if (length)
                    {
                        int size = Coordinate(pixels);
                        if (size <= 0) throw new ArgumentException("This dimension is smaller than one image pixel.");
                        bool horizontal = key == "L1" || key == "D1";
                        if (horizontal)
                        {
                            left = ellipse ? Coordinate((left + right - size) / 2) : left;
                            right = left + size;
                        }
                        else
                        {
                            top = ellipse ? Coordinate((top + bottom - size) / 2) : top;
                            bottom = top + size;
                        }
                    }
                    else if (ellipse)
                    {
                        if (key == "XC") { left = Coordinate(pixels - Math.Abs((double)width) / 2); right = left + Math.Abs((double)width); }
                        if (key == "YC") { top = Coordinate(pixels - Math.Abs((double)height) / 2); bottom = top + Math.Abs((double)height); }
                    }
                    else
                    {
                        double position = Coordinate(pixels);
                        switch (key)
                        {
                            case "X1": case "X4": left = position; break;
                            case "X2": case "X3": right = position; break;
                            case "Y1": case "Y2": top = position; break;
                            case "Y3": case "Y4": bottom = position; break;
                        }
                        if (left >= right || top >= bottom) throw new ArgumentException("A corner must stay on its side of the opposite edge.");
                    }
                    // Preserve the original signed drawing direction and axis-aligned shape.
                    x = Coordinate(width < 0 ? right : left);
                    y = Coordinate(height < 0 ? bottom : top);
                    width = Coordinate((right - left) * (width < 0 ? -1 : 1));
                    height = Coordinate((bottom - top) * (height < 0 ? -1 : 1));
                }
                Coordinate((double)x + width); Coordinate((double)y + height);
                if (x == shape.Left && y == shape.Top && width == shape.Width && height == shape.Height) return true;
                shape.MakeBoundsChangeUndoable(false);
                shape.Invalidate();
                shape.Left = x; shape.Top = y; shape.Width = width; shape.Height = height;
                shape.Invalidate();
                if (shape.Parent != null) shape.Parent.Modified = true;
                return true;
            }
            catch (ArgumentException ex) { error = ex.Message; return false; }
        }

        private static int Coordinate(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || Math.Abs(value) > int.MaxValue / 4d)
                throw new ArgumentException("The value is outside the supported image geometry range.");
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }
    }
}
