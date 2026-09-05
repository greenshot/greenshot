/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Text.Json.Serialization;
using Greenshot.Editor.Drawing;

namespace Greenshot.Editor.FileFormat.Dto.Container;

/// <summary>
/// Data transfer object to serialize <see cref="MetafileContainer"/> objects.
/// Simplified version that supports properties from <see cref="VectorGraphicsContainer"/> as well.
/// </summary>
public sealed class MetafileContainerDto : DrawableContainerDto
{
    /// <summary>
    /// Main Metafile data for this container.<br/>
    /// <inheritdoc cref="MetafileContainer.Metafile"/>
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [GreenshotImageData(nameof(MetafilePath), extensionPropertyName: nameof(MetafileDataExtension))]
    public byte[] MetafileData { get; set; }

    /// <summary>
    /// Extension of the main Metafile data, without dot, e.g. "emf" or "wmf". Is used to determine the file extension for the image file in the zip archive during serialization.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string MetafileDataExtension { get; set; }

    /// <summary>
    /// Relative path to the Metafile within the archive. Is defined during serialization and used while deserialization.
    /// </summary>
    [GreenshotImagePath(nameof(MetafileData))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string MetafilePath { get; set; }

    public int RotationAngle { get; set; }
}

