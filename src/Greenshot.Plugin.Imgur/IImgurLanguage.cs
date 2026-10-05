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

namespace Greenshot.Plugin.Imgur
{
    /// <summary>
    /// The texts of the [Imgur] section of greenshot.imgur.{ietf}.ini, the English text is shown per property.
    /// Generated from the language files, add a key to the en-US file and a property here.
    /// </summary>
    [IniLanguageSection("Imgur", ModuleName = "imgur")]
    public interface IImgurLanguage : INotifyPropertyChanged
    {
        /// <summary>
        /// Use anonymous access
        /// </summary>
        string AnonymousAccess { get; }

        /// <summary>
        /// Are you sure you want to delete the local Imgur history?
        /// </summary>
        string ClearQuestion { get; }

        /// <summary>
        /// Communicating with Imgur. Please wait...
        /// </summary>
        string CommunicationWait { get; }

        /// <summary>
        /// Configure Imgur
        /// </summary>
        string Configure { get; }

        /// <summary>
        /// Are you sure you want to delete the image {0} from Imgur?
        /// </summary>
        string DeleteQuestion { get; }

        /// <summary>
        /// Delete Imgur {0}
        /// </summary>
        string DeleteTitle { get; }

        /// <summary>
        /// History
        /// </summary>
        string History { get; }

        /// <summary>
        /// Clear history
        /// </summary>
        string HistoryClear { get; }

        /// <summary>
        /// Date
        /// </summary>
        string HistoryColumnDate { get; }

        /// <summary>
        /// Hash
        /// </summary>
        string HistoryColumnHash { get; }

        /// <summary>
        /// Title
        /// </summary>
        string HistoryColumnTitle { get; }

        /// <summary>
        /// Copy link(s) to clipboard
        /// </summary>
        string HistoryCopyToClipboard { get; }

        /// <summary>
        /// Delete
        /// </summary>
        string HistoryDelete { get; }

        /// <summary>
        /// Open
        /// </summary>
        string HistoryOpen { get; }

        /// <summary>
        /// Image format
        /// </summary>
        string LabelUploadFormat { get; }

        /// <summary>
        /// Url
        /// </summary>
        string LabelUrl { get; }

        /// <summary>
        /// Imgur settings
        /// </summary>
        string SettingsTitle { get; }

        /// <summary>
        /// An error occurred while uploading to Imgur:
        /// </summary>
        string UploadFailure { get; }

        /// <summary>
        /// Upload to Imgur
        /// </summary>
        string UploadMenuItem { get; }

        /// <summary>
        /// Successfully uploaded image to Imgur!
        /// </summary>
        string UploadSuccess { get; }

        /// <summary>
        /// Use page link instead of image link on clipboard
        /// </summary>
        string UsePageLink { get; }
    }
}
