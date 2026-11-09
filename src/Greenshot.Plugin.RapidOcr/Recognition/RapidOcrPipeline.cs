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
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces.Plugin;
using Microsoft.ML.OnnxRuntime;

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
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
            // Leave cores to the rest of the machine
            IntraOpNumThreads = Math.Max(1, Environment.ProcessorCount / 2)
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
        var widths = new List<double>(boxes.Count);
        var rotated = new List<bool>(boxes.Count);
        foreach (var box in boxes)
        {
            int width = Math.Max(1, (int)Math.Max(box.TopLeft.DistanceTo(box.TopRight), box.BottomRight.DistanceTo(box.BottomLeft)));
            int height = Math.Max(1, (int)Math.Max(box.TopLeft.DistanceTo(box.BottomLeft), box.TopRight.DistanceTo(box.BottomRight)));
            var crop = image.Crop(box.TopLeft, box.TopRight, box.BottomLeft, width, height);
            bool upright = (double)crop.Height / crop.Width >= 1.5;
            crops.Add(upright ? crop.RotateCounterClockwise() : crop);
            widths.Add(width);
            rotated.Add(upright);
        }

        var recognized = _recognizer.Recognize(crops);
        var lines = new List<IOcrLineFeature>();
        for (int i = 0; i < boxes.Count; i++)
        {
            var line = recognized[i];
            if (string.IsNullOrWhiteSpace(line.Text) || line.Score < TextScore) continue;

            var box = boxes[i];
            PointD ToOriginal(PointD point) => new PointD(point.X * ratioX, (point.Y - padding) * ratioY);
            var corners = box.Corners.Select(ToOriginal).ToArray();
            var lineBounds = Bounds(corners, original.Width, original.Height);
            var words = new List<OcrWordInfo>();
            foreach (var (text, start, end) in line.Words)
            {
                NativeRect wordBounds = lineBounds;
                if (!rotated[i])
                {
                    // Along the top and bottom edge of the box
                    double from = Math.Min(1, start / widths[i]), to = Math.Min(1, end / widths[i]);
                    PointD Along(PointD a, PointD b, double fraction) => new PointD(a.X + (b.X - a.X) * fraction, a.Y + (b.Y - a.Y) * fraction);
                    wordBounds = Bounds(new[]
                    {
                        ToOriginal(Along(box.TopLeft, box.TopRight, from)), ToOriginal(Along(box.TopLeft, box.TopRight, to)),
                        ToOriginal(Along(box.BottomLeft, box.BottomRight, from)), ToOriginal(Along(box.BottomLeft, box.BottomRight, to))
                    }, original.Width, original.Height);
                }
                words.Add(new OcrWordInfo { Text = text, Bounds = wordBounds });
            }

            lines.Add(new DetectedOcrLine(lineBounds, string.Join(" ", words.Select(word => word.Text)), words));
        }

        return lines;
    }

    private static BgrImage ResizeTo(BgrImage image, double ratio)
    {
        int width = Math.Max(32, (int)Math.Round((int)(image.Width * ratio) / 32.0) * 32);
        int height = Math.Max(32, (int)Math.Round((int)(image.Height * ratio) / 32.0) * 32);
        return image.Resize(width, height);
    }

    private static NativeRect Bounds(PointD[] points, int width, int height)
    {
        int left = (int)Math.Max(0, Math.Floor(points.Min(point => point.X)));
        int top = (int)Math.Max(0, Math.Floor(points.Min(point => point.Y)));
        int right = (int)Math.Min(width, Math.Ceiling(points.Max(point => point.X)));
        int bottom = (int)Math.Min(height, Math.Ceiling(points.Max(point => point.Y)));
        return new NativeRect(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));
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
