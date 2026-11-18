/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Languages;
using Greenshot.Helpers;

namespace Greenshot.Settings.ViewModels
{
    /// <summary>
    /// The update check on the General tab: on or off, the days between the checks (UpdateCheckInterval, 0 = off),
    /// and "Check now" with the current version and the newer one
    /// </summary>
    public partial class SettingsViewModel
    {
        private const int DefaultUpdateCheckDays = 14;
        private UpdateService _updateService;
        private int _updateCheckDays = DefaultUpdateCheckDays;
        private bool _isCheckingForUpdates;
        private bool _updateCheckFailed;

        private UpdateService Updates => _updateService ??= SimpleServiceProvider.Current?.GetInstance<UpdateService>(isOptional: true) ?? new UpdateService();

        /// <summary>
        /// Check for updates (UpdateCheckInterval above 0); switching it on again takes the days shown
        /// </summary>
        public bool UpdateCheckEnabled
        {
            get => CoreConfiguration.UpdateCheckInterval > 0;
            set
            {
                if (value == UpdateCheckEnabled)
                {
                    return;
                }
                if (CoreConfiguration.UpdateCheckInterval > 0)
                {
                    _updateCheckDays = CoreConfiguration.UpdateCheckInterval;
                }
                CoreConfiguration.UpdateCheckInterval = value ? _updateCheckDays : 0;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UpdateCheckDays));
            }
        }

        /// <summary>
        /// The days between the checks, also shown while the check is off
        /// </summary>
        public int UpdateCheckDays
        {
            get => CoreConfiguration.UpdateCheckInterval > 0 ? CoreConfiguration.UpdateCheckInterval : _updateCheckDays;
            set
            {
                if (value < 1 || value == UpdateCheckDays)
                {
                    return;
                }
                _updateCheckDays = value;
                if (UpdateCheckEnabled)
                {
                    CoreConfiguration.UpdateCheckInterval = value;
                }
                OnPropertyChanged();
            }
        }

        public bool IsCheckingForUpdates
        {
            get => _isCheckingForUpdates;
            private set
            {
                _isCheckingForUpdates = value;
                OnPropertyChanged();
                OnUpdateStateChanged();
            }
        }

        /// <summary>
        /// A newer version is offered, see <see cref="UpdateService.OfferedUpdate"/>
        /// </summary>
        public bool IsUpdateOffered => !_isCheckingForUpdates && Updates.OfferedUpdate != null;

        /// <summary>
        /// The current version, and the newer one when the last check found one
        /// </summary>
        public string UpdateStateText
        {
            get
            {
                var texts = Texts.Welcome;
                if (_isCheckingForUpdates)
                {
                    return texts.SetupVersionChecking;
                }
                if (_updateCheckFailed)
                {
                    return texts.SetupVersionUnknown;
                }
                var offered = Updates.OfferedUpdate;
                if (offered != null)
                {
                    return string.Format(texts.SetupVersionOld, offered, Updates.CurrentVersion);
                }
                return Updates.LatestReleaseVersion != null
                    ? string.Format(texts.SetupVersionOk, Updates.CurrentVersion)
                    : string.Format(Texts.Settings.UpdateCurrentVersion, Updates.CurrentVersion);
            }
        }

        /// <summary>
        /// "Check now": asks the update feed, without a notification
        /// </summary>
        public async Task CheckForUpdatesNowAsync()
        {
            if (_isCheckingForUpdates)
            {
                return;
            }
            IsCheckingForUpdates = true;
            try
            {
                _updateCheckFailed = !await Updates.CheckForUpdatesAsync(showNotification: false).ConfigureAwait(true);
            }
            finally
            {
                IsCheckingForUpdates = false;
            }
        }

        /// <summary>
        /// Opens the downloads page of this edition
        /// </summary>
        public void OpenUpdateDownloads()
        {
            try
            {
                Process.Start(new ProcessStartInfo(Updates.DownloadsUrl.AbsoluteUri) { UseShellExecute = true })?.Dispose();
            }
            catch (Exception ex)
            {
                Log.Warn("Couldn't open the downloads page", ex);
            }
        }

        private void OnUpdateStateChanged()
        {
            OnPropertyChanged(nameof(IsUpdateOffered));
            OnPropertyChanged(nameof(UpdateStateText));
        }
    }
}
