/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2014 Thomas Braun, Jens Klingen, Robin Krom
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
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;

namespace GreenshotPhotobucketPlugin {
	/// <summary>
	/// Description of PhotobucketConfiguration.
	/// </summary>
	[IniSection("Photobucket", Description="Greenshot Photobucket Plugin configuration")]
	public class PhotobucketConfiguration : IniSection {
		[IniProperty("UploadFormat", Description="What file type to use for uploading", DefaultValue="png")]
		public OutputFormat UploadFormat;
		[IniProperty("UploadJpegQuality", Description="JPEG file save quality in %.", DefaultValue="80")]
		public int UploadJpegQuality;
		[IniProperty("UploadReduceColors", Description="Reduce color amount of the uploaded image to 256", DefaultValue="False")]
		public bool UploadReduceColors;
		[IniProperty("UsePageLink", Description = "Use pagelink instead of direct link on the clipboard", DefaultValue = "False")]
		public bool UsePageLink;
		[IniProperty("Token", Description = "The Photobucket token", Encrypted=true, ExcludeIfNull=true)]
		public string Token;
		[IniProperty("TokenSecret", Description = "The Photobucket token secret", Encrypted=true, ExcludeIfNull=true)]
		public string TokenSecret;
		[IniProperty("SubDomain", Description = "The Photobucket api subdomain", Encrypted = true, ExcludeIfNull = true)]
		public string SubDomain;
		[IniProperty("Username", Description = "The Photobucket api username", ExcludeIfNull = true)]
		public string Username;
		
		public int Credits {
			get;
			set;
		}

			/// <summary>
		/// A form for username/password
		/// </summary>
		/// <returns>bool true if OK was pressed, false if cancel</returns>
		public bool ShowConfigDialog() {
			SettingsForm settingsForm = null;

			new PleaseWaitForm().ShowAndWait(PhotobucketPlugin.Attributes.Name, Language.GetString("photobucket", LangKey.communication_wait), 
				delegate() {
					settingsForm = new SettingsForm(this);
				}
			);
			DialogResult result = settingsForm.ShowDialog();
			if (result == DialogResult.OK) {
				return true;
			}
			return false;
		}
	}
}
