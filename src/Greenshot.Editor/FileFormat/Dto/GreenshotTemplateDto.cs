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
using Greenshot.Editor.FileFormat.Dto.Container;

namespace Greenshot.Editor.FileFormat.Dto;

/// <summary>
/// Is Data Transfer Object (DTO) for <see cref="GreenshotTemplate"/>
/// This represents the main class for a .gst file.
/// </summary>
public sealed class GreenshotTemplateDto
{
    /// <summary>
    /// <inheritdoc cref="GreenshotTemplate.SchemaVersion"/>
    /// </summary>
    public int SchemaVersion { get; set; } = GreenshotFileVersionHandler.CurrentSchemaVersion;

    /// <summary>
    /// <inheritdoc cref="GreenshotTemplate.FormatVersion"/>
    /// </summary>
    public GreenshotFileVersionHandler.GreenshotFileFormatVersion FormatVersion { get; set; } = GreenshotFileVersionHandler.GreenshotFileFormatVersion.Unknown;

    /// <summary>
    /// <inheritdoc cref="GreenshotTemplate.ContainerList"/>
    /// </summary>
    public DrawableContainerListDto ContainerList { get; set; } = new();
}
