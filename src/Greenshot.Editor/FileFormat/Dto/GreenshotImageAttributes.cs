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
using System;

namespace Greenshot.Editor.FileFormat.Dto;

/// <summary>
/// This attribute on an image data property indicates that the property holds image data that should be stored as a separate file 
/// in the zip archive when serializing to a .gsa file, and loaded from a separate file in the zip archive when deserializing from a .gsa file.
/// </summary> 
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
internal sealed class GreenshotImageDataAttribute : Attribute
{
    public GreenshotImageDataAttribute(string pathPropertyName, string staticExtension = null, string extensionPropertyName = null, string targetZipFolder = null, string staticFilename = null)
    {
        if (string.IsNullOrEmpty(pathPropertyName))
        {
            throw new ArgumentException("Path property name must be specified.", nameof(pathPropertyName));
        }

        if (string.IsNullOrEmpty(staticExtension) == string.IsNullOrEmpty(extensionPropertyName))
        {
            throw new ArgumentException("Specify either a static extension or an extension property name, but not both or none.", nameof(staticExtension));
        }

        PathPropertyName = pathPropertyName;
        StaticExtension = staticExtension;
        ExtensionPropertyName = extensionPropertyName;
        TargetZipFolder = targetZipFolder;
        StaticFilename = staticFilename;
    }
    /// <summary>
    /// Property name of the property that holds the relative path to the image file within the archive. 
    /// This is set during serialization to a zip file.
    /// During deserialization, this is used to determine from which path in the zip archive to load the image data.
    /// The property with this name must be decorated with <see cref="GreenshotImagePathAttribute"/> and link back to the image data property decorated with this attribute.
    /// </summary>
    public string PathPropertyName { get; }

    /// <summary>
    /// Defines the static file extension (without dot) to use for the image file in the zip archive.
    /// If not set, the file extension will be determined from the property specified in <see cref="ExtensionPropertyName"/>.
    /// </summary>
    public string StaticExtension { get; }

    /// <summary>
    /// Defines the property name that holds the file extension (without dot) to use for the image file in the zip archive.
    /// </summary>
    public string ExtensionPropertyName { get; }

    /// <summary>
    /// Defines the target folder within the zip archive where the image file should be stored. If not set, the image file will be stored <see cref="V2.V2Helper.DefaultImageFolder"/> ("images")
    /// </summary>
    public string TargetZipFolder { get; }

    /// <summary>
    /// Defines the static filename (without extension) to use for the image file in the zip archive. If not set, a unique filename will be generated during serialization.
    /// </summary>
    public string StaticFilename { get; }
}

/// <summary>
/// This attribute on a property indicates that the property holds the relative path to the image file within the archive for an image data property decorated with <see cref="GreenshotImageDataAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
internal sealed class GreenshotImagePathAttribute : Attribute
{
    public GreenshotImagePathAttribute(string imagePropertyName)
    {
        ImagePropertyName = imagePropertyName;
    }

    /// <summary>
    /// Property name of the property that holds the image data. This is used to link back to the image data property decorated with <see cref="GreenshotImageDataAttribute"/>.
    /// This is set during deserialization wich
    /// </summary>
    public string ImagePropertyName { get; }
}
