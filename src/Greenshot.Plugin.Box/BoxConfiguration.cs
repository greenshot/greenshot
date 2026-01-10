/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.IniFile;
using Greenshot.Plugin.Box.Forms;

namespace Greenshot.Plugin.Box
{
    /// <summary>
    /// Description of ImgurConfiguration.
    /// </summary>
    [IniSection("Box", Description = "Greenshot Box Plugin configuration")]
    public class BoxConfiguration : IniSection
    {
        [IniProperty("UploadFormat", Description = "What file type to use for uploading", DefaultValue = "png")]
        public OutputFormat UploadFormat { get; set; }

        [IniProperty("UploadJpegQuality", Description = "JPEG file save quality in %.", DefaultValue = "80")]
        public int UploadJpegQuality { get; set; }

        [IniProperty("AfterUploadLinkToClipBoard", Description = "After upload send Box link to clipboard.", DefaultValue = "true")]
        public bool AfterUploadLinkToClipBoard { get; set; }

        [IniProperty("UseSharedLink", Description = "Use the shared link, instead of the private, on the clipboard", DefaultValue = "True")]
        public bool UseSharedLink { get; set; }

        [IniProperty("FolderId", Description = "Folder ID to upload to, only change if you know what you are doing!", DefaultValue = "0")]
        public string FolderId { get; set; }

        [IniProperty("RefreshToken", Description = "Box authorization refresh Token", Encrypted = true)]
        public string RefreshToken { get; set; }

        /// <summary>
        /// Not stored
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// Not stored
        /// </summary>
        public DateTimeOffset AccessTokenExpires { get; set; }

        /// <summary>
        /// A form for token
        /// </summary>
        /// <returns>bool true if OK was pressed, false if cancel</returns>
        public bool ShowConfigDialog()
        {
            DialogResult result = new SettingsForm().ShowDialog();
            if (result == DialogResult.OK)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Upgrade certain values
        /// </summary>
        public override void AfterLoad()
        {
            var coreConfiguration = IniConfig.GetIniSection<CoreConfiguration>();
            bool isUpgradeFrom12 = coreConfiguration.LastSaveWithVersion?.StartsWith("1.2") ?? false;
            // Clear token when we upgrade from 1.2 to 1.3 as it is no longer valid, discussed in #421
            if (!isUpgradeFrom12) return;

            // We have an upgrade, remove all previous credentials.
            RefreshToken = null;
            AccessToken = null;
        }
    }
}