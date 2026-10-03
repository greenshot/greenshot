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

namespace Greenshot.Plugin.Dropbox
{
    /// <summary>
    /// The texts of the [Dropbox] section of greenshot.dropbox.{ietf}.ini, the English text is shown per property.
    /// Generated from the language files, add a key to the en-US file and a property here.
    /// </summary>
    [IniLanguageSection("Dropbox", ModuleName = "dropbox")]
    public interface IDropboxLanguage : INotifyPropertyChanged
    {
        /// <summary>
        /// Communicating with Dropbox. Please wait...
        /// </summary>
        string CommunicationWait { get; }

        /// <summary>
        /// Configure Dropbox
        /// </summary>
        string Configure { get; }

        /// <summary>
        /// After upload
        /// </summary>
        string LabelAfterUpload { get; }

        /// <summary>
        /// Link to clipboard
        /// </summary>
        string LabelAfterUploadLinkToClipBoard { get; }

        /// <summary>
        /// Open history
        /// </summary>
        string LabelAfterUploadOpenHistory { get; }

        /// <summary>
        /// Image format
        /// </summary>
        string LabelUploadFormat { get; }

        /// <summary>
        /// Dropbox settings
        /// </summary>
        string SettingsTitle { get; }

        /// <summary>
        /// An error occurred while uploading to Dropbox:
        /// </summary>
        string UploadFailure { get; }

        /// <summary>
        /// Upload to Dropbox
        /// </summary>
        string UploadMenuItem { get; }

        /// <summary>
        /// Successfully uploaded image to Dropbox!
        /// </summary>
        string UploadSuccess { get; }
    }
}
