//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System.Collections.Generic;
using System.ComponentModel;
using Dapplo.Ini;
using Dapplo.HttpExtensions.OAuth;
using Greenshot.Core.Configuration;

#endregion

namespace Greenshot.Addon.Imgur
{
	/// <summary>
	///     Description of ImgurConfiguration.
	/// </summary>
	[IniSection("Imgur")]
	[Description("Greenshot Imgur Plugin configuration")]
	public interface IImgurConfiguration : IIniSection<IImgurConfiguration>, IOAuth2Token
	{
		[Description("Is the filename passed on to Imgur")]
		[DefaultValue(false)]
		bool AddFilename { get; set; }

		[Description("Is the title passed on to Imgur")]
		[DefaultValue(false)]
		bool AddTitle { get; set; }

		[Description("Use anonymous access to Imgur")]
		[DefaultValue(true)]
		bool AnonymousAccess { get; set; }

		[Description("Url to Imgur API.")]
		[DefaultValue("https://api.imgur.com/3")]
		string ApiUrl { get; set; }

		/// <summary>
		///     Not stored, but read so people could theoretically specify their own consumer key.
		/// </summary>
		[IniPropertyBehavior(Write = false)]
		[DefaultValue("@credentials_imgur_consumer_key@")]
		string ClientId { get; set; }

		/// <summary>
		///     Not stored, but read so people could theoretically specify their own consumer secret.
		/// </summary>
		[IniPropertyBehavior(Write = false)]
		[DefaultValue("@credentials_imgur_consumer_secret@")]
		string ClientSecret { get; set; }

		[Description("Copy the URL to the clipboard")]
		[DefaultValue(true)]
		bool CopyUrlToClipboard { get; set; }

		/// <summary>
		///     Available credits
		/// </summary>
		[IniPropertyBehavior(Read = false, Write = false)]
		int Credits { get; set; }

		[Description("Filename for the Imgur upload")]
		[DefaultValue("${capturetime:d\"yyyyMMdd-HHmm\"}")]
		string FilenamePattern { get; set; }

		[Description("Imgur upload history (ImgurUploadHistory.hash=deleteHash)")]
		IDictionary<string, string> ImgurUploadHistory { get; set; }

		/// <summary>
		///     runtimeImgurHistory, not stored
		/// </summary>
		[IniPropertyBehavior(Read = false, Write = false)]
		IDictionary<string, ImageInfo> RuntimeImgurHistory { get; set; }

		[Description("Track the upload history")]
		[DefaultValue(true)]
		bool TrackHistory { get; set; }

		[Description("What file type to use for uploading")]
		[DefaultValue(OutputFormat.png)]
		OutputFormat UploadFormat { get; set; }

		[Description("JPEG file save quality in %.")]
		[DefaultValue(80)]
		int UploadJpegQuality { get; set; }

		[Description("Reduce color amount of the uploaded image to 256")]
		[DefaultValue(false)]
		bool UploadReduceColors { get; set; }

		[Description("Use pagelink instead of direct link (in clipboard and notification)")]
		[DefaultValue(false)]
		bool UsePageLink { get; set; }
	}
}