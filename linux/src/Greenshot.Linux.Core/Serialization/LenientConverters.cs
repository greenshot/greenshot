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
using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Greenshot.Linux.Core.Serialization
{
    /// <summary>
    /// Reads a number or a boolean into a text property as it is written, e.g. "version": 1.0 gives "1.0".
    /// Hand written files did this with Json.NET, so they must keep loading.
    /// </summary>
    public sealed class LenientStringConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    return reader.GetString();
                case JsonTokenType.Number:
                    return Encoding.UTF8.GetString(reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan.ToArray());
                case JsonTokenType.True:
                    return "true";
                case JsonTokenType.False:
                    return "false";
                default:
                    throw new JsonException($"Expected a text, got {reader.TokenType}.");
            }
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) => writer.WriteStringValue(value);
    }

    /// <summary>
    /// Reads "true" / "false" texts and numbers (0 is false) into a boolean property, as Json.NET did.
    /// </summary>
    public sealed class LenientBooleanConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.True:
                    return true;
                case JsonTokenType.False:
                    return false;
                case JsonTokenType.String when bool.TryParse(reader.GetString()?.Trim(), out bool parsed):
                    return parsed;
                case JsonTokenType.Number:
                    return reader.GetDouble() != 0;
                default:
                    throw new JsonException($"Expected true or false, got {reader.TokenType}.");
            }
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) => writer.WriteBooleanValue(value);
    }

    /// <summary>
    /// Writes a double as short as it reads back the same, as Json.NET did (0.65, not 0.65000000000000002 as System.Text.Json
    /// writes it on the .NET Framework), with ".0" for whole numbers; NaN and infinity as texts. Reads numbers and numbers in texts.
    /// </summary>
    public sealed class RoundTripDoubleConverter : JsonConverter<double>
    {
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string text = reader.GetString();
                return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) ? parsed : throw new JsonException($"'{text}' isn't a number.");
            }
            return reader.GetDouble();
        }

        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                writer.WriteStringValue(value.ToString(CultureInfo.InvariantCulture));
                return;
            }
            string text = value.ToString("R", CultureInfo.InvariantCulture);
            writer.WriteRawValue(text.IndexOfAny(new[] { '.', 'E', 'e' }) < 0 ? text + ".0" : text);
        }
    }
}
