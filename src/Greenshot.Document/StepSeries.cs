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
using System.Linq;
using System.Text;

namespace Greenshot.Document
{
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
    /// Numbers step labels: the label at place n (from 0) of Steps shows Start + n in Format.
    /// The number isn't stored on the label, so reordering Steps renumbers without changing the labels.
    /// </summary>
    public sealed record StepSeries(Guid Id, IReadOnlyList<Guid> Steps)
    {
        public int Start { get; init; } = 1;

        public StepFormat Format { get; init; } = StepFormat.Numbers;

        /// <summary>
        /// The shared style of the step labels of the series which have none of their own
        /// </summary>
        public Guid? StyleId { get; init; }

        /// <summary>
        /// The text of the step label with the id, null when it isn't in the series
        /// </summary>
        public string Label(Guid stepLabelId)
        {
            int index = Steps.ToList().IndexOf(stepLabelId);
            return index < 0 ? null : ToText(Start + index, Format);
        }

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
