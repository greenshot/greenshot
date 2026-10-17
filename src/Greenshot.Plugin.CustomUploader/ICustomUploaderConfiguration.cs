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

using System.Collections.Generic;
using System.ComponentModel;
using Dapplo.Ini.Attributes;
using Dapplo.Ini.Interfaces;

namespace Greenshot.Plugin.CustomUploader;

[IniSection("CustomUploader")]
[Description("Greenshot custom uploader plugin configuration (beta)")]
public interface ICustomUploaderConfiguration : IIniSection, IAfterLoad
{
    [Description("The imported uploaders, each encrypted for the Windows user. Import them again in the settings, they can't be edited here.")]
    Dictionary<string, string> Uploaders { get; set; }

    [Description("The uploads with their deletion links, each encrypted for the Windows user.")]
    Dictionary<string, string> UploadHistory { get; set; }

    /// <summary>
    /// Mark the section as changed after the dictionaries were changed.
    /// Implemented in <see cref="CustomUploaderConfigurationImpl"/>.
    /// </summary>
    void MarkChanged();
}
