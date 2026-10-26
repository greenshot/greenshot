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
}
