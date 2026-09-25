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
using System.IO;
using System.Text;
using System.Windows;
using Greenshot.Base.Core;
using log4net;

namespace Greenshot.UI.SelfService
{
    public class FileInfoSectionViewModel : SelfServiceSectionViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(FileInfoSectionViewModel));

        public override string Id => "files";
        public override string Title => "File Information & Logs";
        public override string Subtitle => "Log and config locations, Explorer access and separate log viewer";
        public override string Icon => "📁";

        // Log file properties
        private string _logFilePath;
        private bool _logFileExists;
        private string _logFileSizeText;
        private string _logFileModifiedText;

        // Config file properties
        private string _configFilePath;
        private bool _configFileExists;
        private string _configFileSizeText;
        private string _configFileModifiedText;

        // Log glance preview
        private string _recentLogContent;
        private string _statusMessage;

        public string LogFilePath { get => _logFilePath; private set { _logFilePath = value; OnPropertyChanged(); } }
        public bool LogFileExists { get => _logFileExists; private set { _logFileExists = value; OnPropertyChanged(); } }
        public string LogFileSizeText { get => _logFileSizeText; private set { _logFileSizeText = value; OnPropertyChanged(); } }
        public string LogFileModifiedText { get => _logFileModifiedText; private set { _logFileModifiedText = value; OnPropertyChanged(); } }

        public string ConfigFilePath { get => _configFilePath; private set { _configFilePath = value; OnPropertyChanged(); } }
        public bool ConfigFileExists { get => _configFileExists; private set { _configFileExists = value; OnPropertyChanged(); } }
        public string ConfigFileSizeText { get => _configFileSizeText; private set { _configFileSizeText = value; OnPropertyChanged(); } }
        public string ConfigFileModifiedText { get => _configFileModifiedText; private set { _configFileModifiedText = value; OnPropertyChanged(); } }

        public string RecentLogContent { get => _recentLogContent; private set { _recentLogContent = value; OnPropertyChanged(); } }
        public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }

        public FileInfoSectionViewModel()
        {
            Refresh();
        }

        public override void OnNavigatedTo()
        {
            Refresh();
        }

        public override void Refresh()
        {
            try
            {
                // Log file info
                LogFilePath = GreenshotMain.LogFileLocation ?? string.Empty;
                if (!string.IsNullOrEmpty(LogFilePath) && File.Exists(LogFilePath))
                {
                    LogFileExists = true;
                    var fi = new FileInfo(LogFilePath);
                    LogFileSizeText = FormatFileSize(fi.Length);
                    LogFileModifiedText = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
                }
                else
                {
                    LogFileExists = false;
                    LogFileSizeText = "File not found";
                    LogFileModifiedText = "N/A";
                }

                // Config file info
                ConfigFilePath = GreenshotEnvironment.ConfigLocation ?? string.Empty;
                if (!string.IsNullOrEmpty(ConfigFilePath) && File.Exists(ConfigFilePath))
                {
                    ConfigFileExists = true;
                    var fi = new FileInfo(ConfigFilePath);
                    ConfigFileSizeText = FormatFileSize(fi.Length);
                    ConfigFileModifiedText = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
                }
                else
                {
                    ConfigFileExists = false;
                    ConfigFileSizeText = "File not found";
                    ConfigFileModifiedText = "N/A";
                }

                // Brief preview snippet
                LoadRecentLog();

                StatusMessage = $"Updated at {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                Log.Error("Error updating file information", ex);
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        public void LoadRecentLog()
        {
            if (string.IsNullOrEmpty(LogFilePath) || !File.Exists(LogFilePath))
            {
                RecentLogContent = "Log file does not currently exist at: " + LogFilePath;
                return;
            }

            try
            {
                // Read the tail of the log safely sharing read/write (up to last 16KB for quick summary)
                using var fs = new FileStream(LogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                const int maxBytesToRead = 16 * 1024;
                long startOffset = Math.Max(0, fs.Length - maxBytesToRead);
                fs.Seek(startOffset, SeekOrigin.Begin);

                using var reader = new StreamReader(fs, Encoding.UTF8);
                string text = reader.ReadToEnd();

                if (startOffset > 0)
                {
                    int firstNewLine = text.IndexOf('\n');
                    if (firstNewLine >= 0 && firstNewLine < text.Length - 1)
                    {
                        text = text.Substring(firstNewLine + 1);
                    }
                }

                RecentLogContent = string.IsNullOrWhiteSpace(text) ? "(Log file is empty)" : text.TrimEnd();
            }
            catch (Exception ex)
            {
                Log.Error("Error reading log tail", ex);
                RecentLogContent = $"Error reading log file:\n{ex.Message}";
            }
        }

        public void OpenLogInExplorer()
        {
            OpenPathInExplorer(LogFilePath, "Log file");
        }

        public void OpenConfigInExplorer()
        {
            OpenPathInExplorer(ConfigFilePath, "Configuration file");
        }

        private void OpenPathInExplorer(string path, string fileDescription)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    StatusMessage = $"No path specified for {fileDescription}.";
                    return;
                }

                if (File.Exists(path) || Directory.Exists(path))
                {
                    // Use the ExplorerHelper!
                    ExplorerHelper.OpenInExplorer(path);
                    StatusMessage = $"Opened Explorer for {fileDescription}.";
                }
                else
                {
                    // If file doesn't exist, open its directory if that exists
                    string dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    {
                        ExplorerHelper.OpenInExplorer(dir);
                        StatusMessage = $"File not found, opened containing folder: {dir}";
                    }
                    else
                    {
                        StatusMessage = $"{fileDescription} does not exist at: {path}";
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error opening {fileDescription} in Explorer", ex);
                StatusMessage = $"Failed to open Explorer: {ex.Message}";
            }
        }

        public void OpenLogViewer(Window owner = null)
        {
            try
            {
                var viewer = new LogViewerWindow(LogFilePath);
                if (owner != null && owner.IsLoaded && owner.IsVisible)
                {
                    viewer.Owner = owner;
                }
                viewer.Show();
                StatusMessage = "Opened Log Viewer window.";
            }
            catch (Exception ex)
            {
                Log.Error("Error opening LogViewerWindow", ex);
                StatusMessage = $"Could not open log viewer: {ex.Message}";
            }
        }

        public void CopyLogPath()
        {
            CopyText(LogFilePath, "Log path copied to clipboard.");
        }

        public void CopyConfigPath()
        {
            CopyText(ConfigFilePath, "Config path copied to clipboard.");
        }

        private void CopyText(string text, string successMessage)
        {
            try
            {
                if (!string.IsNullOrEmpty(text))
                {
                    Clipboard.SetText(text);
                    StatusMessage = successMessage;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Copy failed: {ex.Message}";
            }
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes >= 1024 * 1024)
            {
                return $"{bytes / (1024.0 * 1024.0):F2} MB ({bytes:N0} bytes)";
            }
            if (bytes >= 1024)
            {
                return $"{bytes / 1024.0:F1} KB ({bytes:N0} bytes)";
            }
            return $"{bytes} bytes";
        }
    }
}
