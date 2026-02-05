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

namespace Greenshot.Editor.FileFormat.V2;

/// <summary>
/// Provides methods for loading and saving Greenshot file format version V2.
/// </summary>
internal static class GreenshotFileV2
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(GreenshotFileV2));

    private const string ContentJsonName = "content.json";
    private const string MetadataJsonName = "meta.json";

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

            // We only check the format version and do not deserialize the full DTO.
            return V2Helper.GetFormatVersion(document.RootElement, "formatVersion") == GreenshotFileVersionHandler.GreenshotFileFormatVersion.V2;
        }
        catch
        {
            return false;
        }
    }


    /// <summary>
    /// Saves the specified <see cref="GreenshotFile"/> to the provided stream in the Greenshot file format version V2.
    /// </summary>
    /// <remarks>
    /// V2 stores the domain model as JSON and intentionally does not store the image payload.
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

        metaInfoDto.FormatVersion = GreenshotFileVersionHandler.GreenshotFileFormatVersion.V2;
        metaInfoDto.SchemaVersion = GreenshotFileVersionHandler.CurrentSchemaVersion;
        metaInfoDto.SavedByGreenshotVersion = EnvironmentInfo.GetGreenshotVersion();
        metaInfoDto.CaptureSize = greenshotFile.Image != null ? $"{greenshotFile.Image.Width}x{greenshotFile.Image.Height}px" : string.Empty;

        using (var zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            SaveImages(dto, zipArchive);
            SaveMetadata(metaInfoDto, zipArchive);

            // Remove metadata from DTO before serializing content to avoid duplication
            dto.MetaInformation = null;

            var jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dto, V2Helper.GetJsonSerializerOptions()));

            var contentEntry = zipArchive.CreateEntry(ContentJsonName, CompressionLevel.Optimal);
            using var contentStream = contentEntry.Open();
            contentStream.Write(jsonBytes, 0, jsonBytes.Length);
        }

        return true;
    }

    private static void SaveMetadata(GreenshotFileMetaInformationDto metaInfoDto, ZipArchive zipArchive)
    {
        var jsonBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(metaInfoDto, V2Helper.GetJsonSerializerOptions()));

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

        foreach (var kvp in V2Helper.ExtractImages(dto, ref imageIndex))
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
            foreach (var kvp in V2Helper.ExtractImages(container, ref imageIndex))
            {
                imagesToSave[kvp.Key] = kvp.Value;
            }
        }

        return imagesToSave;
    }


    /// <summary>
    /// Loads a Greenshot V2 file from stream.
    /// </summary>
    internal static GreenshotFile LoadFromStream(Stream greenshotFileStream)
    {
        try
        {
            greenshotFileStream.Seek(0, SeekOrigin.Begin);
            using var zipArchive = new ZipArchive(greenshotFileStream, ZipArchiveMode.Read, true);

            var dto = LoadContent(zipArchive);

            LoadImages(dto, zipArchive);
            LoadMetadata(dto, zipArchive);

            var currentVersionDto = MigrateToCurrentVersion(dto);
            return ConvertDtoToDomain.ToDomain(currentVersionDto);
        }
        catch (Exception e)
        {
            Log.Error("Error deserializing Greenshot file (V2) from stream.", e);
            throw;
        }
    }

    private static GreenshotFileDto LoadContent(ZipArchive zipArchive)
    {
        var contentEntry = zipArchive.GetEntry(ContentJsonName);
        if (contentEntry == null)
        {
            throw new FileFormatException($"Content file '{ContentJsonName}' not found in the Greenshot file.");
        }
        using var contentStream = contentEntry.Open();

        GreenshotFileDto dto;
        try
        {
            dto = JsonSerializer.Deserialize<GreenshotFileDto>(contentStream, V2Helper.GetJsonSerializerOptions());
        }
        catch (JsonException e)
        {
            throw new FileFormatException("Failed to deserialize content.json into GreenshotFileDto.", e);
        }

        if (dto == null)
        {
            throw new FileFormatException("Failed to deserialize content.json into GreenshotFileDto. GreenshotFileDto is null.");
        }
        return dto;

    }

    private static void LoadMetadata(GreenshotFileDto dto, ZipArchive zipArchive)
    {
        if (dto is null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        var metadataEntry = zipArchive.GetEntry(MetadataJsonName);

        if (metadataEntry == null)
        {
            Log.Warn("Metadata file 'meta.json' not found, using default metadata.");
            if (dto.MetaInformation == null)
            {
                dto.MetaInformation = new GreenshotFileMetaInformationDto();
            }
            return;
        }

        try
        {
            using var metadataStream = metadataEntry.Open();
            var metaInfoDto = JsonSerializer.Deserialize<GreenshotFileMetaInformationDto>(metadataStream, V2Helper.GetJsonSerializerOptions());
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

    private static void LoadImages(GreenshotFileDto dto, ZipArchive zipArchive)
    {
        V2Helper.LoadImagesForDto(dto, zipArchive);

        var containerList = dto.ContainerList?.ContainerList;

        if (containerList is null)
        {
            return;
        }

        foreach (var container in containerList)
        {
            V2Helper.LoadImagesForDto(container, zipArchive);
        }
    }

    /// <summary>
    /// Main method for migrating an <see cref="GreenshotFileDto"/> to the current version.
    /// </summary>
    /// <remarks>Does nothing if the version is already current.</remarks>
    /// <param name="dto"></param>
    private static GreenshotFileDto MigrateToCurrentVersion(GreenshotFileDto dto)
    {
        var schemaVersion = dto.MetaInformation?.SchemaVersion ?? GreenshotFileVersionHandler.CurrentSchemaVersion;

        switch (schemaVersion)
        {
            case GreenshotFileVersionHandler.CurrentSchemaVersion:
                return dto; // is already at the current version
            case > GreenshotFileVersionHandler.CurrentSchemaVersion:
                Log.Warn($"Greenshot file schema version {schemaVersion} is newer than the current version {GreenshotFileVersionHandler.CurrentSchemaVersion}. No migration will be performed.");
                return dto; // no migration possible, just return the dto as is
            //case 1:
            // Uncomment the next line if the first migration is needed
            // return MigrateFromV1ToV2(dto); 
            default:
                return dto; // no migration needed, just return the dto as is
        }
    }

    /*
    // uncomment and implement if the first migration is needed

     private GreenshotFileDto MigrateFromV1ToV2(GreenshotFileDto dto)
     {
         // Chenge properties as needed for migration

         dto.SchemaVersion = 2;
         return dto;
     }
    */
}
