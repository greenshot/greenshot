/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Dapplo.Ini.Attributes;
using Dapplo.Ini.Interfaces;
using Greenshot.Base.Core.Enums;

namespace Greenshot.Plugin.Box;

[IniSection("Box")]
[Description("Greenshot Box Plugin configuration")]
public interface IBoxConfiguration : IIniSection, IAfterLoad, IBeforeSave
{
    [Description("What file type to use for uploading")]
    [DefaultValue("png")]
    OutputFormat UploadFormat { get; set; }

    [Description("JPEG file save quality in %.")]
    [DefaultValue(80)]
    [Range(0, 100, ErrorMessage = "JPEG quality must be between 0 and 100.")]
    int UploadJpegQuality { get; set; }

    [Description("After upload send Box link to clipboard.")]
    [DefaultValue(true)]
    bool AfterUploadLinkToClipBoard { get; set; }

    [Description("Use the shared link, instead of the private, on the clipboard")]
    [DefaultValue(true)]
    bool UseSharedLink { get; set; }

    [Description("Folder ID to upload to, only change if you know what you are doing!")]
    [DefaultValue("0")]
    string FolderId { get; set; }

    [Description("Box authorization refresh Token (stored encrypted)")]
    string RefreshToken { get; set; }

    /// <summary>Runtime-only token - never written to disk, reset to default on every reload.</summary>
    [IniValue(RuntimeOnly = true)]
    string AccessToken { get; set; }

    /// <summary>Runtime-only token expiry - never written to disk, reset to default on every reload.</summary>
    [IniValue(RuntimeOnly = true)]
    DateTimeOffset AccessTokenExpires { get; set; }
}
