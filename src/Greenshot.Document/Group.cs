/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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
using System.Text;
using System.Text.Json;

namespace Greenshot.Document
{
    /// <summary>
    /// The group types the core knows, a plugin uses its own (e.g. "zxing.barcodes")
    /// </summary>
    public static class GroupTypes
    {
        /// <summary>
        /// Numbers its step labels in member order, with "start" and "format" in the properties
        /// </summary>
        public const string Steps = "steps";

        /// <summary>
        /// Only gives its members a style
        /// </summary>
        public const string Style = "style";

        /// <summary>
        /// Its members are selected, moved, copied and deleted together
        /// </summary>
        public const string Set = "set";
    }

    /// <summary>
    /// Elements which belong together. The group lists its members, an element can be in several groups (at most one
    /// steps group and one group with a style). A null member is a gap, e.g. a skipped step number.
    /// Properties are defined by the type and, like the extensions and groups of an unknown type, kept unchanged.
    /// </summary>
    public sealed record Group(Guid Id, string Type, IReadOnlyList<Guid?> Members)
    {
        public string Name { get; init; }

        /// <summary>
        /// The style of the members, below their own
        /// </summary>
        public Style Style { get; init; }

        public RawJson Properties { get; init; }

        public RawJson Extensions { get; init; }
    }

    public enum StepFormat
    {
        /// <summary>1, 2, 3</summary>
        Numbers,

        /// <summary>a, b, c, ..., z, aa</summary>
        LowerLetters,

        /// <summary>A, B, C, ..., Z, AA</summary>
        UpperLetters,

        /// <summary>i, ii, iii</summary>
        LowerRoman,

        /// <summary>I, II, III</summary>
        UpperRoman
    }

    /// <summary>
    /// The numbering of a steps group: the member at place n (from 0) shows Start + n in Format.
    /// </summary>
    public sealed record StepNumbering(int Start = 1, StepFormat Format = StepFormat.Numbers)
    {
        private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) } };

        /// <summary>
        /// The numbering in the properties of a steps group, missing values are the defaults
        /// </summary>
        public static StepNumbering Of(Group group) =>
            group.Properties == null ? new StepNumbering() : JsonSerializer.Deserialize<StepNumbering>(group.Properties.Json, Options) ?? new StepNumbering();

        /// <summary>
        /// The properties of a steps group with this numbering
        /// </summary>
        public RawJson ToProperties() => new(JsonSerializer.Serialize(this, Options));

        /// <summary>
        /// The text of the member at the index
        /// </summary>
        public string Label(int index) => ToText(Start + index, Format);

        /// <summary>
        /// The number in the format, letters and roman numerals only from 1 (roman up to 3999), otherwise the number
        /// </summary>
        public static string ToText(int number, StepFormat format) => format switch
        {
            StepFormat.LowerLetters when number > 0 => Letters(number).ToLowerInvariant(),
            StepFormat.UpperLetters when number > 0 => Letters(number),
            StepFormat.LowerRoman when number is > 0 and < 4000 => Roman(number).ToLowerInvariant(),
            StepFormat.UpperRoman when number is > 0 and < 4000 => Roman(number),
            _ => number.ToString()
        };

        private static string Letters(int number)
        {
            var letters = new StringBuilder();
            for (; number > 0; number = (number - 1) / 26)
            {
                letters.Insert(0, (char)('A' + (number - 1) % 26));
            }

            return letters.ToString();
        }

        private static readonly (int Value, string Numeral)[] Numerals =
        {
            (1000, "M"), (900, "CM"), (500, "D"), (400, "CD"), (100, "C"), (90, "XC"), (50, "L"), (40, "XL"), (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I")
        };

        private static string Roman(int number)
        {
            var roman = new StringBuilder();
            foreach (var (value, numeral) in Numerals)
            {
                for (; number >= value; number -= value)
                {
                    roman.Append(numeral);
                }
            }

            return roman.ToString();
        }
    }
}
