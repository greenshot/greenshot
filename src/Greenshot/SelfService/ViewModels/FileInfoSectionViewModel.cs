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
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Wpf;
using Greenshot.SelfService.Views;
using log4net;
using Greenshot.Base.Languages;

namespace Greenshot.SelfService.ViewModels
{
    public class FileInfoSectionViewModel : SelfServiceSectionViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(FileInfoSectionViewModel));
        private static ICoreConfiguration CoreConfig => IniConfigRegistry.GetSection<ICoreConfiguration>();

        public override string Id => "files";
        public override string Title
        {
            get
            {
                return Texts.SelfService.CategoryFiles;
            }
        }

        public override string Subtitle
        {
            get
            {
                return Texts.SelfService.CategoryFilesSub;
            }
        }
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

        // Output folder check
        private string _outputConfiguredPath;
        private string _outputExpandedPath;
        private string _outputExampleFile;
        private IList<OutputPathCheckRow> _outputChecks = new List<OutputPathCheckRow>();

        public string OutputConfiguredPath { get => _outputConfiguredPath; private set { _outputConfiguredPath = value; OnPropertyChanged(); } }
        public string OutputExpandedPath { get => _outputExpandedPath; private set { _outputExpandedPath = value; OnPropertyChanged(); } }
        public string OutputExampleFile { get => _outputExampleFile; private set { _outputExampleFile = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasOutputExampleFile)); } }
        public bool HasOutputExampleFile => !string.IsNullOrEmpty(OutputExampleFile);
        public IList<OutputPathCheckRow> OutputChecks { get => _outputChecks; private set { _outputChecks = value; OnPropertyChanged(); } }

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
                    LogFileSizeText = Texts.SelfService.FilesNotFound;
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
                    ConfigFileSizeText = Texts.SelfService.FilesNotFound;
                    ConfigFileModifiedText = "N/A";
                }

                CheckOutputPath();

                // Brief preview snippet
                LoadRecentLog();

                StatusMessage = string.Format(Texts.SelfService.StatusUpdatedAt, DateTime.Now.ToString("HH:mm:ss"));
            }
            catch (Exception ex)
            {
                Log.Error("Error updating file information", ex);
                StatusMessage = string.Format(Texts.SelfService.StatusError, ex.Message);
            }
        }

        public override void OnLanguageChanged()
        {
            base.OnLanguageChanged();
            Refresh();
        }

        /// <summary>
        /// Checks where screenshots are saved, the result is shown and logged (a helpdesk asks for the log)
        /// </summary>
        public void CheckOutputPath()
        {
            var result = OutputPathCheck.Run(CoreConfig.OutputFilePath, CoreConfig.OutputFileFilenamePattern, CoreConfig.OutputFileFormat);
            OutputConfiguredPath = result.ConfiguredPath;
            OutputExpandedPath = result.ExpandedPath ?? string.Empty;
            OutputExampleFile = result.ExampleFile;
            OutputChecks = result.Items.Select(item => new OutputPathCheckRow(item)).ToList();
            // The section shows in the navigation when screenshots probably don't end up where the user expects them
            bool problem = result.Worst >= OutputPathSeverity.Warning;
            BadgeText = problem ? (result.Worst == OutputPathSeverity.Error ? "❌" : "⚠️") : null;
            BadgeBrush = problem ? WpfThemeHelper.ErrorBackground : null;
            Log.Info($"Output folder check: '{result.ConfiguredPath}' -> '{result.ExpandedPath}': {string.Join(", ", result.Items.Select(i => i.Finding + (i.Detail == null ? string.Empty : $" ({i.Detail})")))}");
        }

        public void OpenOutputFolder()
        {
            OpenPathInExplorer(OutputExpandedPath, Texts.SelfService.FilesScreenshotFolder);
        }

        public void CopyOutputPath()
        {
            CopyText(OutputExpandedPath, Texts.SelfService.FilesScreenshotFolderCopied);
        }

        /// <summary>
        /// The screenshot folder becomes the default folder again (the Desktop, or the portable folder)
        /// </summary>
        public void ResetOutputPath()
        {
            string previous = CoreConfig.OutputFilePath;
            CoreConfig.OutputFilePath = null;
            CoreConfig.ValidateAndCorrectOutputFilePath();
            Log.Info($"The user reset the screenshot folder in Self-Service: '{previous}' -> '{CoreConfig.OutputFilePath}'");
            CheckOutputPath();
            StatusMessage = string.Format(Texts.SelfService.FilesOutputResetDone, CoreConfig.OutputFilePath);
        }

        /// <summary>
        /// The filename pattern becomes the default pattern again
        /// </summary>
        public void ResetFilenamePattern()
        {
            string previous = CoreConfig.OutputFileFilenamePattern;
            string defaultPattern = typeof(ICoreConfiguration).GetProperty(nameof(ICoreConfiguration.OutputFileFilenamePattern))?
                .GetCustomAttributes(typeof(DefaultValueAttribute), false).OfType<DefaultValueAttribute>().FirstOrDefault()?.Value as string;
            CoreConfig.OutputFileFilenamePattern = defaultPattern;
            Log.Info($"The user reset the filename pattern in Self-Service: '{previous}' -> '{defaultPattern}'");
            CheckOutputPath();
            StatusMessage = string.Format(Texts.SelfService.FilesOutputResetDone, defaultPattern);
        }

        public void LoadRecentLog()
        {
            if (string.IsNullOrEmpty(LogFilePath) || !File.Exists(LogFilePath))
            {
                RecentLogContent = string.Format(Texts.SelfService.LogviewerStatusNotfound, LogFilePath);
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

                string emptyFallback = Texts.SelfService.LogviewerEmpty;
                RecentLogContent = string.IsNullOrWhiteSpace(text) ? emptyFallback : text.TrimEnd();
            }
            catch (Exception ex)
            {
                Log.Error("Error reading log tail", ex);
                RecentLogContent = string.Format(Texts.SelfService.LogviewerStatusError, ex.Message);
            }
        }

        public void OpenLogInExplorer()
        {
            OpenPathInExplorer(LogFilePath, Texts.SelfService.FilesLogFile);
        }

        public void OpenConfigInExplorer()
        {
            OpenPathInExplorer(ConfigFilePath, Texts.SelfService.FilesConfigFile);
        }

        private void OpenPathInExplorer(string path, string fileDescription)
        {
            try
            {
                if (string.IsNullOrEmpty(path))
                {
                    StatusMessage = string.Format(Texts.SelfService.FilesNoPath, fileDescription);
                    return;
                }

                if (File.Exists(path) || Directory.Exists(path))
                {
                    // Use the ExplorerHelper!
                    ExplorerHelper.OpenInExplorer(path);
                    StatusMessage = string.Format(Texts.SelfService.FilesOpenedExplorer, fileDescription);
                }
                else
                {
                    // If file doesn't exist, open its directory if that exists
                    string dir = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    {
                        ExplorerHelper.OpenInExplorer(dir);
                        StatusMessage = string.Format(Texts.SelfService.FilesOpenedFolder, dir);
                    }
                    else
                    {
                        StatusMessage = string.Format(Texts.SelfService.FilesDoesNotExist, fileDescription, path);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error opening {fileDescription} in Explorer", ex);
                StatusMessage = string.Format(Texts.SelfService.FilesExplorerFailed, ex.Message);
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
                StatusMessage = Texts.SelfService.FilesLogviewerOpened;
            }
            catch (Exception ex)
            {
                Log.Error("Error opening LogViewerWindow", ex);
                StatusMessage = string.Format(Texts.SelfService.FilesLogviewerFailed, ex.Message);
            }
        }

        public void CopyLogPath()
        {
            CopyText(LogFilePath, Texts.SelfService.FilesLogPathCopied);
        }

        public void CopyConfigPath()
        {
            CopyText(ConfigFilePath, Texts.SelfService.FilesConfigPathCopied);
        }

        private void CopyText(string text, string successMessage)
        {
            try
            {
                if (!string.IsNullOrEmpty(text))
                {
                    ClipboardHelper.SetClipboardData(text);
                    StatusMessage = successMessage;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = string.Format(Texts.SelfService.StatusCopyFailed, ex.Message);
            }
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes >= 1024 * 1024)
            {
                return $"{bytes / (1024.0 * 1024.0):F2} MB ({string.Format(Texts.SelfService.FilesSizeBytes, bytes)})";
            }
            if (bytes >= 1024)
            {
                return $"{bytes / 1024.0:F1} KB ({string.Format(Texts.SelfService.FilesSizeBytes, bytes)})";
            }
            return string.Format(Texts.SelfService.FilesSizeBytes, bytes);
        }
    }
}
