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
using Greenshot.Base.Core;

namespace Greenshot.Editor.FileFormat.V3;

/// <summary>
/// Provides methods for loading and saving Greenshot file format version V3.
/// </summary>
internal static class GreenshotFileV3
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(GreenshotFileV3));

    private const string ContentJsonName = "content.json";
    private const string MetadataJsonName = "meta.json";
    private const string ImageFolder = "Images";

    /// <summary>
    /// Creates a new instance of JsonSerializerOptions with settings for Greenshot file serialization.
    /// </summary>
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
            var entry = zipArchive.GetEntry(MetadataJsonName);
            if (entry == null)
            {
                return false;
            }

            using var entryStream = entry.Open();
            using var document = JsonDocument.Parse(entryStream);

            // We only check the format version, do not deserialize the full DTO.
            return GetFormatVersion(document.RootElement, "formatVersion") == GreenshotFileVersionHandler.GreenshotFileFormatVersion.V3;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Determines the Greenshot file format version from the specified JSON element and property name.
    /// </summary>
    /// <remarks>The method supports both numeric and string representations of the file format version in the
    /// JSON property. If the property is not present or cannot be parsed as a valid version, the method returns <see
    /// cref="GreenshotFileVersionHandler.GreenshotFileFormatVersion.Unknown"/>.</remarks>
    /// <param name="jsonElement">The <see cref="System.Text.Json.JsonElement"/> containing the property to inspect.</param>
    /// <param name="propertyName">The name of the property within <paramref name="jsonElement"/> that specifies the file format version.</param>
    /// <returns>A <see cref="GreenshotFileVersionHandler.GreenshotFileFormatVersion"/> value representing the detected file
    /// format version. Returns <see cref="GreenshotFileVersionHandler.GreenshotFileFormatVersion.Unknown"/> if the
    /// property is missing or cannot be parsed.</returns>
    private static GreenshotFileVersionHandler.GreenshotFileFormatVersion GetFormatVersion(JsonElement jsonElement, string propertyName)
    {
        if (jsonElement.TryGetProperty(propertyName, out var formatVersionElement))
        {
            if (formatVersionElement.ValueKind == JsonValueKind.Number)
            {
                return (GreenshotFileVersionHandler.GreenshotFileFormatVersion)formatVersionElement.GetInt32();
            }

            if (formatVersionElement.ValueKind == JsonValueKind.String)
            {
                var text = formatVersionElement.GetString();
                if (!string.IsNullOrEmpty(text) && Enum.TryParse<GreenshotFileVersionHandler.GreenshotFileFormatVersion>(text, true, out var formatVersion))
                {
                    return formatVersion;
                }
            }
        }

        return GreenshotFileVersionHandler.GreenshotFileFormatVersion.Unknown;
    }

    /// <summary>
    /// Saves the specified <see cref="GreenshotFile"/> to the provided stream in the Greenshot file format version V3.
    /// </summary>
    /// <remarks>
    /// V3 stores the domain model as JSON and intentionally does not store the image payload.
    /// The stream contains a ZIP archive with entries:
    /// - meta.json: Contains file metadata (format version, schema version, capture date, etc.)
    /// - content.json: Contains the main content (containers and other data)
    /// - Images/: Contains image files (if any)
    /// </remarks>
    internal static bool SaveToStream(GreenshotFile greenshotFile, Stream stream)
    {
        if (greenshotFile == null)
        {
            throw new ArgumentNullException(nameof(greenshotFile));
        }

        var dto = ConvertDomainToDto.ToDto(greenshotFile);

        var metaInfoDto = dto.MetaInformation ??= new GreenshotFileMetaInformationDto();

        metaInfoDto.FormatVersion = GreenshotFileVersionHandler.GreenshotFileFormatVersion.V3;
        metaInfoDto.SchemaVersion = GreenshotFileVersionHandler.CurrentSchemaVersion;
        metaInfoDto.SavedByGreenshotVersion = EnvironmentInfo.GetGreenshotVersion();
        metaInfoDto.CaptureSize = greenshotFile.Image != null ? $"{greenshotFile.Image.Width}x{greenshotFile.Image.Height}px" : string.Empty;

        using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            SaveImages(dto, zipArchive);
            SaveMetadata(metaInfoDto, zipArchive);

            // Remove metadata from DTO before serializing content to avoid duplication
            dto.MetaInformation = null;

            var options = GetJsonSerializerOptions();
            var jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dto, options));

            var contentEntry = zipArchive.CreateEntry(ContentJsonName, CompressionLevel.Optimal);
            using var contentStream = contentEntry.Open();
            contentStream.Write(jsonBytes, 0, jsonBytes.Length);
        }

        return true;
    }

    private static void SaveMetadata(GreenshotFileMetaInformationDto metaInfoDto, ZipArchive zipArchive)
    {
        var options = GetJsonSerializerOptions();
        var jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(metaInfoDto, options));

        var metadataEntry = zipArchive.CreateEntry(MetadataJsonName, CompressionLevel.Optimal);
        using var metadataStream = metadataEntry.Open();
        metadataStream.Write(jsonBytes, 0, jsonBytes.Length);
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

            dto.Image = null;
        }

        if (dto.RenderedImage != null)
        {
            dto.RenderedImagePath = $"{ImageFolder}/image_2.png";
            var entry = zipArchive.CreateEntry(dto.RenderedImagePath, CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            entryStream.Write(dto.RenderedImage, 0, dto.RenderedImage.Length);

            dto.RenderedImage = null;
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
            LoadMetadata(dto, zipArchive);

            return ConvertDtoToDomain.ToDomain(dto);
        }
        catch (Exception e)
        {
            Log.Error("Error deserializing Greenshot file (V3) from stream.", e);
            throw;
        }
    }

    private static void LoadMetadata(GreenshotFileDto dto, ZipArchive zipArchive)
    {
        var metadataEntry = zipArchive.GetEntry(MetadataJsonName);
        if (metadataEntry != null)
        {
            try
            {
                using var metadataStream = metadataEntry.Open();
                var metaInfoDto = JsonSerializer.Deserialize<GreenshotFileMetaInformationDto>(metadataStream, GetJsonSerializerOptions());
                if (metaInfoDto != null)
                {
                    dto.MetaInformation = metaInfoDto;
                }
            }
            catch (Exception e)
            {
                Log.Warn("Failed to load metadata from meta.json, using default metadata.", e);
                if (dto.MetaInformation == null)
                {
                    dto.MetaInformation = new GreenshotFileMetaInformationDto();
                }
            }
        }
        else
        {
            Log.Warn("Metadata file 'meta.json' not found, using default metadata.");
            if (dto.MetaInformation == null)
            {
                dto.MetaInformation = new GreenshotFileMetaInformationDto();
            }
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
