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

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Dapplo.Config.Ini;
using Dapplo.HttpExtensions.OAuth;
using Greenshot.Addons.Core;

namespace Greenshot.Addon.Flickr.Configuration
{
    /// <summary>
	///     This defines the configuration for the Flickr addon
	/// </summary>
	[IniSection("Flickr")]
	[Description("Greenshot Flickr Plugin configuration")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public interface IFlickrConfiguration : IIniSection, IDestinationFileConfiguration, IOAuth1Token
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

        /// <summary>
        /// 
        /// </summary>
		[Description("Safety level")]
		[DefaultValue(SafetyLevel.Safe)]
		SafetyLevel SafetyLevel { get; set; }

        /// <summary>
        /// Hide the image from the search results in Flickr
        /// </summary>
		[Description("Hidden from search")]
		[DefaultValue(false)]
		bool HiddenFromSearch { get; set; }

        /// <summary>
        /// Place the link to Flickr onto the clipboard after it's uploaded
        /// </summary>
		[Description("After upload send flickr link to clipboard.")]
		[DefaultValue(true)]
		bool AfterUploadLinkToClipBoard { get; set; }

        /// <summary>
        /// Defines if we use the pagelink or direct link on the clipboard
        /// </summary>
		[Description("Use pagelink instead of direct link on the clipboard")]
		[DefaultValue(false)]
		bool UsePageLink { get; set; }

        /// <summary>
        ///     Not stored, but read so people could theoretically specify their own Client ID.
        /// </summary>
        [DefaultValue("@credentials_flickr_consumer_key@")]
        [DataMember(EmitDefaultValue = false)]
        string ClientId { get; set; }

        /// <summary>
        ///     Not stored, but read so people could theoretically specify their own client secret.
        /// </summary>
        [DefaultValue("@credentials_flickr_consumer_secret@")]
        [DataMember(EmitDefaultValue = false)]
        string ClientSecret { get; set; }
    }
}