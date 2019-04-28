// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using Caliburn.Micro;
using Dapplo.CaliburnMicro;
using Greenshot.Addons;

namespace Greenshot.Ui.Misc.ViewModels
{
    /// <summary>
    /// 
    /// </summary>
    public class ErrorViewModel : Screen
    {
        public IGreenshotLanguage GreenshotLanguage { get; }
        public IVersionProvider VersionProvider { get; }

        public ErrorViewModel(IGreenshotLanguage greenshotLanguage,
            IVersionProvider versionProvider = null)
        {
            GreenshotLanguage = greenshotLanguage;
            VersionProvider = versionProvider;
        }

        /// <summary>
        /// Checks if the current version is the latest
        /// </summary>
        public bool IsMostRecent => VersionProvider?.CurrentVersion?.Equals(VersionProvider?.LatestVersion) ?? true;

        /// <summary>
        /// Set the exception to display
        /// </summary>
        public void SetExceptionToDisplay(Exception exception)
        {
            Stacktrace = exception.ToString(); //ToStringDemystified();
            Message = exception.Message;
        }

        /// <summary>
        /// The stacktrace to display
        /// </summary>
        public string Stacktrace { get; set; }

        /// <summary>
        /// The message to display
        /// </summary>
        public string Message { get; set; }
    }
}
