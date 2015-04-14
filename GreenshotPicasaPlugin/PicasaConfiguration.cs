/*
 * A Picasa Plugin for Greenshot
 * Copyright (C) 2011  Francis Noel
 * 
 * For more information see: http://getgreenshot.org/
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
using System;

namespace GreenshotPicasaPlugin {
	/// <summary>
	/// Description of PicasaConfiguration.
	/// </summary>
	[IniSection("Picasa", Description = "Greenshot Picasa Plugin configuration")]
	public class PicasaConfiguration : IniSection {
		[IniProperty("UploadFormat", Description="What file type to use for uploading", DefaultValue="png")]
		public OutputFormat UploadFormat;

		[IniProperty("UploadJpegQuality", Description="JPEG file save quality in %.", DefaultValue="80")]
		public int UploadJpegQuality;

		[IniProperty("AfterUploadLinkToClipBoard", Description = "After upload send Picasa link to clipboard.", DefaultValue = "true")]
		public bool AfterUploadLinkToClipBoard;

		[IniProperty("RefreshToken", Description = "Picasa refresh Token", Encrypted = true)]
		public string RefreshToken {
			get;
			set;
		}

		[IniProperty("AddFilename", Description = "Is the filename passed on to Picasa", DefaultValue = "False")]
		public bool AddFilename {
			get;
			set;
		}

		[IniProperty("UploadUser", Description = "The picasa user to upload to", DefaultValue = "default")]
		public string UploadUser {
			get;
			set;
		}

		[IniProperty("UploadAlbum", Description = "The picasa album to upload to", DefaultValue = "default")]
		public string UploadAlbum {
			get;
			set;
		}

		/// <summary>
		/// Not stored
		/// </summary>
		public string AccessToken {
			get;
			set;
		}

		/// <summary>
		/// Not stored
		/// </summary>
		public DateTimeOffset AccessTokenExpires {
			get;
			set;
		}

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
