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
using System.Text.RegularExpressions;

namespace Greenshot.Plugin.SmartText.Detection;

/// <summary>
/// The checks of a pattern before it is stored or used: the regular expressions can be read, the values are known ones, the web
/// address to open the value with can't send it to another server than the one written in it.
/// </summary>
public static class SmartTextPatternCheck
{
    /// <summary>The place of the value in <see cref="SmartTextPattern.OpenUrl"/></summary>
    public const string ValuePlaceholder = "{value}";

    /// <summary>
    /// What is wrong with the pattern, empty when it can be used
    /// </summary>
    /// <param name="pattern">the pattern</param>
    /// <param name="allowOpenUrl">false for a pattern inline in a recipe: only the user's own patterns open web addresses</param>
    public static IReadOnlyList<string> Problems(SmartTextPattern pattern, bool allowOpenUrl = true)
    {
        var problems = new List<string>();
        if (pattern == null)
        {
            problems.Add("There is no pattern.");
            return problems;
        }

        if (string.IsNullOrWhiteSpace(pattern.Name)) problems.Add("The pattern has no name.");
        if (string.IsNullOrWhiteSpace(pattern.Pattern))
        {
            problems.Add("The pattern has no regular expression.");
        }
        else if (RegexProblem(pattern.Pattern) is { } patternProblem)
        {
            problems.Add("The regular expression can't be used: " + patternProblem);
        }
        else if (!string.IsNullOrWhiteSpace(pattern.ValueGroup) && !HasGroup(pattern.Pattern, pattern.ValueGroup))
        {
            problems.Add($"The regular expression has no group '{pattern.ValueGroup}'.");
        }

        if (!string.IsNullOrWhiteSpace(pattern.Label) && RegexProblem(pattern.Label) is { } labelProblem)
        {
            problems.Add("The label can't be used: " + labelProblem);
        }

        if (!string.IsNullOrWhiteSpace(pattern.LabelPosition) && !Contains(SmartTextLabelPosition.All, pattern.LabelPosition))
        {
            problems.Add($"LabelPosition '{pattern.LabelPosition}' is not one of {string.Join(", ", SmartTextLabelPosition.All)}.");
        }

        if (!SmartTextNormalizers.IsKnown(pattern.Normalize))
        {
            problems.Add($"Normalize '{pattern.Normalize}' is not one of {string.Join(", ", SmartTextNormalize.All)}.");
        }

        if (!SmartTextValidators.IsKnown(pattern.Validate))
        {
            problems.Add($"Validate '{pattern.Validate}' is not one of {string.Join(", ", SmartTextValidate.All)}.");
        }

        if (pattern.MinDigits < 0 || pattern.MaxDigits < 0 || (pattern.MaxDigits > 0 && pattern.MinDigits > pattern.MaxDigits))
        {
            problems.Add("MinDigits and MaxDigits must be 0 or more, MinDigits not more than MaxDigits.");
        }

        var area = pattern.Area;
        if (area != null && (area.Left < 0 || area.Top < 0 || area.Width <= 0 || area.Height <= 0 || area.Left + area.Width > 1.0001 || area.Top + area.Height > 1.0001))
        {
            problems.Add("The area must be inside the capture: Left, Top, Width and Height are fractions from 0 to 1.");
        }

        if (!string.IsNullOrWhiteSpace(pattern.OpenUrl))
        {
            if (!allowOpenUrl)
            {
                problems.Add("OpenUrl is only for the own patterns of the settings, not for a pattern in a recipe.");
            }
            else if (OpenUrlProblem(pattern.OpenUrl) is { } openUrlProblem)
            {
                problems.Add(openUrlProblem);
            }
        }

        return problems;
    }

    /// <summary>
    /// Why the regular expression can't be used, null when it can
    /// </summary>
    public static string RegexProblem(string regex)
    {
        try
        {
            _ = new Regex(regex, RegexOptions.CultureInvariant, SmartTextDetector.MatchTimeout);
            return null;
        }
        catch (ArgumentException ex)
        {
            return ex.Message;
        }
    }

    private static bool HasGroup(string regex, string group)
    {
        var compiled = new Regex(regex, RegexOptions.CultureInvariant, SmartTextDetector.MatchTimeout);
        return int.TryParse(group, out int number) ? number >= 0 && number < compiled.GetGroupNumbers().Length : compiled.GroupNumberFromName(group) >= 0;
    }

    private static bool Contains(IEnumerable<string> values, string value)
    {
        foreach (string known in values)
        {
            if (string.Equals(known, value, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }

    /// <summary>
    /// Why the web address can't be used, null when it can: http or https, the server written out, {value} only after it
    /// </summary>
    public static string OpenUrlProblem(string openUrl)
    {
        string sample = openUrl.Replace(ValuePlaceholder, "x");
        if (!Uri.TryCreate(sample, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return "OpenUrl must be a web address starting with https:// or http://.";
        }

        if (openUrl.IndexOf(ValuePlaceholder, StringComparison.Ordinal) < 0)
        {
            return "OpenUrl must contain {value}, the place of the value.";
        }

        if (HostPart(openUrl).IndexOf('{') >= 0)
        {
            return "{value} can't be part of the server of OpenUrl, only of the path or the query.";
        }

        return null;
    }

    /// <summary>
    /// The web address of the value: {value} replaced by the value, encoded for a web address. Null when the template can't be used or
    /// the result goes to another server than the template.
    /// </summary>
    public static string RenderOpenUrl(string openUrl, string value)
    {
        if (string.IsNullOrWhiteSpace(openUrl) || OpenUrlProblem(openUrl) != null) return null;
        string url = openUrl.Replace(ValuePlaceholder, Uri.EscapeDataString(value ?? string.Empty));
        if (!Uri.TryCreate(url, UriKind.Absolute, out var rendered) || !Uri.TryCreate(openUrl.Replace(ValuePlaceholder, "x"), UriKind.Absolute, out var template))
        {
            return null;
        }

        return string.Equals(rendered.Host, template.Host, StringComparison.OrdinalIgnoreCase) && rendered.Scheme == template.Scheme && rendered.Port == template.Port
            ? rendered.AbsoluteUri
            : null;
    }

    /// <summary>
    /// The scheme and server of the address: up to the first / after ://
    /// </summary>
    private static string HostPart(string url)
    {
        int schemeEnd = url.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd < 0) return url;
        int pathStart = url.IndexOfAny(new[] { '/', '?', '#' }, schemeEnd + 3);
        return pathStart < 0 ? url : url.Substring(0, pathStart);
    }
}
