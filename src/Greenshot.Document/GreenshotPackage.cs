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
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Greenshot.Document
{
    /// <summary>
    /// Reads and writes a GreenshotDocument as a .greenshot package: a zip with the entries "mimetype" (first),
    /// "document.json" and the resources in "resources/".
    /// </summary>
    public static class GreenshotPackage
    {
        public const string MediaType = "application/vnd.greenshot.document+zip";

        /// <summary>
        /// The version of document.json this code writes, newer versions are refused.
        /// </summary>
        public const int Version = 1;

        private const int MaxEntries = 10_000;
        private const long MaxEntrySize = 256L * 1024 * 1024;

        private sealed record ResourceEntry(string Path, string MediaType);

        private sealed record DocumentJson(int Version, Guid Id, Canvas Canvas, IReadOnlyList<SharedStyle> Styles, IReadOnlyList<StepSeries> Series,
            IReadOnlyList<Layer> Layers, IReadOnlyDictionary<Guid, ResourceEntry> Resources);

        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IgnoreReadOnlyProperties = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase), new RgbaConverter(), new ElementConverter() }
        };

        /// <summary>
        /// Writes the document with the resources its elements link to, other resources are left out.
        /// A link to a missing resource, or a resource without a file extension, is refused.
        /// </summary>
        /// <param name="document">GreenshotDocument</param>
        /// <param name="stream">Stream to write the zip to</param>
        /// <param name="extensionFor">The file extension of a media type (e.g. from the file format registry), null when the media type isn't supported</param>
        public static void Write(GreenshotDocument document, Stream stream, Func<string, string> extensionFor)
        {
            var linked = document.Layers.SelectMany(layer => layer.Elements).SelectMany(document.Links).Distinct()
                .Select(id => document.Resources.TryGetValue(id, out var resource) ? resource : throw new InvalidDataException($"An element links to the missing resource {id}"))
                .ToList();
            var entries = linked.ToDictionary(resource => resource.Id, resource => new ResourceEntry($"resources/{resource.Id:D}.{Extension(resource, extensionFor)}", resource.MediaType));
            var json = new DocumentJson(Version, document.Id, document.Canvas, document.Styles, document.Series, document.Layers, entries);

            using var zip = new ZipArchive(stream, ZipArchiveMode.Create, true);
            WriteEntry(zip, "mimetype", Encoding.ASCII.GetBytes(MediaType), CompressionLevel.NoCompression);
            WriteEntry(zip, "document.json", JsonSerializer.SerializeToUtf8Bytes(json, Options), CompressionLevel.Optimal);
            foreach (var resource in linked)
            {
                WriteEntry(zip, entries[resource.Id].Path, resource.Data, CompressionLevel.Optimal);
            }
        }

        /// <summary>
        /// Elements written like those in document.json (a JSON array), e.g. in a recipe
        /// </summary>
        public static IReadOnlyList<Element> ReadElements(string json) =>
            JsonSerializer.Deserialize<List<Element>>(json, Options) ?? throw new InvalidDataException("No elements");

        private static string Extension(Resource resource, Func<string, string> extensionFor) =>
            extensionFor(resource.MediaType)?.TrimStart('.') ?? throw new InvalidDataException($"The resource {resource.Id} has the unsupported media type {resource.MediaType}");

        /// <summary>
        /// Reads a package written by Write, throws an InvalidDataException (or JsonException) for anything else
        /// </summary>
        public static GreenshotDocument Read(Stream stream)
        {
            using var zip = new ZipArchive(stream, ZipArchiveMode.Read, true);
            if (zip.Entries.Count > MaxEntries)
            {
                throw new InvalidDataException($"The package has more than {MaxEntries} entries");
            }

            if (Encoding.ASCII.GetString(ReadEntry(zip, "mimetype")) != MediaType)
            {
                throw new InvalidDataException("The package is not a Greenshot document");
            }

            var json = JsonSerializer.Deserialize<DocumentJson>(ReadEntry(zip, "document.json"), Options) ?? throw new InvalidDataException("document.json is empty");
            if (json.Version > Version)
            {
                throw new InvalidDataException($"The document has version {json.Version}, this Greenshot reads up to version {Version}");
            }

            var resources = json.Resources.ToDictionary(pair => pair.Key, pair => new Resource(pair.Key, pair.Value.MediaType, ReadEntry(zip, pair.Value.Path)));
            return new GreenshotDocument(json.Id, json.Canvas, json.Layers, resources)
            {
                Styles = json.Styles ?? Array.Empty<SharedStyle>(),
                Series = json.Series ?? Array.Empty<StepSeries>()
            };
        }

        private static void WriteEntry(ZipArchive zip, string path, byte[] data, CompressionLevel compressionLevel)
        {
            using var entryStream = zip.CreateEntry(path, compressionLevel).Open();
            entryStream.Write(data, 0, data.Length);
        }

        /// <summary>
        /// Reads at most the size the entry declares, which may not be more than MaxEntrySize
        /// </summary>
        private static byte[] ReadEntry(ZipArchive zip, string path)
        {
            var entry = zip.GetEntry(path) ?? throw new InvalidDataException($"The package has no {path}");
            if (entry.Length > MaxEntrySize)
            {
                throw new InvalidDataException($"{path} is larger than {MaxEntrySize} bytes");
            }

            var data = new byte[entry.Length];
            using var entryStream = entry.Open();
            int offset = 0;
            int read;
            while (offset < data.Length && (read = entryStream.Read(data, offset, data.Length - offset)) > 0)
            {
                offset += read;
            }

            if (offset < data.Length)
            {
                throw new InvalidDataException($"{path} is shorter than declared");
            }

            return data;
        }

        /// <summary>
        /// Colors are written as "#RRGGBBAA"
        /// </summary>
        /// <summary>
        /// Elements are written with a short "type" name, never with a CLR type name. An element of a type this version
        /// doesn't know is read as UnknownElement and written back unchanged. The data of a plugin element is JSON.
        /// </summary>
        private sealed class ElementConverter : JsonConverter<Element>
        {
            private static readonly Dictionary<string, Type> Types = new()
            {
                ["rectangle"] = typeof(RectangleElement),
                ["ellipse"] = typeof(EllipseElement),
                ["line"] = typeof(LineElement),
                ["arrow"] = typeof(ArrowElement),
                ["freehand"] = typeof(FreehandElement),
                ["text"] = typeof(TextElement),
                ["speechBubble"] = typeof(SpeechBubbleElement),
                ["stepLabel"] = typeof(StepLabelElement),
                ["highlight"] = typeof(HighlightElement),
                ["obfuscate"] = typeof(ObfuscateElement),
                ["image"] = typeof(ImageElement),
                ["emoji"] = typeof(EmojiElement),
                ["cursor"] = typeof(CursorElement),
                ["cutMark"] = typeof(CutMarkElement),
                ["tornEdge"] = typeof(TornEdgeElement),
                ["plugin"] = typeof(PluginElement)
            };

            private static readonly Dictionary<Type, string> Names = Types.ToDictionary(pair => pair.Value, pair => pair.Key);

            public override Element Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var json = JsonNode.Parse(ref reader) as JsonObject ?? throw new JsonException("An element is not a JSON object");
                string name = json["type"] is JsonValue type && type.TryGetValue(out string value) ? value : null;
                if (name == null || !Types.TryGetValue(name, out var elementType))
                {
                    var id = json["id"] is JsonValue idValue && idValue.TryGetValue(out string idText) && Guid.TryParse(idText, out var guid) ? guid : Guid.Empty;
                    var area = json["area"] is JsonObject areaObject ? areaObject.Deserialize<RectD>(options) : default;
                    return new UnknownElement(id, area, name, json.ToJsonString());
                }

                var data = json["data"];
                json.Remove("data");
                var element = (Element)json.Deserialize(elementType, options);
                return element is PluginElement plugin ? plugin with { Data = data?.ToJsonString() } : element;
            }

            public override void Write(Utf8JsonWriter writer, Element value, JsonSerializerOptions options)
            {
                if (value is UnknownElement unknown)
                {
                    writer.WriteRawValue(unknown.Json);
                    return;
                }

                writer.WriteStartObject();
                writer.WriteString("type", Names[value.GetType()]);
                foreach (var property in JsonSerializer.SerializeToElement(value, value.GetType(), options).EnumerateObject())
                {
                    if (value is PluginElement plugin && property.NameEquals("data"))
                    {
                        writer.WritePropertyName(property.Name);
                        writer.WriteRawValue(plugin.Data);
                    }
                    else
                    {
                        property.WriteTo(writer);
                    }
                }

                writer.WriteEndObject();
            }
        }

        private sealed class RgbaConverter : JsonConverter<Rgba>
        {
            public override Rgba Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                string value = reader.GetString();
                if (value is not { Length: 9 } || value[0] != '#' || !uint.TryParse(value.Substring(1), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint rgba))
                {
                    throw new JsonException($"{value} is not a color in the format #RRGGBBAA");
                }

                return unchecked(new Rgba((byte)(rgba >> 24), (byte)(rgba >> 16), (byte)(rgba >> 8), (byte)rgba));
            }

            public override void Write(Utf8JsonWriter writer, Rgba value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
        }
    }
}
