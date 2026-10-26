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

// Ported from Greenshot.Base, which has no nullable annotations
#nullable disable

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Greenshot.Linux.Core.Serialization
{
    /// <summary>
    /// The System.Text.Json settings Greenshot reads its files and messages with. They accept what Json.NET accepted, so files and
    /// messages written for it keep working: comments, trailing commas, property names in any casing, unknown properties,
    /// numbers in quotes and numbers or booleans where a text is expected. Collections and objects which a property already
    /// has are filled, not replaced (as Json.NET did), so e.g. case insensitive dictionaries stay case insensitive.
    /// </summary>
    public static class GreenshotJson
    {
        /// <summary>
        /// For parsing into a JsonNode: comments and trailing commas are skipped
        /// </summary>
        public static readonly JsonDocumentOptions DocumentOptions = new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        /// <summary>
        /// The tolerant settings described above, property names as they are in the classes (or their JsonPropertyName)
        /// </summary>
        public static readonly JsonSerializerOptions Options = CreateOptions();

        /// <summary>
        /// <see cref="Options"/>, written indented, e.g. for files
        /// </summary>
        public static readonly JsonSerializerOptions IndentedOptions = new JsonSerializerOptions(Options) { WriteIndented = true };

        /// <summary>
        /// Creates new serializer options with the tolerant reading described above, which the caller can extend
        /// (e.g. with a naming policy) before using them. Texts are written without escaping non ASCII characters.
        /// </summary>
        public static JsonSerializerOptions CreateOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                Converters =
                {
                    new LenientStringConverter(),
                    new LenientBooleanConverter(),
                    new RoundTripDoubleConverter(),
                    new ObjectValueConverter()
                }
            };
        }

        /// <summary>
        /// Parses JSON text into a JsonNode, null for the JSON null. A JsonObject can't have the same property twice.
        /// </summary>
        public static JsonNode Parse(string json) => JsonNode.Parse(json, documentOptions: DocumentOptions);

        /// <summary>
        /// Parses JSON text into a JsonElement; as with Json.NET, the last of the same properties counts
        /// </summary>
        public static JsonElement ParseElement(string json) => JsonSerializer.Deserialize<JsonElement>(json, Options);
    }
}
