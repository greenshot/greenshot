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
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// Handles robust JSON serialization, deserialization, and schema validation for capture recipes.
    /// </summary>
    public static class RecipeSerializer
    {
        public const string RecipeFileExtension = ".gsrecipe.json";
        public const string RecipeFileFilter = "Greenshot Recipe (*.gsrecipe.json)|*.gsrecipe.json|JSON Recipe (*.json)|*.json|All files (*.*)|*.*";

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = new List<JsonConverter>
            {
                new StringEnumConverter()
            }
        };

        private static readonly JsonSerializerSettings DeserializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Converters = new List<JsonConverter>
            {
                new StringEnumConverter()
            }
        };

        /// <summary>
        /// Serializes a CaptureRecipe into a formatted JSON string.
        /// </summary>
        public static string Serialize(CaptureRecipe recipe, bool indented = true)
        {
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            var settings = indented ? SerializerSettings : new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = new List<JsonConverter> { new StringEnumConverter() }
            };
            return JsonConvert.SerializeObject(recipe, settings);
        }

        /// <summary>
        /// Serializes multiple recipes into a formatted JSON array string.
        /// </summary>
        public static string SerializeList(IEnumerable<CaptureRecipe> recipes, bool indented = true)
        {
            if (recipes == null) throw new ArgumentNullException(nameof(recipes));
            var settings = indented ? SerializerSettings : new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Converters = new List<JsonConverter> { new StringEnumConverter() }
            };
            return JsonConvert.SerializeObject(recipes, settings);
        }

        /// <summary>
        /// Deserializes a CaptureRecipe from a JSON string, optionally validating against the schema contract.
        /// </summary>
        public static CaptureRecipe Deserialize(string json, bool validate = true)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("JSON content cannot be empty", nameof(json));

            CaptureRecipe recipe = JsonConvert.DeserializeObject<CaptureRecipe>(json.Trim(), DeserializerSettings);
            if (recipe == null)
            {
                throw new JsonException("Failed to deserialize CaptureRecipe from JSON.");
            }

            if (validate)
            {
                var validationResult = RecipeValidator.Validate(recipe);
                if (!validationResult.IsValid)
                {
                    throw new JsonException($"Recipe validation failed: {string.Join("; ", validationResult.Errors)}");
                }
            }

            return recipe;
        }

        /// <summary>
        /// Deserializes one or more recipes from a JSON string (accepts either a single recipe object or a JSON array).
        /// </summary>
        public static List<CaptureRecipe> DeserializeList(string json, bool validate = true)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("JSON content cannot be empty", nameof(json));

            string trimmed = json.Trim();
            var list = new List<CaptureRecipe>();

            if (trimmed.StartsWith("["))
            {
                var deserialized = JsonConvert.DeserializeObject<List<CaptureRecipe>>(trimmed, DeserializerSettings);
                if (deserialized != null)
                {
                    list.AddRange(deserialized);
                }
            }
            else
            {
                var single = Deserialize(trimmed, validate);
                if (single != null)
                {
                    list.Add(single);
                }
                return list;
            }

            if (validate)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var recipe = list[i];
                    var validationResult = RecipeValidator.Validate(recipe);
                    if (!validationResult.IsValid)
                    {
                        throw new JsonException($"Recipe at index {i} ('{recipe.Id}') validation failed: {string.Join("; ", validationResult.Errors)}");
                    }
                }
            }

            return list;
        }

        /// <summary>
        /// Deserializes a recipe from a file path and attaches the file path to the recipe.
        /// </summary>
        public static CaptureRecipe LoadFromFile(string filePath, bool validate = true)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException($"Recipe file not found: {filePath}", filePath);

            string json = File.ReadAllText(filePath);
            var recipe = Deserialize(json, validate);
            if (recipe != null)
            {
                recipe.FilePath = filePath;
            }
            return recipe;
        }

        /// <summary>
        /// Deserializes recipes from a file path (single or list) and attaches the file path to each recipe.
        /// </summary>
        public static List<CaptureRecipe> LoadListFromFile(string filePath, bool validate = true)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException($"Recipe file not found: {filePath}", filePath);

            string json = File.ReadAllText(filePath);
            var recipes = DeserializeList(json, validate);
            foreach (var recipe in recipes)
            {
                recipe.FilePath = filePath;
            }
            return recipes;
        }
    }
}
