/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;
using System.Windows.Forms;
using Greenshot.IniFile;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using GreenshotPlugin.IniFile;
using System;

namespace GreenshotImgurPlugin {
	/// <summary>
	/// Description of ImgurConfiguration.
	/// </summary>
	[IniSection("Imgur", Description="Greenshot Imgur Plugin configuration")]
	public class ImgurConfiguration : IniSection {
		[IniProperty("ApiUrl", Description = "Url to Imgur API.", DefaultValue = "https://api.imgur.com/3")]
		public string ApiUrl {
			get;
			set;
		}
		
		[IniProperty("UploadFormat", Description="What file type to use for uploading", DefaultValue="png")]
		public OutputFormat UploadFormat {
			get;
			set;
		}

		[IniProperty("UploadJpegQuality", Description="JPEG file save quality in %.", DefaultValue="80")]
		public int UploadJpegQuality {
			get;
			set;
		}

		[IniProperty("UploadReduceColors", Description="Reduce color amount of the uploaded image to 256", DefaultValue="False")]
		public bool UploadReduceColors {
			get;
			set;
		}

		[IniProperty("CopyUrlToClipboard", Description = "Copy the URL to the clipboard", DefaultValue = "true")]
		public bool CopyUrlToClipboard {
			get;
			set;
		}

		[IniProperty("UsePageLink", Description = "Use pagelink instead of direct link (in clipboard and notification)", DefaultValue = "False")]
		public bool UsePageLink {
			get;
			set;
		}

		[IniProperty("AnonymousAccess", Description = "Use anonymous access to Imgur", DefaultValue="true")]
		public bool AnonymousAccess {
			get;
			set;
		}

		[IniProperty("TrackHistory", Description = "Track the upload history", DefaultValue = "true")]
		public bool TrackHistory {
			get;
			set;
		}

		[IniProperty("ImgurToken", Description = "The Imgur token", Encrypted=true, ExcludeIfNull=true)]
		public string ImgurToken {
			get;
			set;
		}

		[IniProperty("ImgurTokenSecret", Description = "The Imgur token secret", Encrypted=true, ExcludeIfNull=true)]
		public string ImgurTokenSecret {
			get;
			set;
		}

		[IniProperty("AddTitle", Description = "Is the title passed on to Imgur", DefaultValue = "False")]
		public bool AddTitle {
			get;
			set;
		}

		[IniProperty("AddFilename", Description = "Is the filename passed on to Imgur", DefaultValue = "False")]
		public bool AddFilename {
			get;
			set;
		}

		[IniProperty("FilenamePattern", Description = "Filename for the Imgur upload", DefaultValue = "${capturetime:d\"yyyyMMdd-HHmm\"}")]
		public string FilenamePattern {
			get;
			set;
		}

		[IniProperty("ImgurUploadHistory", Description = "Imgur upload history (ImgurUploadHistory.hash=deleteHash)")]
		public Dictionary<string, string> ImgurUploadHistory {
			get;
			set;
		}
		

		[IniProperty("RefreshToken", Description = "Imgur authorization refresh Token", Encrypted = true)]
		public string RefreshToken {
			get;
			set;
		}


		// Not stored, only run-time!
		/// <summary>
		/// AccessToken, not stored
		/// </summary>
		public string AccessToken {
			get;
			set;
		}

		/// <summary>
		/// AccessTokenExpires, not stored
		/// </summary>
		public DateTimeOffset AccessTokenExpires {
			get;
			set;
		}

		public Dictionary<string, ImageInfo> runtimeImgurHistory = new Dictionary<string, ImageInfo>();

		/// <summary>
		/// Available credits
		/// </summary>
		public int Credits {
			get;
			set;
		}

		/// <summary>
		/// Supply values we can't put as defaults
		/// </summary>
		/// <param name="property">The property to return a default for</param>
		/// <returns>object with the default value for the supplied property</returns>
		public override object GetDefault(string property) {
			switch(property) {
				case "ImgurUploadHistory":
					return new Dictionary<string, string>();
			}
			return null;
		}
			/// <summary>
		/// A form for username/password
		/// </summary>
		/// <returns>bool true if OK was pressed, false if cancel</returns>
		public bool ShowConfigDialog() {
			var settingsForm = new SettingsForm(this);
			var result = settingsForm.ShowDialog();
			if (result == DialogResult.OK) {
				return true;
			}
			return false;
		}
	}
}
