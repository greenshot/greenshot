/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Greenshot.Base.Core;
using Greenshot.Base.Wpf;
using Greenshot.Helpers;

namespace Greenshot.UI.ViewModels
{
    public class BugReportViewModel : INotifyPropertyChanged
    {
        private readonly Exception _exception;
        private readonly UpdateService _updateService;
        private string _fullReport;
        private string _exceptionType;
        private string _exceptionMessage;
        private string _stackTrace;
        private string _stackTraceHash;
        private string _searchGitHubUrl;
        private string _newIssueUrl;
        private bool _isDetailsExpanded;
        private string _copyButtonText = "Copy Details";

        // Version check properties
        private string _currentVersion;
        private string _latestReleaseVersion;
        private bool _isCheckingVersion = true;
        private bool _isOutdated;
        private bool _versionCheckFailed;
        private string _upgradeDownloadUrl;

        public event PropertyChangedEventHandler PropertyChanged;

        public BugReportViewModel(Exception ex, string fullReport = null, UpdateService updateService = null)
        {
            _exception = ex;
            _fullReport = fullReport ?? (ex != null ? EnvironmentInfo.BuildReport(ex) : string.Empty);
            _updateService = updateService ?? SimpleServiceProvider.Current?.GetInstance<UpdateService>(isOptional: true) ?? new UpdateService();

            if (updateService?.CurrentVersion != null)
            {
                _currentVersion = updateService.CurrentVersion.ToString();
            }
            else
            {
                string greenshotVer = EnvironmentInfo.GetGreenshotVersion();
                if (string.IsNullOrWhiteSpace(greenshotVer) || greenshotVer == "Unknown")
                {
                    greenshotVer = _updateService.CurrentVersion?.ToString() ?? "Unknown";
                }
                if (!string.IsNullOrWhiteSpace(greenshotVer) && !greenshotVer.Contains("bit") && OsInfo.Bits != 0)
                {
                    greenshotVer += (GreenshotEnvironment.IsPortable ? " Portable" : "") + $" ({OsInfo.Bits} bit)";
                }
                _currentVersion = greenshotVer;
            }
            _upgradeDownloadUrl = UpdateService.DownloadsUri.AbsoluteUri;

            if (_updateService.LatestReleaseVersion != null)
            {
                _latestReleaseVersion = _updateService.LatestReleaseVersion.ToString();
                _isOutdated = _updateService.IsUpdateAvailable;
            }

            InitializeData();
            _ = CheckVersionAsync();
        }

        private void InitializeData()
        {
            if (_exception != null)
            {
                _exceptionType = _exception.GetType().FullName;
                _exceptionMessage = _exception.Message;
                _stackTrace = FormatExceptionStackTrace(_exception);
            }
            else if (!string.IsNullOrWhiteSpace(_fullReport))
            {
                // Parse exception type and message from full report if exception object is not available
                var lines = _fullReport.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                foreach (var line in lines)
                {
                    if (string.IsNullOrEmpty(_exceptionType) && line.StartsWith("Exception: ", StringComparison.OrdinalIgnoreCase))
                    {
                        _exceptionType = line.Substring("Exception: ".Length).Trim();
                    }
                    else if (string.IsNullOrEmpty(_exceptionMessage) && line.StartsWith("Message: ", StringComparison.OrdinalIgnoreCase))
                    {
                        _exceptionMessage = line.Substring("Message: ".Length).Trim();
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(_exceptionType))
            {
                _exceptionType = "Unhandled Exception";
            }
            if (string.IsNullOrWhiteSpace(_exceptionMessage))
            {
                _exceptionMessage = "An unexpected error occurred.";
            }

            if (string.IsNullOrWhiteSpace(_stackTrace) && !string.IsNullOrWhiteSpace(_fullReport))
            {
                _stackTrace = ExtractStackTraceFromReport(_fullReport);
            }
            if (string.IsNullOrWhiteSpace(_stackTrace) && _exception != null)
            {
                _stackTrace = _exception.ToString();
            }

            string normalized = ExceptionHelper.NormalizeStackTrace(_fullReport);
            if (string.IsNullOrWhiteSpace(normalized) && _exception != null)
            {
                normalized = ExceptionHelper.NormalizeException(_exception);
            }
            _stackTraceHash = ExceptionHelper.ComputeHash(normalized);

            _searchGitHubUrl = ExceptionHelper.GetGitHubSearchUrl(_stackTraceHash);
            _newIssueUrl = ExceptionHelper.GetNewIssueUrl(_stackTraceHash, _exceptionType);
        }

        private static string FormatExceptionStackTrace(Exception ex)
        {
            if (ex == null) return string.Empty;

            var sb = new System.Text.StringBuilder();
            var current = ex;
            int level = 0;
            while (current != null)
            {
                if (level > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("--- Inner Exception: " + current.GetType().FullName + ": " + current.Message + " ---");
                }
                else
                {
                    sb.AppendLine(current.GetType().FullName + ": " + current.Message);
                }

                if (!string.IsNullOrWhiteSpace(current.StackTrace))
                {
                    sb.AppendLine(current.StackTrace);
                }

                current = current.InnerException;
                level++;
            }
            return sb.ToString().Trim();
        }

        private static string ExtractStackTraceFromReport(string report)
        {
            if (string.IsNullOrWhiteSpace(report)) return string.Empty;

            var lines = report.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var sb = new System.Text.StringBuilder();
            bool inStackSection = false;

            foreach (var line in lines)
            {
                if (line.StartsWith("Stack:", StringComparison.OrdinalIgnoreCase) ||
                    line.StartsWith("Exception: ", StringComparison.OrdinalIgnoreCase))
                {
                    inStackSection = true;
                }
                else if (line.StartsWith("Configuration dump:", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                if (inStackSection)
                {
                    sb.AppendLine(line);
                }
            }

            string extracted = sb.ToString().Trim();
            return !string.IsNullOrWhiteSpace(extracted) ? extracted : string.Empty;
        }

        public async Task CheckVersionAsync()
        {
            IsCheckingVersion = true;
            try
            {
                bool success = await _updateService.CheckForUpdatesAsync(showNotification: false).ConfigureAwait(false);
                if (success && _updateService.LatestReleaseVersion != null)
                {
                    LatestReleaseVersion = _updateService.LatestReleaseVersion.ToString();
                    IsOutdated = _updateService.IsUpdateAvailable;
                    VersionCheckFailed = false;
                }
                else if (_updateService.LatestReleaseVersion != null)
                {
                    LatestReleaseVersion = _updateService.LatestReleaseVersion.ToString();
                    IsOutdated = _updateService.IsUpdateAvailable;
                }
                else
                {
                    VersionCheckFailed = true;
                    LatestReleaseVersion = "Unavailable";
                }
            }
            catch
            {
                VersionCheckFailed = true;
                if (LatestReleaseVersion == null)
                {
                    LatestReleaseVersion = "Unavailable";
                }
            }
            finally
            {
                IsCheckingVersion = false;
            }
        }

        public Exception Exception => _exception;

        public string FullReport
        {
            get => _fullReport;
            set
            {
                if (_fullReport != value)
                {
                    _fullReport = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ExceptionType
        {
            get => _exceptionType;
            set
            {
                if (_exceptionType != value)
                {
                    _exceptionType = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ExceptionMessage
        {
            get => _exceptionMessage;
            set
            {
                if (_exceptionMessage != value)
                {
                    _exceptionMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StackTrace
        {
            get => _stackTrace;
            set
            {
                if (_stackTrace != value)
                {
                    _stackTrace = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasStackTrace));
                }
            }
        }

        public bool HasStackTrace => !string.IsNullOrWhiteSpace(_stackTrace);

        public string StackTraceHash
        {
            get => _stackTraceHash;
            set
            {
                if (_stackTraceHash != value)
                {
                    _stackTraceHash = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HasStackTraceHash));
                    OnPropertyChanged(nameof(FormattedHashDisplay));
                }
            }
        }

        public bool HasStackTraceHash => !string.IsNullOrWhiteSpace(_stackTraceHash);

        public string FormattedHashDisplay => HasStackTraceHash ? $"[{_stackTraceHash}]" : "[No Stack]";

        public string SearchGitHubUrl => _searchGitHubUrl;

        public string NewIssueUrl => _newIssueUrl;

        public string CurrentVersion
        {
            get => _currentVersion;
            set
            {
                if (_currentVersion != value)
                {
                    _currentVersion = value;
                    OnPropertyChanged();
                }
            }
        }

        public string LatestReleaseVersion
        {
            get => _latestReleaseVersion;
            set
            {
                if (_latestReleaseVersion != value)
                {
                    _latestReleaseVersion = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(VersionComparisonText));
                }
            }
        }

        public bool IsCheckingVersion
        {
            get => _isCheckingVersion;
            set
            {
                if (_isCheckingVersion != value)
                {
                    _isCheckingVersion = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsOutdated
        {
            get => _isOutdated;
            set
            {
                if (_isOutdated != value)
                {
                    _isOutdated = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(VersionComparisonText));
                }
            }
        }

        public bool VersionCheckFailed
        {
            get => _versionCheckFailed;
            set
            {
                if (_versionCheckFailed != value)
                {
                    _versionCheckFailed = value;
                    OnPropertyChanged();
                }
            }
        }

        public string UpgradeDownloadUrl
        {
            get => _upgradeDownloadUrl;
            set
            {
                if (_upgradeDownloadUrl != value)
                {
                    _upgradeDownloadUrl = value;
                    OnPropertyChanged();
                }
            }
        }

        public string VersionComparisonText
        {
            get
            {
                if (_isOutdated && !string.IsNullOrEmpty(_latestReleaseVersion) && _latestReleaseVersion != "Unavailable")
                {
                    return $"A newer version ({_latestReleaseVersion}) is available online. Upgrading may already resolve this issue!";
                }
                if (!string.IsNullOrEmpty(_latestReleaseVersion) && _latestReleaseVersion != "Unavailable")
                {
                    return $"You are using the latest version ({_currentVersion}).";
                }
                return string.Empty;
            }
        }

        public bool IsDetailsExpanded
        {
            get => _isDetailsExpanded;
            set
            {
                if (_isDetailsExpanded != value)
                {
                    _isDetailsExpanded = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ToggleDetailsText));
                }
            }
        }

        public string ToggleDetailsText => _isDetailsExpanded ? "▲ Hide Details" : "▼ Show Details";

        public string CopyButtonText
        {
            get => _copyButtonText;
            set
            {
                if (_copyButtonText != value)
                {
                    _copyButtonText = value;
                    OnPropertyChanged();
                }
            }
        }

        public void CopyReportToClipboard()
        {
            try
            {
                if (!string.IsNullOrEmpty(_fullReport))
                {
                    Clipboard.SetText(_fullReport);
                    CopyButtonText = "✓ Copied!";
                }
            }
            catch
            {
                CopyButtonText = "Failed to copy";
            }
        }

        public void CopyStackTraceToClipboard()
        {
            try
            {
                if (!string.IsNullOrEmpty(_stackTrace))
                {
                    Clipboard.SetText(_stackTrace);
                }
            }
            catch
            {
            }
        }

        public void SearchGitHub()
        {
            ExceptionHelper.OpenUrl(_searchGitHubUrl);
        }

        public void ReportIssue()
        {
            ExceptionHelper.OpenUrl(_newIssueUrl);
        }

        public void OpenUpgradePage()
        {
            ExceptionHelper.OpenUrl(_upgradeDownloadUrl);
        }

        public void ToggleDetails()
        {
            IsDetailsExpanded = !IsDetailsExpanded;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
