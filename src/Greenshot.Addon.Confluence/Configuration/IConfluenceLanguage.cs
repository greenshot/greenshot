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

using Dapplo.Config.Language;

namespace Greenshot.Addon.Confluence.Configuration
{
    /// <summary>
    /// The translations for the Confluence add-on
    /// </summary>
    [Language("Confluence")]
    public interface IConfluenceLanguage : ILanguage
    {
#pragma warning disable 1591
        string PluginSettings { get; }
        string LoginError { get; }
        string LabelUrl { get; }
        string LabelTimeout { get; }
        string LabelUser { get; }
        string LabelPassword { get; }
        string LoginTitle { get; }
        string Ok { get; }
        string Cancel { get; }
        string OpenPageAfterUpload { get; }
        string UploadFormat { get; }
        string CopyWikimarkup { get; }
        string Filename { get; }
        string Upload { get; }
        string UploadMenuItem { get; }
        string OpenPages { get; }
        string SearchPages { get; }
        string BrowsePages { get; }
        string SearchText { get; }
        string Search { get; }
        string Loading { get; }
        string IncludePersonSpaces { get; }
        string CommunicationWait { get; }
    }
}
