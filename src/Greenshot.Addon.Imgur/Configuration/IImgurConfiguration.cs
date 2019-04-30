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

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using Dapplo.Config.Ini;
using Dapplo.HttpExtensions.OAuth;
using Greenshot.Addon.Imgur.Entities;
using Greenshot.Addons.Core;

namespace Greenshot.Addon.Imgur.Configuration
{
	/// <summary>
	///  Configuration for the Imgur add-on
	/// </summary>
	[IniSection("Imgur")]
	[Description("Greenshot Imgur Plugin configuration")]
	public interface IImgurConfiguration : IIniSection, IDestinationFileConfiguration, IOAuth2Token
	{
#pragma warning disable 1591

        [Description("Url to Imgur system.")]
		[DefaultValue("https://api.imgur.com/3")]
		string ImgurApi3Url { get; set; }

		[Description("Copy the link, which one is controlled by the UsePageLink, on the clipboard")]
		[DefaultValue(true)]
		bool CopyLinkToClipboard { get; set; }

		[Description("Use pagelink instead of direct link on the clipboard")]
	    [DefaultValue(false)]
		bool UsePageLink { get; set; }

		[Description("Use anonymous access to Imgur")]
		[DefaultValue(true)]
		bool AnonymousAccess { get; set; }

        /// <summary>
        ///     Not stored, but read so people could theoretically specify their own consumer key.
        /// </summary>
        [DefaultValue("@credentials_imgur_consumer_key@")]
        [DataMember(EmitDefaultValue = false)]
		string ClientId { get; set; }

		/// <summary>
		///     Not stored, but read so people could theoretically specify their own consumer secret.
		/// </summary>
		[DefaultValue("@credentials_imgur_consumer_secret@")]
		[DataMember(EmitDefaultValue = false)]
		string ClientSecret { get; set; }

		[Description("Url to Imgur API.")]
		[DefaultValue("https://api.imgur.com/3")]
		string ApiUrl { get; set; }

		[Description("Is the title passed on to Imgur")]
		[DefaultValue(false)]
		bool AddTitle { get; set; }

		[Description("Is the filename passed on to Imgur")]
		[DefaultValue(false)]
		bool AddFilename { get; set; }

		[Description("Filename for the Imgur upload")]
		[DefaultValue("${capturetime:d\"yyyyMMdd-HHmm\"}")]
		string FilenamePattern { get; set; }

		[Description("Track the upload history")]
		[DefaultValue(true)]
		bool TrackHistory { get; set; }

		[Description("Imgur upload history (ImgurUploadHistory.hash=deleteHash)")]
		IDictionary<string, string> ImgurUploadHistory { get; set; }

	    [IniPropertyBehavior(Read = false, Write = false)]
	    int Credits { get; set; }

        // Not stored, only run-time!
	    [IniPropertyBehavior(Read = false, Write = false)]
	    IDictionary<string, ImgurImage> RuntimeImgurHistory { get; set; }
	}
}