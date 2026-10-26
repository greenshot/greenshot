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

using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SkiaSharp;

namespace Greenshot.Linux.Helpers;

internal static class BitmapConverter
{
    /// <summary>
    /// Copy a Skia bitmap into an Avalonia bitmap
    /// </summary>
    public static Bitmap ToAvalonia(SKBitmap source)
    {
        if (source.ColorType == SKColorType.Bgra8888 && source.AlphaType == SKAlphaType.Premul)
        {
            return Create(source);
        }

        using var converted = new SKBitmap(new SKImageInfo(source.Width, source.Height, SKColorType.Bgra8888, SKAlphaType.Premul));
        using (var canvas = new SKCanvas(converted))
        {
            canvas.DrawBitmap(source, 0, 0);
        }

        return Create(converted);
    }

    private static Bitmap Create(SKBitmap bgra) =>
        new(PixelFormat.Bgra8888, AlphaFormat.Premul, bgra.GetPixels(), new PixelSize(bgra.Width, bgra.Height), new Vector(96, 96), bgra.RowBytes);
}
