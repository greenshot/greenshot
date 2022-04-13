﻿/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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

using Greenshot.Base.Core.Enums;
using Greenshot.Base.IniFile;

namespace Greenshot.Plugin.Jira
{
    /// <summary>
    /// Description of JiraConfiguration.
    /// </summary>
    [IniSection("Jira", Description = "Greenshot Jira Plugin configuration")]
    public class JiraConfiguration : IniSection
    {
        public const string DefaultPrefix = "https://";
        private const string DefaultUrl = DefaultPrefix + "jira";

        [IniProperty("Url", Description = "Base url to Jira system, without anything else", DefaultValue = DefaultUrl)]
        public string Url { get; set; }

        [IniProperty("UploadFormat", Description = "What file type to use for uploading", DefaultValue = "png")]
        public OutputFormat UploadFormat { get; set; }

        [IniProperty("UploadJpegQuality", Description = "JPEG file save quality in %.", DefaultValue = "80")]
        public int UploadJpegQuality { get; set; }

        [IniProperty("UploadReduceColors", Description = "Reduce color amount of the uploaded image to 256", DefaultValue = "False")]
        public bool UploadReduceColors { get; set; }
    }
}