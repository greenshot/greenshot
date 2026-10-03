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

namespace Greenshot.Plugin.Box
{
    /// <summary>
    /// The texts of the [Box] section of greenshot.box.{ietf}.ini, the English text is shown per property.
    /// Generated from the language files, add a key to the en-US file and a property here.
    /// </summary>
    [IniLanguageSection("Box", ModuleName = "box")]
    public interface IBoxLanguage : INotifyPropertyChanged
    {
        /// <summary>
        /// Communicating with Box. Please wait...
        /// </summary>
        string CommunicationWait { get; }

        /// <summary>
        /// Configure Box
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
        /// Image format
        /// </summary>
        string LabelUploadFormat { get; }

        /// <summary>
        /// Box settings
        /// </summary>
        string SettingsTitle { get; }

        /// <summary>
        /// An error occurred while uploading to Box:
        /// </summary>
        string UploadFailure { get; }

        /// <summary>
        /// Upload to Box
        /// </summary>
        string UploadMenuItem { get; }

        /// <summary>
        /// Successfully uploaded image to Box!
        /// </summary>
        string UploadSuccess { get; }
    }
}
