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

using System.Drawing;
using Greenshot.Editor.Drawing;

namespace Greenshot.Editor.FileFormat;

/// <summary>
/// Represents a .greenshot file as domain object.
/// </summary>
public sealed class GreenshotFile
{
    /// <summary>
    /// List of drawable containers that are positioned on the surface. These are graphical elements like text, shapes, and images that can be drawn on the surface.
    /// </summary>
    public DrawableContainerList ContainerList { get; set; } = new();

    /// <summary>
    /// The main image of the surface. It is captured from the screen or opend from a file.
    /// </summary>
    public Image Image { get; set; }

    /// <summary>
    /// The image rendered from the surface, which includes all drawable containers.
    /// </summary>
    public Image RenderedImage { get; set; }

    /// <summary>
    /// Metadata information about the file, including format version, schema version, and capture details.
    /// </summary>
    public GreenshotFileMetaInformation MetaInformation { get; set; } = new();
}