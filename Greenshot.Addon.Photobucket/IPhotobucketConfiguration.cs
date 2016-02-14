/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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

using System.ComponentModel;
using Dapplo.Config.Converters;
using Dapplo.Config.Ini;
using Greenshot.Addon.Configuration;

namespace Greenshot.Addon.Photobucket
{
	/// <summary>
	/// Description of IPhotobucketConfiguration.
	/// </summary>
	[IniSection("Photobucket"), Description("Greenshot Photobucket Plugin configuration")]
	public interface IPhotobucketConfiguration : IIniSection<IPhotobucketConfiguration>
	{
		[Description("What file type to use for uploading"), DefaultValue(OutputFormat.png)]
		OutputFormat UploadFormat
		{
			get;
			set;
		}

		[Description("JPEG file save quality in %."), DefaultValue(80)]
		int UploadJpegQuality
		{
			get;
			set;
		}

		[Description("Reduce color amount of the uploaded image to 256"), DefaultValue(false)]
		bool UploadReduceColors
		{
			get;
			set;
		}

		[Description("Use pagelink instead of direct link on the clipboard"), DefaultValue(false)]
		bool UsePageLink
		{
			get;
			set;
		}

		[Description("Place upload link on the clipboard"), DefaultValue(true)]
		bool AfterUploadLinkToClipBoard
		{
			get;
			set;
		}

		[Description("The Photobucket token"), TypeConverter(typeof (StringEncryptionTypeConverter))]
		string Token
		{
			get;
			set;
		}

		[Description("The Photobucket token secret"), TypeConverter(typeof (StringEncryptionTypeConverter))]
		string TokenSecret
		{
			get;
			set;
		}

		[Description("The Photobucket api subdomain"), TypeConverter(typeof (StringEncryptionTypeConverter))]
		string SubDomain
		{
			get;
			set;
		}

		[Description("The Photobucket api username")]
		string Username
		{
			get;
			set;
		}


		[IniPropertyBehavior(Read = false, Write = false)]
		int Credits
		{
			get;
			set;
		}

		/// <summary>
		/// Not stored, but read so people could theoretically specify their own Client ID.
		/// </summary>
		[IniPropertyBehavior(Write = false), DefaultValue("@credentials_photobucket_consumer_key@")]
		string ClientId
		{
			get;
			set;
		}

		/// <summary>
		/// Not stored, but read so people could theoretically specify their own client secret.
		/// </summary>
		[IniPropertyBehavior(Write = false), DefaultValue("@credentials_photobucket_consumer_secret@")]
		string ClientSecret
		{
			get;
			set;
		}
	}
}