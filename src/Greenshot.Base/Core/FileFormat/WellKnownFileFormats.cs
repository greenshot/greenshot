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
using log4net;

namespace Greenshot.Base.Core.FileFormat;

/// <summary>
/// Represents well-known file formats (format-IDs) in core Greenshot.
/// </summary>
public static class WellKnownFileFormats
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(WellKnownFileFormats));

    public const string Bmp = "bmp";
    public const string Gif = "gif";
    public const string Jpg = "jpg";
    public const string Png = "png";
    public const string Tiff = "tiff";
    public const string Jxr = "jxr";
    public const string Greenshot = "greenshot";
    public const string Ico = "ico";

    public static bool IsEqualFormat(string wellKnownFileFormat, string compareFormat)
    {
        if (!IsWellKnownFormat(wellKnownFileFormat))
        {
            Log.WarnFormat("'{0}' is not a well-known file format.", wellKnownFileFormat);
            return false;
        }

        return string.Equals(wellKnownFileFormat, compareFormat, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsWellKnownFormat(string format)
    {
        return string.Equals(format, Bmp, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(format, Gif, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(format, Jpg, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(format, Png, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(format, Tiff, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(format, Jxr, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(format, Greenshot, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(format, Ico, StringComparison.OrdinalIgnoreCase);
    }
}