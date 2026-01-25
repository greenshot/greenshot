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
using System.Text.Json.Serialization;
using Greenshot.Editor.Drawing;
using MessagePack;

namespace Greenshot.Editor.FileFormat.Dto.Container;

/// <summary>
/// Data transfer object to serialize <see cref="ImageContainer"/> objects.
/// </summary>
[MessagePackObject]
public sealed class ImageContainerDto : DrawableContainerDto
{
    [Key(100)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [GreenshotImageData(nameof(ImagePath), extensionPropertyName: nameof(ImageExtension))]
    public byte[] Image { get; set; } // Store image as byte array

    /// <summary>
    /// Extension of the main image payload (e.g., png, jpg).
    /// </summary>
    [Key(16)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string ImageExtension { get; set; }

    /// <summary>
    /// Relative path to the image file within the archive.
    /// </summary>
    [GreenshotImagePath(nameof(Image))]
    [Key(14)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string ImagePath { get; set; }
}
