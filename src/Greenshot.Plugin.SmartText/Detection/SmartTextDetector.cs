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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Dapplo.Windows.Common.Structs;
using log4net;

namespace Greenshot.Plugin.SmartText.Detection;

/// <summary>
/// A pattern ready to run on a <see cref="TextLayout"/>: its regular expressions compiled once, with a time limit.
/// </summary>
public sealed class SmartTextDetector
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(SmartTextDetector));

    /// <summary>
    /// The time one regular expression may take on one line: a pattern which takes longer is stopped and skipped for the capture
    /// </summary>
    private readonly bool _hasValueGroup;

    public static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(250);

    private readonly Regex _pattern;
    private readonly Regex _label;
    private readonly SmartTextPattern _definition;
    private readonly CultureInfo _culture;

    /// <param name="id">the id of a built-in detector, the name of an own pattern</param>
    /// <param name="name">the name shown to the user</param>
    /// <param name="definition">what it finds</param>
    /// <param name="isOwnPattern">a pattern of the user or of the recipe: it comes before the built-in detectors</param>
    /// <param name="culture">the culture of dates, null: the current culture</param>
    /// <exception cref="ArgumentException">a regular expression is invalid, see <see cref="SmartTextPatternCheck"/></exception>
    public SmartTextDetector(string id, string name, SmartTextPattern definition, bool isOwnPattern, CultureInfo culture = null)
    {
        _definition = definition ?? throw new ArgumentNullException(nameof(definition));
        if (string.IsNullOrWhiteSpace(definition.Pattern)) throw new ArgumentException("The pattern is empty", nameof(definition));
        Id = id ?? definition.Name;
        Name = name ?? definition.Name ?? id;
        IsOwnPattern = isOwnPattern;
        _culture = culture ?? CultureInfo.CurrentCulture;
        var options = RegexOptions.CultureInvariant | (definition.MatchCase ? RegexOptions.None : RegexOptions.IgnoreCase);
        _pattern = new Regex(definition.Pattern, options, MatchTimeout);
        _label = string.IsNullOrWhiteSpace(definition.Label) ? null : new Regex(definition.Label, options, MatchTimeout);
        _hasValueGroup = _pattern.GroupNumberFromName(SmartTextPattern.ValueGroupName) >= 0;
    }

    public string Id { get; }

    public string Name { get; }

    public bool IsOwnPattern { get; }

    public SmartTextPattern Definition => _definition;

    /// <summary>
    /// What the detector finds in the layout; nothing when a regular expression takes too long
    /// </summary>
    public IReadOnlyList<SmartTextItem> Detect(TextLayout layout)
    {
        var items = new List<SmartTextItem>();
        if (layout == null) return items;
        try
        {
            if (_label == null)
            {
                foreach (var line in layout.Lines)
                {
                    foreach (var candidate in Candidates(layout, line, 0))
                    {
                        items.Add(candidate);
                    }
                }
            }
            else
            {
                items.AddRange(NextToLabels(layout));
            }
        }
        catch (RegexMatchTimeoutException)
        {
            // The text of the capture is never logged, it can be personal
            Log.WarnFormat("The Smart text pattern {0} took longer than {1} ms, it is skipped for this capture", Id, MatchTimeout.TotalMilliseconds);
            items.Clear();
        }

        return items;
    }

    /// <summary>
    /// The values of the line from the character on, which pass the checks and are in the area
    /// </summary>
    private IEnumerable<SmartTextItem> Candidates(TextLayout layout, LayoutLine line, int from)
    {
        string text = _definition.DigitLookAlikes ? FixDigitLookAlikes(line.Text) : line.Text;
        for (var match = _pattern.Match(text, Math.Min(from, text.Length)); match.Success; match = match.NextMatch())
        {
            var group = ValueGroup(match);
            if (group == null || group.Length == 0) continue;
            int start = group.Index;
            int length = group.Length;
            string value = group.Value;
            string readText = line.Text.Substring(start, length);

            // A web address OCR broke at the end of the line goes on in the next line
            if (SmartTextValidators.Is(_definition.Normalize, SmartTextNormalize.Url) && start + length == text.Length && "/-?=&_".IndexOf(value[value.Length - 1]) >= 0)
            {
                value += Continuation(layout, line);
            }

            var item = CreateItem(layout, line, start, length, value, readText);
            if (item != null)
            {
                yield return item;
            }
        }
    }

    /// <summary>
    /// The value: the group named "value" when the pattern has one (e.g. "Order (?&lt;value&gt;\d+)"), else the whole match
    /// </summary>
    private Group ValueGroup(Match match)
    {
        if (!_hasValueGroup) return match;
        var group = match.Groups[SmartTextPattern.ValueGroupName];
        return group.Success ? group : null;
    }

    private SmartTextItem CreateItem(TextLayout layout, LayoutLine line, int start, int length, string value, string readText)
    {
        string normalized = SmartTextNormalizers.Normalize(_definition.Normalize, value, _culture);
        if (string.IsNullOrEmpty(normalized)) return null;

        int digits = SmartTextValidators.Digits(normalized).Length;
        if ((_definition.MinDigits > 0 && digits < _definition.MinDigits) || (_definition.MaxDigits > 0 && digits > _definition.MaxDigits)) return null;
        if (!SmartTextValidators.IsValid(_definition.Validate, normalized)) return null;

        var box = line.BoxOf(start, length);
        if (!IsInArea(layout, box.Bounds)) return null;

        bool validated = !string.IsNullOrEmpty(_definition.Validate) && !SmartTextValidators.Is(_definition.Validate, SmartTextValidate.None)
                         || SmartTextValidators.Is(_definition.Normalize, SmartTextNormalize.Date);
        var (action, target) = ActionOf(normalized);
        return new SmartTextItem(Id, Name, normalized, readText, box, line.Index, start, length, validated, action, target, IsOwnPattern);
    }

    private (SmartTextAction, string) ActionOf(string value)
    {
        if (!string.IsNullOrWhiteSpace(_definition.OpenUrl))
        {
            string url = SmartTextPatternCheck.RenderOpenUrl(_definition.OpenUrl, value);
            return url == null ? (SmartTextAction.None, null) : (SmartTextAction.OpenUrl, url);
        }

        if (SmartTextValidators.Is(_definition.Normalize, SmartTextNormalize.Url)) return (SmartTextAction.Open, value);
        if (SmartTextValidators.Is(_definition.Normalize, SmartTextNormalize.Email)) return (SmartTextAction.Mail, "mailto:" + value);
        if (SmartTextValidators.Is(_definition.Normalize, SmartTextNormalize.Phone)) return (SmartTextAction.Call, "tel:" + value);
        return (SmartTextAction.None, null);
    }

    /// <summary>
    /// The center of the value is in the area of the pattern
    /// </summary>
    private bool IsInArea(TextLayout layout, NativeRect bounds)
    {
        var area = _definition.Area;
        if (area == null || layout.Bounds.IsEmpty) return true;
        double x = (bounds.Left + bounds.Width / 2.0 - layout.Bounds.Left) / Math.Max(1, layout.Bounds.Width);
        double y = (bounds.Top + bounds.Height / 2.0 - layout.Bounds.Top) / Math.Max(1, layout.Bounds.Height);
        return x >= area.Left && x <= area.Left + area.Width && y >= area.Top && y <= area.Top + area.Height;
    }

    /// <summary>
    /// The first word of the next line, when the web address was broken because the line was full: the next line starts about
    /// where the line starts, the line reaches as far right as the lines around it, and the word can be a part of a web address.
    /// A list of links (each ending with /) followed by a bullet or the next item is never joined.
    /// </summary>
    private static string Continuation(TextLayout layout, LayoutLine line)
    {
        if (line.Index + 1 >= layout.Lines.Count) return string.Empty;
        var next = layout.Lines[line.Index + 1];
        int height = Math.Max(1, line.Bounds.Height);
        bool below = next.Bounds.Top >= line.Bounds.Top + height / 2 && next.Bounds.Top <= line.Bounds.Bottom + height;
        bool aligned = Math.Abs(next.Bounds.Left - line.Bounds.Left) <= 2 * height;
        if (!below || !aligned || next.Words.Count == 0) return string.Empty;

        // Text breaks at the end of a full line: a line which ends before its neighbours wasn't broken
        int right = next.Bounds.Right;
        if (line.Index > 0) right = Math.Max(right, layout.Lines[line.Index - 1].Bounds.Right);
        if (line.Bounds.Right < right - 2 * height) return string.Empty;

        var first = next.Words[0];
        string word = next.Text.Substring(first.Start, first.Length);
        return IsUrlPart(word) && word.IndexOf("://", StringComparison.Ordinal) < 0 ? word : string.Empty;
    }

    /// <summary>
    /// Only the characters of a web address (RFC 3986), starting with a letter or digit: not a bullet, a dash of a list or a word with an umlaut
    /// </summary>
    private static bool IsUrlPart(string word) =>
        word.Length > 0 && IsAsciiLetterOrDigit(word[0]) && word.All(c => IsAsciiLetterOrDigit(c) || "-._~:/?#[]@!$&'()*+,;=%".IndexOf(c) >= 0);

    private static bool IsAsciiLetterOrDigit(char c) => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9';

    /// <summary>
    /// For every label: the value right of it (in its line, or in the next line on the same row), else below it
    /// </summary>
    private IEnumerable<SmartTextItem> NextToLabels(TextLayout layout)
    {
        string position = string.IsNullOrWhiteSpace(_definition.LabelPosition) ? SmartTextLabelPosition.RightOrBelow : _definition.LabelPosition;
        bool right = SmartTextValidators.Is(position, SmartTextLabelPosition.Right) || SmartTextValidators.Is(position, SmartTextLabelPosition.RightOrBelow);
        bool below = SmartTextValidators.Is(position, SmartTextLabelPosition.Below) || SmartTextValidators.Is(position, SmartTextLabelPosition.RightOrBelow);
        bool anywhere = SmartTextValidators.Is(position, SmartTextLabelPosition.Anywhere);

        if (anywhere)
        {
            // The label only has to be in the capture
            if (!layout.Lines.Any(line => _label.IsMatch(line.Text))) yield break;
            foreach (var line in layout.Lines)
            {
                foreach (var candidate in Candidates(layout, line, 0))
                {
                    yield return candidate;
                }
            }

            yield break;
        }

        foreach (var line in layout.Lines)
        {
            for (var label = _label.Match(line.Text); label.Success; label = label.NextMatch())
            {
                if (label.Length == 0) continue;
                var labelBounds = line.BoundsOf(label.Index, label.Length);
                SmartTextItem found = null;
                if (right)
                {
                    found = Candidates(layout, line, label.Index + label.Length).FirstOrDefault()
                            ?? RightOf(layout, line, labelBounds);
                }

                if (found == null && below)
                {
                    found = Below(layout, line, labelBounds);
                }

                if (found != null)
                {
                    yield return found;
                }
            }
        }
    }

    /// <summary>
    /// The first value in the nearest line to the right of the label on the same row (a form field next to its label)
    /// </summary>
    private SmartTextItem RightOf(TextLayout layout, LayoutLine labelLine, NativeRect label)
    {
        int height = Math.Max(1, label.Height);
        return layout.Lines
            .Where(line => line.Index != labelLine.Index && line.Bounds.Left >= label.Right - height / 2 && Overlap(line.Bounds.Top, line.Bounds.Bottom, label.Top, label.Bottom) >= Math.Min(height, line.Bounds.Height) / 2)
            .OrderBy(line => line.Bounds.Left - label.Right)
            .Select(line => Candidates(layout, line, 0).FirstOrDefault())
            .FirstOrDefault(item => item != null);
    }

    /// <summary>
    /// The value nearest to the label in the nearest line below it which overlaps the label horizontally (a field under its label, a table column)
    /// </summary>
    private SmartTextItem Below(TextLayout layout, LayoutLine labelLine, NativeRect label)
    {
        int height = Math.Max(1, label.Height);
        // From the left of the label to the right of it by its width: a value under a short label can be wider
        int left = label.Left - height;
        int right = label.Right + Math.Max(label.Width, 4 * height);
        foreach (var line in layout.Lines
                     .Where(line => line.Index != labelLine.Index && line.Bounds.Top >= label.Bottom - height / 2 && line.Bounds.Top <= label.Bottom + 4 * height &&
                                    Overlap(line.Bounds.Left, line.Bounds.Right, left, right) > 0)
                     .OrderBy(line => line.Bounds.Top))
        {
            var item = Candidates(layout, line, 0)
                .Where(candidate => Overlap(candidate.Bounds.Left, candidate.Bounds.Right, left, right) > 0)
                .OrderBy(candidate => Math.Abs(candidate.Bounds.Left - label.Left))
                .FirstOrDefault();
            if (item != null) return item;
        }

        return null;
    }

    private static int Overlap(int start1, int end1, int start2, int end2) => Math.Max(0, Math.Min(end1, end2) - Math.Max(start1, start2));

    /// <summary>
    /// In a word which is mostly digits, the letters OCR confuses with digits as those digits; the text keeps its length, so the
    /// characters still map to the words
    /// </summary>
    public static string FixDigitLookAlikes(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var result = new StringBuilder(text);
        int i = 0;
        while (i < text.Length)
        {
            if (!char.IsLetterOrDigit(text[i]))
            {
                i++;
                continue;
            }

            int start = i;
            while (i < text.Length && char.IsLetterOrDigit(text[i])) i++;
            int digits = 0;
            int lookAlikes = 0;
            for (int j = start; j < i; j++)
            {
                if (char.IsDigit(text[j])) digits++;
                else if (DigitOf(text[j]) != text[j]) lookAlikes++;
            }

            // Mostly digits: "4O11" is 4011, "BOX" stays a word
            if (digits > 0 && digits + lookAlikes == i - start && digits >= lookAlikes)
            {
                for (int j = start; j < i; j++)
                {
                    result[j] = DigitOf(text[j]);
                }
            }
        }

        return result.ToString();
    }

    private static char DigitOf(char c) => c switch
    {
        'O' or 'o' or 'D' or 'Q' => '0',
        'I' or 'l' or 'i' => '1',
        'Z' or 'z' => '2',
        'S' or 's' => '5',
        'B' => '8',
        'G' => '6',
        _ => c
    };

    public override string ToString() => $"{Id}: {_definition.Pattern}";
}
