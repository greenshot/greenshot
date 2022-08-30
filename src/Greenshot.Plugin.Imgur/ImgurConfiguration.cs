/*
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

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.IniFile;
using Greenshot.Plugin.Imgur.Forms;

namespace Greenshot.Plugin.Imgur
{
    /// <summary>
    /// Description of ImgurConfiguration.
    /// </summary>
    [IniSection("Imgur", Description = "Greenshot Imgur Plugin configuration")]
    public class ImgurConfiguration : IniSection
    {
        [IniProperty("ImgurApi3Url", Description = "Url to Imgur system.", DefaultValue = "https://api.imgur.com/3")]
        public string ImgurApi3Url { get; set; }

        [IniProperty("UploadFormat", Description = "What file type to use for uploading", DefaultValue = "png")]
        public OutputFormat UploadFormat { get; set; }

        [IniProperty("UploadJpegQuality", Description = "JPEG file save quality in %.", DefaultValue = "80")]
        public int UploadJpegQuality { get; set; }

        [IniProperty("UploadReduceColors", Description = "Reduce color amount of the uploaded image to 256", DefaultValue = "False")]
        public bool UploadReduceColors { get; set; }

        [IniProperty("CopyLinkToClipboard", Description = "Copy the link, which one is controlled by the UsePageLink, on the clipboard", DefaultValue = "True")]
        public bool CopyLinkToClipboard { get; set; }

        [IniProperty("UsePageLink", Description = "Use pagelink instead of direct link on the clipboard", DefaultValue = "False")]
        public bool UsePageLink { get; set; }

        [IniProperty("AnonymousAccess", Description = "Use anonymous access to Imgur", DefaultValue = "true")]
        public bool AnonymousAccess { get; set; }

        [IniProperty("RefreshToken", Description = "Imgur refresh Token", Encrypted = true, ExcludeIfNull = true)]
        public string RefreshToken { get; set; }

        /// <summary>
        /// AccessToken, not stored
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// AccessTokenExpires, not stored
        /// </summary>
        public DateTimeOffset AccessTokenExpires { get; set; }

        [IniProperty("AddTitle", Description = "Is the title passed on to Imgur", DefaultValue = "False")]
        public bool AddTitle { get; set; }

        [IniProperty("AddFilename", Description = "Is the filename passed on to Imgur", DefaultValue = "False")]
        public bool AddFilename { get; set; }

        [IniProperty("FilenamePattern", Description = "Filename for the Imgur upload", DefaultValue = "${capturetime:d\"yyyyMMdd-HHmm\"}")]
        public string FilenamePattern { get; set; }

        [IniProperty("ImgurUploadHistory", Description = "Imgur upload history (ImgurUploadHistory.hash=deleteHash)")]
        public Dictionary<string, string> ImgurUploadHistory { get; set; }

        // Not stored, only run-time!
        public Dictionary<string, ImgurInfo> runtimeImgurHistory = new Dictionary<string, ImgurInfo>();
        public int Credits { get; set; }

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
            AccessToken = null;
            RefreshToken = null;
            AccessTokenExpires = default;
        }

        /// <summary>
        /// Supply values we can't put as defaults
        /// </summary>
        /// <param name="property">The property to return a default for</param>
        /// <returns>object with the default value for the supplied property</returns>
        public override object GetDefault(string property) =>
            property switch
            {
                nameof(ImgurUploadHistory) => new Dictionary<string, string>(),
                _ => null
            };

        /// <summary>
        /// A form for username/password
        /// </summary>
        /// <returns>bool true if OK was pressed, false if cancel</returns>
        public bool ShowConfigDialog()
        {
            SettingsForm settingsForm = new SettingsForm();
            DialogResult result = settingsForm.ShowDialog();
            return result == DialogResult.OK;
        }
    }
}