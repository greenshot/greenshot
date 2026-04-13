/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using Greenshot.Base.Core;
using Greenshot.Helpers;

namespace Greenshot.ViewModels
{
    public class BugReportViewModel : INotifyPropertyChanged
    {
        private readonly Exception _exception;
        private readonly string _bugReport;
        private readonly string _hash;
        private bool _isUpdateAvailable;
        private string _latestVersion;

        public event PropertyChangedEventHandler PropertyChanged;

        public BugReportViewModel(Exception exception)
        {
            _exception = exception;
            _hash = ExceptionHelper.GetStacktraceHash(exception);
            _bugReport = EnvironmentInfo.BuildReport(exception);
            
            CheckForUpdates();
        }

        public string ExceptionType => _exception?.GetType().Name ?? "Unknown Exception";
        public string Message => _exception?.Message ?? "No message available";
        public string Hash => _hash;
        public string BugReport => _bugReport;

        public bool IsUpdateAvailable
        {
            get => _isUpdateAvailable;
            set
            {
                _isUpdateAvailable = value;
                OnPropertyChanged();
            }
        }

        public string LatestVersion
        {
            get => _latestVersion;
            set
            {
                _latestVersion = value;
                OnPropertyChanged();
            }
        }

        public string GitHubSearchUrl => $"https://github.com/greenshot/greenshot/issues?q=is:issue+{_hash}";

        private void CheckForUpdates()
        {
            try
            {
                var updateService = SimpleServiceProvider.Current.GetInstance<UpdateService>(true);
                if (updateService != null)
                {
                    IsUpdateAvailable = updateService.IsUpdateAvailable || updateService.IsBetaUpdateAvailable;
                    
                    Version latest = updateService.LatestReleaseVersion;
                    if (updateService.LatestBetaVersion > latest)
                    {
                        latest = updateService.LatestBetaVersion;
                    }
                    LatestVersion = latest.ToString();
                }
            }
            catch (Exception)
            {
                // Ignore update check errors
            }
        }

        public void CopyToClipboard()
        {
            try
            {
                Clipboard.SetText(_bugReport);
            }
            catch (Exception)
            {
                // Ignore
            }
        }

        public void SearchGitHub()
        {
            try
            {
                Process.Start(GitHubSearchUrl);
            }
            catch (Exception)
            {
                // Ignore
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
