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
using System.Collections;
using System.Globalization;

namespace Greenshot.Base.Expressions
{
    /// <summary>
    /// Conversions and comparisons of expression values: texts, numbers (long, double), booleans, dates, null.
    /// </summary>
    public static class ExpressionValues
    {
        /// <summary>null or an empty text</summary>
        public static bool IsMissing(object value) => value == null || (value is string text && text.Length == 0);

        public static bool IsNumber(object value) =>
            value is byte || value is sbyte || value is short || value is ushort || value is int || value is uint ||
            value is long || value is ulong || value is float || value is double || value is decimal;

        public static bool TryNumber(object value, out double number)
        {
            switch (value)
            {
                case null:
                    number = 0;
                    return false;
                case bool b:
                    number = b ? 1 : 0;
                    return true;
                case string text:
                    return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number);
                default:
                    if (IsNumber(value))
                    {
                        number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                        return true;
                    }
                    number = 0;
                    return false;
            }
        }

        public static double ToNumber(object value) => TryNumber(value, out double number) ? number : 0;

        /// <summary>A whole number stays a long, so "${counter.global + 1}" formats as "42" and not "42.0"</summary>
        public static object Normalize(double number)
        {
            if (!double.IsNaN(number) && !double.IsInfinity(number) && Math.Abs(number) < 9.0e15 && Math.Floor(number) == number)
            {
                return (long)number;
            }
            return number;
        }

        public static bool ToBoolean(object value)
        {
            switch (value)
            {
                case null:
                    return false;
                case bool b:
                    return b;
                case string text:
                    if (bool.TryParse(text, out bool parsed)) return parsed;
                    return text.Length > 0;
                default:
                    if (IsNumber(value)) return ToNumber(value) != 0;
                    return true;
            }
        }

        public static string ToText(object value, string format, IFormatProvider culture)
        {
            switch (value)
            {
                case null:
                    return string.Empty;
                case string text:
                    return text;
                case bool b:
                    return b ? "true" : "false";
                case IFormattable formattable:
                    return formattable.ToString(string.IsNullOrEmpty(format) ? null : format, culture);
                case IEnumerable list:
                    var parts = new System.Collections.Generic.List<string>();
                    foreach (var item in list) parts.Add(ToText(item, format, culture));
                    return string.Join(", ", parts);
                default:
                    return value.ToString();
            }
        }

        public static bool AreEqual(object left, object right)
        {
            if (left == null || right == null) return IsMissing(left) && IsMissing(right);
            if (left is bool || right is bool) return ToBoolean(left) == ToBoolean(right);
            if ((IsNumber(left) || IsNumber(right)) && TryNumber(left, out double l) && TryNumber(right, out double r))
            {
                return Math.Abs(l - r) < 1e-9;
            }
            if (left is DateTime ld && right is DateTime rd) return ld == rd;
            return string.Equals(ToText(left, null, CultureInfo.InvariantCulture), ToText(right, null, CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
        }

        public static int Compare(object left, object right)
        {
            if (left is DateTime ld && right is DateTime rd) return ld.CompareTo(rd);
            if (TryNumber(left, out double l) && TryNumber(right, out double r)) return l.CompareTo(r);
            return string.Compare(ToText(left, null, CultureInfo.InvariantCulture), ToText(right, null, CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
        }
    }
}
