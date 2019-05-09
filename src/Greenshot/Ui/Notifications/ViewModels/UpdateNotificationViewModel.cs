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
using System.Diagnostics;
using System.Windows.Media;
using Dapplo.CaliburnMicro.Toasts.ViewModels;
using Dapplo.Log;
using Greenshot.Addons;
using Greenshot.Addons.Resources;

namespace Greenshot.Ui.Notifications.ViewModels
{
    /// <inheritdoc />
    public class UpdateNotificationViewModel : ToastBaseViewModel
    {
        private static readonly LogSource Log = new LogSource();
        private const string StableDownloadLink = "https://getgreenshot.org/downloads/";
        private readonly IGreenshotLanguage _greenshotLanguage;

        public string Url => StableDownloadLink;

        public Version LatestVersion { get; }

        public UpdateNotificationViewModel(IGreenshotLanguage greenshotLanguage, Version latestVersion)
        {
            _greenshotLanguage = greenshotLanguage;
            LatestVersion = latestVersion;
        }

        public string Message => string.Format(_greenshotLanguage.UpdateFound, LatestVersion);

        public ImageSource GreenshotIcon => GreenshotResources.Instance.GreenshotIconAsBitmapSource();

        /// <summary>
        /// Handle the click
        /// </summary>
        public void OpenDownloads()
        {
            try
            {
                var processStartInfo = new ProcessStartInfo(StableDownloadLink)
                {
                    CreateNoWindow = true,
                    UseShellExecute = true
                };
                Process.Start(processStartInfo);
            }
            catch (Exception ex)
            {
                Log.Error().WriteLine(ex, string.Format(_greenshotLanguage.ErrorOpenlink, StableDownloadLink));
            }
            finally
            {
                Close();
            }
        }
    }
}
