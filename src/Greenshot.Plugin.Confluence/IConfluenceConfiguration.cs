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

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Dapplo.Ini.Attributes;
using Dapplo.Ini.Interfaces;
using Greenshot.Base.Core.Enums;

namespace Greenshot.Plugin.Confluence;

[IniSection("Confluence")]
[Description("Greenshot Confluence Plugin configuration")]
public interface IConfluenceConfiguration : IIniSection
{
    [Description("Url to Confluence system (e.g., https://confluence.example.com or https://domain.atlassian.net/wiki for Cloud).")]
    [DefaultValue("https://confluence")]
    [Required(ErrorMessage = "Confluence URL is required.")]
    string Url { get; set; }

    [Description("Session timeout in minutes")]
    [DefaultValue(30)]
    [Range(1, 1440, ErrorMessage = "Session timeout must be between 1 and 1440 minutes.")]
    int Timeout { get; set; }

    [Description("What file type to use for uploading")]
    [DefaultValue("png")]
    OutputFormat UploadFormat { get; set; }

    [Description("JPEG file save quality in %.")]
    [DefaultValue(80)]
    [Range(0, 100, ErrorMessage = "JPEG quality must be between 0 and 100.")]
    int UploadJpegQuality { get; set; }

    [Description("Reduce color amount of the uploaded image to 256")]
    [DefaultValue(false)]
    bool UploadReduceColors { get; set; }

    [Description("Open the page where the picture is uploaded after upload")]
    [DefaultValue(true)]
    bool OpenPageAfterUpload { get; set; }

    [Description("Copy the Wikimarkup for the recently uploaded image to the Clipboard")]
    [DefaultValue(true)]
    bool CopyWikiMarkupForImageToClipboard { get; set; }

    [Description("Key of last space that was searched for")]
    string SearchSpaceKey { get; set; }

    [Description("Include personal spaces in the search & browse spaces list")]
    [DefaultValue(false)]
    bool IncludePersonSpaces { get; set; }
}
