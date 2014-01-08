/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2014 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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
using Greenshot.IniFile;
using GreenshotPlugin.Core;

namespace GreenshotBoxPlugin {
	/// <summary>
	/// Description of ImgurConfiguration.
	/// </summary>
	[IniSection("Box", Description = "Greenshot Box Plugin configuration")]
	public class BoxConfiguration : IniSection {
		[IniProperty("UploadFormat", Description="What file type to use for uploading", DefaultValue="png")]
		public OutputFormat UploadFormat;

		[IniProperty("UploadJpegQuality", Description="JPEG file save quality in %.", DefaultValue="80")]
		public int UploadJpegQuality;

		[IniProperty("AfterUploadLinkToClipBoard", Description = "After upload send Box link to clipboard.", DefaultValue = "true")]
		public bool AfterUploadLinkToClipBoard;

		[IniProperty("UseSharedLink", Description = "Use the shared link, instead of the private, on the clipboard", DefaultValue = "True")]
		public bool UseSharedLink;

		[IniProperty("BoxToken", Description = "Token.", DefaultValue = "")]
		public string BoxToken;

		/// <summary>
		/// A form for token
		/// </summary>
		/// <returns>bool true if OK was pressed, false if cancel</returns>
		public bool ShowConfigDialog() {
			DialogResult result = new SettingsForm(this).ShowDialog();
			if (result == DialogResult.OK) {
				return true;
			}
			return false;
		}

	}
}
