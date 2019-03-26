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

using System.Diagnostics.CodeAnalysis;
using Dapplo.Config.Language;

namespace Greenshot.Addon.Flickr.Configuration.Impl
{
    /// <summary>
    /// This implements IFlickrLanguage and takes care of storing, all setters are replaced via AutoProperties.Fody
    /// </summary>
#pragma warning disable CS1591
    [SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty")]
    public class FlickrLanguageImpl : LanguageBase<IFlickrLanguage>, IFlickrLanguage
    {
        public string CommunicationWait { get; }
        public string Configure { get; }
        public string LabelAfterUpload { get; }
        public string LabelAfterUploadLinkToClipBoard { get; }
        public string LabelHiddenFromSearch { get; }
        public string LabelSafetyLevel { get; }
        public string LabelUploadFormat { get; }
        public string SettingsTitle { get; }
        public string UploadFailure { get; }
        public string UploadMenuItem { get; }
        public string UploadSuccess { get; }
        public string Public { get; }
        public string Family { get; }
        public string Friend { get; }
    }
}
