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

using System.Collections.Generic;
using System.Windows.Forms;
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

        [IniProperty("UsePageLink", Description = "Use pagelink instead of direct link on the clipboard", DefaultValue = "False")]
        public bool UsePageLink { get; set; }


        [IniProperty("AnonymousAccess", Description = "Use anonymous access to Imgur", DefaultValue = "true")]
        public bool AnonymousAccess { get; set; }
        [IniProperty("ImgurUploadHistory", Description = "Imgur upload history (ImgurUploadHistory.hash=deleteHash)")]
        public Dictionary<string, string> ImgurUploadHistory { get; set; }

        // Not stored, only run-time!
        public Dictionary<string, ImgurInfo> runtimeImgurHistory = new Dictionary<string, ImgurInfo>();


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