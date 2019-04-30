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
using System.Runtime.Serialization;
using Dapplo.Config.Ini;
using Dapplo.HttpExtensions.OAuth;
using Greenshot.Addons.Core;

namespace Greenshot.Addon.OneDrive.Configuration
{
    /// <summary>
    /// The one drive configuration
    /// </summary>
    [IniSection("OneDrive")]
    [Description("Greenshot OneDrive Addon configuration")]
    public interface IOneDriveConfiguration : IIniSection, IDestinationFileConfiguration, IOAuth2Token
    {
#pragma warning disable 1591
        [Description("After upload copy OneDrive link to clipboard.")]
        [DefaultValue("true")]
        bool AfterUploadLinkToClipBoard { get; set; }

        [Description("Should the link be sharable or restricted.")]
        [DefaultValue(OneDriveLinkType.@private)]
        OneDriveLinkType LinkType { get; set; }

        /// <summary>
        ///     Not stored, but read so people could theoretically specify their own Client ID.
        /// </summary>
        [DefaultValue("@credentials_onedrive_client_id@")]
        [DataMember(EmitDefaultValue = false)]
        string ClientId { get; set; }

    }
}