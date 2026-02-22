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
using System.Text.Json;
using Greenshot.Base.Core;
using Greenshot.Editor.FileFormat.Dto;
using Greenshot.Editor.FileFormat.Dto.Container;
using log4net;

namespace Greenshot.Editor.FileFormat.V2;

/// <summary>
/// Provides methods for loading and saving Greenshot file format version V2.
/// </summary>
internal static class GreenshotTemplateV2
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(GreenshotTemplateV2));

    private const string ContentJsonName = "content.json";
    private const string MetadataJsonName = "meta.json";

    /// <summary>
    /// Checks if the provided stream matches the Greenshot template file format version V2.
    /// </summary>
    /// <remarks>
    /// This method only checks the format version in the metadata file within the ZIP archive and does not deserialize the full DTO.
    /// </remarks>
    /// <param name="greenshotFileStream">The stream containing the Greenshot template file.</param>
    /// <returns>True if the stream matches the V2 format; otherwise, false.</returns>
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

            var formatVersionPropertyName = nameof(GreenshotTemplateMetaInformationDto.FormatVersion);

            // We only check the format version, do not deserialize the full DTO.
            return V2Helper.GetFormatVersion(document.RootElement, formatVersionPropertyName) == GreenshotFileVersionHandler.GreenshotFileFormatVersion.V2;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Saves the specified <see cref="GreenshotTemplate"/> to the provided stream in the Greenshot file format version V2.
    /// </summary>
    /// <remarks>
    /// V2 stores the domain model as JSON and intentionally does not store the image payload in JSON.<br/>
    /// The stream contains a ZIP archive with entries:<br/>
    /// - meta.json: Contains file metadata (format version, schema version, capture date, etc.) <see cref="GreenshotTemplate.MetaInformation"/><br/>
    /// - content.json: containers <br/>
    /// - different subfolder: Contains image files<br/>
    /// ../../FileFormat/readme.md for more information<br/>
    /// The corresponding loading method is <see cref="LoadFromStream"/>
    /// </remarks>
    internal static bool SaveToStream(GreenshotTemplate greenshotTemplate, Stream stream)
    {
        if (greenshotTemplate == null)
        {
            throw new ArgumentNullException(nameof(greenshotTemplate));
        }

        if (!(greenshotTemplate?.ContainerList?.Count > 0))
        {
            throw new ArgumentException("GreenshotTemplate must contain at least one container to be saved.", nameof(greenshotTemplate));
        }

        if (stream.CanSeek)
        {
            // Clear the stream before writing to ensure no leftover data (e.g overwriting a larger file with a smaller one)
            // only a safeguard, File.Create() should already handle this by truncating the old file
            stream.SetLength(0);
        }

        var dto = ConvertDomainToDto.ToDto(greenshotTemplate);

        // maybe the DTO already contains some meta information, if not create a new one
        var metaInfoDto = dto.MetaInformation ??= new GreenshotTemplateMetaInformationDto();

        // Remove metadata from DTO before serializing content to avoid duplication, because meta information is stored in a separate file in V2
        dto.MetaInformation = null;

        // ensure meta information is set correctly for the current version
        metaInfoDto.FormatVersion = GreenshotFileVersionHandler.GreenshotFileFormatVersion.V2;
        metaInfoDto.SchemaVersion = GreenshotFileVersionHandler.CurrentSchemaVersion;
        metaInfoDto.SavedByGreenshotVersion = EnvironmentInfo.GetGreenshotVersion();

        using var zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, true);

        // extract images from the DTO and save them to the zip archive
        // update the DTO by removing the image data and setting the image paths to the corresponding entry paths in the zip archive
        SaveImages(dto, zipArchive);

        // serialize the meta information in "meta.json" in the zip archive
        SaveMetadata(metaInfoDto, zipArchive);

        // serialize the content in "content.json" in the zip archive
        SaveContent(dto, zipArchive);

        return true;
    }

    /// <summary>
    /// Serialize the <see cref="GreenshotTemplateDto"/> to JSON and saves it as "content.json" <see cref="ContentJsonName"/> in the provided <see cref="ZipArchive"/>"
    /// </summary>
    /// <param name="dto"></param>
    /// <param name="zipArchive"></param>
    private static void SaveContent(GreenshotTemplateDto dto, ZipArchive zipArchive)
    {
        var contentEntry = zipArchive.CreateEntry(ContentJsonName, CompressionLevel.Optimal);
        using var entryStream = contentEntry.Open();

        JsonSerializer.Serialize(
            entryStream,
            dto,
            V2Helper.DefaultJsonSerializerOptions
        );
    }

    /// <summary>
    /// Serializes the <see cref="GreenshotTemplateMetaInformationDto"/> to JSON and saves it as "meta.json"  <see cref="MetadataJsonName"/> in the provided <see cref="ZipArchive"/>"
    /// </summary>
    /// <param name="metaInfoDto"></param>
    /// <param name="zipArchive"></param>
    private static void SaveMetadata(GreenshotTemplateMetaInformationDto metaInfoDto, ZipArchive zipArchive)
    {
        var metadataEntry = zipArchive.CreateEntry(MetadataJsonName, CompressionLevel.Optimal);
        using var metadataStream = metadataEntry.Open();

        JsonSerializer.Serialize(
            metadataStream,
            metaInfoDto,
            V2Helper.DefaultJsonSerializerOptions
        );
    }

    /// <summary>
    /// Saves all images extracted from the specified Greenshot file Dto to the provided zip archive as individual entries.
    /// </summary>
    /// <remarks>
    /// For each image wich is extracted, the DTO is changed by removing the image data and setting the image path to the path of the corresponding entry in the zip archive <see cref="V2Helper.ExtractImages"/>.
    /// </remarks>
    /// <param name="dto">The GreenshotFileDto instance containing the images to be saved.</param>
    /// <param name="zipArchive">The ZipArchive to which the extracted images will be added as entries. The archive must be open and writable.</param>
    private static void SaveImages(GreenshotTemplateDto dto, ZipArchive zipArchive)
    {
        var imagesToSave = ExtractAllImages(dto);

        foreach (var kvp in imagesToSave)
        {
            var entry = zipArchive.CreateEntry(kvp.Key, CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            entryStream.Write(kvp.Value, 0, kvp.Value.Length);
        }
    }

    /// <summary>
    /// Calls <see cref="V2Helper.ExtractImages"/> for all <see cref="DrawableContainerDto"/>.
    /// </summary>
    /// <param name="dto">The GreenshotTemplateDto instance containing the images to be extracted.</param>
    /// <returns>A dictionary containing the extracted images, where the key is the image path and the value is the image data.</returns>
    private static Dictionary<string, byte[]> ExtractAllImages(GreenshotTemplateDto dto)
    {
        var imageIndex = 1;
        var imagesToSave = new Dictionary<string, byte[]>();

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
    /// Loads a Greenshot template file (Zip) in the Greenshot file format version V2 from the provided stream and deserializes it into a <see cref="GreenshotTemplate"/> domain model instance.
    /// </summary>
    /// <remarks>
    /// The corresponding saving method is <see cref="SaveToStream"/>.
    /// </remarks>
    /// <param name="greenshotFileStream">The stream containing the Greenshot template data.</param>
    /// <returns>A <see cref="GreenshotTemplate"/> instance representing the deserialized Greenshot template.</returns>
    internal static GreenshotTemplate LoadFromStream(Stream greenshotFileStream)
    {
        try
        {
            greenshotFileStream.Seek(0, SeekOrigin.Begin);
            using var zipArchive = new ZipArchive(greenshotFileStream, ZipArchiveMode.Read, true);

            var dto = LoadContent(zipArchive);

            LoadImages(dto, zipArchive);
            LoadMetadata(dto, zipArchive);

            var currentVersionDto = MigrateToCurrentVersion(dto);
            var greenshotTemplate = ConvertDtoToDomain.ToDomain(currentVersionDto);
            return greenshotTemplate;
        }
        catch (Exception e)
        {
            Log.Error("Error deserializing Greenshot file (V2) from stream.", e);
            throw;
        }
    }

    /// <summary>
    /// Deserializes the content "content.json" <see cref="ContentJsonName"/> of a Greenshot template file (Zip) into a <see cref="GreenshotTemplateDto"/> instance.
    /// </summary>
    /// <param name="zipArchive">The <see cref="ZipArchive"/> containing the Greenshot template data.</param>
    /// <returns>A <see cref="GreenshotTemplateDto"/> instance representing the deserialized content.</returns>
    /// <exception cref="FileFormatException"></exception>
    private static GreenshotTemplateDto LoadContent(ZipArchive zipArchive)
    {
        var contentEntry = zipArchive.GetEntry(ContentJsonName);
        if (contentEntry == null)
        {
            throw new FileFormatException($"Content file '{ContentJsonName}' not found in the Greenshot file.");
        }
        using var contentStream = contentEntry.Open();

        GreenshotTemplateDto dto;
        try
        {
            dto = JsonSerializer.Deserialize<GreenshotTemplateDto>(contentStream, V2Helper.DefaultJsonSerializerOptions);
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

    /// <summary>
    /// Deserializes the metadata "meta.json" <see cref="MetadataJsonName"/> of a Greenshot template file (Zip) and assigns it to the provided <see cref="GreenshotTemplateDto"/> instance.
    /// </summary>
    /// <remarks>If the metadata file 'meta.json' is not found or an error occurs during loading, default
    /// metadata will be assigned to the GreenshotTemplateDto object.</remarks>
    /// <param name="dto">The GreenshotTemplateDto instance that will receive the loaded metadata. Cannot be null.</param>
    /// <param name="zipArchive">The ZipArchive containing the metadata file named 'meta.json'.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="dto"/> is null.</exception>
    private static void LoadMetadata(GreenshotTemplateDto dto, ZipArchive zipArchive)
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
                dto.MetaInformation = new GreenshotTemplateMetaInformationDto();
            }
            return;
        }

        try
        {
            using var metadataStream = metadataEntry.Open();
            var metaInfoDto = JsonSerializer.Deserialize<GreenshotTemplateMetaInformationDto>(metadataStream, V2Helper.DefaultJsonSerializerOptions);
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
                dto.MetaInformation = new GreenshotTemplateMetaInformationDto();
            }
        }

    }


    /// <summary>
    /// Calls <see cref="V2Helper.LoadImagesForDto"/> for the <see cref="GreenshotTemplateDto"/> and for all <see cref="DrawableContainerDto"/> to load the images from the provided <see cref="ZipArchive"/> and assign the image data to the corresponding properties in the DTOs.
    /// </summary>
    /// <param name="dto">The GreenshotTemplateDto instance containing the images to be loaded.</param>
    /// <param name="zipArchive">The ZipArchive containing the image files.</param>
    private static void LoadImages(GreenshotTemplateDto dto, ZipArchive zipArchive)
    {
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
    /// Main method for migrating an <see cref="GreenshotTemplateDto"/> to the current version.
    /// </summary>
    /// <remarks>Does nothing if the version is already current.</remarks>
    /// <param name="dto"></param>
    private static GreenshotTemplateDto MigrateToCurrentVersion(GreenshotTemplateDto dto)
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
