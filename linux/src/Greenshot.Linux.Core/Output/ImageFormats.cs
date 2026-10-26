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

namespace Greenshot.Linux.Core.Output;

/// <summary>
/// The file formats Greenshot writes. Skia writes png, jpg and webp; the other formats of the Windows version
/// (bmp, gif, tiff, ico) are not there yet and are written as png.
/// </summary>
public static class ImageFormats
{
    public static readonly IReadOnlyList<string> Supported = ["png", "jpg", "webp"];

    /// <summary>
    /// The format for a configured value or a file extension, png for anything unknown
    /// </summary>
    public static string Normalize(string? format)
    {
        var value = (format ?? string.Empty).Trim().TrimStart('.').ToLowerInvariant();
        return value switch
        {
            "jpeg" or "jpg" => "jpg",
            "webp" => "webp",
            "png" => "png",
            _ => "png",
        };
    }

    public static string MimeType(string format) => Normalize(format) switch
    {
        "jpg" => "image/jpeg",
        "webp" => "image/webp",
        _ => "image/png",
    };

    public static byte[] Encode(SKBitmap image, string format, int quality = 80)
    {
        var normalized = Normalize(format);
        if (format.Trim().TrimStart('.').ToLowerInvariant() is not ("png" or "jpg" or "jpeg" or "webp"))
        {
            Log.Warn($"The format {format} can't be written yet, writing png");
        }

        var skFormat = normalized switch
        {
            "jpg" => SKEncodedImageFormat.Jpeg,
            "webp" => SKEncodedImageFormat.Webp,
            _ => SKEncodedImageFormat.Png,
        };

        using var data = image.Encode(skFormat, normalized == "png" ? 100 : quality)
            ?? throw new InvalidOperationException($"Could not encode the image as {normalized}");
        return data.ToArray();
    }
}
