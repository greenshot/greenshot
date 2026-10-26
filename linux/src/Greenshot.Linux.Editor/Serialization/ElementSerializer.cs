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

using System.Text.Json;
using System.Text.Json.Serialization;
using Greenshot.Linux.Editor.Elements;
using Greenshot.Linux.Editor.Fields;
using SkiaSharp;

namespace Greenshot.Linux.Editor.Serialization;

/// <summary>
/// The elements as JSON: used for undo, for copy and paste of elements and in .greenshot files.
/// Unlike the BinaryFormatter of the Windows editor it can't create arbitrary types, only the known elements.
/// </summary>
public static class ElementSerializer
{
    public const int FormatVersion = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        Converters =
        {
            new RectConverter(),
            new PointConverter(),
            new ColorConverter(),
            new JsonStringEnumConverter(),
        },
        WriteIndented = false,
    };

    private sealed record Document(int Version, int CounterStart, List<DrawableElement> Elements);

    public static string Serialize(IEnumerable<DrawableElement> elements, int counterStart = 1) =>
        JsonSerializer.Serialize(new Document(FormatVersion, counterStart, elements.ToList()), Options);

    public static (List<DrawableElement> Elements, int CounterStart) Deserialize(string json)
    {
        var document = JsonSerializer.Deserialize<Document>(json, Options) ?? throw new JsonException("No elements");
        if (document.Version > FormatVersion)
        {
            throw new NotSupportedException($"The elements were written by a newer Greenshot (format {document.Version})");
        }

        return (document.Elements, document.CounterStart);
    }

    /// <summary>
    /// A deep copy of the elements
    /// </summary>
    public static List<DrawableElement> Clone(IEnumerable<DrawableElement> elements) => Deserialize(Serialize(elements)).Elements;

    private sealed class RectConverter : JsonConverter<SKRect>
    {
        public override SKRect Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var values = JsonSerializer.Deserialize<float[]>(ref reader, options);
            return values is { Length: 4 } ? new SKRect(values[0], values[1], values[2], values[3]) : throw new JsonException("A rectangle needs 4 numbers");
        }

        public override void Write(Utf8JsonWriter writer, SKRect value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, new[] { value.Left, value.Top, value.Right, value.Bottom }, options);
    }

    private sealed class PointConverter : JsonConverter<SKPoint>
    {
        public override SKPoint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var values = JsonSerializer.Deserialize<float[]>(ref reader, options);
            return values is { Length: 2 } ? new SKPoint(values[0], values[1]) : throw new JsonException("A point needs 2 numbers");
        }

        public override void Write(Utf8JsonWriter writer, SKPoint value, JsonSerializerOptions options) =>
            JsonSerializer.Serialize(writer, new[] { value.X, value.Y }, options);
    }

    private sealed class ColorConverter : JsonConverter<SKColor>
    {
        public override SKColor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            SKColor.TryParse(reader.GetString(), out var color) ? color : throw new JsonException("Not a color");

        public override void Write(Utf8JsonWriter writer, SKColor value, JsonSerializerOptions options) =>
            writer.WriteStringValue(ElementStyle.Format(value));
    }
}
