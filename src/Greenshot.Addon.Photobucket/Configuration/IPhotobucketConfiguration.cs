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

using System.ComponentModel;
using System.Runtime.Serialization;
using Dapplo.Config.Ini;
using Dapplo.Config.Ini.Converters;
using Dapplo.HttpExtensions.OAuth;
using Greenshot.Addons.Core;
#pragma warning disable 1591

namespace Greenshot.Addon.Photobucket.Configuration
{
	/// <summary>
	///     The Photobucket configuration.
	/// </summary>
	[IniSection("Photobucket")]
	[Description("Greenshot Photobucket Plugin configuration")]
	public interface IPhotobucketConfiguration : IIniSection, IDestinationFileConfiguration, IOAuth1Token
    {
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