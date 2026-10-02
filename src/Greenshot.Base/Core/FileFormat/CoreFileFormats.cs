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

namespace Greenshot.Base.Core.FileFormat;

public static class CoreFileFormats
{
    public static void RegisterCoreFileFormats(IFileFormatRegistry registry)
    {
        if (registry == null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        registry.RegisterIfMissing(new FileFormatDefinition(WellKnownFileFormats.Bmp, ["bmp"], ["bmp"], "bmp", "image/bmp", null, "Bitmap"));
        registry.RegisterIfMissing(new FileFormatDefinition(WellKnownFileFormats.Gif, ["gif"], ["gif"], "gif", "image/gif", null, "Graphics Interchange Format"));
        registry.RegisterIfMissing(new FileFormatDefinition(WellKnownFileFormats.Jpg, ["jpg", "jpeg"], ["jpg", "jpeg"], "jpg", "image/jpeg", null, "JPEG"));
        registry.RegisterIfMissing(new FileFormatDefinition(WellKnownFileFormats.Png, ["png"], ["png"], "png", "image/png", null, "Portable Network Graphics"));
        registry.RegisterIfMissing(new FileFormatDefinition(WellKnownFileFormats.Tiff, ["tif", "tiff"], ["tif", "tiff"], "tiff", "image/tiff", null, "Tagged Image File Format"));
        registry.RegisterIfMissing(new FileFormatDefinition(WellKnownFileFormats.Jxr, ["jxr"], ["jxr"], "jxr", "image/vnd.ms-photo", null, "JPEG XR"));
        registry.RegisterIfMissing(new FileFormatDefinition(WellKnownFileFormats.Greenshot, ["greenshot"], ["greenshot"], "greenshot", "application/vnd.greenshot", null, "Greenshot Editor"));
        registry.RegisterIfMissing(new FileFormatDefinition(WellKnownFileFormats.Ico, ["ico"], ["ico"], "ico", "image/vnd.microsoft.icon", ["image/x-icon"], "Icon"));
    }
}