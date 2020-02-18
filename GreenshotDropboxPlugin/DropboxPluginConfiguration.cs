/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using System.Windows.Forms;
using GreenshotPlugin.Core;
using GreenshotPlugin.IniFile;


namespace GreenshotDropboxPlugin {
	/// <summary>
	/// Description of ImgurConfiguration.
	/// </summary>
	[IniSection("Dropbox", Description = "Greenshot Dropbox Plugin configuration")]
	public class DropboxPluginConfiguration : IniSection {
		[IniProperty("UploadFormat", Description="What file type to use for uploading", DefaultValue="png")]
		public OutputFormat UploadFormat { get; set; }

		[IniProperty("UploadJpegQuality", Description="JPEG file save quality in %.", DefaultValue="80")]
		public int UploadJpegQuality { get; set; }

		[IniProperty("AfterUploadLinkToClipBoard", Description = "After upload send Dropbox link to clipboard.", DefaultValue = "true")]
		public bool AfterUploadLinkToClipBoard { get; set; }

		[IniProperty("DropboxToken", Description = "The Dropbox token", Encrypted = true, ExcludeIfNull = true)]
		public string DropboxToken { get; set; }
		[IniProperty("DropboxTokenSecret", Description = "The Dropbox token secret", Encrypted = true, ExcludeIfNull = true)]
		public string DropboxTokenSecret { get; set; }

		/// <summary>
		/// A form for token
		/// </summary>
		/// <returns>bool true if OK was pressed, false if cancel</returns>
		public bool ShowConfigDialog() {
			DialogResult result = new SettingsForm().ShowDialog();
			if (result == DialogResult.OK) {
				return true;
			}
			return false;
		}
	}
}
