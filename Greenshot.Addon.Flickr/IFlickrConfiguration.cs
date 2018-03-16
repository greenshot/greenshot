#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

#region Usings

using System.ComponentModel;
using System.Runtime.Serialization;
using Dapplo.Ini;
using Dapplo.Ini.Converters;
using GreenshotPlugin.Core.Enums;

#endregion

namespace Greenshot.Addon.Flickr
{
	public enum SafetyLevel
	{
		Safe = 1,
		Moderate = 2,
		Restricted = 3
	}

	/// <summary>
	///     Description of FlickrConfiguration.
	/// </summary>
	[IniSection("Flickr")]
	[Description("Greenshot Flickr Plugin configuration")]
	public interface IFlickrConfiguration : IIniSection
	{
		[Description("IsPublic.")]
		[DefaultValue(true)]
		bool IsPublic { get; set; }

		[Description("IsFamily.")]
		[DefaultValue(true)]
		bool IsFamily { get; set; }

		[Description("IsFriend.")]
		[DefaultValue(true)]
		bool IsFriend { get; set; }

		[Description("Safety level")]
		[DefaultValue(SafetyLevel.Safe)]
		SafetyLevel SafetyLevel { get; set; }

		[Description("Hidden from search")]
		[DefaultValue(false)]
		bool HiddenFromSearch { get; set; }

		[Description("What file type to use for uploading")]
		[DefaultValue(OutputFormats.png)]
		OutputFormats UploadFormat { get; set; }

		[Description("JPEG file save quality in %.")]
		[DefaultValue(80)]
		int UploadJpegQuality { get; set; }

		[Description("After upload send flickr link to clipboard.")]
		[DefaultValue(true)]
		bool AfterUploadLinkToClipBoard { get; set; }

		[Description("Use pagelink instead of direct link on the clipboard")]
		[DefaultValue(false)]
		bool UsePageLink { get; set; }

	    [TypeConverter(typeof(StringEncryptionTypeConverter))]
	    [DataMember(EmitDefaultValue = false)]
        [Description("The Flickr token")]
		string FlickrToken { get; set; }

	    [TypeConverter(typeof(StringEncryptionTypeConverter))]
	    [DataMember(EmitDefaultValue = false)]
        [Description("The Flickr token secret")]
		string FlickrTokenSecret { get; set; }
	}
}