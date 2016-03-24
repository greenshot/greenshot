/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using Greenshot.IniFile;
using GreenshotPlugin.Core;

namespace GreenshotFlickrPlugin {
	public enum SafetyLevel {
		Safe = 1,
		Moderate = 2,
		Restricted = 3
	}
	/// <summary>
	/// Description of FlickrConfiguration.
	/// </summary>
	[IniSection("Flickr", Description = "Greenshot Flickr Plugin configuration")]
	public class FlickrConfiguration : IniSection {
		[IniProperty("flickrIsPublic", Description = "IsPublic.", DefaultValue = "true")]
		public bool IsPublic;

		[IniProperty("flickrIsFamily", Description = "IsFamily.", DefaultValue = "true")]
		public bool IsFamily;

		[IniProperty("flickrIsFriend", Description = "IsFriend.", DefaultValue = "true")]
		public bool IsFriend;

		[IniProperty("SafetyLevel", Description = "Safety level", DefaultValue = "Safe")]
		public SafetyLevel SafetyLevel;

		[IniProperty("HiddenFromSearch", Description = "Hidden from search", DefaultValue = "false")]
		public bool HiddenFromSearch;
	   
		[IniProperty("UploadFormat", Description="What file type to use for uploading", DefaultValue="png")]
		public OutputFormat UploadFormat;

		[IniProperty("UploadJpegQuality", Description="JPEG file save quality in %.", DefaultValue="80")]
		public int UploadJpegQuality;

		[IniProperty("AfterUploadLinkToClipBoard", Description = "After upload send flickr link to clipboard.", DefaultValue = "true")]
		public bool AfterUploadLinkToClipBoard;

		[IniProperty("UsePageLink", Description = "Use pagelink instead of direct link on the clipboard", DefaultValue = "False")]
		public bool UsePageLink;
		
		[IniProperty("FlickrToken", Description = "The Flickr token", Encrypted = true, ExcludeIfNull = true)]
		public string FlickrToken;
		[IniProperty("FlickrTokenSecret", Description = "The Flickr token secret", Encrypted = true, ExcludeIfNull = true)]
		public string FlickrTokenSecret;

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
