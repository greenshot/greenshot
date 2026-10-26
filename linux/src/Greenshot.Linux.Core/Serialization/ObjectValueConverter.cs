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

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Greenshot.Linux.Core.Serialization
{
    /// <summary>
    /// Reads JSON values of properties typed "object" into plain .NET values: null, string, bool, long, double,
    /// Dictionary&lt;string, object&gt; (keys in any casing) and List&lt;object&gt;; the same values recipes built in code use.
    /// Writes dictionary keys as they are, everything else as its runtime type.
    /// </summary>
    public sealed class ObjectValueConverter : JsonConverter<object>
    {
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.Null:
                    return null;
                case JsonTokenType.True:
                    return true;
                case JsonTokenType.False:
                    return false;
                case JsonTokenType.String:
                    return reader.GetString();
                case JsonTokenType.Number:
                    return reader.TryGetInt64(out long integer) ? (object)integer : reader.GetDouble();
                case JsonTokenType.StartArray:
                    var list = new List<object>();
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                    {
                        list.Add(Read(ref reader, typeof(object), options));
                    }
                    return list;
                case JsonTokenType.StartObject:
                    var dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
                    {
                        string name = reader.GetString();
                        reader.Read();
                        dictionary[name] = Read(ref reader, typeof(object), options);
                    }
                    return dictionary;
                default:
                    throw new JsonException($"Unexpected JSON token {reader.TokenType}.");
            }
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case null:
                    writer.WriteNullValue();
                    return;
                case IDictionary<string, object> dictionary:
                    writer.WriteStartObject();
                    foreach (var pair in dictionary)
                    {
                        writer.WritePropertyName(pair.Key);
                        Write(writer, pair.Value, options);
                    }
                    writer.WriteEndObject();
                    return;
            }

            var type = value.GetType();
            if (type == typeof(object))
            {
                writer.WriteStartObject();
                writer.WriteEndObject();
                return;
            }
            JsonSerializer.Serialize(writer, value, type, options);
        }
    }
}
