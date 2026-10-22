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
using System.Globalization;
using Greenshot.Base.Recipes.Contracts;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// The values of step parameters in the type the step declares (<see cref="ContractDataType"/>): a whole number (int) for Integer,
    /// a double for Decimal, a bool for Boolean, a text for the text types; Object, Window and Region take any value.
    /// An expression ("${...}") stays a text, it is evaluated when the node runs.
    /// </summary>
    public static class RecipeParameterTypes
    {
        /// <summary>
        /// The value in the declared type, false (and what was expected) when it doesn't fit
        /// </summary>
        public static bool TryConvert(object value, ContractDataType type, out object converted, out string expected)
        {
            converted = value;
            expected = null;
            if (value == null || value is string text && text.IndexOf("${", StringComparison.Ordinal) >= 0)
            {
                return true;
            }

            switch (type)
            {
                case ContractDataType.Integer:
                    expected = "a whole number";
                    switch (value)
                    {
                        case int:
                            return true;
                        case long whole when whole >= int.MinValue && whole <= int.MaxValue:
                            converted = (int)whole;
                            return true;
                        default:
                            return false;
                    }
                case ContractDataType.Decimal:
                    expected = "a number";
                    switch (value)
                    {
                        case double:
                            return true;
                        case int or long or float or decimal:
                            converted = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                            return true;
                        default:
                            return false;
                    }
                case ContractDataType.Boolean:
                    expected = "true or false";
                    return value is bool;
                case ContractDataType.String:
                case ContractDataType.FilePath:
                case ContractDataType.DirectoryPath:
                case ContractDataType.Enum:
                case ContractDataType.Color:
                    expected = "a text";
                    return value is string || value is Enum;
                default:
                    // Object, Window, Region: any value
                    return true;
            }
        }

        /// <summary>
        /// The text of an input field (e.g. in the recipe editor) in the declared type: "5" is 5 for Integer.
        /// A text which isn't a value of the type stays a text, the validator reports it.
        /// </summary>
        public static object FromText(string text, ContractDataType type)
        {
            if (text == null || text.IndexOf("${", StringComparison.Ordinal) >= 0) return text;
            string trimmed = text.Trim();
            switch (type)
            {
                case ContractDataType.Integer when int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out int whole):
                    return whole;
                case ContractDataType.Decimal when double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out double number):
                    return number;
                case ContractDataType.Boolean when bool.TryParse(trimmed, out bool flag):
                    return flag;
                default:
                    return text;
            }
        }
    }
}
