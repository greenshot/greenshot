/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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
using System.Windows;
using Dapplo.Windows.Common.Structs;

namespace Greenshot.Base.Core;

/// <summary>
/// A rectangle rotated around a centre, the way an OCR engine reports rotated text:
/// the rectangle is axis-aligned as if the text were horizontal, it is where the text is after rotating it by Angle around the centre.
/// </summary>
public readonly struct RotatedRect
{
    /// <summary>
    /// A rectangle which is not rotated
    /// </summary>
    /// <param name="rect">NativeRect</param>
    public RotatedRect(NativeRect rect) : this(rect.X, rect.Y, rect.Width, rect.Height, 0, 0, 0)
    {
    }

    /// <summary>
    /// A rectangle rotated by angle around the centre
    /// </summary>
    /// <param name="x">X of the rectangle before the rotation</param>
    /// <param name="y">Y of the rectangle before the rotation</param>
    /// <param name="width">Width of the rectangle</param>
    /// <param name="height">Height of the rectangle</param>
    /// <param name="angle">Angle in degrees, clockwise on the screen (y points down)</param>
    /// <param name="centerX">X of the centre of the rotation</param>
    /// <param name="centerY">Y of the centre of the rotation</param>
    public RotatedRect(double x, double y, double width, double height, double angle, double centerX, double centerY)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Angle = angle;
        CenterX = centerX;
        CenterY = centerY;
    }

    /// <summary>
    /// X of the rectangle before the rotation
    /// </summary>
    public double X { get; }

    /// <summary>
    /// Y of the rectangle before the rotation
    /// </summary>
    public double Y { get; }

    public double Width { get; }

    public double Height { get; }

    /// <summary>
    /// The angle in degrees, clockwise on the screen, 0 when the rectangle is not rotated
    /// </summary>
    public double Angle { get; }

    /// <summary>
    /// X of the centre of the rotation
    /// </summary>
    public double CenterX { get; }

    /// <summary>
    /// Y of the centre of the rotation
    /// </summary>
    public double CenterY { get; }

    public bool IsEmpty => Width <= 0 || Height <= 0;

    /// <summary>
    /// The 4 corners after the rotation, clockwise on the screen starting with the top left corner of the rectangle before the rotation
    /// </summary>
    public Point[] Corners => new[] { Rotate(X, Y), Rotate(X + Width, Y), Rotate(X + Width, Y + Height), Rotate(X, Y + Height) };

    /// <summary>
    /// The axis-aligned bounds around the corners, rounded outwards: for a rotated rectangle a little larger than it
    /// </summary>
    public NativeRect Bounds
    {
        get
        {
            double left = double.MaxValue, top = double.MaxValue, right = double.MinValue, bottom = double.MinValue;
            foreach (var corner in Corners)
            {
                left = Math.Min(left, corner.X);
                top = Math.Min(top, corner.Y);
                right = Math.Max(right, corner.X);
                bottom = Math.Max(bottom, corner.Y);
            }
            int boundsLeft = (int)Math.Floor(left);
            int boundsTop = (int)Math.Floor(top);
            return new NativeRect(boundsLeft, boundsTop, (int)Math.Ceiling(right) - boundsLeft, (int)Math.Ceiling(bottom) - boundsTop);
        }
    }

    /// <summary>
    /// The same rectangle, moved
    /// </summary>
    public RotatedRect Offset(double x, double y) => new(X + x, Y + y, Width, Height, Angle, CenterX + x, CenterY + y);

    /// <summary>
    /// The smallest rectangle containing both: rotated like them when they have the same rotation (the words of an OCR line),
    /// otherwise the axis-aligned union of their bounds
    /// </summary>
    public RotatedRect Union(RotatedRect other)
    {
        if (IsEmpty)
        {
            return other;
        }
        if (other.IsEmpty)
        {
            return this;
        }
        if (!HasSameRotation(other))
        {
            var bounds = Bounds;
            var otherBounds = other.Bounds;
            int left = Math.Min(bounds.Left, otherBounds.Left);
            int top = Math.Min(bounds.Top, otherBounds.Top);
            return new RotatedRect(new NativeRect(left, top, Math.Max(bounds.Right, otherBounds.Right) - left, Math.Max(bounds.Bottom, otherBounds.Bottom) - top));
        }
        double x = Math.Min(X, other.X);
        double y = Math.Min(Y, other.Y);
        return new RotatedRect(x, y, Math.Max(X + Width, other.X + other.Width) - x, Math.Max(Y + Height, other.Y + other.Height) - y, Angle, CenterX, CenterY);
    }

    /// <summary>
    /// True when the point is inside the rotated rectangle
    /// </summary>
    public bool Contains(double x, double y)
    {
        // Rotate the point back, then it's a test against the rectangle before the rotation
        var point = Rotate(x, y, -Angle);
        return point.X >= X && point.X < X + Width && point.Y >= Y && point.Y < Y + Height;
    }

    /// <summary>
    /// True when the rotated rectangle and the axis-aligned rectangle overlap (separating axis test)
    /// </summary>
    public bool IntersectsWith(NativeRect rect)
    {
        if (IsEmpty || rect.IsEmpty)
        {
            return false;
        }
        var corners = Corners;
        var rectCorners = new[] { new Point(rect.Left, rect.Top), new Point(rect.Right, rect.Top), new Point(rect.Right, rect.Bottom), new Point(rect.Left, rect.Bottom) };
        double radians = Angle * Math.PI / 180d;
        var axes = new[] { new Vector(1, 0), new Vector(0, 1), new Vector(Math.Cos(radians), Math.Sin(radians)), new Vector(-Math.Sin(radians), Math.Cos(radians)) };
        foreach (var axis in axes)
        {
            Project(corners, axis, out double min, out double max);
            Project(rectCorners, axis, out double rectMin, out double rectMax);
            // Touching edges don't overlap, like NativeRect.IntersectsWith
            if (max <= rectMin || rectMax <= min)
            {
                return false;
            }
        }
        return true;
    }

    public override string ToString() => $"{{X: {X}; Y: {Y}; Width: {Width}; Height: {Height}; Angle: {Angle}; Center: {CenterX}, {CenterY}}}";

    private bool HasSameRotation(RotatedRect other) => Angle == 0 && other.Angle == 0 || Angle == other.Angle && CenterX == other.CenterX && CenterY == other.CenterY;

    private Point Rotate(double x, double y) => Rotate(x, y, Angle);

    private Point Rotate(double x, double y, double angle)
    {
        if (angle == 0)
        {
            return new Point(x, y);
        }
        double radians = angle * Math.PI / 180d;
        double cos = Math.Cos(radians);
        double sin = Math.Sin(radians);
        double dx = x - CenterX;
        double dy = y - CenterY;
        // y points down, so a positive angle rotates clockwise on the screen
        return new Point(CenterX + dx * cos - dy * sin, CenterY + dx * sin + dy * cos);
    }

    private static void Project(Point[] points, Vector axis, out double min, out double max)
    {
        min = double.MaxValue;
        max = double.MinValue;
        foreach (var point in points)
        {
            double projection = point.X * axis.X + point.Y * axis.Y;
            min = Math.Min(min, projection);
            max = Math.Max(max, projection);
        }
    }
}
