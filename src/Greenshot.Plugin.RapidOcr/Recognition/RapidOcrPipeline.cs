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
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Plugin;
using Microsoft.ML.OnnxRuntime;
using Point = System.Windows.Point;

namespace Greenshot.Plugin.RapidOcr.Recognition;

/// <summary>
/// The RapidOCR steps (Apache 2.0, https://github.com/RapidAI/RapidOCR) with the PaddleOCR models: bring the image into bounds,
/// find the text lines, cut them out, read them. No orientation step: text on the screen isn't upside down.
/// </summary>
internal sealed class RapidOcrPipeline : IDisposable
{
    private const int MaxSideLength = 2000;
    private const int MinSideLength = 30;
    private const int MinHeight = 30;
    private const double WidthHeightRatio = 8;
    private const double TextScore = 0.5;

    /// <summary>
    /// The largest side found in one go: shrunk to MaxSideLength the lines are found as well as at full size (a 4K screen), smaller
    /// they get lost (three screens side by side shrunk to a quarter: 22 of about 600 lines)
    /// </summary>
    private const int MaxTileSide = 2 * MaxSideLength;

    /// <summary>The overlap of the tiles of a large capture: more than the height of a line, so a line across an edge is found in both</summary>
    private const int TileOverlap = 400;
    private readonly TextDetector _detector;
    private readonly TextRecognizer _recognizer;

    public RapidOcrPipeline(string detectionModel, string recognitionModel)
    {
        var options = new SessionOptions
        {
            // The threads are left to ONNX Runtime: one per physical core. The user waits for the text, half of them took 1.7 times as long
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
        };
        _detector = new TextDetector(new InferenceSession(detectionModel, options));
        try
        {
            _recognizer = new TextRecognizer(new InferenceSession(recognitionModel, options));
        }
        catch
        {
            _detector.Dispose();
            throw;
        }
    }

    /// <summary>
    /// The lines of text with their words, in pixels of the image
    /// </summary>
    public List<IOcrLineFeature> Read(Bitmap bitmap) => Read(FromBitmap(bitmap));

    internal List<IOcrLineFeature> Read(BgrImage original)
    {
        var boxes = Math.Max(original.Width, original.Height) > MaxTileSide ? FindBoxesInTiles(original) : FindBoxes(original);
        if (boxes.Count == 0) return new List<IOcrLineFeature>();

        var crops = new List<BgrImage>(boxes.Count);
        // The corners in reading direction (top left, top right, bottom left, bottom right) and the length along the text, in pixels of
        // the capture: the lines are cut out of the capture itself, at its full resolution
        var reading = new List<(PointD TopLeft, PointD TopRight, PointD BottomLeft, PointD BottomRight, double Length)>(boxes.Count);
        foreach (var box in boxes)
        {
            PointD topLeft = box.TopLeft, topRight = box.TopRight, bottomRight = box.BottomRight, bottomLeft = box.BottomLeft;
            int width = Math.Max(1, (int)Math.Max(topLeft.DistanceTo(topRight), bottomRight.DistanceTo(bottomLeft)));
            int height = Math.Max(1, (int)Math.Max(topLeft.DistanceTo(bottomLeft), topRight.DistanceTo(bottomRight)));
            var crop = original.Crop(topLeft, topRight, bottomLeft, width, height);
            if ((double)crop.Height / crop.Width >= 1.5)
            {
                // Read turned counter clockwise: the text runs from the top to the bottom of the box, its top is the right edge
                crops.Add(crop.RotateCounterClockwise());
                reading.Add((topRight, bottomRight, topLeft, bottomLeft, height));
            }
            else
            {
                crops.Add(crop);
                reading.Add((topLeft, topRight, bottomLeft, bottomRight, width));
            }
        }

        var recognized = _recognizer.Recognize(crops);
        var lines = new List<IOcrLineFeature>();
        for (int i = 0; i < boxes.Count; i++)
        {
            var line = recognized[i];
            if (string.IsNullOrWhiteSpace(line.Text) || line.Score < TextScore) continue;

            var (topLeft, topRight, bottomLeft, bottomRight, length) = reading[i];
            Point ToPoint(PointD point) => new Point(point.X, point.Y);
            PointD Along(PointD a, PointD b, double fraction) => new PointD(a.X + (b.X - a.X) * fraction, a.Y + (b.Y - a.Y) * fraction);
            var lineBox = OcrBox.FromCorners(ToPoint(topLeft), ToPoint(topRight), ToPoint(bottomLeft));
            var words = new List<OcrWordInfo>();
            foreach (var (text, start, end, score) in line.Words)
            {
                // Along the top and bottom edge of the box
                double from = Math.Min(1, start / length), to = Math.Min(1, end / length);
                var wordBox = OcrBox.FromCorners(ToPoint(Along(topLeft, topRight, from)), ToPoint(Along(topLeft, topRight, to)),
                    ToPoint(Along(bottomLeft, bottomRight, from)));
                words.Add(new OcrWordInfo(text, wordBox, score));
            }

            lines.Add(new DetectedOcrLine(lineBox, string.Join(" ", words.Select(word => word.Text)), words, line.Score));
        }

        return lines;
    }

    /// <summary>
    /// The text lines of an image of at most MaxSideLength per side, in its pixels: kept within 30..2000 pixels, low or very wide
    /// images get black rows above and below (as RapidOCR)
    /// </summary>
    private List<TextBox> FindBoxes(BgrImage original)
    {
        var image = original;
        if (Math.Max(image.Width, image.Height) > MaxSideLength)
        {
            image = ResizeTo(image, (double)MaxSideLength / Math.Max(image.Width, image.Height));
        }
        if (Math.Min(image.Width, image.Height) < MinSideLength)
        {
            image = ResizeTo(image, (double)MinSideLength / Math.Min(image.Width, image.Height));
        }
        double ratioX = (double)original.Width / image.Width, ratioY = (double)original.Height / image.Height;

        int padding = 0;
        if (image.Height <= MinHeight || (double)image.Width / image.Height > WidthHeightRatio)
        {
            int paddedHeight = Math.Max((int)(image.Width / WidthHeightRatio), MinHeight) * 2;
            padding = Math.Abs(paddedHeight - image.Height) / 2;
            image = image.PadVertically(padding, padding);
        }

        PointD ToOriginal(PointD point) => new PointD(point.X * ratioX, (point.Y - padding) * ratioY);
        return _detector.Detect(image)
            .Select(box => new TextBox(ToOriginal(box.TopLeft), ToOriginal(box.TopRight), ToOriginal(box.BottomRight), ToOriginal(box.BottomLeft), box.Score))
            .ToList();
    }

    /// <summary>
    /// The text lines of a very large capture (several screens side by side): RapidOCR shrinks it to MaxSideLength, which loses small
    /// text (three screens: a quarter of the size, most lines are gone). Instead the lines are found in overlapping tiles of at most
    /// MaxTileSide, each shrunk at most to half; a line in the overlap or across the edge of a tile is found twice and joined.
    /// </summary>
    private List<TextBox> FindBoxesInTiles(BgrImage original)
    {
        var found = new List<(TextBox Box, int Tile)>();
        int tile = 0;
        foreach (var (x, y, width, height) in Tiles(original.Width, original.Height, MaxTileSide, TileOverlap))
        {
            var part = original.Crop(new PointD(x, y), new PointD(x + width, y), new PointD(x, y + height), width, height);
            found.AddRange(FindBoxes(part).Select(box => (Move(box, x, y), tile)));
            tile++;
        }
        return JoinAcrossTiles(found);
    }

    /// <summary>
    /// Tiles of at most size per side which overlap by overlap pixels, covering the image
    /// </summary>
    internal static IEnumerable<(int X, int Y, int Width, int Height)> Tiles(int width, int height, int size, int overlap)
    {
        IEnumerable<(int Start, int Length)> Split(int length)
        {
            if (length <= size)
            {
                yield return (0, length);
                yield break;
            }
            // Evenly: n tiles of the same size with the overlap between them
            int count = (int)Math.Ceiling((double)(length - overlap) / (size - overlap));
            int tileLength = (int)Math.Ceiling((double)(length + (count - 1) * overlap) / count);
            for (int i = 0; i < count; i++)
            {
                int start = Math.Min(i * (tileLength - overlap), length - tileLength);
                yield return (start, tileLength);
            }
        }

        foreach (var (y, h) in Split(height))
        {
            foreach (var (x, w) in Split(width))
            {
                yield return (x, y, w, h);
            }
        }
    }

    /// <summary>
    /// Boxes of different tiles which are parts of the same line (they overlap and lie on the same height, at the same angle) become
    /// one box around both; the same line found in two tiles becomes one
    /// </summary>
    internal static List<TextBox> JoinAcrossTiles(List<(TextBox Box, int Tile)> found)
    {
        var result = new List<(TextBox Box, HashSet<int> Tiles)>();
        foreach (var (box, tile) in found)
        {
            var current = box;
            var tiles = new HashSet<int> { tile };
            bool joined;
            do
            {
                joined = false;
                for (int i = 0; i < result.Count; i++)
                {
                    if (result[i].Tiles.Overlaps(tiles) || !SameLine(result[i].Box, current)) continue;
                    current = Around(result[i].Box, current);
                    tiles.UnionWith(result[i].Tiles);
                    result.RemoveAt(i);
                    joined = true;
                    break;
                }
            } while (joined);
            result.Add((current, tiles));
        }
        return result.Select(entry => entry.Box).ToList();
    }

    private static bool SameLine(TextBox a, TextBox b)
    {
        double angleA = AngleOf(a), angleB = AngleOf(b);
        if (Math.Abs(angleA - angleB) > 3) return false;
        // In the frame of a: b's corners turned back
        double radians = -angleA * Math.PI / 180, cos = Math.Cos(radians), sin = Math.Sin(radians);
        (double X, double Y) Turn(PointD p) => (p.X * cos - p.Y * sin, p.X * sin + p.Y * cos);
        var ca = a.Corners.Select(Turn).ToList();
        var cb = b.Corners.Select(Turn).ToList();
        double aLeft = ca.Min(p => p.X), aRight = ca.Max(p => p.X), aTop = ca.Min(p => p.Y), aBottom = ca.Max(p => p.Y);
        double bLeft = cb.Min(p => p.X), bRight = cb.Max(p => p.X), bTop = cb.Min(p => p.Y), bBottom = cb.Max(p => p.Y);
        double vertical = Math.Min(aBottom, bBottom) - Math.Max(aTop, bTop);
        double horizontal = Math.Min(aRight, bRight) - Math.Max(aLeft, bLeft);
        return horizontal > 0 && vertical >= 0.6 * Math.Min(aBottom - aTop, bBottom - bTop);
    }

    private static double AngleOf(TextBox box) => Math.Atan2(box.TopRight.Y - box.TopLeft.Y, box.TopRight.X - box.TopLeft.X) * 180 / Math.PI;

    /// <summary>
    /// The box around both, at the angle of the first
    /// </summary>
    private static TextBox Around(TextBox a, TextBox b)
    {
        double radians = AngleOf(a) * Math.PI / 180, cos = Math.Cos(radians), sin = Math.Sin(radians);
        // Along the text (u) and down (v)
        (double U, double V) ToFrame(PointD p) => (p.X * cos + p.Y * sin, -p.X * sin + p.Y * cos);
        PointD FromFrame(double u, double v) => new PointD(u * cos - v * sin, u * sin + v * cos);
        var points = a.Corners.Concat(b.Corners).Select(ToFrame).ToList();
        double left = points.Min(p => p.U), right = points.Max(p => p.U), top = points.Min(p => p.V), bottom = points.Max(p => p.V);
        return new TextBox(FromFrame(left, top), FromFrame(right, top), FromFrame(right, bottom), FromFrame(left, bottom), Math.Max(a.Score, b.Score));
    }

    private static TextBox Move(TextBox box, int x, int y)
    {
        PointD Offset(PointD p) => new PointD(p.X + x, p.Y + y);
        return new TextBox(Offset(box.TopLeft), Offset(box.TopRight), Offset(box.BottomRight), Offset(box.BottomLeft), box.Score);
    }

    private static BgrImage ResizeTo(BgrImage image, double ratio)
    {
        int width = Math.Max(32, (int)Math.Round((int)(image.Width * ratio) / 32.0) * 32);
        int height = Math.Max(32, (int)Math.Round((int)(image.Height * ratio) / 32.0) * 32);
        return image.Resize(width, height);
    }

    /// <summary>
    /// The pixels as blue, green, red; transparent pixels on white
    /// </summary>
    internal static BgrImage FromBitmap(Bitmap bitmap)
    {
        int width = bitmap.Width, height = bitmap.Height;
        var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var row = new byte[width * 4];
            var result = new BgrImage(width, height);
            for (int y = 0; y < height; y++)
            {
                Marshal.Copy(data.Scan0 + y * data.Stride, row, 0, row.Length);
                for (int x = 0; x < width; x++)
                {
                    int alpha = row[x * 4 + 3];
                    int target = (y * width + x) * 3;
                    for (int c = 0; c < 3; c++)
                    {
                        result.Data[target + c] = (byte)((row[x * 4 + c] * alpha + 255 * (255 - alpha)) / 255);
                    }
                }
            }

            return result;
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    public void Dispose()
    {
        _detector.Dispose();
        _recognizer.Dispose();
    }
}
