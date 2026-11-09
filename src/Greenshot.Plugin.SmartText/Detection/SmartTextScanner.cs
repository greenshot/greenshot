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
        var found = detectors.Where(detector => detector != null).SelectMany(detector => detector.Detect(layout)).ToList();

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
}
