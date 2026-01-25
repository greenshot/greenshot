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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Greenshot.Base.Core;
using Greenshot.Editor.FileFormat.Dto;
using Greenshot.Editor.FileFormat.Dto.Container;
using log4net;

namespace Greenshot.Editor.FileFormat.V3;

/// <summary>
/// Provides methods for loading and saving Greenshot file format version V3.
/// </summary>
internal static class GreenshotFileV3
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(GreenshotFileV3));

    private const string ContentJsonName = "content.json";
    private const string MetadataJsonName = "meta.json";
    private const string ImageFolder = "images";

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

    private static void SaveImages(GreenshotFileDto dto, ZipArchive zipArchive)
    {
        var imagesToSave = ExtractAllImages(dto);

        foreach (var kvp in imagesToSave)
        {
            var entry = zipArchive.CreateEntry(kvp.Key, CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            entryStream.Write(kvp.Value, 0, kvp.Value.Length);
        }
    }

    private static Dictionary<string, byte[]> ExtractAllImages(GreenshotFileDto dto)
    {
        var imageIndex = 1;
        var imagesToSave = new Dictionary<string, byte[]>();

        foreach (var kvp in ExtractImages(dto, ref imageIndex))
        {
            imagesToSave[kvp.Key] = kvp.Value;
        }

        var containerList = dto.ContainerList?.ContainerList;

        if (containerList is null)
        {
            return imagesToSave;
        }

        foreach (var container in containerList)
        {
            foreach (var kvp in ExtractImages(container, ref imageIndex))
            {
                imagesToSave[kvp.Key] = kvp.Value;
            }
        }

        return imagesToSave;
    }

    private static Dictionary<string, byte[]> ExtractImages(object dto, ref int imageIndex)
    {
        if (dto is not (GreenshotFileDto or DrawableContainerDto))
        {
            Log.Error($"DTO must be of type {nameof(GreenshotFileDto)} or {nameof(DrawableContainerDto)}.");
            throw new ArgumentException($"DTO must be of type {nameof(GreenshotFileDto)} or {nameof(DrawableContainerDto)}.", nameof(dto));
        }

        var imagesToSave = new Dictionary<string, byte[]>();
        var dtoType = dto.GetType();
        var allDtoProperties = dtoType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

        foreach (var property in allDtoProperties)
        {
            var imageAttribute = property.GetCustomAttribute<GreenshotImageDataAttribute>();
            if (imageAttribute == null || property.PropertyType != typeof(byte[]))
            {
                // ignore alle properties without the attribute GreenshotImageDataAttribute or not of type byte[]
                continue;
            }

            var pathProperty = dtoType.GetProperty(imageAttribute.PathPropertyName, BindingFlags.Instance | BindingFlags.Public);
            if (pathProperty == null || pathProperty.PropertyType != typeof(string))
            {
                Log.Warn($"Path property '{imageAttribute.PathPropertyName}' for image property '{property.Name}' not found or not string.");
                continue;
            }

            var imageBytes = property.GetValue(dto) as byte[];
            if (imageBytes == null)
            {
                continue;
            }

            var extension = ResolveExtension(dto, dtoType, imageAttribute);
            var folder = ResolveTargetFolder(imageAttribute);
            var fileName = string.IsNullOrWhiteSpace(imageAttribute.StaticFilename) ? $"image_{imageIndex}" : imageAttribute.StaticFilename;
            var imagePath = $"{folder}/{fileName}.{extension}";
            imageIndex++;

            imagesToSave[imagePath] = imageBytes;

            pathProperty.SetValue(dto, imagePath);
            property.SetValue(dto, null);
        }

        return imagesToSave;
    }

    private static string ResolveTargetFolder(GreenshotImageDataAttribute imageAttribute)
    {
        var folder = imageAttribute.TargetZipFolder;
        if (string.IsNullOrWhiteSpace(folder))
        {
            return ImageFolder;
        }

        folder = folder.Trim();
        folder = folder.Trim('/').Trim('\\');

        return string.IsNullOrWhiteSpace(folder) ? ImageFolder : folder;
    }

    private static string ResolveExtension(object dto, Type dtoType, GreenshotImageDataAttribute imageAttribute)
    {
        PropertyInfo extensionProperty = null;
        string extension = imageAttribute.StaticExtension;

        if (!string.IsNullOrEmpty(imageAttribute.ExtensionPropertyName))
        {
            extensionProperty = dtoType.GetProperty(imageAttribute.ExtensionPropertyName, BindingFlags.Instance | BindingFlags.Public);
            if (extensionProperty == null || extensionProperty.PropertyType != typeof(string))
            {
                Log.Warn($"Extension property '{imageAttribute.ExtensionPropertyName}' for image property '{imageAttribute.PathPropertyName}' not found or not string.");
            }
            else
            {
                var propertyValue = extensionProperty.GetValue(dto) as string;
                if (!string.IsNullOrWhiteSpace(propertyValue))
                {
                    extension = propertyValue;
                }
            }
        }

        extension = NormalizeExtension(extension);

        extensionProperty?.SetValue(dto, null);

        return extension;
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return "png";
        }

        extension = extension.Trim();
        extension = extension.TrimStart('.');

        return string.IsNullOrWhiteSpace(extension) ? "png" : extension;
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
        var containerList = dto.ContainerList?.ContainerList;
  
        LoadImagesForDto(dto, zipArchive);

        if (containerList is null)
        {
            return;
        }

        foreach (var container in containerList)
        {
            LoadImagesForDto(container, zipArchive);
        }
    }

    private static void LoadImagesForDto(object dto, ZipArchive zipArchive)
    {
        if (dto is not (GreenshotFileDto or DrawableContainerDto))
        {
            Log.Error($"DTO must be of type {nameof(GreenshotFileDto)} or {nameof(DrawableContainerDto)}.");
            throw new ArgumentException($"DTO must be of type {nameof(GreenshotFileDto)} or {nameof(DrawableContainerDto)}.", nameof(dto));
        }

        var dtoType = dto.GetType();
        var allDtoProperties = dtoType.GetProperties(BindingFlags.Instance | BindingFlags.Public);

        foreach (var property in allDtoProperties)
        {
            var pathAttribute = property.GetCustomAttribute<GreenshotImagePathAttribute>();
            if (pathAttribute == null || property.PropertyType != typeof(string))
            {
                continue;
            }

            var entryPath = property.GetValue(dto) as string;
            if (string.IsNullOrEmpty(entryPath))
            {
                continue;
            }

            var imageProperty = dtoType.GetProperty(pathAttribute.ImagePropertyName, BindingFlags.Instance | BindingFlags.Public);
            if (imageProperty == null || imageProperty.PropertyType != typeof(byte[]))
            {
                Log.Warn($"Image property '{pathAttribute.ImagePropertyName}' for path property '{property.Name}' not found or not byte array.");
                continue;
            }

            var imageBytes = ReadEntryBytes(zipArchive, entryPath);
            imageProperty.SetValue(dto, imageBytes);
            property.SetValue(dto, null);
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
