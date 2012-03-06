/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
using System;
using System.Collections.Generic;
using System.Windows.Forms;

using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using Greenshot.IniFile;

namespace GreenshotImgurPlugin {
	/// <summary>
	/// Description of ImgurConfiguration.
	/// </summary>
	[IniSection("Imgur", Description="Greenshot Imgur Plugin configuration")]
	public class ImgurConfiguration : IniSection {
		[IniProperty("ImgurApiUrl", Description="Url to Imgur system.", DefaultValue="http://api.imgur.com/2")]
		public string ImgurApiUrl;
		
		[IniProperty("UploadFormat", Description="What file type to use for uploading", DefaultValue="png")]
		public OutputFormat UploadFormat;
		[IniProperty("UploadJpegQuality", Description="JPEG file save quality in %.", DefaultValue="80")]
		public int UploadJpegQuality;
		[IniProperty("UsePageLink", Description = "Use pagelink instead of direct link on the clipboard", DefaultValue = "False")]
		public bool UsePageLink;
		
		[IniProperty("ImgurUploadHistory", Description="Imgur upload history (ImgurUploadHistory.hash=deleteHash)")]
		public Dictionary<string, string> ImgurUploadHistory;
		
		// Not stored, only run-time!
		public Dictionary<string, ImgurInfo> runtimeImgurHistory = new Dictionary<string, ImgurInfo>();

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
			SettingsForm settingsForm;
			ILanguage lang = Language.GetInstance();

			BackgroundForm backgroundForm = BackgroundForm.ShowAndWait(ImgurPlugin.Attributes.Name, lang.GetString(LangKey.communication_wait));
			try {
				settingsForm = new SettingsForm(this);
			} finally {
				backgroundForm.CloseDialog();
			}
			settingsForm.Url = ImgurApiUrl;
			settingsForm.UsePageLink = UsePageLink;
			settingsForm.UploadFormat = UploadFormat.ToString();
			DialogResult result = settingsForm.ShowDialog();
			if (result == DialogResult.OK) {
				if (!settingsForm.Url.Equals(ImgurApiUrl) || !settingsForm.UploadFormat.Equals(UploadFormat.ToString())
					|| !!settingsForm.UsePageLink.Equals(UsePageLink)) {
					ImgurApiUrl = settingsForm.Url;
					UploadFormat = (OutputFormat)Enum.Parse(typeof(OutputFormat), settingsForm.UploadFormat.ToLower());
					UsePageLink = settingsForm.UsePageLink;
				}
				IniConfig.Save();
				return true;
			}
			return false;
		}
	}
}
