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

namespace Greenshot.Editor.FileFormat.Dto;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
internal sealed class GreenshotImageDataAttribute : Attribute
{
    public GreenshotImageDataAttribute(string pathPropertyName, string staticExtension = null, string extensionPropertyName = null, string targetZipFolder = null, string staticFilename = null)
    {
        if (!string.IsNullOrEmpty(staticExtension) && !string.IsNullOrEmpty(extensionPropertyName))
        {
            throw new ArgumentException("Specify either a static extension or an extension property name, not both.", nameof(staticExtension));
        }

        PathPropertyName = pathPropertyName;
        StaticExtension = staticExtension;
        ExtensionPropertyName = extensionPropertyName;
        TargetZipFolder = targetZipFolder;
        StaticFilename = staticFilename;
    }

    public string PathPropertyName { get; }

    public string StaticExtension { get; }

    public string ExtensionPropertyName { get; }

    public string TargetZipFolder { get; }

    public string StaticFilename { get; }
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
internal sealed class GreenshotImagePathAttribute : Attribute
{
    public GreenshotImagePathAttribute(string imagePropertyName)
    {
        ImagePropertyName = imagePropertyName;
    }

    public string ImagePropertyName { get; }
}
