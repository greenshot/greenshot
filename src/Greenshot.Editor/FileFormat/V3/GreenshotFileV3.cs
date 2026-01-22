/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2025 Thomas Braun, Jens Klingen, Robin Krom
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
using System.IO.Compression;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Greenshot.Editor.FileFormat.Dto;
using log4net;

namespace Greenshot.Editor.FileFormat.V3;

/// <summary>
/// Provides methods for loading and saving Greenshot file format version V3.
/// </summary>
internal static class GreenshotFileV3
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(GreenshotFileV3));

    private const string ContentJsonName = "content.json";
    private const string ImageFolder = "Images";

    public static JsonSerializerOptions GetJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new JsonStringEnumConverter() 
            }
        };

        return options;
    }

    internal static bool DoesFileFormatMatch(Stream greenshotFileStream)
    {
        try
        {
            greenshotFileStream.Seek(0, SeekOrigin.Begin);

            using var zipArchive = new ZipArchive(greenshotFileStream, ZipArchiveMode.Read, true);
            var entry = zipArchive.GetEntry(ContentJsonName);
            if (entry == null)
            {
                return false;
            }

            using var entryStream = entry.Open();
            using var document = JsonDocument.Parse(entryStream);

            // We only check the format version, do not deserialize the full DTO.
            if (!document.RootElement.TryGetProperty("formatVersion", out var formatVersionElement))
            {
                return false;
            }

            GreenshotFileVersionHandler.GreenshotFileFormatVersion formatVersion;
            if (formatVersionElement.ValueKind == JsonValueKind.Number)
            {
                formatVersion = (GreenshotFileVersionHandler.GreenshotFileFormatVersion)formatVersionElement.GetInt32();
            }
            else if (formatVersionElement.ValueKind == JsonValueKind.String)
            {
                var text = formatVersionElement.GetString();
                if (string.IsNullOrEmpty(text))
                {
                    return false;
                }

                if (!Enum.TryParse(text, true, out formatVersion))
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            return formatVersion == GreenshotFileVersionHandler.GreenshotFileFormatVersion.V3;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Saves the specified <see cref="GreenshotFile"/> to the provided stream in the Greenshot file format version V3.
    /// </summary>
    /// <remarks>
    /// V3 stores only the domain model as JSON and intentionally does not store the image payload.
    /// The stream contains a ZIP archive with a single entry <c>content.json</c>.
    /// </remarks>
    internal static bool SaveToStream(GreenshotFile greenshotFile, Stream stream)
    {
        if (greenshotFile == null)
        {
            throw new ArgumentNullException(nameof(greenshotFile));
        }

        var dto = ConvertDomainToDto.ToDto(greenshotFile);

        dto.FormatVersion = GreenshotFileVersionHandler.GreenshotFileFormatVersion.V3;
        dto.SchemaVersion = GreenshotFileVersionHandler.CurrentSchemaVersion;

        using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            SaveImages(dto, zipArchive);

            var options = GetJsonSerializerOptions();
            var jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dto, options));

            var contentEntry = zipArchive.CreateEntry(ContentJsonName, CompressionLevel.Optimal);
            using var contentStream = contentEntry.Open();
            contentStream.Write(jsonBytes, 0, jsonBytes.Length);
        }

        return true;
    }

    //TODO: handle image formats properly
    //TODO: handle all Images from all dto classes , maby use dictionary 
    //TODO: offtopic here but: support a base64 variant as well, for using it in clipboard or other non-file usages
    private static void SaveImages(GreenshotFileDto dto, ZipArchive zipArchive)
    {
        if (dto.Image != null)
        {
            dto.ImagePath = $"{ImageFolder}/image_1.png";
            var entry = zipArchive.CreateEntry(dto.ImagePath, CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            entryStream.Write(dto.Image, 0, dto.Image.Length);
        }

        if (dto.RenderedImage != null)
        {
            dto.RenderedImagePath = $"{ImageFolder}/image_2.png";
            var entry = zipArchive.CreateEntry(dto.RenderedImagePath, CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            entryStream.Write(dto.RenderedImage, 0, dto.RenderedImage.Length);
        }
    }

    /// <summary>
    /// Loads a Greenshot V3 file from stream.
    /// </summary>
    internal static GreenshotFile LoadFromStream(Stream greenshotFileStream)
    {
        try
        {
            greenshotFileStream.Seek(0, SeekOrigin.Begin);
            using var zipArchive = new ZipArchive(greenshotFileStream, ZipArchiveMode.Read, true);
            var entry = zipArchive.GetEntry(ContentJsonName);
            if (entry == null)
            {
                throw new InvalidDataException($"Missing '{ContentJsonName}' in Greenshot V3 zip.");
            }

            GreenshotFileDto dto;
            using (var entryStream = entry.Open())
            dto = JsonSerializer.Deserialize<GreenshotFileDto>(entryStream, GetJsonSerializerOptions());
            

            if (dto == null)
            {
                throw new InvalidDataException("Failed to deserialize Greenshot V3 file.");
            }

            LoadImages(dto, zipArchive);

            return ConvertDtoToDomain.ToDomain(dto);
        }
        catch (Exception e)
        {
            Log.Error("Error deserializing Greenshot file (V3) from stream.", e);
            throw;
        }
    }

    private static void LoadImages(GreenshotFileDto dto, ZipArchive zipArchive)
    {
        if (!string.IsNullOrEmpty(dto.ImagePath))
        {
            dto.Image = ReadEntryBytes(zipArchive, dto.ImagePath);
        }

        if (!string.IsNullOrEmpty(dto.RenderedImagePath))
        {
            dto.RenderedImage = ReadEntryBytes(zipArchive, dto.RenderedImagePath);
        }
    }

    private static byte[] ReadEntryBytes(ZipArchive zipArchive, string entryPath)
    {
        var entry = zipArchive.GetEntry(entryPath);
        if (entry == null)
        {
            return null;
        }

        using var entryStream = entry.Open();
        using var ms = new MemoryStream();
        entryStream.CopyTo(ms);
        return ms.ToArray();
    }
}
