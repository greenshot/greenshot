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

namespace Greenshot.Addon.Confluence.Configuration.Impl
{
    /// <summary>
    /// This implements IConfluenceLanguage and takes care of storing, all setters are replaced via AutoProperties.Fody
    /// </summary>
    [SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty")]
#pragma warning disable CS1591
    internal class ConfluenceLanguageImpl : LanguageBase<IConfluenceLanguage>, IConfluenceLanguage
    {
#pragma warning disable 1591
        public string PluginSettings { get; }
        public string LoginError { get; }
        public string LabelUrl { get; }
        public string LabelTimeout { get; }
        public string LabelUser { get; }
        public string LabelPassword { get; }
        public string LoginTitle { get; }
        public string Ok { get; }
        public string Cancel { get; }
        public string OpenPageAfterUpload { get; }
        public string UploadFormat { get; }
        public string CopyWikimarkup { get; }
        public string Filename { get; }
        public string Upload { get; }
        public string UploadMenuItem { get; }
        public string OpenPages { get; }
        public string SearchPages { get; }
        public string BrowsePages { get; }
        public string SearchText { get; }
        public string Search { get; }
        public string Loading { get; }
        public string IncludePersonSpaces { get; }
        public string CommunicationWait { get; }
    }
}
