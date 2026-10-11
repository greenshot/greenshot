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
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

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

        private sealed record DocumentJson(int Version, Guid Id, Canvas Canvas, IReadOnlyList<Layer> Layers, IReadOnlyDictionary<Guid, ResourceEntry> Resources);

        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IgnoreReadOnlyProperties = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            AllowOutOfOrderMetadataProperties = true,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase), new RgbaConverter() },
            TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { AddElementTypes } }
        };

        /// <summary>
        /// Elements are written with a short "type" name, never with a CLR type name
        /// </summary>
        private static void AddElementTypes(JsonTypeInfo typeInfo)
        {
            if (typeInfo.Type != typeof(Element))
            {
                return;
            }

            typeInfo.PolymorphismOptions = new JsonPolymorphismOptions { TypeDiscriminatorPropertyName = "type" };
            foreach (var derivedType in new[]
                     {
                         new JsonDerivedType(typeof(RectangleElement), "rectangle"),
                         new JsonDerivedType(typeof(EllipseElement), "ellipse"),
                         new JsonDerivedType(typeof(LineElement), "line"),
                         new JsonDerivedType(typeof(ArrowElement), "arrow"),
                         new JsonDerivedType(typeof(FreehandElement), "freehand"),
                         new JsonDerivedType(typeof(TextElement), "text"),
                         new JsonDerivedType(typeof(SpeechBubbleElement), "speechBubble"),
                         new JsonDerivedType(typeof(StepLabelElement), "stepLabel"),
                         new JsonDerivedType(typeof(HighlightElement), "highlight"),
                         new JsonDerivedType(typeof(ObfuscateElement), "obfuscate"),
                         new JsonDerivedType(typeof(ImageElement), "image"),
                         new JsonDerivedType(typeof(EmojiElement), "emoji"),
                         new JsonDerivedType(typeof(CursorElement), "cursor"),
                         new JsonDerivedType(typeof(CutMarkElement), "cutMark"),
                         new JsonDerivedType(typeof(TornEdgeElement), "tornEdge"),
                         new JsonDerivedType(typeof(UnknownElement), "unknown")
                     })
            {
                typeInfo.PolymorphismOptions.DerivedTypes.Add(derivedType);
            }
        }

        public static void Write(GreenshotDocument document, Stream stream)
        {
            var entries = document.Resources.Values.ToDictionary(resource => resource.Id, resource => new ResourceEntry($"resources/{resource.Id:D}{Extension(resource.MediaType)}", resource.MediaType));
            var json = new DocumentJson(Version, document.Id, document.Canvas, document.Layers, entries);

            using var zip = new ZipArchive(stream, ZipArchiveMode.Create, true);
            WriteEntry(zip, "mimetype", Encoding.ASCII.GetBytes(MediaType), CompressionLevel.NoCompression);
            WriteEntry(zip, "document.json", JsonSerializer.SerializeToUtf8Bytes(json, Options), CompressionLevel.Optimal);
            foreach (var resource in document.Resources.Values)
            {
                WriteEntry(zip, entries[resource.Id].Path, resource.Data, CompressionLevel.Optimal);
            }
        }

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
            return new GreenshotDocument(json.Id, json.Canvas, json.Layers, resources);
        }

        private static string Extension(string mediaType) => mediaType switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/svg+xml" => ".svg",
            "image/x-icon" => ".ico",
            _ => ".bin"
        };

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
