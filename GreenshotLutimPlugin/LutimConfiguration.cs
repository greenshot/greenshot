/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using GreenshotPlugin.IniFile;
using GreenshotPlugin.Core.Enums;

namespace GreenshotLutimPlugin {
	/// <summary>
	/// Description of LutimConfiguration.
	/// </summary>
	[IniSection("Lutim", Description="Greenshot Lutim Plugin configuration")]
	public class LutimConfiguration : IniSection {
		[IniProperty("LutimUrl", Description = "Url to Lutim system.", DefaultValue = "https://lut.im/")]
		public string LutimUrl { get; set; }

		[IniProperty("UploadFormat", Description="What file type to use for uploading", DefaultValue="png")]
		public OutputFormats UploadFormat { get; set; }
		[IniProperty("UploadJpegQuality", Description="JPEG file save quality in %.", DefaultValue="80")]
		public int UploadJpegQuality { get; set; }
		[IniProperty("UploadReduceColors", Description="Reduce color amount of the uploaded image to 256", DefaultValue="False")]
		public bool UploadReduceColors { get; set; }
		[IniProperty("CopyLinkToClipboard", Description = "Copy the link, which one is controlled by the UsePageLink, on the clipboard", DefaultValue = "True")]
		public bool CopyLinkToClipboard { get; set; }
		
		[IniProperty("LutimUploadHistory", Description= "Lutim upload history (LutimUploadHistory.Short=CreatedAt;DelAtView;Ext;Filename;Limit;RealShort;Token;LutimBaseUri;Thumb(base64))")]
		public Dictionary<string, string> LutimUploadHistory { get; set; }

        [IniProperty("FilenamePattern", Description = "Filename for the Lutim upload", DefaultValue = "${capturetime:d\"yyyyMMdd-HHmm\"}")]
        public string FilenamePattern { get; set; }

        // Not stored, only run-time!
        public Dictionary<string, LutimInfo> runtimeLutimHistory = new Dictionary<string, LutimInfo>();
		
        /// <summary>
		/// Supply values we can't put as defaults
		/// </summary>
		/// <param name="property">The property to return a default for</param>
		/// <returns>object with the default value for the supplied property</returns>
		public override object GetDefault(string property) {
			switch(property) {
				case "LutimUploadHistory":
					return new Dictionary<string, string>();
			}
			return null;
		}
			/// <summary>
		/// A form for username/password
		/// </summary>
		/// <returns>bool true if OK was pressed, false if cancel</returns>
		public bool ShowConfigDialog() {
			SettingsForm settingsForm = new SettingsForm();
			DialogResult result = settingsForm.ShowDialog();
			return result == DialogResult.OK;
		}
	}
}
