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
using Greenshot.Editor.FileFormat.Dto.Container;
using MessagePack;

namespace Greenshot.Editor.FileFormat.Dto;

/// <summary>
/// Is Data Transfer Object (DTO) for <see cref="GreenshotFile"/>
/// This represents the main class for a .greenshot file.
/// </summary>
[MessagePackObject]
public sealed class GreenshotFileDto
{
    /// <summary>
    /// <inheritdoc cref="GreenshotFile.MetaInformation"/>
    /// </summary>
    [Key(0)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GreenshotFileMetaInformationDto MetaInformation { get; set; } = new();

    /// <summary>
    /// <inheritdoc cref="GreenshotFile.Image"/>
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Key(11)]
    public byte[] Image { get; set; }

    /// <summary>
    /// <inheritdoc cref="GreenshotFile.RenderedImage"/>
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Key(13)]
    public byte[] RenderedImage { get; set; }

    /// <summary>
    /// Relative path to the image file within the archive.
    /// </summary>
    [Key(14)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string ImagePath { get; set; }

    /// <summary>
    /// Relative path to the rendered image file within the archive.
    /// </summary>
    [Key(15)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string RenderedImagePath { get; set; }

    /// <summary>
    /// <inheritdoc cref="GreenshotFile.ContainerList"/>
    /// </summary>
    [Key(12)]
    public DrawableContainerListDto ContainerList { get; set; } = new();

}
