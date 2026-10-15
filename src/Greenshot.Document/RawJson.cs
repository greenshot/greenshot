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
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Greenshot.Document
{
    /// <summary>
    /// A JSON value the model doesn't interpret (extensions, properties of a group), read and written unchanged
    /// (without whitespace)
    /// </summary>
    [JsonConverter(typeof(RawJsonConverter))]
    public sealed record RawJson(string Json);

    internal sealed class RawJsonConverter : JsonConverter<RawJson>
    {
        public override RawJson Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Without whitespace, so a value reads the same however it was indented
            using var document = JsonDocument.ParseValue(ref reader);
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                document.RootElement.WriteTo(writer);
            }

            return new RawJson(Encoding.UTF8.GetString(stream.ToArray()));
        }

        public override void Write(Utf8JsonWriter writer, RawJson value, JsonSerializerOptions options) => writer.WriteRawValue(value.Json);
    }
}
