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
using System.Text.Json.Serialization;

namespace Greenshot.Editor.FileFormat.Dto;

/// <summary>
/// Data transfer object for <see cref="GreenshotFileMetaInformation"/>.
/// </summary>
public sealed class GreenshotFileMetaInformationDto
{
    /// <summary>
    /// Static file type for .greenshot files. Only for serialization, so users who opens the JSON file directly can see the file type in the JSON content.
    /// </summary>
    public GreenshotFileVersionHandler.GreenshotFileType FileType { get; } = GreenshotFileVersionHandler.GreenshotFileType.GreenshotFile;

    /// <inheritdoc cref="GreenshotFileMetaInformation.FormatVersion"/>
    public GreenshotFileVersionHandler.GreenshotFileFormatVersion FormatVersion { get; set; } = GreenshotFileVersionHandler.GreenshotFileFormatVersion.Unknown;

    /// <inheritdoc cref="GreenshotFileMetaInformation.SchemaVersion"/>
    public int SchemaVersion { get; set; } = GreenshotFileVersionHandler.CurrentSchemaVersion;

    /// <inheritdoc cref="GreenshotFileMetaInformation.SavedByGreenshotVersion"/>
    public string SavedByGreenshotVersion { get; set; }

    /// <inheritdoc cref="GreenshotFileMetaInformation.CaptureDate"/>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public DateTime CaptureDate { get; set; }

    /// <inheritdoc cref="GreenshotFileMetaInformation.CaptureSize"/>
    //TODO: ignore for deserialization, because this should be calculated on load
    public string CaptureSize { get; set; }
}
