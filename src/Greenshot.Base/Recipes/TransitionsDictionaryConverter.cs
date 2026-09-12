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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Greenshot.Base.Recipes
{
    /// <summary>
    /// Converts JSON transition mappings where values can be either a single target string or an array of target strings.
    /// E.g. { "a": "b", "c": ["d", "e"] }
    /// </summary>
    public class TransitionsDictionaryConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(IDictionary<string, List<string>>).IsAssignableFrom(objectType) ||
                   objectType == typeof(Dictionary<string, List<string>>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            }

            var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var token = JToken.Load(reader);

            if (token is JObject obj)
            {
                foreach (var prop in obj.Properties())
                {
                    string key = prop.Name;
                    var list = new List<string>();

                    if (prop.Value is JArray arr)
                    {
                        foreach (var item in arr)
                        {
                            string s = item?.ToString();
                            if (!string.IsNullOrWhiteSpace(s))
                            {
                                list.Add(s);
                            }
                        }
                    }
                    else if (prop.Value != null && prop.Value.Type != JTokenType.Null)
                    {
                        string s = prop.Value.ToString();
                        if (!string.IsNullOrWhiteSpace(s))
                        {
                            list.Add(s);
                        }
                    }

                    result[key] = list;
                }
            }

            return result;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is not Dictionary<string, List<string>> dict)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();
            foreach (var kvp in dict)
            {
                writer.WritePropertyName(kvp.Key);
                if (kvp.Value == null || kvp.Value.Count == 0)
                {
                    writer.WriteStartArray();
                    writer.WriteEndArray();
                }
                else if (kvp.Value.Count == 1)
                {
                    writer.WriteValue(kvp.Value[0]);
                }
                else
                {
                    writer.WriteStartArray();
                    foreach (var item in kvp.Value)
                    {
                        writer.WriteValue(item);
                    }
                    writer.WriteEndArray();
                }
            }
            writer.WriteEndObject();
        }
    }
}
