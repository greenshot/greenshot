using System;
using System.Drawing.Drawing2D;

namespace Greenshot.Editor.Measurements
{
    public enum MeasurementUnit { Pixels, Millimetres, Centimetres, Metres, Inches, Feet }

    /// <summary>Immutable, document-owned calibration. Drawing objects stay in image pixels.</summary>
    public sealed class MeasurementState
    {
        public static readonly MeasurementState Pixels = new MeasurementState(0, MeasurementUnit.Pixels);
        public double MetresPerPixel { get; }
        public MeasurementUnit Unit { get; }
        public bool IsCalibrated => MetresPerPixel > 0;

        public MeasurementState(double metresPerPixel, MeasurementUnit unit)
        {
            if (double.IsNaN(metresPerPixel) || double.IsInfinity(metresPerPixel) || metresPerPixel < 0)
                throw new ArgumentOutOfRangeException(nameof(metresPerPixel));
            if (!Enum.IsDefined(typeof(MeasurementUnit), unit)) throw new ArgumentOutOfRangeException(nameof(unit));
            MetresPerPixel = metresPerPixel;
            Unit = metresPerPixel == 0 ? MeasurementUnit.Pixels : unit;
        }

        public static MeasurementState Calibrate(double pixels, double distance, MeasurementUnit unit)
        {
            if (!IsPositiveFinite(pixels) || !IsPositiveFinite(distance) || unit == MeasurementUnit.Pixels)
                throw new ArgumentOutOfRangeException(nameof(distance), "Enter a positive distance and draw a nonzero line.");
            var scale = distance * MetresPerUnit(unit) / pixels;
            if (!IsPositiveFinite(scale)) throw new ArgumentOutOfRangeException(nameof(distance));
            return new MeasurementState(scale, unit);
        }

        private static bool IsPositiveFinite(double value) => value > 0 && !double.IsInfinity(value) && !double.IsNaN(value);
        public MeasurementState WithUnit(MeasurementUnit unit) => new MeasurementState(MetresPerPixel, unit);
        public double Convert(double pixels) => Unit == MeasurementUnit.Pixels ? pixels : pixels * MetresPerPixel / MetresPerUnit(Unit);

        public static double MetresPerUnit(MeasurementUnit unit) => unit switch
        {
            MeasurementUnit.Millimetres => 0.001,
            MeasurementUnit.Centimetres => 0.01,
            MeasurementUnit.Metres => 1,
            MeasurementUnit.Inches => 0.0254,
            MeasurementUnit.Feet => 0.3048,
            _ => throw new ArgumentOutOfRangeException(nameof(unit))
        };

        public static string Symbol(MeasurementUnit unit) => unit switch
        {
            MeasurementUnit.Millimetres => "mm", MeasurementUnit.Centimetres => "cm",
            MeasurementUnit.Metres => "m", MeasurementUnit.Inches => "in", MeasurementUnit.Feet => "ft", _ => "px"
        };

        public MeasurementState Transform(Matrix matrix)
        {
            if (!IsCalibrated || matrix == null) return this;
            var m = matrix.Elements;
            double sx = Math.Sqrt((double)m[0] * m[0] + (double)m[1] * m[1]);
            double sy = Math.Sqrt((double)m[2] * m[2] + (double)m[3] * m[3]);
            double dot = (double)m[0] * m[2] + (double)m[1] * m[3];
            if (!IsPositiveFinite(sx) || !IsPositiveFinite(sy) || Math.Abs(sx - sy) > 1e-5 * Math.Max(sx, sy)
                || Math.Abs(dot) > 1e-5 * sx * sy) return Pixels;
            return new MeasurementState(MetresPerPixel / sx, Unit);
        }
    }

    public interface IMeasurementSurface
    {
        MeasurementState Measurements { get; }
        void RestoreMeasurements(MeasurementState state);
    }
}
