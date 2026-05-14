/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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

using System.Collections.Generic;
using System.ComponentModel;
using Dapplo.Ini.Attributes;
using Dapplo.Ini.Interfaces;
using Greenshot.Plugin.Imgur.Forms;

namespace Greenshot.Plugin.Imgur;

[IniSection("Imgur")]
[Description("Greenshot Imgur Plugin configuration")]
public interface IImgurConfiguration : IIniSection
{
    [Description("Url to Imgur system.")]
    [DefaultValue("https://api.imgur.com/3")]
    string ImgurApi3Url { get; set; }

    [Description("Use pagelink instead of direct link on the clipboard")]
    [DefaultValue(false)]
    bool UsePageLink { get; set; }

    [Description("Use anonymous access to Imgur")]
    [DefaultValue(true)]
    bool AnonymousAccess { get; set; }

    [Description("Imgur upload history (ImgurUploadHistory.hash=deleteHash)")]
    Dictionary<string, string> ImgurUploadHistory { get; set; }

    /// <summary>Runtime-only upload history details - never written to disk, reset to default on every reload.</summary>
    [IniValue(RuntimeOnly = true)]
    Dictionary<string, ImgurInfo> RuntimeImgurHistory { get; set; }
}
