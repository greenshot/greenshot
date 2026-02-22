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

namespace Greenshot.Editor.FileFormat;

/// <summary>
/// Contains metadata information about a .gst file.
/// </summary>
public sealed class GreenshotTemplateMetaInformation
{
    /// <summary>
    /// Indicates the version of the file format, which is used to determine the serializer and deserializer for the file.
    /// For now this is not really needed within the domain object, because you need to know the serializer before deserializing the file.
    /// The format version is part of the complete file version, so we include it here for completeness.
    /// May be in the future used to handle backward compatibility issues.
    /// </summary>
    public GreenshotFileVersionHandler.GreenshotFileFormatVersion FormatVersion { get; set; } = GreenshotFileVersionHandler.GreenshotFileFormatVersion.Unknown;

    /// <summary>
    /// Version of the file schema
    /// </summary>
    public int SchemaVersion { get; set; }

    /// <summary>
    /// The version of Greenshot that was used to save this file.
    /// </summary>
    public string SavedByGreenshotVersion { get; set; }

}
