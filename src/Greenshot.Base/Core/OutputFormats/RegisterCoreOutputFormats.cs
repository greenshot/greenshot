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

namespace Greenshot.Base.Core.OutputFormats;

public static class CoreOutputFormats
{
    public static void RegisterCoreOutputFormats(IOutputFormatRegistry registry)
    {
        if (registry == null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        registry.RegisterIfMissing(new OutputFormatDefinition(WellKnownOutputFormats.Bmp, ["bmp"], "bmp", "image/bmp", null, "output_format_display_name_bmp", "Bitmap Image File"));
        registry.RegisterIfMissing(new OutputFormatDefinition(WellKnownOutputFormats.Gif, ["gif"], "gif", "image/gif", null, "output_format_display_name_gif", "Graphics Interchange Format File"));
        registry.RegisterIfMissing(new OutputFormatDefinition(WellKnownOutputFormats.Jpg, ["jpg", "jpeg"], "jpg", "image/jpeg", null, "output_format_display_name_jpg", "JPEG Image File"));
        registry.RegisterIfMissing(new OutputFormatDefinition(WellKnownOutputFormats.Png, ["png"], "png", "image/png", null, "output_format_display_name_png", "Portable Network Graphics File"));
        registry.RegisterIfMissing(new OutputFormatDefinition(WellKnownOutputFormats.Tiff, ["tif", "tiff"], "tiff", "image/tiff", null, "output_format_display_name_tiff", "Tagged Image File Format"));
        registry.RegisterIfMissing(new OutputFormatDefinition(WellKnownOutputFormats.Jxr, ["jxr"], "jxr", "image/vnd.ms-photo", null, "output_format_display_name_jxr", "JPEG XR Image File"));
        registry.RegisterIfMissing(new OutputFormatDefinition(WellKnownOutputFormats.Greenshot, ["greenshot"], "greenshot", "application/vnd.greenshot", null, "output_format_display_name_greenshot", "Greenshot Editor File"));
        registry.RegisterIfMissing(new OutputFormatDefinition(WellKnownOutputFormats.Ico, ["ico"], "ico", "image/vnd.microsoft.icon", ["image/x-icon"], "output_format_display_name_ico", "Icon File"));
    }
}