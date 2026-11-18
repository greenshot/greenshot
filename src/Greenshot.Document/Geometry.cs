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
using System.Linq;

namespace Greenshot.Document
{
    /// <summary>
    /// A point on the canvas, in canvas pixels with the origin top-left.
    /// </summary>
    public readonly record struct PointD(double X, double Y)
    {
        /// <summary>
        /// The point turned clockwise by degrees around a center, multiples of 90 degrees are exact
        /// </summary>
        public PointD Turned(double degrees, PointD center)
        {
            double radians = degrees * Math.PI / 180;
            double cos = Math.Round(Math.Cos(radians), 15), sin = Math.Round(Math.Sin(radians), 15);
            double dx = X - center.X, dy = Y - center.Y;
            return new PointD(center.X + dx * cos - dy * sin, center.Y + dx * sin + dy * cos);
        }
    }

    /// <summary>
    /// An area on the canvas, in canvas pixels with the origin top-left.
    /// </summary>
    public readonly record struct RectD(double X, double Y, double Width, double Height)
    {
        public double Right => X + Width;

        public double Bottom => Y + Height;

        /// <summary>
        /// The smallest area containing both points, independent of their order.
        /// </summary>
        public static RectD FromPoints(PointD a, PointD b)
        {
            double left = a.X < b.X ? a.X : b.X;
            double top = a.Y < b.Y ? a.Y : b.Y;
            return new RectD(left, top, (a.X < b.X ? b.X : a.X) - left, (a.Y < b.Y ? b.Y : a.Y) - top);
        }

        /// <summary>
        /// The smallest area containing both areas
        /// </summary>
        public static RectD Union(RectD a, RectD b) =>
            FromPoints(new PointD(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y)), new PointD(Math.Max(a.Right, b.Right), Math.Max(a.Bottom, b.Bottom)));

        public RectD Offset(double dx, double dy) => this with { X = X + dx, Y = Y + dy };

        public PointD Center => new(X + Width / 2, Y + Height / 2);

        /// <summary>
        /// The axis-aligned box around this area turned clockwise by degrees around its centre
        /// </summary>
        public RectD Turned(double degrees) => Turned(degrees, Center);

        /// <summary>
        /// The axis-aligned box around this area turned clockwise by degrees around a point
        /// </summary>
        public RectD Turned(double degrees, PointD center)
        {
            if (degrees % 360 == 0)
            {
                return this;
            }

            var corners = new[] { new PointD(X, Y), new PointD(Right, Y), new PointD(Right, Bottom), new PointD(X, Bottom) }.Select(corner => corner.Turned(degrees, center)).ToList();
            return FromPoints(new PointD(corners.Min(p => p.X), corners.Min(p => p.Y)), new PointD(corners.Max(p => p.X), corners.Max(p => p.Y)));
        }

        /// <summary>
        /// The area grown by the amount on every side
        /// </summary>
        public RectD Inflate(double amount) => new(X - amount, Y - amount, Width + 2 * amount, Height + 2 * amount);
    }

    /// <summary>
    /// A color with alpha, written as #RRGGBBAA.
    /// </summary>
    public readonly record struct Rgba(byte R, byte G, byte B, byte A)
    {
        public static readonly Rgba Transparent = new(0, 0, 0, 0);

        public static Rgba FromArgb(int argb) => unchecked(new Rgba((byte)(argb >> 16), (byte)(argb >> 8), (byte)argb, (byte)(argb >> 24)));

        public int ToArgb() => unchecked((A << 24) | (R << 16) | (G << 8) | B);

        public override string ToString() => $"#{R:X2}{G:X2}{B:X2}{A:X2}";
    }
}
