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

using System.ComponentModel;
using Dapplo.Ini.Internationalization.Attributes;

namespace Greenshot.Plugin.Confluence
{
    /// <summary>
    /// The texts of the [Confluence] section of greenshot.confluence.{ietf}.ini, the English text is shown per property.
    /// Generated from the language files, add a key to the en-US file and a property here.
    /// </summary>
    [IniLanguageSection("Confluence", ModuleName = "confluence")]
    public interface IConfluenceLanguage : INotifyPropertyChanged
    {
        /// <summary>
        /// Browse pages
        /// </summary>
        string BrowsePages { get; }

        /// <summary>
        /// Cancel
        /// </summary>
        string Cancel { get; }

        /// <summary>
        /// Transferring data to Confluence, please wait...
        /// </summary>
        string CommunicationWait { get; }

        /// <summary>
        /// Copy Wikimarkup to the Clipboard
        /// </summary>
        string CopyWikimarkup { get; }

        /// <summary>
        /// Filename
        /// </summary>
        string Filename { get; }

        /// <summary>
        /// Include personal spaces in search and browsing
        /// </summary>
        string IncludePersonSpaces { get; }

        /// <summary>
        /// Password
        /// </summary>
        string LabelPassword { get; }

        /// <summary>
        /// Timeout
        /// </summary>
        string LabelTimeout { get; }

        /// <summary>
        /// Url
        /// </summary>
        string LabelUrl { get; }

        /// <summary>
        /// User
        /// </summary>
        string LabelUser { get; }

        /// <summary>
        /// Confluence data is loading, please wait...
        /// </summary>
        string Loading { get; }

        /// <summary>
        /// There was a problem during the login: {0}
        /// </summary>
        string LoginError { get; }

        /// <summary>
        /// Please enter your Confluence login data
        /// </summary>
        string LoginTitle { get; }

        /// <summary>
        /// OK
        /// </summary>
        string Ok { get; }

        /// <summary>
        /// Open page after upload
        /// </summary>
        string OpenPageAfterUpload { get; }

        /// <summary>
        /// Open pages
        /// </summary>
        string OpenPages { get; }

        /// <summary>
        /// Confluence settings
        /// </summary>
        string PluginSettings { get; }

        /// <summary>
        /// Search
        /// </summary>
        string Search { get; }

        /// <summary>
        /// Search pages
        /// </summary>
        string SearchPages { get; }

        /// <summary>
        /// Search text
        /// </summary>
        string SearchText { get; }

        /// <summary>
        /// Upload
        /// </summary>
        string Upload { get; }

        /// <summary>
        /// An error occurred while uploading to Confluence:
        /// </summary>
        string UploadFailure { get; }

        /// <summary>
        /// Upload format
        /// </summary>
        string UploadFormat { get; }

        /// <summary>
        /// Upload to Confluence
        /// </summary>
        string UploadMenuItem { get; }

        /// <summary>
        /// Successfully uploaded image to Confluence!
        /// </summary>
        string UploadSuccess { get; }
    }
}
