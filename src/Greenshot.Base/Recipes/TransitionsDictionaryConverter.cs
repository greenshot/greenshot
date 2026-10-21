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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// Converts JSON transition mappings where values can be either a single target string or an array of target strings.
    /// E.g. { "a": "b", "c": ["d", "e"] }
    /// </summary>
    public class TransitionsDictionaryConverter : JsonConverter<Dictionary<string, List<string>>>
    {
        public override bool HandleNull => true;

        public override Dictionary<string, List<string>> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var token = JsonElement.ParseValue(ref reader);
            if (token.ValueKind != JsonValueKind.Object)
            {
                return result;
            }

            foreach (var prop in token.EnumerateObject())
            {
                var list = new List<string>();
                if (prop.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in prop.Value.EnumerateArray())
                    {
                        string s = item.ValueKind == JsonValueKind.Null ? null : item.ToString();
                        if (!string.IsNullOrWhiteSpace(s))
                        {
                            list.Add(s);
                        }
                    }
                }
                else if (prop.Value.ValueKind != JsonValueKind.Null)
                {
                    string s = prop.Value.ToString();
                    if (!string.IsNullOrWhiteSpace(s))
                    {
                        list.Add(s);
                    }
                }

                result[prop.Name] = list;
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<string, List<string>> value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();
            foreach (var kvp in value)
            {
                writer.WritePropertyName(kvp.Key);
                writer.WriteStartArray();
                if (kvp.Value != null)
                {
                    foreach (var item in kvp.Value)
                    {
                        if (!string.IsNullOrWhiteSpace(item))
                        {
                            writer.WriteStringValue(item);
                        }
                    }
                }
                writer.WriteEndArray();
            }
            writer.WriteEndObject();
        }
    }
}
