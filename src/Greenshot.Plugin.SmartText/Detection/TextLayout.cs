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
using System.Linq;
using System.Text;
using Dapplo.Windows.Common.Extensions;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Interfaces.Plugin;

namespace Greenshot.Plugin.SmartText.Detection;

/// <summary>
/// The OCR lines of a capture as texts, with the bounds of every word: a match in the text of a line maps back to the words it
/// covers. Built once per capture, every detector runs on it.
/// </summary>
public sealed class TextLayout
{
    private TextLayout(IReadOnlyList<LayoutLine> lines, NativeRect bounds)
    {
        Lines = lines;
        Bounds = bounds;
    }

    /// <summary>The lines in the order OCR read them</summary>
    public IReadOnlyList<LayoutLine> Lines { get; }

    /// <summary>The capture (or, without its size, all lines): the area of a pattern is a part of it</summary>
    public NativeRect Bounds { get; }

    /// <summary>
    /// The layout of the OCR lines
    /// </summary>
    /// <param name="ocrLines">the lines with their words</param>
    /// <param name="captureBounds">the size of the capture, empty: the bounds of all lines</param>
    public static TextLayout From(IEnumerable<IOcrLineFeature> ocrLines, NativeRect captureBounds)
    {
        var lines = new List<LayoutLine>();
        foreach (var ocrLine in ocrLines ?? Enumerable.Empty<IOcrLineFeature>())
        {
            var line = LayoutLine.From(ocrLine, lines.Count);
            if (line != null)
            {
                lines.Add(line);
            }
        }

        var bounds = captureBounds;
        if (bounds.IsEmpty && lines.Count > 0)
        {
            bounds = lines.Select(l => l.Bounds).Aggregate((a, b) => a.Union(b));
        }

        return new TextLayout(lines, bounds);
    }

    /// <summary>
    /// The layout of plain text, one line per line of the text, without real bounds (for the test box of the settings and tests)
    /// </summary>
    public static TextLayout FromText(string text)
    {
        var lines = new List<IOcrLineFeature>();
        int y = 0;
        foreach (string lineText in (text ?? string.Empty).Replace("\r\n", "\n").Split('\n'))
        {
            var words = new List<OcrWordInfo>();
            int x = 0;
            foreach (string word in lineText.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            {
                words.Add(new OcrWordInfo { Text = word, Bounds = new NativeRect(x, y, word.Length * 10, 20) });
                x += (word.Length + 1) * 10;
            }

            if (words.Count > 0)
            {
                lines.Add(new Greenshot.Base.Core.DetectedOcrLine(words.Select(w => w.Bounds).Aggregate((a, b) => a.Union(b)), string.Join(" ", words.Select(w => w.Text)), words));
            }

            y += 30;
        }

        return From(lines, NativeRect.Empty);
    }
}

/// <summary>
/// One OCR line: its text, the words in it and where each one is
/// </summary>
public sealed class LayoutLine
{
    private readonly List<LayoutWord> _words;

    private LayoutLine(int index, string text, NativeRect bounds, List<LayoutWord> words)
    {
        Index = index;
        Text = text;
        Bounds = bounds;
        _words = words;
    }

    public int Index { get; }

    /// <summary>The words joined by a space (no space for a line OCR read without spaces, Chinese or Japanese)</summary>
    public string Text { get; }

    public NativeRect Bounds { get; }

    public IReadOnlyList<LayoutWord> Words => _words;

    internal static LayoutLine From(IOcrLineFeature ocrLine, int index)
    {
        if (ocrLine == null) return null;
        var words = (ocrLine.Words ?? new List<OcrWordInfo>()).Where(w => !string.IsNullOrEmpty(w?.Text)).ToList();
        var layoutWords = new List<LayoutWord>();
        var text = new StringBuilder();
        if (words.Count == 0)
        {
            if (string.IsNullOrWhiteSpace(ocrLine.Text)) return null;
            layoutWords.Add(new LayoutWord(0, ocrLine.Text.Length, ocrLine.Bounds));
            return new LayoutLine(index, ocrLine.Text, ocrLine.Bounds, layoutWords);
        }

        // Windows OCR puts a space between the words of a Chinese or Japanese line, its line text has none
        string separator = words.Count > 1 && ocrLine.Text != null && ocrLine.Text.IndexOf(' ') < 0 ? string.Empty : " ";
        foreach (var word in words)
        {
            if (text.Length > 0) text.Append(separator);
            layoutWords.Add(new LayoutWord(text.Length, word.Text.Length, word.Bounds));
            text.Append(word.Text);
        }

        var bounds = ocrLine.Bounds.IsEmpty ? words.Select(w => w.Bounds).Aggregate((a, b) => a.Union(b)) : ocrLine.Bounds;
        return new LayoutLine(index, text.ToString(), bounds, layoutWords);
    }

    /// <summary>
    /// The bounds of the words the characters are in
    /// </summary>
    public NativeRect BoundsOf(int start, int length)
    {
        int end = start + Math.Max(1, length);
        var covered = _words.Where(w => w.Start < end && w.Start + w.Length > start).Select(w => w.Bounds).Where(b => !b.IsEmpty).ToList();
        return covered.Count == 0 ? Bounds : covered.Aggregate((a, b) => a.Union(b));
    }

    public override string ToString() => Text;
}

/// <summary>
/// A word in the text of a line
/// </summary>
public sealed class LayoutWord
{
    public LayoutWord(int start, int length, NativeRect bounds)
    {
        Start = start;
        Length = length;
        Bounds = bounds;
    }

    public int Start { get; }

    public int Length { get; }

    public NativeRect Bounds { get; }
}
