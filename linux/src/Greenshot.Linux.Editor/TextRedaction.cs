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

using System.Text.RegularExpressions;
using Greenshot.Linux.Core.Ocr;
using Greenshot.Linux.Editor.Elements;
using SkiaSharp;

namespace Greenshot.Linux.Editor;

/// <summary>
/// "Redact text": find text on the capture with OCR and cover it, the TEXT_OBFUSCATION_FEATURE of Windows
/// (docs/vNext/TEXT_OBFUSCATION_FEATURE.md). The areas grow a little (10% to the sides, 20% up and down by default),
/// so the edges of the letters are covered too.
/// </summary>
public static class TextRedaction
{
    /// <summary>
    /// The areas of the words which contain a match; a match over more words covers all of them
    /// </summary>
    /// <exception cref="ArgumentException">An invalid regular expression</exception>
    public static IReadOnlyList<SKRect> FindMatches(IReadOnlyList<OcrLine> lines, string search, bool useRegex, bool caseSensitive,
        int paddingHorizontalPercent = 10, int paddingVerticalPercent = 20)
    {
        if (string.IsNullOrEmpty(search))
        {
            return [];
        }

        var options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
        var regex = new Regex(useRegex ? search : Regex.Escape(search), options, TimeSpan.FromSeconds(2));
        var result = new List<SKRect>();
        foreach (var line in lines)
        {
            // The line as text, with where each word starts and ends
            var spans = new List<(int Start, int End, OcrWord Word)>();
            var text = new System.Text.StringBuilder();
            foreach (var word in line.Words)
            {
                if (text.Length > 0)
                {
                    text.Append(' ');
                }

                spans.Add((text.Length, text.Length + word.Text.Length, word));
                text.Append(word.Text);
            }

            foreach (Match match in regex.Matches(text.ToString()))
            {
                if (match.Length == 0)
                {
                    continue;
                }

                var words = spans.Where(s => s.Start < match.Index + match.Length && s.End > match.Index).Select(s => s.Word.Bounds).ToList();
                if (words.Count == 0)
                {
                    continue;
                }

                var area = words.Aggregate((a, b) => SKRectI.Union(a, b));
                float padX = area.Width * paddingHorizontalPercent / 200f;
                float padY = area.Height * paddingVerticalPercent / 200f;
                result.Add(new SKRect(area.Left - padX, area.Top - padY, area.Right + padX, area.Bottom + padY));
            }
        }

        return result;
    }

    /// <summary>
    /// Cover the areas with obfuscate elements of the style, one undo step for all
    /// </summary>
    public static IReadOnlyList<ObfuscateElement> Apply(EditorController controller, IReadOnlyList<SKRect> areas)
    {
        if (areas.Count == 0)
        {
            return [];
        }

        var style = controller.ToolStyle(EditorTool.Obfuscate);
        var elements = areas.Select(area => new ObfuscateElement { Style = style.Clone(), Bounds = area }).ToList();
        controller.Surface.Checkpoint();
        foreach (var element in elements)
        {
            controller.Surface.Add(element, select: false);
        }

        controller.Tool = EditorTool.Select;
        controller.Surface.SelectMany(elements);
        return elements;
    }
}
