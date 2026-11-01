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
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Greenshot.Base.Serialization;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// Handles robust JSON serialization, deserialization, and schema validation for capture recipes.
    /// </summary>
    public static class RecipeSerializer
    {
        public const string RecipeFileExtension = ".gsrecipe.json";
        public const string RecipeFileFilter = "Greenshot Recipe (*.gsrecipe.json)|*.gsrecipe.json|JSON Recipe (*.json)|*.json|All files (*.*)|*.*";

        /// <summary>
        /// The settings of recipe files: the properties camelCase, parameter keys as the step declares them, enums as names, no nulls.
        /// Reading is strict, a mistake is an error and not guessed: the names exactly as written, no unknown or duplicate properties,
        /// numbers, booleans and texts only as such. Comments and trailing commas are allowed.
        /// </summary>
        public static readonly JsonSerializerOptions Options = CreateOptions(true);

        private static readonly JsonSerializerOptions CompactOptions = CreateOptions(false);

        private static readonly JsonDocumentOptions DocumentOptions = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            AllowDuplicateProperties = false
        };

        private static JsonSerializerOptions CreateOptions(bool indented)
        {
            return new JsonSerializerOptions
            {
                WriteIndented = indented,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                AllowDuplicateProperties = false,
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
                // Fills the collections a property already has, so the case insensitive dictionaries stay case insensitive
                PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                Converters =
                {
                    new JsonStringEnumConverter(null, false),
                    new RoundTripDoubleConverter(),
                    new ObjectValueConverter()
                }
            };
        }

        /// <summary>
        /// For <see cref="ConvertValue{T}"/>: the objects inside a parameter value (set in code or by the recipe editor as dictionaries)
        /// are matched by name in any casing, other names are skipped.
        /// </summary>
        private static readonly JsonSerializerOptions ConvertOptions = new JsonSerializerOptions(Options)
        {
            PropertyNameCaseInsensitive = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip
        };

        /// <summary>
        /// Converts a parameter value (as read from a file, or set in code) to another type, e.g. a list of dictionaries to a list of
        /// <see cref="PromptChoice"/>, by writing it as JSON and reading that back.
        /// </summary>
        public static T ConvertValue<T>(object value)
        {
            if (value is T typed)
            {
                return typed;
            }
            return JsonSerializer.SerializeToElement(value, ConvertOptions).Deserialize<T>(ConvertOptions);
        }

        /// <summary>
        /// A parameter value as plain values (dictionaries, lists, texts, numbers and booleans), as it would be read from a file
        /// </summary>
        public static object ToPlainValue(object value) =>
            value is null or string or bool or long or double ? value : JsonSerializer.SerializeToElement(value, ConvertOptions).Deserialize<object>(ConvertOptions);

        /// <summary>
        /// Serializes a CaptureRecipe into a formatted JSON string.
        /// </summary>
        public static string Serialize(CaptureRecipe recipe, bool indented = true)
        {
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            return JsonSerializer.Serialize(recipe, indented ? Options : CompactOptions);
        }

        /// <summary>
        /// Serializes multiple recipes into a formatted JSON array string.
        /// </summary>
        public static string SerializeList(IEnumerable<CaptureRecipe> recipes, bool indented = true)
        {
            if (recipes == null) throw new ArgumentNullException(nameof(recipes));
            return JsonSerializer.Serialize(recipes, indented ? Options : CompactOptions);
        }

        /// <summary>
        /// The "kind" of a definition in a file: "recipe" (also when it is missing) or "extension"
        /// </summary>
        public static string ReadKind(JsonElement definition)
        {
            if (definition.ValueKind != JsonValueKind.Object || !definition.TryGetProperty("kind", out var kind) || kind.ValueKind == JsonValueKind.Null)
            {
                return FlowDefinition.KindRecipe;
            }
            if (kind.ValueKind != JsonValueKind.String)
            {
                throw new JsonException("'kind' must be \"recipe\" or \"extension\".");
            }
            string text = kind.GetString()?.Trim();
            return string.IsNullOrEmpty(text) ? FlowDefinition.KindRecipe : text;
        }

        /// <summary>
        /// Parses the text of a recipe file: comments and trailing commas are allowed, the same property twice is an error
        /// </summary>
        public static JsonElement Parse(string json)
        {
            using var document = JsonDocument.Parse(json, DocumentOptions);
            return document.RootElement.Clone();
        }

        private static JsonElement ParseObject(string json)
        {
            var definition = Parse(json);
            if (definition.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException("A recipe or an extension must be a JSON object.");
            }
            CheckFormat(definition);
            return WithoutRuntimeProperties(definition);
        }

        /// <summary>
        /// What Greenshot knows about a loaded recipe, which earlier betas also wrote into the file. Greenshot sets these itself,
        /// so they are skipped when read and never written.
        /// </summary>
        private static readonly string[] RuntimeProperties = { "isBuiltIn", "isOverridden", "isEnabled", "filePath" };

        /// <summary>
        /// The definition without the <see cref="RuntimeProperties"/>, so a file an earlier beta saved still loads
        /// </summary>
        private static JsonElement WithoutRuntimeProperties(JsonElement definition)
        {
            if (!RuntimeProperties.Any(name => definition.TryGetProperty(name, out _)))
            {
                return definition;
            }
            var node = JsonObject.Create(definition);
            foreach (var name in RuntimeProperties)
            {
                node.Remove(name);
            }
            using var document = JsonDocument.Parse(node.ToJsonString());
            return document.RootElement.Clone();
        }

        /// <summary>
        /// A file of an earlier beta (transitions per node, conditional and error transitions apart, a condition in "enabled",
        /// error routes on the node)
        /// gets an error which says what changed, instead of the error of the first property which doesn't fit.
        /// </summary>
        private static void CheckFormat(JsonElement definition)
        {
            const string earlierFormat = "This recipe uses the format of an earlier Greenshot beta: ";
            if (definition.TryGetProperty("flow", out var flow) && flow.ValueKind == JsonValueKind.Object)
            {
                if (flow.TryGetProperty("transitions", out var transitions) && transitions.ValueKind == JsonValueKind.Object ||
                    flow.TryGetProperty("conditionalTransitions", out _) || flow.TryGetProperty("errorTransitions", out _))
                {
                    throw new JsonException(earlierFormat + "\"transitions\" is now one list for all transitions, " +
                                            "e.g. [ { \"from\": \"a\", \"to\": \"b\" }, { \"from\": \"check\", \"to\": \"c\", \"branch\": \"yes\" }, { \"from\": \"*\", \"to\": \"d\", \"onError\": \"*\" } ].");
                }
            }
            if (definition.TryGetProperty("nodes", out var nodes) && nodes.ValueKind == JsonValueKind.Array)
            {
                foreach (var node in nodes.EnumerateArray())
                {
                    if (node.ValueKind == JsonValueKind.Object && node.TryGetProperty("enabled", out var enabled) && enabled.ValueKind == JsonValueKind.String)
                    {
                        throw new JsonException(earlierFormat + "\"enabled\" is now true or false, an expression like \"${option.key}\" goes into \"when\".");
                    }
                    if (node.ValueKind == JsonValueKind.Object && (node.TryGetProperty("onErrorNodeId", out _) || node.TryGetProperty("onErrorRecipeId", out _)))
                    {
                        throw new JsonException(earlierFormat + "\"onErrorNodeId\" and \"onErrorRecipeId\" are now transitions, " +
                                                "e.g. { \"from\": \"upload\", \"onError\": \"*\", \"to\": \"save\" } or { \"from\": \"upload\", \"onError\": \"*\", \"recipe\": \"recipe_id\" }.");
                    }
                }
            }
        }

        /// <summary>
        /// Serializes a recipe or an extension into a formatted JSON string.
        /// </summary>
        public static string Serialize(FlowDefinition definition, bool indented = true)
        {
            if (definition is CaptureRecipe recipe) return Serialize(recipe, indented);
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            return JsonSerializer.Serialize(definition, definition.GetType(), indented ? Options : CompactOptions);
        }

        /// <summary>
        /// Deserializes a recipe extension ("kind": "extension"), optionally validating it.
        /// </summary>
        public static RecipeExtension DeserializeExtension(string json, bool validate = true)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("JSON content cannot be empty", nameof(json));

            var definition = ParseObject(json.Trim());
            string kind = ReadKind(definition);
            if (kind != FlowDefinition.KindExtension)
            {
                throw new JsonException($"Expected an extension (\"kind\": \"extension\"), but the kind is '{kind}'.");
            }
            if (definition.TryGetProperty("triggers", out _))
            {
                throw new JsonException("An extension can't have triggers; it runs in the recipes it extends.");
            }

            var extension = definition.Deserialize<RecipeExtension>(Options);
            if (extension == null)
            {
                throw new JsonException("Failed to deserialize RecipeExtension from JSON.");
            }

            if (validate)
            {
                var validationResult = RecipeValidator.Validate(extension);
                if (!validationResult.IsValid)
                {
                    throw new JsonException($"Extension validation failed: {string.Join("; ", validationResult.Errors)}");
                }
            }

            return extension;
        }

        /// <summary>
        /// Deserializes a CaptureRecipe from a JSON string, optionally validating against the schema contract.
        /// </summary>
        public static CaptureRecipe Deserialize(string json, bool validate = true)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("JSON content cannot be empty", nameof(json));

            var definition = ParseObject(json.Trim());
            string kind = ReadKind(definition);
            if (kind != FlowDefinition.KindRecipe)
            {
                throw new JsonException(kind == FlowDefinition.KindExtension
                    ? "This file is an automatic step (\"kind\": \"extension\"), not a recipe."
                    : $"Unknown kind '{kind}', use \"recipe\" or \"extension\".");
            }

            CaptureRecipe recipe = definition.Deserialize<CaptureRecipe>(Options);
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
                foreach (var item in Parse(trimmed).EnumerateArray().Where(item => item.ValueKind == JsonValueKind.Object))
                {
                    CheckFormat(item);
                    string kind = ReadKind(item);
                    if (kind != FlowDefinition.KindRecipe)
                    {
                        throw new JsonException($"The list contains a definition of kind '{kind}'; a recipe list can only hold recipes.");
                    }
                    var recipe = WithoutRuntimeProperties(item).Deserialize<CaptureRecipe>(Options);
                    if (recipe != null)
                    {
                        list.Add(recipe);
                    }
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

        /// <summary>
        /// Saves a recipe to a file path formatted as JSON.
        /// </summary>
        public static void SaveToFile(CaptureRecipe recipe, string filePath, bool indented = true)
        {
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path cannot be empty", nameof(filePath));

            string json = Serialize(recipe, indented);
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(filePath, json);
            recipe.FilePath = filePath;
        }

        /// <summary>
        /// Saves a list of recipes to a file path formatted as JSON array.
        /// </summary>
        public static void SaveListToFile(IEnumerable<CaptureRecipe> recipes, string filePath, bool indented = true)
        {
            if (recipes == null) throw new ArgumentNullException(nameof(recipes));
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("File path cannot be empty", nameof(filePath));

            string json = SerializeList(recipes, indented);
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(filePath, json);
        }
    }
}
