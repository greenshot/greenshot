/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
 * 
 * For more information see: http://getgreenshot.org/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using Greenshot.IniFile;
using GreenshotPlugin.Core;
using System.Windows.Forms;

namespace GreenshotWebDavPlugin
{
    /// <summary>
	/// Description of WebDavConfiguration.
	/// </summary>
	[IniSection("WebDAV", Description = "Greenshot WebDAV Plugin configuration")]
    class WebDavConfiguration : IniSection
    {
        [IniProperty("UploadFormat", Description = "What file type to use for uploading", DefaultValue = "png")]
        public OutputFormat UploadFormat { get; set; }

        [IniProperty("UploadJpegQuality", Description = "JPEG file save quality in %.", DefaultValue = "80")]
        public int UploadJpegQuality { get; set; }

        [IniProperty("Url", Description = "The WebDAV url you want the files to be uploaded to.", DefaultValue = "")]
        public string Url { get; set; }

        [IniProperty("Username", Description = "Username for your WebDAV server", DefaultValue = "", Encrypted = true)]
        public string Username { get; set; }

        [IniProperty("Password", Description = "Password for your WebDAV server", DefaultValue = "", Encrypted = true)]
        public string Password { get; set; }

        /// <summary>
		/// A form for username / password
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
