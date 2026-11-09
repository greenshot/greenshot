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
using System.Collections.Generic;
using System.Linq;

namespace Greenshot.Plugin.RapidOcr.Recognition;

/// <summary>
/// A found text line: the corners top left, top right, bottom right, bottom left in pixels of the image
/// </summary>
internal sealed class TextBox
{
    public TextBox(PointD topLeft, PointD topRight, PointD bottomRight, PointD bottomLeft, double score)
    {
        TopLeft = topLeft;
        TopRight = topRight;
        BottomRight = bottomRight;
        BottomLeft = bottomLeft;
        Score = score;
    }

    public PointD TopLeft { get; }
    public PointD TopRight { get; }
    public PointD BottomRight { get; }
    public PointD BottomLeft { get; }
    public double Score { get; }

    public IEnumerable<PointD> Corners => new[] { TopLeft, TopRight, BottomRight, BottomLeft };
}

/// <summary>
/// Turns the probability map of the detection model (Differentiable Binarization) into text boxes,
/// as the DBPostProcess of PaddleOCR / RapidOCR (Apache 2.0): threshold, dilate, the smallest rotated rectangle of each area,
/// its mean probability, then the rectangle grown by area * ratio / perimeter.
/// </summary>
internal static class TextBoxFinder
{
    internal const double Threshold = 0.3;
    internal const double BoxThreshold = 0.5;
    internal const double UnclipRatio = 1.6;
    internal const int MaxCandidates = 1000;
    private const int MinSize = 3;

    /// <summary>
    /// The boxes in the probability map, scaled to the image size
    /// </summary>
    /// <param name="probabilities">the map, row after row</param>
    /// <param name="width">width of the map</param>
    /// <param name="height">height of the map</param>
    /// <param name="imageWidth">width of the image the boxes are for</param>
    /// <param name="imageHeight">height of the image the boxes are for</param>
    public static List<TextBox> Find(float[] probabilities, int width, int height, int imageWidth, int imageHeight)
    {
        var mask = Dilate(probabilities, width, height);
        var boxes = new List<TextBox>();
        foreach (var hull in Areas(mask, width, height).Take(MaxCandidates))
        {
            var rectangle = RotatedRectangle.Smallest(hull);
            if (rectangle.ShortSide < MinSize) continue;

            double score = MeanInside(probabilities, width, height, rectangle.Corners());
            if (score < BoxThreshold) continue;

            double distance = rectangle.Area * UnclipRatio / rectangle.Perimeter;
            var grown = rectangle.Grow(distance);
            if (grown.ShortSide < MinSize + 2) continue;

            var corners = grown.Corners()
                .Select(point => new PointD(
                    Clamp(Math.Round(point.X / width * imageWidth), 0, imageWidth),
                    Clamp(Math.Round(point.Y / height * imageHeight), 0, imageHeight)))
                .ToArray();
            var box = Order(corners, imageWidth, imageHeight, score);
            if ((int)box.TopLeft.DistanceTo(box.TopRight) <= 3 || (int)box.TopLeft.DistanceTo(box.BottomLeft) <= 3) continue;
            boxes.Add(box);
        }

        return Sort(boxes);
    }

    /// <summary>
    /// Top to bottom, a box less than 10 pixels lower than the one before is on its line: left to right per line
    /// </summary>
    internal static List<TextBox> Sort(List<TextBox> boxes)
    {
        var byTop = boxes.Select((box, index) => (box, index)).OrderBy(entry => entry.box.TopLeft.Y).ThenBy(entry => entry.index).Select(entry => entry.box).ToList();
        var lineIds = new int[byTop.Count];
        for (int i = 1; i < byTop.Count; i++)
        {
            lineIds[i] = lineIds[i - 1] + (byTop[i].TopLeft.Y - byTop[i - 1].TopLeft.Y >= 10 ? 1 : 0);
        }

        return byTop.Select((box, index) => (box, line: lineIds[index], index))
            .OrderBy(entry => entry.line).ThenBy(entry => entry.box.TopLeft.X).ThenBy(entry => entry.index)
            .Select(entry => entry.box).ToList();
    }

    /// <summary>
    /// Above the threshold, dilated with a 2x2 kernel (anchor in its center as OpenCV: a pixel is set when it or its left/top neighbours are)
    /// </summary>
    private static bool[] Dilate(float[] probabilities, int width, int height)
    {
        var mask = new bool[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool on = probabilities[y * width + x] > Threshold
                          || x > 0 && probabilities[y * width + x - 1] > Threshold
                          || y > 0 && probabilities[(y - 1) * width + x] > Threshold
                          || x > 0 && y > 0 && probabilities[(y - 1) * width + x - 1] > Threshold;
                mask[y * width + x] = on;
            }
        }

        return mask;
    }

    /// <summary>
    /// The 8-connected areas of the mask, as the convex hull of their pixel centers
    /// </summary>
    private static IEnumerable<List<PointD>> Areas(bool[] mask, int width, int height)
    {
        var seen = new bool[mask.Length];
        var stack = new Stack<int>();
        for (int start = 0; start < mask.Length; start++)
        {
            if (!mask[start] || seen[start]) continue;

            // Per row the leftmost and rightmost pixel, their hull is the hull of the area
            var rows = new Dictionary<int, (int Left, int Right)>();
            seen[start] = true;
            stack.Push(start);
            while (stack.Count > 0)
            {
                int index = stack.Pop();
                int x = index % width, y = index / width;
                rows[y] = rows.TryGetValue(y, out var row) ? (Math.Min(row.Left, x), Math.Max(row.Right, x)) : (x, x);
                for (int dy = -1; dy <= 1; dy++)
                {
                    int ny = y + dy;
                    if (ny < 0 || ny >= height) continue;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int nx = x + dx;
                        if (nx < 0 || nx >= width || dx == 0 && dy == 0) continue;
                        int next = ny * width + nx;
                        if (!mask[next] || seen[next]) continue;
                        seen[next] = true;
                        stack.Push(next);
                    }
                }
            }

            var points = new List<PointD>(rows.Count * 2);
            foreach (var row in rows)
            {
                points.Add(new PointD(row.Value.Left, row.Key));
                if (row.Value.Right != row.Value.Left) points.Add(new PointD(row.Value.Right, row.Key));
            }

            yield return ConvexHull(points);
        }
    }

    /// <summary>Monotone chain, counter clockwise</summary>
    internal static List<PointD> ConvexHull(List<PointD> points)
    {
        var sorted = points.Distinct().OrderBy(point => point.X).ThenBy(point => point.Y).ToList();
        if (sorted.Count < 3) return sorted;

        var hull = new List<PointD>();
        foreach (var pass in new[] { sorted, Enumerable.Reverse(sorted).ToList() })
        {
            int start = hull.Count;
            foreach (var point in pass)
            {
                while (hull.Count >= start + 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0)
                {
                    hull.RemoveAt(hull.Count - 1);
                }
                hull.Add(point);
            }
            hull.RemoveAt(hull.Count - 1);
        }

        return hull;
    }

    private static double Cross(PointD o, PointD a, PointD b) => (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);

    /// <summary>
    /// The mean probability of the pixels inside the polygon (box_score_fast)
    /// </summary>
    private static double MeanInside(float[] probabilities, int width, int height, PointD[] polygon)
    {
        int xMin = (int)Clamp(Math.Floor(polygon.Min(point => point.X)), 0, width - 1);
        int xMax = (int)Clamp(Math.Ceiling(polygon.Max(point => point.X)), 0, width - 1);
        int yMin = (int)Clamp(Math.Floor(polygon.Min(point => point.Y)), 0, height - 1);
        int yMax = (int)Clamp(Math.Ceiling(polygon.Max(point => point.Y)), 0, height - 1);
        // OpenCV's fillPoly draws the polygon at integer corners
        var corners = polygon.Select(point => new PointD((int)point.X, (int)point.Y)).ToArray();
        double sum = 0;
        int count = 0;
        for (int y = yMin; y <= yMax; y++)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                if (!Inside(corners, x, y)) continue;
                sum += probabilities[y * width + x];
                count++;
            }
        }

        return count == 0 ? 0 : sum / count;
    }

    /// <summary>Inside or on the border of the convex polygon</summary>
    private static bool Inside(PointD[] polygon, double x, double y)
    {
        bool hasPositive = false, hasNegative = false;
        var point = new PointD(x, y);
        for (int i = 0; i < polygon.Length; i++)
        {
            double cross = Cross(polygon[i], polygon[(i + 1) % polygon.Length], point);
            if (cross > 1e-9) hasPositive = true;
            if (cross < -1e-9) hasNegative = true;
            if (hasPositive && hasNegative) return false;
        }

        return true;
    }

    /// <summary>
    /// order_points_clockwise: the two leftmost are top left and bottom left, the others top right and bottom right; clipped to the image
    /// </summary>
    private static TextBox Order(PointD[] corners, int imageWidth, int imageHeight, double score)
    {
        var byX = corners.OrderBy(point => point.X).ToArray();
        var left = byX.Take(2).OrderBy(point => point.Y).ToArray();
        var right = byX.Skip(2).OrderBy(point => point.Y).ToArray();
        PointD Clip(PointD point) => new PointD((int)Clamp(point.X, 0, imageWidth - 1), (int)Clamp(point.Y, 0, imageHeight - 1));
        return new TextBox(Clip(left[0]), Clip(right[0]), Clip(right[1]), Clip(left[1]), score);
    }

    private static double Clamp(double value, double min, double max) => value < min ? min : value > max ? max : value;
}

/// <summary>
/// A rectangle at any angle: center, two unit axes and the half sizes along them
/// </summary>
internal readonly struct RotatedRectangle
{
    private readonly PointD _center;
    private readonly PointD _axis1;
    private readonly PointD _axis2;
    private readonly double _half1;
    private readonly double _half2;

    private RotatedRectangle(PointD center, PointD axis1, PointD axis2, double half1, double half2)
    {
        _center = center;
        _axis1 = axis1;
        _axis2 = axis2;
        _half1 = half1;
        _half2 = half2;
    }

    public double ShortSide => 2 * Math.Min(_half1, _half2);

    public double Area => 4 * _half1 * _half2;

    public double Perimeter => 4 * (_half1 + _half2);

    /// <summary>
    /// The same rectangle, each side moved out by the distance (the bounding rectangle of the rounded offset)
    /// </summary>
    public RotatedRectangle Grow(double distance) => new RotatedRectangle(_center, _axis1, _axis2, _half1 + distance, _half2 + distance);

    public PointD[] Corners()
    {
        var corners = new PointD[4];
        var signs = new[] { (-1, -1), (1, -1), (1, 1), (-1, 1) };
        for (int i = 0; i < 4; i++)
        {
            var (s, t) = signs[i];
            corners[i] = new PointD(
                _center.X + s * _half1 * _axis1.X + t * _half2 * _axis2.X,
                _center.Y + s * _half1 * _axis1.Y + t * _half2 * _axis2.Y);
        }

        return corners;
    }

    /// <summary>
    /// The rectangle with the smallest area around the convex hull (rotating calipers: one side lies on an edge of the hull)
    /// </summary>
    public static RotatedRectangle Smallest(List<PointD> hull)
    {
        if (hull.Count == 0) return default;
        if (hull.Count == 1) return new RotatedRectangle(hull[0], new PointD(1, 0), new PointD(0, 1), 0, 0);

        RotatedRectangle best = default;
        double bestArea = double.MaxValue;
        for (int i = 0; i < hull.Count; i++)
        {
            var a = hull[i];
            var b = hull[(i + 1) % hull.Count];
            double length = a.DistanceTo(b);
            if (length < 1e-9) continue;
            var axis1 = new PointD((b.X - a.X) / length, (b.Y - a.Y) / length);
            var axis2 = new PointD(-axis1.Y, axis1.X);
            double min1 = double.MaxValue, max1 = double.MinValue, min2 = double.MaxValue, max2 = double.MinValue;
            foreach (var point in hull)
            {
                double p1 = point.X * axis1.X + point.Y * axis1.Y;
                double p2 = point.X * axis2.X + point.Y * axis2.Y;
                min1 = Math.Min(min1, p1);
                max1 = Math.Max(max1, p1);
                min2 = Math.Min(min2, p2);
                max2 = Math.Max(max2, p2);
            }

            double area = (max1 - min1) * (max2 - min2);
            if (area >= bestArea) continue;
            bestArea = area;
            double c1 = (min1 + max1) / 2, c2 = (min2 + max2) / 2;
            var center = new PointD(c1 * axis1.X + c2 * axis2.X, c1 * axis1.Y + c2 * axis2.Y);
            best = new RotatedRectangle(center, axis1, axis2, (max1 - min1) / 2, (max2 - min2) / 2);
        }

        return best;
    }
}
