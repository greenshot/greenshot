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
        // Within 30..2000 pixels, multiples of 32 when resized
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

        // Low or very wide images get black rows above and below
        int padding = 0;
        if (image.Height <= MinHeight || (double)image.Width / image.Height > WidthHeightRatio)
        {
            int paddedHeight = Math.Max((int)(image.Width / WidthHeightRatio), MinHeight) * 2;
            padding = Math.Abs(paddedHeight - image.Height) / 2;
            image = image.PadVertically(padding, padding);
        }

        var boxes = _detector.Detect(image);
        if (boxes.Count == 0) return new List<IOcrLineFeature>();

        var crops = new List<BgrImage>(boxes.Count);
        // The corners in reading direction (top left, top right, bottom left, bottom right) and the length along the text, in pixels of
        // the capture: the lines are cut out of the capture itself, not of the image the detection read, which is smaller for captures
        // over MaxSideLength (several screens): reading the smaller pixels makes errors (8ithub.com, gmailcom)
        PointD ToOriginal(PointD point) => new PointD(point.X * ratioX, (point.Y - padding) * ratioY);
        var reading = new List<(PointD TopLeft, PointD TopRight, PointD BottomLeft, PointD BottomRight, double Length)>(boxes.Count);
        foreach (var box in boxes)
        {
            PointD topLeft = ToOriginal(box.TopLeft), topRight = ToOriginal(box.TopRight), bottomRight = ToOriginal(box.BottomRight), bottomLeft = ToOriginal(box.BottomLeft);
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
