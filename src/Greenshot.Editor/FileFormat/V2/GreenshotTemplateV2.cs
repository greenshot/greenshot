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
using System.IO;
using System.Text;
using Greenshot.Editor.FileFormat.Dto;
using log4net;
using MessagePack;
using static Greenshot.Editor.FileFormat.GreenshotFileVersionHandler;

namespace Greenshot.Editor.FileFormat.V2;

/// <summary>
/// Provides methods for loading and saving Greenshot template file format version V2.
/// </summary>
internal static class GreenshotTemplateV2
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(GreenshotTemplateV2));

    /// <summary>
    /// Represents the marker string used to identify the file format as specific to Greenshot templates.
    /// </summary>
    private const string FileFormatMarker = "GreenshotTemplate";

    /// <summary>
    /// Represents the file format version used for Greenshot files, formatted as a two-digit string.
    /// </summary>
    /// <remarks> Thie ist fixed to <see cref="GreenshotFileFormatVersion.V2"/> ("02") .</remarks>
    private static readonly string FileFormatVersion = ((int)GreenshotFileFormatVersion.V2).ToString("D2");

    /// <summary>
    /// Represents the current schema version of the Greenshot file in a two-digit string format.
    /// </summary>
    /// <remarks> It is derived from the <see cref="GreenshotFileVersionHandler.CurrentSchemaVersion"/> property.</remarks>
    private static readonly string SchemaVersion = CurrentSchemaVersion.ToString("D2");

    /// <summary>
    /// Represents the complete version string, combining the file format version and schema version.
    /// </summary>
    /// <remarks>This value is a concatenation of <see cref="FileFormatVersion"/> and <see
    /// cref="SchemaVersion"/>,  separated by a period ("."), e.g. "02.01" for version 2.1.</remarks>
    private static readonly string CompleteVersion = FileFormatVersion + "." + SchemaVersion;

    /// <summary>
    /// Determines whether the stream matches the Greenshot file format version V2.
    /// </summary>
    /// <remarks>The stream's position will be modified during the operation but will remain open after the method completes.</remarks>
    /// <param name="greenshotTemplateFileStream">The stream containing the file to check. The stream must support seeking.</param>
    /// <returns><see langword="true"/> if the file format matches the Greenshot version V2; otherwise, <see langword="false"/>.</returns>
    internal static bool DoesFileFormatMatch(Stream greenshotTemplateFileStream)
    {
        //reset position
        greenshotTemplateFileStream.Seek(0, SeekOrigin.Begin);

        // set leaveOpen to prevent the automatic closing of the file stream
        using var streamReader = new StreamReader(greenshotTemplateFileStream, Encoding.ASCII, false, 1024, true);

        // file should start with the marker text and format version. Schema version is not relevant for the file format check.
        string expectedfileFormatMarker = FileFormatMarker + FileFormatVersion;

        var markerInFile = new char[expectedfileFormatMarker.Length];
        streamReader.Read(markerInFile, 0, expectedfileFormatMarker.Length);
        var markerText = new string(markerInFile);

        var foundMarkerV02 = expectedfileFormatMarker.Equals(markerText);

        if (foundMarkerV02)
        {
            Log.InfoFormat("Greenshot file format: {0}", markerText);
            return true;
        }

        return false;
    }

    /// <summary>
    /// This load function supports file format version V2.
    /// </summary>
    /// <param name="greenshotTemplateFileStream">A <see cref="Stream"/> containing the Greenshot template data.</param>
    /// <returns>The loaded Greenshot template.</returns>
    internal static GreenshotTemplate LoadFromStream(Stream greenshotTemplateFileStream)
    {
        GreenshotTemplate returnGreenshotTemplate;
        try
        {
            // ignore marker and use offset 
            var completeMarker = FileFormatMarker + CompleteVersion;

            greenshotTemplateFileStream.Seek(completeMarker.Length, SeekOrigin.Begin);
            using var ms = new MemoryStream();
            greenshotTemplateFileStream.CopyTo(ms);
            var allBytes = ms.ToArray();

            returnGreenshotTemplate = Deserialize(allBytes);
        }
        catch (Exception e)
        {
            Log.Error("Error deserializing Greenshot template from stream.", e);
            throw;
        }

        return returnGreenshotTemplate;
    }

    /// <summary>
    /// Saves the <see cref="GreenshotTemplate"/> to the provided stream in the Greenshot template format.
    /// </summary>
    /// <remarks>This ignores the file format version and schema version in the GreenshotTemplate instance.
    /// File format version is always V2, schema version is always the current schema version.<br/>
    ///  The file parts are:<br/>
    ///  1. 19 bytes for the Greenshot marker string <see cref="FileFormatMarker"/> + <see cref="FileFormatVersion"/> (i.e. `"GreenshotTemplate02"`).<br/>
    ///  2. 3 bytes for the Greenshot file schema version <see cref="GreenshotFileVersionHandler.CurrentSchemaVersion"/> (i.e. `".01"`).<br/>
    ///  3. The binary data of the <see cref="GreenshotTemplateDto"/>. (serialized with MessagePack).<br/>
    /// </remarks>
    internal static bool SaveToStream(GreenshotTemplate greenshotTemplate, Stream stream)
    {
        // 1./2. file part - Greenshot template Marker and version information
        // writes constant marker and complete version information to the stream
        var headerBytes = Encoding.ASCII.GetBytes(FileFormatMarker);
        stream.Write(headerBytes, 0, headerBytes.Length);

        var versionBytes = Encoding.ASCII.GetBytes(CompleteVersion);
        stream.Write(versionBytes, 0, versionBytes.Length);

        //3. file part - Greenshot template data
        // writes the serialized GreenshotTemplate to the stream
        byte[] templateFileBytes = Serialize(greenshotTemplate);
        stream.Write(templateFileBytes, 0, templateFileBytes.Length);

        return true;
    }

    /// <summary>
    /// Serializes the specified <see cref="GreenshotTemplate"/> instance into a byte array by using MessagePackSerializer.
    /// </summary>
    /// <param name="data">The <see cref="GreenshotTemplate"/> instance to serialize. Cannot be <see langword="null"/>.</param>
    /// <returns>A byte array representing the serialized form of the <see cref="GreenshotTemplate"/> instance.</returns>
    private static byte[] Serialize(GreenshotTemplate data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data), "Cannot serialize a null GreenshotTemplate.");
        }

        var dto = ConvertDomainToDto.ToDto(data);
        return MessagePackSerializer.Serialize(dto);
    }

    /// <summary>
    /// Deserializes a byte array into an <see cref="GreenshotTemplate"/> object by using MessagePackSerializer.
    /// </summary>
    /// <param name="bytes">The byte array containing the serialized data of an <see cref="GreenshotTemplate"/>.</param>
    /// <returns>An <see cref="GreenshotTemplate"/> object deserialized from the provided byte array.</returns>
    private static GreenshotTemplate Deserialize(byte[] bytes)
    {
        var dto = MessagePackSerializer.Deserialize<GreenshotTemplateDto>(bytes);
        var currentVersionDto = MigrateToCurrentVersion(dto);
        return ConvertDtoToDomain.ToDomain(currentVersionDto);
    }

    /// <summary>
    /// Main method for migrating an <see cref="GreenshotTemplateDto"/> to the current version.
    /// </summary>
    /// <remarks>Does nothing if the version is already current.</remarks>
    /// <param name="dto"></param>
    private static GreenshotTemplateDto MigrateToCurrentVersion(GreenshotTemplateDto dto)
    {
        switch (dto.SchemaVersion)
        {
            case CurrentSchemaVersion:
                return dto; // is already at the current version
            case > CurrentSchemaVersion:
                Log.Warn($"Greenshot template schema version {dto.SchemaVersion} is newer than the current version {CurrentSchemaVersion}. No migration will be performed.");
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

     private GreenshotTemplateDto MigrateFromV1ToV2(GreenshotTemplateDto dto)
     {
         // Chenge properties as needed for migration

         dto.SchemaVersion = 2;
         return dto;
     }
    */
}