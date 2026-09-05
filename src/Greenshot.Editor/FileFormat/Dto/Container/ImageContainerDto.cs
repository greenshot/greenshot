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
/// Data transfer object to serialize <see cref="ImageContainer"/> objects.
/// </summary>
public sealed class ImageContainerDto : DrawableContainerDto
{
    /// <summary>
    /// Main image data for this container.<br/>
    /// <inheritdoc cref="ImageContainer.Image"/>
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [GreenshotImageData(nameof(ImagePath), extensionPropertyName: nameof(ImageExtension))]
    public byte[] Image { get; set; }

    /// <summary>
    /// backing field for <see cref="ImageExtension"/> to ensure that the value is stored in lowercase.
    /// </summary>
    private string _imageExtension;

    /// <summary>
    /// Extension of the main image payload (e.g., png, jpg). Is used to determine the file extension for the image file in the zip archive during serialization.
    /// </summary>
    /// <remarks>
    /// The value is automatically converted to lowercase to ensure consistency.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string ImageExtension 
    { 
        get => _imageExtension; 
        set => _imageExtension = value?.ToLowerInvariant(); 
    }

    /// <summary>
    /// Relative path to the image file within the archive. Is defined during serialization and used while deserialization.
    /// </summary>
    [GreenshotImagePath(nameof(Image))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string ImagePath { get; set; }
}
