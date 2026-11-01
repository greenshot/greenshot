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
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using Greenshot.Base.Recipes.Contracts;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// The values of step parameters in the type the step declares (<see cref="ContractDataType"/>): a whole number (int) for Integer,
    /// a double for Decimal, a bool for Boolean, a text for the text types, a whole number for Window (or an expression); Object and Region take any value.
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
                case ContractDataType.Hotkey:
                    expected = "a text";
                    return value is string || value is Enum;
                case ContractDataType.Window:
                    // A window handle; the window itself is passed with an expression (e.g. ${Window})
                    expected = "a window handle (a whole number) or the variable of a window";
                    switch (value)
                    {
                        case int:
                            return true;
                        case long whole:
                            // Window handles have 32 significant bits, also in 64-bit Windows
                            if (whole >= int.MinValue && whole <= int.MaxValue) converted = (int)whole;
                            return true;
                        default:
                            return false;
                    }
                default:
                    // Object, Region: any value
                    return true;
            }
        }

        // The JSON types of parameters with a ValueType: the core ones, and those plugins registered
        private static readonly List<IJsonTypeInfoResolver> Resolvers = new List<IJsonTypeInfoResolver> { RecipeParameterJsonContext.Default };
        private static readonly object ResolversLock = new object();
        private static JsonSerializerOptions _readOptions;

        /// <summary>
        /// Adds the JSON types of a plugin, see <see cref="Pipeline.IStepRegistry.RegisterJsonTypes"/>.
        /// </summary>
        public static void AddJsonTypes(IJsonTypeInfoResolver resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            lock (ResolversLock)
            {
                if (Resolvers.Contains(resolver)) return;
                Resolvers.Add(resolver);
                _readOptions = null;
            }
        }

        /// <summary>
        /// Reading a value into its declared type is strict, like reading a recipe: the names exactly as declared, no unknown or
        /// duplicate properties, numbers, booleans and texts only as such.
        /// </summary>
        private static JsonSerializerOptions ReadOptions
        {
            get
            {
                lock (ResolversLock)
                {
                    return _readOptions ??= new JsonSerializerOptions
                    {
                        TypeInfoResolver = JsonTypeInfoResolver.Combine(Resolvers.ToArray()),
                        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
                        AllowDuplicateProperties = false,
                        // Values of open objects (Variables, annotation properties) as they are read from a recipe
                        Converters = { new Serialization.ObjectValueConverter() }
                    };
                }
            }
        }

        /// <summary>
        /// True when a registered JSON context has the type
        /// </summary>
        public static bool IsKnown(Type valueType) => valueType != null && ReadOptions.TryGetTypeInfo(valueType, out _);

        /// <summary>
        /// A parameter value (as read from the file, or set in code) in the declared type, false with what doesn't fit.
        /// </summary>
        public static bool TryRead(object value, Type valueType, out object typed, out string error)
        {
            typed = value;
            error = null;
            if (value == null || valueType == null || valueType.IsInstanceOfType(value)) return true;
            var options = ReadOptions;
            try
            {
                // The value as it is written in the file
                var json = JsonSerializer.SerializeToElement(value, RecipeSerializer.Options);
                typed = json.Deserialize(options.GetTypeInfo(valueType));
                return true;
            }
            catch (JsonException ex)
            {
                typed = value;
                error = Describe(ex, valueType, options);
                return false;
            }
        }

        private static readonly Regex UnmappedProperty = new Regex("The JSON property '(?<name>[^']*)' could not be mapped", RegexOptions.Compiled);
        private static readonly Regex MissingProperties = new Regex("missing required properties including: (?<names>'[^']*'(, '[^']*')*)", RegexOptions.Compiled);
        private static readonly Regex ItemIndex = new Regex(@"^\$\[(?<index>\d+)\]", RegexOptions.Compiled);

        /// <summary>
        /// What doesn't fit, in the words of the recipe file instead of the .NET types
        /// </summary>
        private static string Describe(JsonException ex, Type valueType, JsonSerializerOptions options)
        {
            string where = string.Empty;
            var index = ItemIndex.Match(ex.Path ?? string.Empty);
            if (index.Success) where = $"item {int.Parse(index.Groups["index"].Value, CultureInfo.InvariantCulture) + 1}: ";

            var itemType = valueType.IsGenericType && valueType.GetGenericArguments().Length == 1 ? valueType.GetGenericArguments()[0] : valueType;
            var names = options.TryGetTypeInfo(itemType, out var itemInfo) && itemInfo.Kind == JsonTypeInfoKind.Object
                ? itemInfo.Properties.Select(p => p.Name).ToList()
                : new List<string>();

            var unmapped = UnmappedProperty.Match(ex.Message);
            if (unmapped.Success)
            {
                string name = unmapped.Groups["name"].Value;
                string declared = names.FirstOrDefault(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
                return declared != null
                    ? $"{where}'{name}' is written '{declared}'."
                    : $"{where}'{name}' is not one of {string.Join(", ", names)}.";
            }
            var missing = MissingProperties.Match(ex.Message);
            if (missing.Success)
            {
                return $"{where}{missing.Groups["names"].Value} is missing.";
            }
            string expected = names.Count > 0 ? $"a list of objects with {string.Join(", ", names)}" : itemType == typeof(string) ? "a list of texts" : "of another type";
            return $"{where}the value doesn't fit, it must be {expected}.";
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
