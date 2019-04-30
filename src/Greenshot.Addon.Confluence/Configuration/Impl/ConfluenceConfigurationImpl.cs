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

using Dapplo.Config.Ini;
using Greenshot.Core.Enums;

namespace Greenshot.Addon.Confluence.Configuration.Impl
{
    internal class ConfluenceConfigurationImpl : IniSectionBase<IConfluenceConfiguration>, IConfluenceConfiguration
    {
#pragma warning disable 1591
        public string Url { get; set; }
        public int Timeout { get; set; }
        public OutputFormats UploadFormat { get; set; }
        public int UploadJpegQuality { get; set; }
        public bool UploadReduceColors { get; set; }
        public string OutputFileFilenamePattern { get; set; }
        public bool OpenPageAfterUpload { get; set; }
        public bool CopyWikiMarkupForImageToClipboard { get; set; }
        public string SearchSpaceKey { get; set; }
        public bool IncludePersonSpaces { get; set; }
    }
}
