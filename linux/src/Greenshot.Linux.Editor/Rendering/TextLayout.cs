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

using Greenshot.Linux.Editor.Fields;
using SkiaSharp;

namespace Greenshot.Linux.Editor.Rendering;

/// <summary>
/// Text in a rectangle: wrapped at word boundaries, aligned like the StringFormat of the Windows editor
/// </summary>
public static class TextLayout
{
    public static SKFont CreateFont(string family, float size, bool bold, bool italic)
    {
        var typeface = SKTypeface.FromFamilyName(family,
            bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
            SKFontStyleWidth.Normal,
            italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright) ?? SKTypeface.Default;
        return new SKFont(typeface, size) { Subpixel = true, Edging = SKFontEdging.Antialias };
    }

    public static SKFont CreateFont(ElementStyle style) => CreateFont(style.FontFamily, style.FontSize, style.FontBold, style.FontItalic);

    /// <summary>
    /// Break the text into lines that fit the width; explicit line breaks are kept, a word longer than the width is broken
    /// </summary>
    public static IReadOnlyList<string> Wrap(string text, SKFont font, float width)
    {
        var lines = new List<string>();
        foreach (var paragraph in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (width <= 0 || font.MeasureText(paragraph) <= width)
            {
                lines.Add(paragraph);
                continue;
            }

            var current = string.Empty;
            foreach (var word in paragraph.Split(' '))
            {
                var candidate = current.Length == 0 ? word : current + " " + word;
                if (font.MeasureText(candidate) <= width)
                {
                    current = candidate;
                    continue;
                }

                if (current.Length > 0)
                {
                    lines.Add(current);
                }

                current = word;
                while (current.Length > 1 && font.MeasureText(current) > width)
                {
                    int fit = FitCharacters(current, font, width);
                    lines.Add(current[..fit]);
                    current = current[fit..];
                }
            }

            lines.Add(current);
        }

        return lines;
    }

    /// <summary>
    /// Draw the text in the rectangle with the alignment; the text is clipped to the rectangle
    /// </summary>
    public static void Draw(SKCanvas canvas, string text, SKRect rect, SKFont font, SKColor color,
        TextHorizontalAlignment horizontal, TextVerticalAlignment vertical)
    {
        if (string.IsNullOrEmpty(text) || rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        var lines = Wrap(text, font, rect.Width);
        var metrics = font.Metrics;
        float lineHeight = font.Spacing;
        float totalHeight = lineHeight * lines.Count;
        float y = vertical switch
        {
            TextVerticalAlignment.Top => rect.Top,
            TextVerticalAlignment.Bottom => rect.Bottom - totalHeight,
            _ => rect.MidY - totalHeight / 2,
        };

        using var paint = new SKPaint { Color = color, IsAntialias = true };
        canvas.Save();
        canvas.ClipRect(rect);
        foreach (var line in lines)
        {
            float baseline = y - metrics.Ascent;
            float width = font.MeasureText(line);
            float x = horizontal switch
            {
                TextHorizontalAlignment.Left => rect.Left,
                TextHorizontalAlignment.Right => rect.Right - width,
                _ => rect.MidX - width / 2,
            };
            canvas.DrawText(line, x, baseline, SKTextAlign.Left, font, paint);
            y += lineHeight;
        }

        canvas.Restore();
    }

    private static int FitCharacters(string text, SKFont font, float width)
    {
        int fit = 1;
        while (fit < text.Length && font.MeasureText(text[..(fit + 1)]) <= width)
        {
            fit++;
        }

        return fit;
    }
}
