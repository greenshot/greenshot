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

namespace Greenshot.Plugin.SmartText.Detection;

/// <summary>
/// What <see cref="SmartTextNormalize"/> does to a found value
/// </summary>
public static class SmartTextNormalizers
{
    private static readonly char[] Separators = { ' ', '-', '.', '/', ' ', '‐', '‑', '‒', '–' };
    private static readonly char[] UrlTrailing = { '.', ',', ';', ':', '!', '?', '\'', '"', '”', '’' };

    /// <summary>
    /// The normalized value, null when it isn't one (e.g. a date which doesn't exist)
    /// </summary>
    /// <param name="normalize">what to do, see <see cref="SmartTextNormalize"/></param>
    /// <param name="value">the value as found</param>
    /// <param name="culture">the culture for dates (the order of day and month), null: the current culture</param>
    public static string Normalize(string normalize, string value, CultureInfo culture = null)
    {
        if (value == null) return null;
        value = value.Trim();
        if (string.IsNullOrEmpty(normalize) || Is(normalize, SmartTextNormalize.None)) return value;
        if (Is(normalize, SmartTextNormalize.RemoveSeparators)) return RemoveSeparators(value);
        if (Is(normalize, SmartTextNormalize.Upper)) return value.ToUpperInvariant();
        if (Is(normalize, SmartTextNormalize.Lower)) return value.ToLowerInvariant();
        if (Is(normalize, SmartTextNormalize.UpperWithoutSeparators)) return RemoveSeparators(value).ToUpperInvariant();
        if (Is(normalize, SmartTextNormalize.Url)) return Url(value);
        if (Is(normalize, SmartTextNormalize.Email)) return Email(value);
        if (Is(normalize, SmartTextNormalize.Phone)) return Phone(value);
        if (Is(normalize, SmartTextNormalize.Date)) return Date(value, culture ?? CultureInfo.CurrentCulture);
        return value;
    }

    /// <summary>
    /// Is this the name of a normalization (ignoring case)
    /// </summary>
    public static bool IsKnown(string normalize) => string.IsNullOrEmpty(normalize) || SmartTextNormalize.All.Any(known => Is(normalize, known));

    private static bool Is(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    public static string RemoveSeparators(string value) => new string(value.Where(c => Array.IndexOf(Separators, c) < 0).ToArray());

    /// <summary>
    /// Punctuation after a web address belongs to the sentence; a closing bracket only belongs to it when it opened one
    /// </summary>
    public static string Url(string value)
    {
        string url = value;
        while (url.Length > 0)
        {
            char last = url[url.Length - 1];
            if (Array.IndexOf(UrlTrailing, last) >= 0)
            {
                url = url.Substring(0, url.Length - 1);
                continue;
            }

            if ((last == ')' && url.Count(c => c == '(') < url.Count(c => c == ')')) ||
                (last == ']' && url.Count(c => c == '[') < url.Count(c => c == ']')))
            {
                url = url.Substring(0, url.Length - 1);
                continue;
            }

            break;
        }

        if (url.Length == 0) return null;
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) && uri.Host.Contains('.')
            ? url
            : null;
    }

    /// <summary>
    /// The domain in lower case, the local part as it is (it may be case-sensitive)
    /// </summary>
    public static string Email(string value)
    {
        string email = value.TrimEnd('.', ',', ';', ':');
        int at = email.LastIndexOf('@');
        return at <= 0 || at == email.Length - 1 ? null : email.Substring(0, at) + "@" + email.Substring(at + 1).ToLowerInvariant();
    }

    /// <summary>
    /// Only the digits, + in front of an international number (also for 00), the (0) after a country code dropped:
    /// "+49 (0) 30 123-45" is +493012345, "(030) 123 45" is 03012345
    /// </summary>
    public static string Phone(string value)
    {
        string phone = value.Trim();
        bool international = phone.StartsWith("+", StringComparison.Ordinal);
        if (!international && phone.StartsWith("00", StringComparison.Ordinal))
        {
            international = true;
            phone = phone.Substring(2);
        }

        if (international)
        {
            // "+49 (0) 30": the trunk prefix in brackets isn't dialed from abroad
            phone = Regex.Replace(phone, @"\(\s*0\s*\)", string.Empty, RegexOptions.None, TimeSpan.FromMilliseconds(100));
        }

        string digits = SmartTextValidators.Digits(phone);
        return digits.Length == 0 ? null : (international ? "+" : string.Empty) + digits;
    }

    /// <summary>
    /// The date as yyyy-MM-dd: 2026-11-09, 09.11.2026, 9. November 2026, November 9, 2026, and 11/09/2026 in the order of the
    /// culture (month first when its short date starts with the month). Null when it is no date.
    /// </summary>
    public static string Date(string value, CultureInfo culture)
    {
        string text = Regex.Replace(value.Trim(), @"\s+", " ", RegexOptions.None, TimeSpan.FromMilliseconds(100)).TrimEnd('.', ',');
        bool monthFirst = culture.DateTimeFormat.ShortDatePattern.TrimStart().StartsWith("M", StringComparison.OrdinalIgnoreCase);
        var formats = new List<string> { "yyyy-MM-dd", "yyyy-M-d", "d.M.yyyy", "d.M.yy" };
        formats.AddRange(monthFirst ? new[] { "M/d/yyyy", "M/d/yy" } : new[] { "d/M/yyyy", "d/M/yy" });
        formats.AddRange(new[] { "d. MMMM yyyy", "d MMMM yyyy", "d. MMM yyyy", "d MMM yyyy", "MMMM d, yyyy", "MMMM d yyyy", "MMM d, yyyy", "MMM d yyyy" });
        foreach (var dateCulture in new[] { culture, CultureInfo.InvariantCulture })
        {
            if (DateTime.TryParseExact(text, formats.ToArray(), dateCulture, DateTimeStyles.AllowWhiteSpaces, out var date))
            {
                return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
        }

        return null;
    }

    /// <summary>
    /// The month names of the culture and of English, for the date pattern ("November|Nov|...")
    /// </summary>
    public static string MonthNames(CultureInfo culture)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var info in new[] { (culture ?? CultureInfo.CurrentCulture).DateTimeFormat, CultureInfo.InvariantCulture.DateTimeFormat })
        {
            foreach (string name in info.MonthNames.Concat(info.AbbreviatedMonthNames).Concat(info.MonthGenitiveNames))
            {
                string trimmed = name?.Trim().TrimEnd('.');
                if (!string.IsNullOrEmpty(trimmed)) names.Add(trimmed);
            }
        }

        var pattern = new StringBuilder();
        // Longest first, so "November" is taken before "Nov"
        foreach (string name in names.OrderByDescending(n => n.Length))
        {
            if (pattern.Length > 0) pattern.Append('|');
            pattern.Append(Regex.Escape(name));
        }

        return pattern.ToString();
    }
}
