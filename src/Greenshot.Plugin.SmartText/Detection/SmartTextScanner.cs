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

namespace Greenshot.Plugin.SmartText.Detection;

/// <summary>
/// Runs the detectors on the text of a capture. Where two values cover the same characters one wins: an own pattern before a
/// built-in detector, a checked value (check digit, a real date) before an unchecked one, then the longer one: an IBAN is no
/// phone number too. The same value of the same detector is listed once.
/// </summary>
public static class SmartTextScanner
{
    /// <summary>
    /// The values found, in the order of the text (line by line)
    /// </summary>
    public static IReadOnlyList<SmartTextItem> Scan(TextLayout layout, IEnumerable<SmartTextDetector> detectors)
    {
        if (layout == null || detectors == null) return Array.Empty<SmartTextItem>();
        var active = detectors.Where(detector => detector != null).ToList();
        var found = active.SelectMany(detector => detector.Detect(layout)).ToList();
        found.AddRange(Links(layout, active));

        var accepted = new List<SmartTextItem>();
        foreach (var item in found
                     .OrderByDescending(item => item.IsOwnPattern)
                     .ThenByDescending(item => item.IsValidated)
                     .ThenByDescending(item => item.Length))
        {
            if (accepted.Any(other => other.LineIndex == item.LineIndex && other.Start < item.Start + item.Length && item.Start < other.Start + other.Length))
            {
                continue;
            }

            if (accepted.Any(other => other.DetectorId == item.DetectorId && string.Equals(other.Value, item.Value, StringComparison.Ordinal)))
            {
                continue;
            }

            accepted.Add(item);
        }

        return accepted.OrderBy(item => item.LineIndex).ThenBy(item => item.Start).ToList();
    }

    /// <summary>
    /// The links the application gave with their targets: a web address for the web address detector, mailto: for the e-mail detector,
    /// tel: for the phone detector, when they are on. The words of a link are those next to each other with the same target.
    /// </summary>
    private static IEnumerable<SmartTextItem> Links(TextLayout layout, IReadOnlyList<SmartTextDetector> detectors)
    {
        foreach (var line in layout.Lines)
        {
            var words = line.Words;
            for (int i = 0; i < words.Count; i++)
            {
                string target = words[i].LinkTarget;
                if (string.IsNullOrWhiteSpace(target)) continue;
                int last = i;
                while (last + 1 < words.Count && string.Equals(words[last + 1].LinkTarget, target, StringComparison.Ordinal)) last++;
                int start = words[i].Start, end = words[last].Start + words[last].Length;
                i = last;

                var (detectorId, value) = Classify(target.Trim());
                var detector = detectorId == null ? null : detectors.FirstOrDefault(d => !d.IsOwnPattern && d.Id == detectorId);
                var item = detector?.FromLink(layout, line, start, end - start, value);
                if (item != null) yield return item;
            }
        }
    }

    /// <summary>
    /// The detector and value of a link target, null for other links (e.g. javascript: or a file)
    /// </summary>
    internal static (string DetectorId, string Value) Classify(string target)
    {
        if (target.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || target.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return (BuiltInDetectors.Url, target);
        }
        if (target.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            // mailto:a@b.c?subject=... : the address
            string address = Uri.UnescapeDataString(target.Substring("mailto:".Length).Split('?')[0]).Split(',')[0].Trim();
            return address.Length == 0 ? (null, null) : (BuiltInDetectors.Email, address);
        }
        if (target.StartsWith("tel:", StringComparison.OrdinalIgnoreCase))
        {
            string number = Uri.UnescapeDataString(target.Substring("tel:".Length)).Trim();
            return number.Length == 0 ? (null, null) : (BuiltInDetectors.Phone, number);
        }
        return (null, null);
    }
}
