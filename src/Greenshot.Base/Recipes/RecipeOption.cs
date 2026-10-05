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
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Greenshot.Base.Recipes.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// A value of a recipe the user sets once in the settings (Settings > Recipes, switches also in the quick settings),
    /// e.g. whether a border is added and its width. The recipe file holds only this definition; the value the user picks
    /// is stored in greenshot.ini (see <see cref="RecipeOptionStore"/>), so changing it doesn't change the approved file.
    /// Nodes read it with ${option.key}.
    /// </summary>
    public class RecipeOption
    {
        /// <summary>
        /// Letters, digits and underscores, starting with a letter (used in ${option.key} and in greenshot.ini)
        /// </summary>
        public static readonly Regex KeyPattern = new Regex("^[A-Za-z][A-Za-z0-9_]*$", RegexOptions.Compiled);

        /// <summary>
        /// The types an option can have
        /// </summary>
        public static readonly IReadOnlyCollection<ContractDataType> SupportedTypes = new[]
        {
            ContractDataType.Boolean, ContractDataType.Integer, ContractDataType.Decimal, ContractDataType.String, ContractDataType.Enum, ContractDataType.Color
        };

        /// <summary>
        /// The types which can be shown in the quick settings of the tray menu
        /// </summary>
        public static readonly IReadOnlyCollection<ContractDataType> QuickSettingsTypes = new[] { ContractDataType.Boolean, ContractDataType.Enum };

        private static readonly Regex ColorPattern = new Regex("^#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$", RegexOptions.Compiled);

        /// <summary>
        /// The name of the option, unique in the recipe
        /// </summary>
        public string Key { get; set; }

        public ContractDataType Type { get; set; } = ContractDataType.Boolean;

        /// <summary>
        /// The value used until the user changes it
        /// </summary>
        [JsonProperty("default")]
        public object DefaultValue { get; set; }

        /// <summary>
        /// The text shown in the settings, the key when empty
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Shown as tooltip
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Smallest value (Integer, Decimal)
        /// </summary>
        public decimal? Min { get; set; }

        /// <summary>
        /// Largest value (Integer, Decimal)
        /// </summary>
        public decimal? Max { get; set; }

        /// <summary>
        /// The values of an Enum option
        /// </summary>
        public List<RecipeOptionChoice> Choices { get; set; }

        /// <summary>
        /// The key of a Boolean option of the same recipe: this option can only be changed while that one is on
        /// </summary>
        public string EnabledWhen { get; set; }

        /// <summary>
        /// Also show the option in the quick settings of the tray menu (Boolean and Enum only)
        /// </summary>
        public bool QuickSettings { get; set; }

        /// <summary>
        /// A hint how a String is meant, e.g. "dateTime" for a .NET date and time format, or <see cref="FormatTemplate"/>
        /// </summary>
        public string Format { get; set; }

        /// <summary>
        /// <see cref="Format"/> of a String option whose value is a template: ${...} in the value is evaluated where the
        /// option is used, e.g. "Captured ${now:yyyy-MM-dd}" (options of the recipe can't be used in it)
        /// </summary>
        public const string FormatTemplate = "template";

        [JsonIgnore]
        public bool IsTemplate => Type == ContractDataType.String && string.Equals(Format, FormatTemplate, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// The description for the settings, translated when it is a language key
        /// </summary>
        [JsonIgnore]
        public string DisplayDescription => RecipeText.Translate(Description);

        /// <summary>
        /// The text for the settings
        /// </summary>
        [JsonIgnore]
        public string DisplayLabel => string.IsNullOrWhiteSpace(Label) ? Key : RecipeText.Translate(Label);

        /// <summary>
        /// The default value as a value of the option type, or the neutral value of the type when there is no valid default
        /// </summary>
        public object GetDefaultValue()
        {
            if (DefaultValue != null && TryConvert(DefaultValue, out var value))
            {
                return value;
            }

            switch (Type)
            {
                case ContractDataType.Boolean:
                    return false;
                case ContractDataType.Integer:
                    return (int)Clamp(0m);
                case ContractDataType.Decimal:
                    return (double)Clamp(0m);
                case ContractDataType.Color:
                    return "#000000";
                case ContractDataType.Enum:
                    return Choices?.FirstOrDefault(c => c != null)?.Value ?? string.Empty;
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Converts a value (from the recipe, greenshot.ini or the settings) to the option type:
        /// bool, int, double or string. Numbers are limited to Min and Max.
        /// </summary>
        /// <returns>false when the value doesn't fit the type, a color or one of the choices</returns>
        public bool TryConvert(object raw, out object value)
        {
            value = null;
            if (raw is JValue jValue)
            {
                raw = jValue.Value;
            }
            if (raw == null)
            {
                return false;
            }

            string text = Convert.ToString(raw, CultureInfo.InvariantCulture)?.Trim();
            switch (Type)
            {
                case ContractDataType.Boolean:
                    if (raw is bool b)
                    {
                        value = b;
                        return true;
                    }
                    if (bool.TryParse(text, out b))
                    {
                        value = b;
                        return true;
                    }
                    return false;
                case ContractDataType.Integer:
                    if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var integer) && decimal.Truncate(integer) == integer)
                    {
                        value = (int)Clamp(Math.Max(int.MinValue, Math.Min(int.MaxValue, integer)));
                        return true;
                    }
                    return false;
                case ContractDataType.Decimal:
                    if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var number))
                    {
                        value = (double)Clamp(number);
                        return true;
                    }
                    return false;
                case ContractDataType.Color:
                    if (text != null && ColorPattern.IsMatch(text))
                    {
                        value = text.ToUpperInvariant();
                        return true;
                    }
                    return false;
                case ContractDataType.Enum:
                    var choice = Choices?.FirstOrDefault(c => c != null && string.Equals(c.Value, text, StringComparison.OrdinalIgnoreCase));
                    if (choice != null)
                    {
                        value = choice.Value;
                        return true;
                    }
                    return false;
                case ContractDataType.String:
                    value = text ?? string.Empty;
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// The value as it is written to greenshot.ini
        /// </summary>
        public string ToStorageString(object value)
        {
            if (!TryConvert(value, out var converted))
            {
                converted = GetDefaultValue();
            }

            return converted switch
            {
                bool b => b ? "True" : "False",
                double d => d.ToString("R", CultureInfo.InvariantCulture),
                _ => Convert.ToString(converted, CultureInfo.InvariantCulture)
            };
        }

        private decimal Clamp(decimal value)
        {
            if (Min.HasValue && value < Min.Value) return Min.Value;
            if (Max.HasValue && value > Max.Value) return Max.Value;
            return value;
        }

        public RecipeOption Clone()
        {
            var clone = (RecipeOption)MemberwiseClone();
            clone.Choices = Choices?.Select(c => c?.Clone()).ToList();
            return clone;
        }

        public override string ToString() => $"{Key} ({Type})";
    }

    /// <summary>
    /// One value of an Enum option
    /// </summary>
    public class RecipeOptionChoice
    {
        public string Value { get; set; }

        /// <summary>
        /// The text shown in the settings, the value when empty
        /// </summary>
        public string Label { get; set; }

        [JsonIgnore]
        public string DisplayLabel => string.IsNullOrWhiteSpace(Label) ? Value : RecipeText.Translate(Label);

        public RecipeOptionChoice Clone() => (RecipeOptionChoice)MemberwiseClone();

        public override string ToString() => DisplayLabel;
    }
}
