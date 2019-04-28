// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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

using Dapplo.Config.Ini;
using Greenshot.Addon.Office.OfficeInterop;
using Microsoft.Office.Interop.PowerPoint;

namespace Greenshot.Addon.Office.Configuration.Impl
{
    /// <summary>
    /// This implements IOfficeConfiguration and takes care of storing, all setters are replaced via AutoProperties.Fody
    /// </summary>
#pragma warning disable CS1591
    internal class OfficeConfigurationImpl : IniSectionBase<IOfficeConfiguration>, IOfficeConfiguration
    {
        public EmailFormat OutlookEmailFormat { get; set; }
        public string EmailSubjectPattern { get; set; }
        public string EmailTo { get; set; }
        public string EmailCC { get; set; }
        public string EmailBCC { get; set; }
        public bool OutlookAllowExportInMeetings { get; set; }
        public bool WordLockAspectRatio { get; set; }
        public bool PowerpointLockAspectRatio { get; set; }
        public PpSlideLayout PowerpointSlideLayout { get; set; }
    }
}
