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
using System.Runtime.Serialization.Formatters.Binary;
using Greenshot.Editor.Drawing;
using Greenshot.Editor.FileFormat.Dto;
using Greenshot.Editor.FileFormat.Dto.Container;

namespace Greenshot.Editor.FileFormat.V1.Legacy;

public static class LegacyFileHelper
{
    /// <summary>
    /// Deserializes a legacy Greenshot file stream and converts it into a <see cref="DrawableContainerList"/> object.
    /// </summary>
    /// <remarks>This method processes a legacy Greenshot file by deserializing its contents to <see cref="LegacyDrawableContainerList"/>,
    /// convert this to <see cref="DrawableContainerListDto"/> and then to <see cref="DrawableContainerList"/>. The input stream must contain data in the expected legacy format.</remarks>
    /// <param name="stream">The input stream containing the serialized legacy Greenshot file data. Must not be <see langword="null"/>.</param>
    /// <returns>A <see cref="DrawableContainerList"/> representing the deserialized and converted data from the legacy file.</returns>
    /// <exception cref="ArgumentException">Thrown if the stream does not contain a valid legacy Greenshot file.</exception>
    public static DrawableContainerList GetContainerListFromLegacyContainerListStream(Stream stream)
    {
        // load file in legacy container classes
        BinaryFormatter binaryRead = new BinaryFormatter
        {
            Binder = new LegacySerializationBinder()
        };
        var loadedElements = binaryRead.Deserialize(stream);

        if (loadedElements is not LegacyDrawableContainerList legacyDrawableContainerList)
        {
            throw new ArgumentException("Stream is not a Greenshot file!");
        }

        // Convert the legacy data to DTO 
        var dto = ConvertLegacyToDto.ToDto(legacyDrawableContainerList);

        // and then to domain object
        var containerList = ConvertDtoToDomain.ToDomain(dto);

        return containerList;
    }

    /// <summary>
    /// This is more or less a helper method for develop. It a shorter way to load a ContainerList from a legacy Greenshot file instead of using the <see cref="GreenshotFileVersionHandler"/>.
    /// It extracts the container list sub stream from the Greenshot file stream and call <see cref="GetContainerListFromLegacyContainerListStream(Stream)"/>.
    /// </summary>
    public static DrawableContainerList GetContainerListFromGreenshotfile(Stream stream)
    {
        // Start at -14 read "GreenshotXX.YY" (XX=Major, YY=Minor)
        const int markerSize = 14;
        stream.Seek(-markerSize, SeekOrigin.End);
        using StreamReader streamReader = new StreamReader(stream);

        var greenshotMarker = streamReader.ReadToEnd();
        if (!greenshotMarker.StartsWith("Greenshot"))
        {
            throw new ArgumentException("Stream is not a Greenshot file!");
        }

        const int filesizeLocation = 8 + markerSize;
        stream.Seek(-filesizeLocation, SeekOrigin.End);
        using BinaryReader reader = new BinaryReader(stream);
        long bytesWritten = reader.ReadInt64();
        stream.Seek(-(bytesWritten + filesizeLocation), SeekOrigin.End);

        return GetContainerListFromLegacyContainerListStream(stream);
    }
}