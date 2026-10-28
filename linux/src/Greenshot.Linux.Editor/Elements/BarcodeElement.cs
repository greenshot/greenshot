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

using System.Text.Json.Serialization;
using Greenshot.Linux.Core.Barcodes;
using Greenshot.Linux.Editor.Fields;
using Greenshot.Linux.Editor.Rendering;
using SkiaSharp;

namespace Greenshot.Linux.Editor.Elements;

/// <summary>
/// A QR code or barcode, made again in its size when it is resized so it stays sharp; 2D codes stay square.
/// The BarcodeContainer of the Zxing plugin of Windows; double-click opens its settings.
/// </summary>
public sealed class BarcodeElement : DrawableElement, IDisposable
{
    /// <summary>
    /// The formats of the editor window of Windows, in its order
    /// </summary>
    public static readonly IReadOnlyList<string> Formats =
    [
        "QR_CODE", "AZTEC", "DATA_MATRIX", "PDF_417", "CODE_128", "CODE_39", "CODE_93", "EAN_13", "EAN_8", "UPC_A", "UPC_E", "CODABAR", "ITF", "MSI", "PLESSEY",
    ];

    /// <summary>
    /// The kinds of QR codes, the QrType of the recipe annotations
    /// </summary>
    public static readonly IReadOnlyList<string> QrTypes = ["Text", "Wifi", "BusinessCard", "Payment", "Email", "CalendarEvent", "Phone", "Sms", "Geo"];

    private SKBitmap? _cache;
    private string? _cacheKey;

    public string Format { get; set; } = "QR_CODE";

    public string QrType { get; set; } = "Text";

    /// <summary>
    /// The inputs: Text, WifiSsid, WifiPassword, WifiEncryption, VcardFirstName, ... (the names of QrPayloads)
    /// </summary>
    public Dictionary<string, string> Values { get; set; } = new();

    public SKColor ForeColor { get; set; } = SKColors.Black;

    public SKColor BackColor { get; set; } = SKColors.White;

    public bool RoundedDots { get; set; }

    /// <summary>
    /// The quiet zone around the code, in modules
    /// </summary>
    public int Margin { get; set; } = 1;

    public override string WindowsTypeName => "BarcodeContainer";

    [JsonIgnore]
    public override IReadOnlyList<FieldType> Fields => [];

    [JsonIgnore]
    public bool Is2D => Barcodes.Is2D(Barcodes.ParseFormat(Format));

    /// <summary>
    /// What is encoded: for a QR code made from its type and inputs, else the text
    /// </summary>
    [JsonIgnore]
    public string Payload => Format == "QR_CODE"
        ? QrPayloads.Create(QrType, key => Values.TryGetValue(key, out var value) && value.Length > 0 ? value : null) ?? string.Empty
        : Values.GetValueOrDefault("Text") ?? string.Empty;

    public override ElementStyle CreateDefaultStyle() => new();

    /// <summary>
    /// The code as bitmap in a size, null when the inputs can't be encoded in the format (e.g. letters in an EAN); the caller owns it
    /// </summary>
    public SKBitmap? Render(int width, int height)
    {
        string payload = Payload;
        if (string.IsNullOrEmpty(payload) || width < 1 || height < 1)
        {
            return null;
        }

        try
        {
            return Barcodes.Generate(payload, Barcodes.ParseFormat(Format), ForeColor, BackColor, RoundedDots, width, height, Margin);
        }
        catch (Exception ex) when (ex is ArgumentException or ZXing.WriterException or FormatException or InvalidOperationException)
        {
            return null;
        }
    }

    public override void Draw(RenderContext context)
    {
        var rect = NormalizedBounds;
        int width = Math.Max(1, (int)Math.Round(rect.Width));
        int height = Math.Max(1, (int)Math.Round(rect.Height));
        string key = $"{Format}|{QrType}|{Payload}|{ForeColor}|{BackColor}|{RoundedDots}|{Margin}|{width}x{height}";
        if (_cacheKey != key)
        {
            _cache?.Dispose();
            _cache = Render(width, height);
            _cacheKey = key;
        }

        if (_cache == null)
        {
            // Can't be encoded: a crossed frame, so the element can still be found and changed
            using var pen = Painting.Stroke(SKColors.Red, 1);
            context.Canvas.DrawRect(rect, pen);
            context.Canvas.DrawLine(rect.Left, rect.Top, rect.Right, rect.Bottom, pen);
            context.Canvas.DrawLine(rect.Right, rect.Top, rect.Left, rect.Bottom, pen);
            return;
        }

        context.Canvas.DrawBitmap(_cache, new SKRect(rect.Left, rect.Top, rect.Left + _cache.Width, rect.Top + _cache.Height));
    }

    /// <summary>
    /// A 2D code keeps its proportions
    /// </summary>
    public override void DragHandle(HandlePosition handle, SKPoint point, bool keepAspect) => base.DragHandle(handle, point, keepAspect || Is2D);

    public void Dispose()
    {
        _cache?.Dispose();
        _cache = null;
    }
}
