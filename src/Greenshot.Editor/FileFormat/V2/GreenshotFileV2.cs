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
using System.Drawing.Imaging;
using System.IO;
using System.Text;
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

    /// <summary>
    /// Represents the marker string used to identify the file format as specific to Greenshot file.
    /// </summary>
    private const string FileFormatMarker = "Greenshot";

    /// <summary>
    /// Represents the file format version used for Greenshot files, formatted as a two-digit string.
    /// </summary>
    /// <remarks> Thie ist fixed to <see cref="GreenshotFileVersionHandler.GreenshotFileFormatVersion.V2"/> ("02") .</remarks>
    private static readonly string FileFormatVersion = ((int)GreenshotFileVersionHandler.GreenshotFileFormatVersion.V2).ToString("D2");

    /// <summary>
    /// Represents the current schema version of the Greenshot file in a two-digit string format.
    /// </summary>
    /// <remarks> It is derived from the <see cref="GreenshotFileVersionHandler.CurrentSchemaVersion"/> property.</remarks>
    private static readonly string SchemaVersion = GreenshotFileVersionHandler.CurrentSchemaVersion.ToString("D2");

    /// <summary>
    /// Represents the complete version string, combining the file format version and schema version.
    /// </summary>
    /// <remarks>This value is a concatenation of <see cref="FileFormatVersion"/> and <see
    /// cref="SchemaVersion"/>,  separated by a period ("."), e.g. "02.01" for version 2.1.</remarks>
    private static readonly string CompleteVersion = FileFormatVersion + Dot + SchemaVersion;

    private const string Dot = ".";

    /// <summary>
    /// Determines whether the stream matches the Greenshot file format version V2.
    /// </summary>
    /// <remarks>It checks for specific markers <see cref="FileFormatMarker"/> + <see cref="FileFormatVersion"/> at the end of the file and validates the size of surface / container list data. 
    /// The stream's position will be modified during the operation but will remain open after the method completes.</remarks>
    /// <param name="greenshotFileStream">The stream containing the file to check. The stream must support seeking.</param>
    /// <returns><see langword="true"/> if the file format matches the Greenshot version V2; otherwise, <see langword="false"/>.</returns>
    internal static bool DoesFileFormatMatch(Stream greenshotFileStream)
    {
        // file should end with the marker text and format version. 
        var expectedfileFormatMarker = FileFormatMarker + FileFormatVersion;

        // amount of bytes to ignore at the end of the file
        var schemaVersionSuffixLength = Dot.Length + SchemaVersion.Length;

        var markerPosition = expectedfileFormatMarker.Length + schemaVersionSuffixLength;
        greenshotFileStream.Seek(-markerPosition, SeekOrigin.End);

        // set leaveOpen to prevent the automatic closing of the file stream
        using var streamReader = new StreamReader(greenshotFileStream, Encoding.ASCII, false, 1024, true);
        var greenshotMarker = streamReader.ReadToEnd();

        // Schema version is not relevant for the file format check.
        greenshotMarker = greenshotMarker.Substring(0, expectedfileFormatMarker.Length);

        // only these two known marker are allowed 
        var foundMarkerV02 = expectedfileFormatMarker.Equals(greenshotMarker);

        // Load 8 byte in front of marker for file size 
        var fileSizeLocation = 8 + markerPosition;
        greenshotFileStream.Seek(-fileSizeLocation, SeekOrigin.End);

        // set leaveOpen to prevent the automatic closing of the file stream
        using var reader = new BinaryReader(greenshotFileStream, Encoding.ASCII, true);
        var bytesWritten = reader.ReadInt64();

        if (foundMarkerV02 && bytesWritten > 0)
        {
            Log.InfoFormat("Greenshot file format: {0}", greenshotMarker);
            return true;
        }

        return false;

    }

    /// <summary>
    /// Saves the specified <see cref="GreenshotFile"/> to the provided stream in the Greenshot file format version V2.
    /// </summary>
    /// <remarks>This ignores the file format version and schema version in the GreenshotFile instance.
    /// File format version is always V2, schema version is always the current schema version.<br/>
    ///  The file parts are:<br/>
    ///  1. The PNG image data<br/>
    ///  2. The binary data of the <see cref="GreenshotFileDto"/>. (serialized with MessagePack).<br/>
    ///  3. 8 bytes for the binary data size(Int64).<br/>
    ///  4. 11 bytes for the Greenshot marker string <see cref="FileFormatMarker"/> + <see cref="FileFormatVersion"/> (i.e. `"Greenshot02"`).<br/>
    ///  5. 3 bytes for the Greenshot file schema version <see cref="GreenshotFileVersionHandler.CurrentSchemaVersion"/> (i.e. `".01"`).<br/>
    /// </remarks>
    internal static bool SaveToStream(GreenshotFile greenshotFile, Stream stream)
    {
        // 1. file part - PNG
        // Use the rendered image for the PNG part if available, otherwise use the surface image.
        var mainImage = greenshotFile.RenderedImage ?? greenshotFile.Image;
        if (mainImage == null)
        {
            Log.Error("Cannot save Greenshot file, no image found!");
            return false;
        }
        mainImage.Save(stream, ImageFormat.Png);

        greenshotFile.RenderedImage = null; // clear the rendered image it is not nessary to serialize it 

        // 2. file part - Greenshot file data 
        byte[] appFileBytes = Serialize(greenshotFile);
        stream.Write(appFileBytes, 0, appFileBytes.Length);

        // 3. file part - Greenshot file data length
        var lengthBytes = BitConverter.GetBytes((long)appFileBytes.Length);
        stream.Write(lengthBytes, 0, lengthBytes.Length);

        // 4./5. file part - Greenshot file Marker and version information
        // writes constant marker and complete version information to the stream
        var headerBytes = Encoding.ASCII.GetBytes(FileFormatMarker);
        stream.Write(headerBytes, 0, headerBytes.Length);

        var versionBytes = Encoding.ASCII.GetBytes(CompleteVersion);
        stream.Write(versionBytes, 0, versionBytes.Length);

        return true;
    }

    /// <summary>
    /// This load function supports file version 02
    /// </summary>
    /// <param name="greenshotFileStream">A <see cref="Stream"/> containing the Greenshot file data.</param>
    /// <returns>The loaded Greenshot file.</returns>
    internal static GreenshotFile LoadFromStream(Stream greenshotFileStream)
    {
        GreenshotFile returnGreenshotFile;

        try
        {
            // constant length of file parts at the end of the file
            var greenshotMarkerAndVersionLength = FileFormatMarker.Length + CompleteVersion.Length;
            var lengthOfTypeLong = 8;

            var fileSizeLocation = lengthOfTypeLong + greenshotMarkerAndVersionLength;
            greenshotFileStream.Seek(-fileSizeLocation, SeekOrigin.End);

            // set leaveOpen to prevent the automatic closing of the file stream
            using var reader = new BinaryReader(greenshotFileStream, Encoding.ASCII, true);
            var bytesWritten = reader.ReadInt64();

            // go to start of the greenshot file data
            var dataStartPosition = fileSizeLocation + bytesWritten;
            greenshotFileStream.Seek(-dataStartPosition, SeekOrigin.End);

            // extract the Greenshot file data
            var allBytes = new byte[bytesWritten];
            var bytesRead = greenshotFileStream.Read(allBytes, 0, (int)bytesWritten);

            // double check if we read all bytes
            if (bytesRead < bytesWritten)
            {
                throw new EndOfStreamException("Unexpected end of the Greenshot file data!");
            }

            returnGreenshotFile = Deserialize(allBytes);
        }
        catch (Exception e)
        {
            Log.Error("Error deserializing Greenshot file from stream.", e);
            throw;
        }

        return returnGreenshotFile;
    }

    /// <summary>
    /// Serializes the specified <see cref="GreenshotFile"/> instance into a byte array by using MessagePackSerializer.
    /// </summary>
    /// <param name="data">The <see cref="GreenshotFile"/> instance to serialize. Cannot be <see langword="null"/>.</param>
    /// <returns>A byte array representing the serialized form of the <see cref="GreenshotFile"/> instance.</returns>
    private static byte[] Serialize(GreenshotFile data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data), "Cannot serialize a null GreenshotFile.");
        }

        var dto = ConvertDomainToDto.ToDto(data);
        //return MessagePackSerializer.Serialize(dto);

        throw new System.Exception("DTO serialization is disabled temporarily - to be fixed later ");
    }

    /// <summary>
    /// Deserializes a byte array into an <see cref="GreenshotFile"/> object by using MessagePackSerializer.
    /// </summary>
    /// <remarks> This also migrates the loaded <see cref="GreenshotFileDto"/> to the current version if necessary.
    /// </remarks>
    /// <param name="bytes">The byte array containing the serialized data of an <see cref="GreenshotFile"/>.</param>
    /// <returns>An <see cref="GreenshotFile"/> object deserialized from the provided byte array.</returns>
    private static GreenshotFile Deserialize(byte[] bytes)
    {
        var dto = new GreenshotFileDto(); // MessagePackSerializer.Deserialize<GreenshotFileDto>(bytes); 
        throw new System.Exception("DTO serialization is disabled temporarily - to be fixed later ");
        var currentVersionDto = MigrateToCurrentVersion(dto);
        return ConvertDtoToDomain.ToDomain(currentVersionDto);
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