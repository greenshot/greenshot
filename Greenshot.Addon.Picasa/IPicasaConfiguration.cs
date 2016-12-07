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

using System.ComponentModel;
using Dapplo.Ini;
using Dapplo.HttpExtensions.OAuth;
using Greenshot.Core.Configuration;

#endregion

namespace Greenshot.Addon.Picasa
{
	/// <summary>
	///     Description of PicasaConfiguration.
	/// </summary>
	[IniSection("Picasa")]
	[Description("Greenshot Picasa Plugin configuration")]
	public interface IPicasaConfiguration : IIniSection<IPicasaConfiguration>, IOAuth2Token
	{
		[Description("Is the filename passed on to Picasa")]
		[DefaultValue(false)]
		bool AddFilename { get; set; }

		[Description("After upload send Picasa link to clipboard.")]
		[DefaultValue(true)]
		bool AfterUploadLinkToClipBoard { get; set; }

		/// <summary>
		///     Not stored, but read so people could theoretically specify their own Client ID.
		/// </summary>
		[IniPropertyBehavior(Write = false)]
		[DefaultValue("@credentials_picasa_consumer_key@")]
		string ClientId { get; set; }

		/// <summary>
		///     Not stored, but read so people could theoretically specify their own client secret.
		/// </summary>
		[IniPropertyBehavior(Write = false)]
		[DefaultValue("@credentials_picasa_consumer_secret@")]
		string ClientSecret { get; set; }

		[Description("The picasa album to upload to")]
		[DefaultValue("default")]
		string UploadAlbum { get; set; }

		[Description("What file type to use for uploading")]
		[DefaultValue(OutputFormat.png)]
		OutputFormat UploadFormat { get; set; }

		[Description("JPEG file save quality in %.")]
		[DefaultValue(80)]
		int UploadJpegQuality { get; set; }

		[Description("The picasa user to upload to")]
		[DefaultValue("default")]
		string UploadUser { get; set; }
	}
}