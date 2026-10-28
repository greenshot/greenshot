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

using System.Collections.Concurrent;
using SkiaSharp;
using SkiaSharp.HarfBuzz;

namespace Greenshot.Linux.Editor.Emoji;

/// <summary>
/// Draws emoji with the Twemoji font of the Windows editor, so they look the same everywhere (the EmojiRenderer of Windows)
/// </summary>
public static class EmojiRenderer
{
    public const string FontFileName = "Twemoji.Mozilla.ttf";

    private static readonly Lazy<SKTypeface?> Typeface = new(LoadTypeface);
    private static readonly ConcurrentDictionary<string, SKBitmap> Cache = new();

    /// <summary>
    /// The folder of the font file, Emoji next to the program
    /// </summary>
    public static string FontPath => Path.Combine(AppContext.BaseDirectory, "Emoji", FontFileName);

    private static SKTypeface? LoadTypeface()
    {
        if (!File.Exists(FontPath))
        {
            Core.Log.Warn($"The emoji font {FontPath} is missing, emoji are drawn with the fonts of the system");
            return SKFontManager.Default.MatchCharacter(0x1F600);
        }

        return SKTypeface.FromFile(FontPath);
    }

    /// <summary>
    /// Draw the emoji centered into the square
    /// </summary>
    public static void Draw(SKCanvas canvas, string emoji, SKRect square)
    {
        if (string.IsNullOrEmpty(emoji) || square.Width <= 0 || square.Height <= 0)
        {
            return;
        }

        float size = Math.Min(square.Width, square.Height);
        var typeface = Typeface.Value ?? SKTypeface.Default;
        using var font = new SKFont(typeface, size * 0.95f);
        using var paint = new SKPaint { IsAntialias = true, Color = SKColors.Black };
        // Shaped, so sequences become one emoji: skin tones, families, flags
        using var shaper = new SKShaper(typeface);
        var shaped = shaper.Shape(emoji, font);
        var metrics = font.Metrics;
        // Centered like on Windows, a little lower for the baseline of the font
        float x = square.MidX - shaped.Width / 2;
        float y = square.MidY - (metrics.Ascent + metrics.Descent) / 2 + font.Size * 0.045f;
        canvas.DrawShapedText(shaper, emoji, x, y, SKTextAlign.Left, font, paint);
    }

    /// <summary>
    /// The emoji as a square bitmap, shared: don't dispose or change it
    /// </summary>
    public static SKBitmap GetBitmap(string emoji, int size) => Cache.GetOrAdd($"{emoji}|{size}", _ =>
    {
        var bitmap = new SKBitmap(new SKImageInfo(size, size, SKColorType.Bgra8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        Draw(canvas, emoji, new SKRect(0, 0, size, size));
        return bitmap;
    });
}
