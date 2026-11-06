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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using static Greenshot.Base.Expressions.ExpressionValues;

namespace Greenshot.Base.Expressions
{
    /// <summary>
    /// The functions of the expression language. All are pure: they never change anything and never touch the disk.
    /// </summary>
    internal static class BuiltInFunctions
    {
        /// <summary>A bad pattern can't hang a capture</summary>
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);
        private static readonly ConcurrentDictionary<string, Regex> RegexCache = new ConcurrentDictionary<string, Regex>(StringComparer.Ordinal);
        private const string RandomCharacters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        private static readonly Random RandomNumbers = new Random();

        private static string T(IReadOnlyList<object> args, int index) => index < args.Count ? ToText(args[index], null, CultureInfo.InvariantCulture) : string.Empty;
        private static int I(IReadOnlyList<object> args, int index, int fallback = 0) => index < args.Count && args[index] != null ? (int)Math.Round(ToNumber(args[index])) : fallback;

        public static IEnumerable<ExpressionFunction> Create()
        {
            // Text
            yield return new ExpressionFunction("left", 2, 2, "left(text, count)", "The first count characters", a =>
            {
                string t = T(a, 0);
                int n = Math.Max(0, I(a, 1));
                return t.Length <= n ? t : t.Substring(0, n);
            });
            yield return new ExpressionFunction("right", 2, 2, "right(text, count)", "The last count characters", a =>
            {
                string t = T(a, 0);
                int n = Math.Max(0, I(a, 1));
                return t.Length <= n ? t : t.Substring(t.Length - n);
            });
            yield return new ExpressionFunction("substring", 2, 3, "substring(text, start, length?)", "Part of the text from start (0 based), a negative start counts from the end", a =>
            {
                string t = T(a, 0);
                int start = I(a, 1);
                if (start < 0) start = Math.Max(0, t.Length + start);
                if (start >= t.Length) return string.Empty;
                int length = a.Count > 2 ? I(a, 2) : t.Length - start;
                if (length < 0) length = Math.Max(0, t.Length - start + length);
                return t.Substring(start, Math.Min(length, t.Length - start));
            });
            yield return new ExpressionFunction("replace", 3, 3, "replace(text, old, new)", "Replace every old by new", a =>
            {
                string old = T(a, 1);
                return old.Length == 0 ? T(a, 0) : T(a, 0).Replace(old, T(a, 2));
            });
            yield return new ExpressionFunction("lower", 1, 1, "lower(text)", "Lower case", a => T(a, 0).ToLower(CultureInfo.CurrentCulture));
            yield return new ExpressionFunction("upper", 1, 1, "upper(text)", "Upper case", a => T(a, 0).ToUpper(CultureInfo.CurrentCulture));
            yield return new ExpressionFunction("trim", 1, 1, "trim(text)", "Without spaces at the start and end", a => T(a, 0).Trim());
            yield return new ExpressionFunction("padLeft", 2, 3, "padLeft(text, width, character?)", "Fill up to width at the start (default a space)", a => T(a, 0).PadLeft(Math.Max(0, I(a, 1)), PadChar(a)));
            yield return new ExpressionFunction("padRight", 2, 3, "padRight(text, width, character?)", "Fill up to width at the end (default a space)", a => T(a, 0).PadRight(Math.Max(0, I(a, 1)), PadChar(a)));
            yield return new ExpressionFunction("length", 1, 1, "length(text)", "The number of characters", a => (long)T(a, 0).Length);
            yield return new ExpressionFunction("contains", 2, 2, "contains(text, part)", "Does the text contain part (ignoring case)", a => T(a, 0).IndexOf(T(a, 1), StringComparison.OrdinalIgnoreCase) >= 0);
            yield return new ExpressionFunction("startsWith", 2, 2, "startsWith(text, part)", "Does the text start with part (ignoring case)", a => T(a, 0).StartsWith(T(a, 1), StringComparison.OrdinalIgnoreCase));
            yield return new ExpressionFunction("endsWith", 2, 2, "endsWith(text, part)", "Does the text end with part (ignoring case)", a => T(a, 0).EndsWith(T(a, 1), StringComparison.OrdinalIgnoreCase));

            // Patterns
            yield return new ExpressionFunction("match", 2, 3, "match(text, pattern, group?)", "The first match of a .NET regular expression, or its group (number or name); empty when none", a =>
            {
                var m = GetRegex(T(a, 1)).Match(T(a, 0));
                if (!m.Success) return string.Empty;
                if (a.Count < 3) return m.Value;
                var group = IsNumber(a[2]) ? m.Groups[I(a, 2)] : m.Groups[T(a, 2)];
                return group.Success ? group.Value : string.Empty;
            });
            yield return new ExpressionFunction("replaceMatch", 3, 3, "replaceMatch(text, pattern, new)", "Replace every match of a .NET regular expression ($1 for a group)", a => GetRegex(T(a, 1)).Replace(T(a, 0), T(a, 2)));

            // Paths, on the value only
            yield return new ExpressionFunction("fileName", 1, 1, "fileName(path)", "The file name with extension", a => SafePath(T(a, 0), Path.GetFileName));
            yield return new ExpressionFunction("fileNameWithoutExtension", 1, 1, "fileNameWithoutExtension(path)", "The file name without extension", a => SafePath(T(a, 0), Path.GetFileNameWithoutExtension));
            yield return new ExpressionFunction("extension", 1, 1, "extension(path)", "The extension with its dot, e.g. .png", a => SafePath(T(a, 0), Path.GetExtension));
            yield return new ExpressionFunction("folder", 1, 1, "folder(path)", "The folder of a path", a => SafePath(T(a, 0), Path.GetDirectoryName));

            // Numbers
            yield return new ExpressionFunction("round", 1, 2, "round(number, digits?)", "Rounded, half away from zero", a => Normalize(Math.Round(ToNumber(a[0]), Math.Max(0, Math.Min(15, I(a, 1))), MidpointRounding.AwayFromZero)));
            yield return new ExpressionFunction("floor", 1, 1, "floor(number)", "Rounded down", a => Normalize(Math.Floor(ToNumber(a[0]))));
            yield return new ExpressionFunction("ceiling", 1, 1, "ceiling(number)", "Rounded up", a => Normalize(Math.Ceiling(ToNumber(a[0]))));
            yield return new ExpressionFunction("min", 2, 2, "min(a, b)", "The smaller number", a => Normalize(Math.Min(ToNumber(a[0]), ToNumber(a[1]))));
            yield return new ExpressionFunction("max", 2, 2, "max(a, b)", "The larger number", a => Normalize(Math.Max(ToNumber(a[0]), ToNumber(a[1]))));
            yield return new ExpressionFunction("abs", 1, 1, "abs(number)", "Without sign", a => Normalize(Math.Abs(ToNumber(a[0]))));

            // Date and time
            yield return new ExpressionFunction("addDays", 2, 2, "addDays(time, days)", "The time plus days (negative: earlier)", a => ToDate(a[0])?.AddDays(ToNumber(a[1])));
            yield return new ExpressionFunction("addHours", 2, 2, "addHours(time, hours)", "The time plus hours", a => ToDate(a[0])?.AddHours(ToNumber(a[1])));
            yield return new ExpressionFunction("addMinutes", 2, 2, "addMinutes(time, minutes)", "The time plus minutes", a => ToDate(a[0])?.AddMinutes(ToNumber(a[1])));
            yield return new ExpressionFunction("isoWeek", 1, 1, "isoWeek(time)", "The ISO 8601 week number (1-53)", a =>
            {
                var date = ToDate(a[0]);
                return date.HasValue ? (object)(long)IsoWeek(date.Value) : null;
            });
            yield return new ExpressionFunction("date", 2, 2, "date(text, format)", "Read a date from a text with a .NET format", a =>
                DateTime.TryParseExact(T(a, 0), T(a, 1), CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsed) ? (object)parsed : null);

            // Conversion
            yield return new ExpressionFunction("number", 1, 1, "number(value)", "The value as a number (0 when it isn't one)", a => Normalize(ToNumber(a[0])));
            yield return new ExpressionFunction("text", 1, 1, "text(value)", "The value as a text", a => ToText(a[0], null, CultureInfo.InvariantCulture));
            yield return new ExpressionFunction("bool", 1, 1, "bool(value)", "The value as true or false", a => ToBoolean(a[0]));
            yield return new ExpressionFunction("empty", 1, 1, "empty(value)", "Is the value missing or an empty text", a => IsMissing(a[0]));

            // Random
            yield return new ExpressionFunction("random", 1, 1, "random(count)", "count random letters and digits", a =>
            {
                int n = Math.Max(0, Math.Min(256, I(a, 0)));
                var sb = new StringBuilder(n);
                lock (RandomNumbers)
                {
                    for (int i = 0; i < n; i++) sb.Append(RandomCharacters[RandomNumbers.Next(RandomCharacters.Length)]);
                }
                return sb.ToString();
            });
            yield return new ExpressionFunction("guid", 0, 0, "guid()", "A new unique id", a => Guid.NewGuid().ToString("D"));
        }

        private static char PadChar(IReadOnlyList<object> args)
        {
            string c = T(args, 2);
            return c.Length > 0 ? c[0] : ' ';
        }

        private static string SafePath(string path, Func<string, string> part)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            try
            {
                return part(path) ?? string.Empty;
            }
            catch (ArgumentException)
            {
                return string.Empty;
            }
        }

        private static Regex GetRegex(string pattern) =>
            RegexCache.GetOrAdd(pattern, p => new Regex(p, RegexOptions.CultureInvariant, RegexTimeout));

        private static DateTime? ToDate(object value) => value switch
        {
            DateTime d => d,
            DateTimeOffset o => o.LocalDateTime,
            string s when DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsed) => parsed,
            _ => null
        };

        private static int IsoWeek(DateTime date)
        {
            // ISO 8601: the week with the year's first Thursday is week 1
            var day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(date);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday)
            {
                date = date.AddDays(3);
            }
            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }
    }
}
