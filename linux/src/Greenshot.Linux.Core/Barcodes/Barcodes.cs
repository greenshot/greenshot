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

using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

namespace Greenshot.Linux.Core.Barcodes;

/// <summary>
/// A QR code or barcode found in a capture
/// </summary>
public sealed record DetectedBarcode(string Text, string Format, IReadOnlyList<SKPoint> Points)
{
    public bool IsUrl => Uri.TryCreate(Text, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

/// <summary>
/// Reading and making QR codes with ZXing.Net, the library of the Zxing plugin of Windows
/// </summary>
public static class Barcodes
{
    /// <summary>
    /// All QR codes and barcodes in the image
    /// </summary>
    public static IReadOnlyList<DetectedBarcode> Detect(SKBitmap image)
    {
        using var bgra = image.ColorType == SKColorType.Bgra8888 ? null : image.Copy(SKColorType.Bgra8888);
        var source = bgra ?? image;
        var reader = new BarcodeReaderGeneric
        {
            AutoRotate = true,
            Options = new DecodingOptions
            {
                TryHarder = true,
                PossibleFormats =
                [
                    BarcodeFormat.QR_CODE, BarcodeFormat.DATA_MATRIX, BarcodeFormat.AZTEC, BarcodeFormat.PDF_417,
                    BarcodeFormat.EAN_13, BarcodeFormat.EAN_8, BarcodeFormat.CODE_128, BarcodeFormat.CODE_39, BarcodeFormat.UPC_A,
                ],
            },
        };

        var results = reader.DecodeMultiple(source.Bytes, source.Width, source.Height, RGBLuminanceSource.BitmapFormat.BGRA32);
        return results?
            .Where(r => !string.IsNullOrEmpty(r.Text))
            .GroupBy(r => r.Text)
            .Select(g => g.First())
            .Select(r => new DetectedBarcode(r.Text, r.BarcodeFormat.ToString(), r.ResultPoints.Select(p => new SKPoint(p.X, p.Y)).ToList()))
            .ToList() ?? [];
    }

    /// <summary>
    /// A QR code for the text, black on white, the caller owns the bitmap
    /// </summary>
    public static SKBitmap CreateQrCode(string text, int size = 256, int margin = 1)
    {
        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions { Width = size, Height = size, Margin = margin, CharacterSet = "UTF-8" },
        };
        var pixels = writer.Write(text);
        var bitmap = new SKBitmap(new SKImageInfo(pixels.Width, pixels.Height, SKColorType.Bgra8888, SKAlphaType.Premul));
        System.Runtime.InteropServices.Marshal.Copy(pixels.Pixels, 0, bitmap.GetPixels(), pixels.Pixels.Length);
        return bitmap;
    }
    /// <summary>
    /// The barcode format of a name like QR_CODE, CODE_128, ean13 (MapBarcodeFormat of Windows), QR code for unknown names
    /// </summary>
    public static BarcodeFormat ParseFormat(string? name)
    {
        string normalized = (name ?? string.Empty).Trim().Replace(" ", "").Replace("-", "").Replace("_", "").ToUpperInvariant();
        if (normalized.EndsWith("2D", StringComparison.Ordinal) || normalized.EndsWith("1D", StringComparison.Ordinal))
        {
            normalized = normalized[..^2];
        }

        return normalized switch
        {
            "AZTEC" => BarcodeFormat.AZTEC,
            "DATAMATRIX" => BarcodeFormat.DATA_MATRIX,
            "PDF417" => BarcodeFormat.PDF_417,
            "CODE128" => BarcodeFormat.CODE_128,
            "CODE39" => BarcodeFormat.CODE_39,
            "CODE93" => BarcodeFormat.CODE_93,
            "EAN13" => BarcodeFormat.EAN_13,
            "EAN8" => BarcodeFormat.EAN_8,
            "UPCA" => BarcodeFormat.UPC_A,
            "UPCE" => BarcodeFormat.UPC_E,
            "CODABAR" => BarcodeFormat.CODABAR,
            "ITF" => BarcodeFormat.ITF,
            "MSI" => BarcodeFormat.MSI,
            "PLESSEY" => BarcodeFormat.PLESSEY,
            _ => BarcodeFormat.QR_CODE,
        };
    }

    public static bool Is2D(BarcodeFormat format) => format is BarcodeFormat.QR_CODE or BarcodeFormat.AZTEC or BarcodeFormat.DATA_MATRIX or BarcodeFormat.PDF_417;

    /// <summary>
    /// A barcode or QR code in the colors, with rounded dots (not the finder patterns of a QR code), like the
    /// ZxingBarcodeGenerator of Windows; a 2D code keeps its square modules. The caller owns the bitmap.
    /// </summary>
    public static SKBitmap Generate(string payload, BarcodeFormat format, SKColor foreColor, SKColor backColor, bool roundedDots = false,
        int? width = null, int? height = null, int margin = 4)
    {
        var hints = new Dictionary<EncodeHintType, object> { [EncodeHintType.CHARACTER_SET] = "UTF-8", [EncodeHintType.MARGIN] = margin };
        var matrix = new MultiFormatWriter().encode(payload, format, 0, 0, hints);
        bool is2D = Is2D(format);
        int targetWidth = width is > 0 ? width.Value : is2D ? 200 : 350;
        int targetHeight = height is > 0 ? height.Value : is2D ? 200 : 100;
        float scaleX = (float)targetWidth / matrix.Width;
        float scaleY = (float)targetHeight / matrix.Height;
        if (is2D)
        {
            scaleX = scaleY = Math.Min(scaleX, scaleY);
            targetWidth = Math.Max(1, (int)Math.Round(matrix.Width * scaleX));
            targetHeight = Math.Max(1, (int)Math.Round(matrix.Height * scaleY));
        }

        var bitmap = new SKBitmap(new SKImageInfo(targetWidth, targetHeight, SKColorType.Bgra8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(backColor);
        using var paint = new SKPaint { Color = foreColor, IsAntialias = roundedDots, Style = SKPaintStyle.Fill };
        for (int y = 0; y < matrix.Height; y++)
        {
            for (int x = 0; x < matrix.Width; x++)
            {
                if (!matrix[x, y])
                {
                    continue;
                }

                var cell = SKRect.Create(x * scaleX, y * scaleY, scaleX, scaleY);
                bool finder = format == BarcodeFormat.QR_CODE && IsFinderPattern(x, y, matrix.Width, matrix.Height, margin);
                if (roundedDots && is2D && !finder)
                {
                    canvas.DrawOval(SKRect.Create(cell.Left + 0.25f, cell.Top + 0.25f, Math.Max(1f, scaleX - 0.5f), Math.Max(1f, scaleY - 0.5f)), paint);
                }
                else
                {
                    canvas.DrawRect(cell, paint);
                }
            }
        }

        return bitmap;
    }

    private static bool IsFinderPattern(int x, int y, int width, int height, int margin) =>
        x >= margin && x < margin + 7 && y >= margin && y < margin + 7
        || x >= width - margin - 7 && x < width - margin && y >= margin && y < margin + 7
        || x >= margin && x < margin + 7 && y >= height - margin - 7 && y < height - margin;
}

/// <summary>
/// The contents of the QR code types of Windows (ZXing plugin): WiFi, business card (vCard 3.0), SEPA payment (EPC), e-mail,
/// calendar event, phone, SMS and location; Text is the text itself. The values are looked up by the property names of recipes.
/// </summary>
public static class QrPayloads
{
    public static string? Create(string? qrType, Func<string, string?> get)
    {
        string Value(string key) => get(key) ?? string.Empty;
        switch ((qrType ?? "Text").Trim().ToLowerInvariant())
        {
            case "wifi":
                return $"WIFI:S:{Value("WifiSsid")};T:{get("WifiEncryption") ?? "WPA"};P:{Value("WifiPassword")};;";
            case "businesscard":
                return "BEGIN:VCARD\r\nVERSION:3.0\r\n" +
                       $"N:{Value("VcardLastName")};{Value("VcardFirstName")}\r\n" +
                       $"FN:{Value("VcardFirstName")} {Value("VcardLastName")}\r\n" +
                       $"ORG:{Value("VcardCompany")}\r\n" +
                       $"TEL;TYPE=CELL:{Value("VcardPhone")}\r\n" +
                       $"EMAIL:{Value("VcardEmail")}\r\n" +
                       $"URL:{Value("VcardUrl")}\r\nEND:VCARD";
            case "payment":
                return "BCD\n002\n1\nSCT\n" +
                       $"{Value("EpcBic")}\n{Value("EpcName")}\n{Value("EpcIban")}\n{FormatEpcAmount(get("EpcAmount"))}\n\n{Value("EpcReference")}\n{Value("EpcMessage")}\n";
            case "email":
                var query = new List<string>();
                if (!string.IsNullOrEmpty(get("EmailSubject"))) query.Add($"subject={Uri.EscapeDataString(get("EmailSubject")!)}");
                if (!string.IsNullOrEmpty(get("EmailBody"))) query.Add($"body={Uri.EscapeDataString(get("EmailBody")!)}");
                return $"mailto:{Value("EmailTo")}" + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty);
            case "calendarevent":
                return "BEGIN:VCALENDAR\r\nVERSION:2.0\r\nBEGIN:VEVENT\r\n" +
                       $"SUMMARY:{Value("EventTitle")}\r\n" +
                       $"LOCATION:{Value("EventLocation")}\r\n" +
                       $"DESCRIPTION:{Value("EventDescription")}\r\n" +
                       (string.IsNullOrEmpty(get("EventStart")) ? "" : $"DTSTART:{get("EventStart")}\r\n") +
                       (string.IsNullOrEmpty(get("EventEnd")) ? "" : $"DTEND:{get("EventEnd")}\r\n") +
                       "END:VEVENT\r\nEND:VCALENDAR";
            case "phone":
                return $"tel:{Value("PhoneNumber")}";
            case "sms":
                return $"smsto:{Value("SmsNumber")}:{Value("SmsMessage")}";
            case "geo":
                return $"geo:{get("Latitude") ?? "0"},{get("Longitude") ?? "0"}";
            default:
                return get("Text");
        }
    }

    /// <summary>
    /// EUR with two decimals, a comma counts as the decimal point
    /// </summary>
    public static string FormatEpcAmount(string? amount)
    {
        if (string.IsNullOrWhiteSpace(amount)) return string.Empty;
        string trimmed = amount.Trim();
        if (double.TryParse(trimmed.Replace(',', '.'), System.Globalization.NumberStyles.AllowDecimalPoint, System.Globalization.CultureInfo.InvariantCulture, out double value))
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "EUR{0:F2}", value);
        }

        return trimmed.StartsWith("EUR", StringComparison.OrdinalIgnoreCase) ? trimmed : $"EUR{trimmed}";
    }
}
