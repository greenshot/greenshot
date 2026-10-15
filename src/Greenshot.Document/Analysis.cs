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
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Greenshot.Document
{
    /// <summary>
    /// What was found in the pixels of one resource, in that resource's own pixels. A resource never changes, so the
    /// results can't go stale; they are a cache which can be made again (except the areas, read at capture time).
    /// </summary>
    public sealed record ImageAnalysis
    {
        public OcrResult Ocr { get; init; }

        public IReadOnlyList<BarcodeResult> Barcodes { get; init; }

        /// <summary>
        /// Windows and UI elements under the capture, read at capture time (optional)
        /// </summary>
        public IReadOnlyList<AreaResult> Areas { get; init; }

        /// <summary>
        /// Values of plugins (keyed by plugin id), written back unchanged
        /// </summary>
        public RawJson Extensions { get; init; }

        /// <summary>
        /// Kinds of results this version doesn't know, written back unchanged
        /// </summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement> Unknown { get; init; }

        [JsonIgnore]
        public bool IsEmpty => Ocr == null && (Barcodes == null || Barcodes.Count == 0) && (Areas == null || Areas.Count == 0) && Extensions == null && (Unknown == null || Unknown.Count == 0);
    }

    /// <summary>
    /// Text found by an OCR engine
    /// </summary>
    public sealed record OcrResult(IReadOnlyList<OcrLine> Lines)
    {
        public string Engine { get; init; }

        public string Language { get; init; }

        public DateTimeOffset? Date { get; init; }

        /// <summary>
        /// The angle of the text in degrees, clockwise, when the engine reports it. The bounds of lines and words are
        /// those of the straightened text: turned by TextAngle around the centre of the image they are where the text is.
        /// </summary>
        public double? TextAngle { get; init; }

        /// <summary>
        /// The area and rotation on the canvas of a line or word (its Bounds) the OCR found in the image the image element
        /// shows, e.g. for an obfuscate or highlight element which covers it exactly. Width and Height are the size of the image.
        /// </summary>
        public (RectD Area, double Rotation) OnCanvas(RectD bounds, ImageElement image, int width, int height)
        {
            double angle = TextAngle ?? 0;
            var center = bounds.Center.Turned(angle, new PointD(width / 2.0, height / 2.0));
            var source = image.Source ?? new RectD(0, 0, 1, 1);
            double scaleX = image.Area.Width / (source.Width * width), scaleY = image.Area.Height / (source.Height * height);
            var onCanvas = new PointD(image.Area.X + (center.X - source.X * width) * scaleX, image.Area.Y + (center.Y - source.Y * height) * scaleY)
                .Turned(image.Rotation, image.Area.Center);
            double areaWidth = bounds.Width * scaleX, areaHeight = bounds.Height * scaleY;
            return (new RectD(onCanvas.X - areaWidth / 2, onCanvas.Y - areaHeight / 2, areaWidth, areaHeight), angle + image.Rotation);
        }
    }

    public sealed record OcrLine(string Text, RectD Bounds, IReadOnlyList<OcrWord> Words)
    {
        /// <summary>
        /// 0..1, only when the engine has it, missing means unknown
        /// </summary>
        public double? Confidence { get; init; }
    }

    public sealed record OcrWord(string Text, RectD Bounds)
    {
        public double? Confidence { get; init; }
    }

    /// <summary>
    /// A barcode with its corner points, so a tilted code is described exactly
    /// </summary>
    public sealed record BarcodeResult(string Format, string Text, IReadOnlyList<PointD> Points)
    {
        [JsonIgnore]
        public RectD Bounds
        {
            get
            {
                if (Points == null || Points.Count == 0)
                {
                    return default;
                }

                var bounds = new RectD(Points[0].X, Points[0].Y, 0, 0);
                foreach (var point in Points)
                {
                    bounds = RectD.Union(bounds, new RectD(point.X, point.Y, 0, 0));
                }

                return bounds;
            }
        }

        /// <summary>
        /// The corners of an upright rectangle, clockwise from the top left
        /// </summary>
        public static IReadOnlyList<PointD> Corners(RectD bounds) =>
            new[] { new PointD(bounds.X, bounds.Y), new PointD(bounds.Right, bounds.Y), new PointD(bounds.Right, bounds.Bottom), new PointD(bounds.X, bounds.Bottom) };
    }

    /// <summary>
    /// A window ("window") or UI element ("element"), Parent is the id of the area it is in
    /// </summary>
    public sealed record AreaResult(int Id, string Type, RectD Bounds)
    {
        public int? Parent { get; init; }

        public string Title { get; init; }

        public string Role { get; init; }

        public string Name { get; init; }

        public string AutomationId { get; init; }

        public int? Z { get; init; }
    }
}
