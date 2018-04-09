#region Greenshot GNU General License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General License for more details.
// 
// You should have received a copy of the GNU General License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

#region Usings

using System.ComponentModel;
using System.Runtime.Serialization;
using Dapplo.HttpExtensions.OAuth;
using Dapplo.Ini;
using Dapplo.Ini.Converters;
using Dapplo.InterfaceImpl.Extensions;
using Greenshot.Addons.Core.Enums;

#endregion

namespace Greenshot.Addon.Photobucket
{
	/// <summary>
	///     Description of PhotobucketConfiguration.
	/// </summary>
	[IniSection("Photobucket")]
	[Description("Greenshot Photobucket Plugin configuration")]
	public interface IPhotobucketConfiguration : IIniSection, INotifyPropertyChanged, ITransactionalProperties, IOAuth1Token
    {
		[Description("What file type to use for uploading")]
	    [DefaultValue(OutputFormats.png)]
		OutputFormats UploadFormat { get; set; }

		[Description("JPEG file save quality in %.")]
		[DefaultValue(80)]
		int UploadJpegQuality { get; set; }

		[Description("Reduce color amount of the uploaded image to 256")]
		[DefaultValue(false)]
		bool UploadReduceColors { get; set; }

		[Description("Use pagelink instead of direct link on the clipboard")]
		[DefaultValue(false)]
		bool UsePageLink { get; set; }

	    [TypeConverter(typeof(StringEncryptionTypeConverter))]
	    [DataMember(EmitDefaultValue = false)]
        [Description("The Photobucket api subdomain")]
		string SubDomain { get; set; }

	    [DataMember(EmitDefaultValue = false)]
        [Description("The Photobucket api username")]
		string Username { get; set; }

        [Description("The Photobucket album to use")]
        string Album { get; set; }

        [IniPropertyBehavior(Read = false, Write = false)]
		int Credits { get; set; }

        /// <summary>
        ///     Not stored, but read so people could theoretically specify their own Client ID.
        /// </summary>
        [DefaultValue("@credentials_photobucket_consumer_key@")]
        [DataMember(EmitDefaultValue = false)]
        string ClientId { get; set; }

        /// <summary>
        ///     Not stored, but read so people could theoretically specify their own client secret.
        /// </summary>
        [DefaultValue("@credentials_photobucket_consumer_secret@")]
        [DataMember(EmitDefaultValue = false)]
        string ClientSecret { get; set; }
    }
}