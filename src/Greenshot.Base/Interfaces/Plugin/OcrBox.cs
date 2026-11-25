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
using System.Windows;
using System.Windows.Media;
using Dapplo.Windows.Common.Structs;

namespace Greenshot.Base.Interfaces.Plugin;

/// <summary>
/// Where a word or line of text is: a rectangle turned by the angle of the text around its centre, in pixels of the image.
/// Both OCR engines give this without loss: Windows OCR as deskewed rects plus one angle for the whole capture, RapidOCR as
/// a rotated rectangle per line. Code which can only work with upright rectangles uses <see cref="Bounds"/>, the rectangle
/// around it, which covers the text and a little more for rotated text.
/// </summary>
public readonly struct OcrBox
{
    /// <summary>Angles below this (degrees) are floating point noise: the text is horizontal</summary>
    public const double MinAngle = 0.01;

    /// <param name="center">the centre of the text</param>
    /// <param name="width">the length along the text</param>
    /// <param name="height">the height of the text</param>
    /// <param name="angle">degrees, clockwise on the screen (y points down), 0 for horizontal text</param>
    public OcrBox(Point center, double width, double height, double angle)
    {
        Center = center;
        Width = Math.Max(0, width);
        Height = Math.Max(0, height);
        Angle = Math.Abs(angle) < MinAngle ? 0 : angle;
        Bounds = CalculateBounds(Center, Width, Height, Angle);
    }

    /// <summary>The centre of the text</summary>
    public Point Center { get; }

    /// <summary>The length along the text</summary>
    public double Width { get; }

    /// <summary>The height of the text, also for rotated text (the height of <see cref="Bounds"/> is larger then)</summary>
    public double Height { get; }

    /// <summary>The height of the text in whole pixels, rounded up (e.g. for the obfuscation strength)</summary>
    public int PixelHeight => (int)Math.Ceiling(Height - 1e-6);

    /// <summary>Degrees, clockwise on the screen, 0 for horizontal text</summary>
    public double Angle { get; }

    /// <summary>The upright rectangle around the text, rounded outwards</summary>
    public NativeRect Bounds { get; }

    public bool IsEmpty => Width <= 0 || Height <= 0;

    public bool IsRotated => Angle != 0;

    /// <summary>The text as if it weren't rotated: the rectangle around the centre, rotate it by <see cref="Transform"/> to place it</summary>
    public Rect Unrotated => new(Center.X - Width / 2, Center.Y - Height / 2, Width, Height);

    /// <summary>Turns <see cref="Unrotated"/> to where the text is</summary>
    public Matrix Transform
    {
        get
        {
            var matrix = Matrix.Identity;
            matrix.RotateAt(Angle, Center.X, Center.Y);
            return matrix;
        }
    }

    /// <summary>A horizontal box</summary>
    public static OcrBox FromRect(Rect rect) =>
        rect.IsEmpty ? default : new OcrBox(new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2), rect.Width, rect.Height, 0);

    /// <summary>A horizontal box</summary>
    public static OcrBox FromRect(NativeRect rect) => FromRect(new Rect(rect.X, rect.Y, rect.Width, rect.Height));

    /// <summary>
    /// The text is where the rect is after turning it by the angle around the point (how Windows OCR reports rotated text)
    /// </summary>
    public static OcrBox FromRect(Rect deskewed, double angle, Point rotationCenter)
    {
        if (deskewed.IsEmpty) return default;
        var matrix = Matrix.Identity;
        matrix.RotateAt(angle, rotationCenter.X, rotationCenter.Y);
        var center = matrix.Transform(new Point(deskewed.X + deskewed.Width / 2, deskewed.Y + deskewed.Height / 2));
        return new OcrBox(center, deskewed.Width, deskewed.Height, angle);
    }

    /// <summary>
    /// The box of three corners in reading direction (how RapidOCR finds a line): top left, top right and bottom left
    /// </summary>
    public static OcrBox FromCorners(Point topLeft, Point topRight, Point bottomLeft)
    {
        var along = topRight - topLeft;
        var down = bottomLeft - topLeft;
        double angle = Math.Atan2(along.Y, along.X) * 180 / Math.PI;
        var center = new Point((topRight.X + bottomLeft.X) / 2, (topRight.Y + bottomLeft.Y) / 2);
        return new OcrBox(center, along.Length, down.Length, angle);
    }

    /// <summary>The corners in reading direction: top left, top right, bottom right, bottom left</summary>
    public Point[] Corners()
    {
        var rect = Unrotated;
        var corners = new[] { rect.TopLeft, rect.TopRight, rect.BottomRight, rect.BottomLeft };
        if (IsRotated) Transform.Transform(corners);
        return corners;
    }

    /// <summary>The point is on the text (for rotated text: on the rotated box, not only in the rectangle around it)</summary>
    public bool Contains(Point point)
    {
        if (IsEmpty) return false;
        if (IsRotated)
        {
            var back = Matrix.Identity;
            back.RotateAt(-Angle, Center.X, Center.Y);
            point = back.Transform(point);
        }
        return Unrotated.Contains(point);
    }

    /// <summary>The box and the upright rectangle (e.g. a selection) overlap, touching edges don't count</summary>
    public bool IntersectsWith(Rect rect)
    {
        if (IsEmpty || rect.IsEmpty) return false;
        var boxCorners = Corners();
        var rectCorners = new[] { rect.TopLeft, rect.TopRight, rect.BottomRight, rect.BottomLeft };
        // Two convex shapes don't touch when one of their edge directions separates them
        var axes = new[] { new Vector(1, 0), new Vector(0, 1), boxCorners[1] - boxCorners[0], boxCorners[3] - boxCorners[0] };
        foreach (var axis in axes)
        {
            var (boxMin, boxMax) = Project(boxCorners, axis);
            var (rectMin, rectMax) = Project(rectCorners, axis);
            if (boxMax <= rectMin || rectMax <= boxMin) return false;
        }
        return true;
    }

    /// <summary>The box and the upright rectangle (e.g. a selection) overlap, touching edges don't count</summary>
    public bool IntersectsWith(NativeRect rect) => IntersectsWith(new Rect(rect.X, rect.Y, rect.Width, rect.Height));

    /// <summary>Moved by the offset</summary>
    public OcrBox Offset(double x, double y) => IsEmpty ? this : new OcrBox(new Point(Center.X + x, Center.Y + y), Width, Height, Angle);

    /// <summary>
    /// This box grown to also hold the other, keeping the angle of this one (the line of its words)
    /// </summary>
    public OcrBox Union(OcrBox other)
    {
        if (other.IsEmpty) return this;
        if (IsEmpty) return other;
        // In the frame of this box: turn the corners of the other back, take the rectangle around both, turn its centre forward
        var back = Matrix.Identity;
        back.RotateAt(-Angle, Center.X, Center.Y);
        var rect = Unrotated;
        foreach (var corner in other.Corners())
        {
            rect.Union(back.Transform(corner));
        }
        var center = Transform.Transform(new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2));
        return new OcrBox(center, rect.Width, rect.Height, Angle);
    }

    public override string ToString() => $"{Center.X:0.#},{Center.Y:0.#} {Width:0.#}x{Height:0.#} {Angle:0.##}°";

    private static (double Min, double Max) Project(Point[] corners, Vector axis)
    {
        double min = double.MaxValue, max = double.MinValue;
        foreach (var corner in corners)
        {
            double value = corner.X * axis.X + corner.Y * axis.Y;
            min = Math.Min(min, value);
            max = Math.Max(max, value);
        }
        return (min, max);
    }

    private static NativeRect CalculateBounds(Point center, double width, double height, double angle)
    {
        if (width <= 0 || height <= 0) return NativeRect.Empty;
        double radians = angle * Math.PI / 180;
        double cos = Math.Abs(Math.Cos(radians)), sin = Math.Abs(Math.Sin(radians));
        double halfWidth = (width * cos + height * sin) / 2;
        double halfHeight = (width * sin + height * cos) / 2;
        // The rotation has rounding errors, e.g. 59.99999 for 60: they shouldn't add a pixel
        const double tolerance = 1e-6;
        int left = (int)Math.Floor(center.X - halfWidth + tolerance);
        int top = (int)Math.Floor(center.Y - halfHeight + tolerance);
        int right = (int)Math.Ceiling(center.X + halfWidth - tolerance);
        int bottom = (int)Math.Ceiling(center.Y + halfHeight - tolerance);
        return new NativeRect(left, top, right - left, bottom - top);
    }
}
