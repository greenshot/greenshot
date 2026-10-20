/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using Newtonsoft.Json;

namespace Greenshot.Plugin.CustomUploader.Sxcu;

/// <summary>
/// An uploader as a ShareX custom uploader file (.sxcu) describes it, with the property names of that format.
/// Only what Greenshot uses is read, the other properties of a file are ignored.
/// See docs/vNext/custom-uploader.md for what is supported.
/// </summary>
internal sealed class UploaderDefinition
{
    /// <summary>The ShareX version which wrote the file, decides the syntax of the placeholders</summary>
    public string Version { get; set; }

    public string Name { get; set; }

    /// <summary>Comma separated: ImageUploader, TextUploader, FileUploader, URLShortener, URLSharingService</summary>
    public string DestinationType { get; set; }

    public string RequestMethod { get; set; }

    /// <summary>The name of <see cref="RequestMethod"/> in files of ShareX before 13.0</summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string RequestType { get; set; }

    public string RequestURL { get; set; }

    /// <summary>Added to the query of the request url</summary>
    public Dictionary<string, string> Parameters { get; set; }

    public Dictionary<string, string> Headers { get; set; }

    /// <summary>None, MultipartFormData, FormURLEncoded, JSON, XML or Binary</summary>
    public string Body { get; set; }

    /// <summary>The form fields of a multipart body</summary>
    public Dictionary<string, string> Arguments { get; set; }

    /// <summary>The name of the form field with the file</summary>
    public string FileFormName { get; set; }

    /// <summary>The link to the upload, made from the response</summary>
    public string URL { get; set; }

    public string ThumbnailURL { get; set; }

    /// <summary>The link which deletes the upload, made from the response</summary>
    public string DeletionURL { get; set; }

    /// <summary>The error the user sees when the upload failed, made from the response</summary>
    public string ErrorMessage { get; set; }
}
