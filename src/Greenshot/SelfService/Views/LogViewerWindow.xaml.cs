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
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Greenshot.Base.Core;
using Greenshot.Base.Wpf;
using log4net;
using log4net.Core;
using Greenshot.Base.Languages;

namespace Greenshot.SelfService.Views
{
    public partial class LogViewerWindow : Window, INotifyPropertyChanged
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(LogViewerWindow));

        public const int DefaultChunkBytes = 64 * 1024; // 64 KB default chunk

        private string _logFilePath;
        private string _logText;
        private string _logInfoText;
        private string _statusMessage;
        private string _chunkStatusText;
        private bool _canLoadMore;
        private bool _wrapText;
        private long _actualStartOffset;
        private long _totalFileLength;
        private string _searchText = string.Empty;
        private readonly DispatcherTimer _searchTimer;
        private int _searchMatchCount;
        private int _searchEntryCount;
        // While the window is open: the entries Greenshot logs, added to the view without reading the file again
        private LogViewerAppender _liveAppender;
        private DispatcherTimer _liveTimer;

        public event PropertyChangedEventHandler PropertyChanged;

        public string LogFilePath
        {
            get => _logFilePath;
            set { _logFilePath = value; OnPropertyChanged(); }
        }

        public string LogText
        {
            get => _logText;
            set { _logText = value; OnPropertyChanged(); }
        }

        public string LogInfoText
        {
            get => _logInfoText;
            set { _logInfoText = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public string ChunkStatusText
        {
            get => _chunkStatusText;
            set { _chunkStatusText = value; OnPropertyChanged(); }
        }

        public bool CanLoadMore
        {
            get => _canLoadMore;
            set { _canLoadMore = value; OnPropertyChanged(); }
        }

        public bool WrapText
        {
            get => _wrapText;
            set
            {
                if (_wrapText != value)
                {
                    _wrapText = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TextWrappingMode));
                }
            }
        }

        /// <summary>
        /// Only the log entries with this text are shown, from the whole log file. Empty shows the end of the log.
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                value ??= string.Empty;
                if (_searchText == value)
                {
                    return;
                }
                _searchText = value;
                OnPropertyChanged();
                // Search when the typing pauses, not for every key
                _searchTimer.Stop();
                _searchTimer.Start();
            }
        }

        /// <summary>
        /// The log levels that can be chosen, from the fewest to the most log entries
        /// </summary>
        public IReadOnlyList<string> LogLevels { get; } = new[] { Level.Error.Name, Level.Warn.Name, Level.Info.Name, Level.Debug.Name };

        /// <summary>
        /// The level of the log, a change applies until Greenshot restarts
        /// </summary>
        public string SelectedLogLevel
        {
            get => LogHelper.RootLevel?.Name;
            set
            {
                var level = string.IsNullOrEmpty(value) ? null : LogManager.GetRepository().LevelMap[value];
                if (level == null || level == LogHelper.RootLevel)
                {
                    return;
                }
                // Written with the old level, so it is in the log also when the new level is less detailed
                Log.Info($"Log level changed from {LogHelper.RootLevel?.Name} to {level.Name} in the log viewer, until Greenshot restarts");
                LogHelper.RootLevel = level;
                OnPropertyChanged();
            }
        }

        public TextWrapping TextWrappingMode => WrapText ? TextWrapping.Wrap : TextWrapping.NoWrap;

        // Theme brushes forwarding
        public SolidColorBrush WindowBackgroundBrush => WpfThemeHelper.WindowBackground;
        public SolidColorBrush CardBackgroundBrush => WpfThemeHelper.CardBackground;
        public SolidColorBrush CardBorderBrush => WpfThemeHelper.CardBorder;
        public SolidColorBrush TextPrimaryBrush => WpfThemeHelper.TextPrimary;
        public SolidColorBrush TextSecondaryBrush => WpfThemeHelper.TextSecondary;
        public SolidColorBrush AccentBrush => WpfThemeHelper.Accent;
        public SolidColorBrush BadgeBackgroundBrush => WpfThemeHelper.BadgeBackground;
        public string ThemeToggleIcon => WpfThemeHelper.IsDarkMode ? "☀️" : "🌙";
        public string ThemeToggleToolTip => WpfThemeHelper.IsDarkMode ? "Switch to Light Mode" : "Switch to Dark Mode";

        // The log viewer opened by greenshot://log links: a link reuses it, so a web page can't open window after window
        private static LogViewerWindow _linkInstance;

        /// <summary>
        /// Shows the log of this Greenshot for a greenshot://log link, in the window of an earlier link when it is still open
        /// </summary>
        /// <param name="search">The search text of the link, null or empty shows the end of the log</param>
        public static void ShowForLink(string search)
        {
            if (_linkInstance == null || !_linkInstance.IsLoaded)
            {
                _linkInstance = new LogViewerWindow(search: search);
                _linkInstance.Closed += (_, _) => _linkInstance = null;
                _linkInstance.Show();
            }
            else
            {
                _linkInstance.SearchText = search?.Trim() ?? string.Empty;
                _linkInstance.ApplySearch();
                if (_linkInstance.WindowState == WindowState.Minimized)
                {
                    _linkInstance.WindowState = WindowState.Normal;
                }
            }
            _linkInstance.Activate();
        }

        /// <param name="logFilePath">The log file, null for the log of this Greenshot</param>
        /// <param name="search">Show only the log entries with this text, e.g. from a greenshot://log?search= link</param>
        public LogViewerWindow(string logFilePath = null, string search = null)
        {
            _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _searchTimer.Tick += (_, _) => ApplySearch();
            _searchText = search?.Trim() ?? string.Empty;

            InitializeComponent();

            try
            {
                var iconUri = new Uri("pack://application:,,,/Greenshot;component/icons/applicationIcon/icon.ico", UriKind.RelativeOrAbsolute);
                Icon = BitmapFrame.Create(iconUri);
            }
            catch
            {
                // Ignore under test runner
            }

            LogFilePath = logFilePath ?? GreenshotMain.LogFileLocation ?? string.Empty;
            DataContext = this;

            LoadLog();

            WpfThemeHelper.ThemeChanged += OnThemeChanged;
            Loaded += OnWindowLoaded;
            Closed += OnWindowClosed;
            KeyDown += OnWindowKeyDown;
        }

        /// <summary>
        /// Shows the end of the log, or the search result when there is a search text
        /// </summary>
        public void LoadLog(int initialChunkBytes = DefaultChunkBytes)
        {
            if (!string.IsNullOrEmpty(SearchText) && File.Exists(LogFilePath))
            {
                ApplySearch();
                return;
            }
            LoadTail(initialChunkBytes);
        }

        /// <summary>
        /// Shows the log entries with the search text from the whole log file, or the end of the log without a search text
        /// </summary>
        public void ApplySearch()
        {
            _searchTimer.Stop();
            if (string.IsNullOrEmpty(SearchText) || !File.Exists(LogFilePath))
            {
                LoadTail(DefaultChunkBytes);
                return;
            }

            try
            {
                var fi = new FileInfo(LogFilePath);
                string text;
                using (var fs = new FileStream(LogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var reader = new StreamReader(fs, Encoding.UTF8))
                {
                    text = reader.ReadToEnd();
                }

                // The entries collected until now are in the file
                _liveAppender?.TakeEntries();
                LogText = LogSearch.Filter(text, SearchText, out _searchMatchCount, out _searchEntryCount);
                _totalFileLength = fi.Length;
                _actualStartOffset = 0;
                CanLoadMore = false;
                ChunkStatusText = string.Empty;
                UpdateSearchInfo();
                StatusMessage = $"Searched at {DateTime.Now:HH:mm:ss} | Last modified: {fi.LastWriteTime:yyyy-MM-dd HH:mm:ss}";
                LogTextBox?.ScrollToEnd();
            }
            catch (Exception ex)
            {
                Log.Error("Error searching log file", ex);
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private void UpdateSearchInfo()
        {
            string matchesTemplate = Texts.SelfService.LogviewerSearchMatches;
            LogInfoText = string.Format(string.IsNullOrEmpty(matchesTemplate) ? "{0:N0} of {1:N0} log entries contain \"{2}\"" : matchesTemplate, _searchMatchCount, _searchEntryCount, SearchText);
        }

        /// <summary>
        /// Adds the entries Greenshot logged since the last time, filtered when there is a search text
        /// </summary>
        private void OnLiveTick(object sender, EventArgs e)
        {
            string added = _liveAppender?.TakeEntries();
            if (string.IsNullOrEmpty(added))
            {
                return;
            }

            bool followEnd = LogTextBox == null || LogTextBox.VerticalOffset + LogTextBox.ViewportHeight >= LogTextBox.ExtentHeight - 2;
            if (string.IsNullOrEmpty(SearchText))
            {
                LogText += added;
                _totalFileLength += Encoding.UTF8.GetByteCount(added);
                UpdateLogDisplayMetrics(DateTime.Now);
            }
            else
            {
                string matches = LogSearch.Filter(added, SearchText, out int matchCount, out int entryCount);
                _searchMatchCount += matchCount;
                _searchEntryCount += entryCount;
                UpdateSearchInfo();
                if (matches.Length == 0)
                {
                    return;
                }
                LogText += matches;
            }

            if (followEnd)
            {
                LogTextBox?.ScrollToEnd();
            }
        }

        private void LoadTail(int initialChunkBytes)
        {
            try
            {
                if (string.IsNullOrEmpty(LogFilePath) || !File.Exists(LogFilePath))
                {
                    string notFoundTemplate = Texts.SelfService.LogviewerStatusNotfound;
                    LogText = string.Format(string.IsNullOrEmpty(notFoundTemplate) ? "Log file does not currently exist at:\n{0}" : notFoundTemplate, LogFilePath);
                    LogInfoText = Texts.SelfService.FilesNotFound ?? "File not found";
                    StatusMessage = LogInfoText;
                    ChunkStatusText = string.Empty;
                    CanLoadMore = false;
                    _actualStartOffset = 0;
                    _totalFileLength = 0;
                    return;
                }

                var fi = new FileInfo(LogFilePath);
                _totalFileLength = fi.Length;

                if (_totalFileLength == 0)
                {
                    LogText = Texts.SelfService.LogviewerEmpty ?? "(Log file is empty)";
                    _actualStartOffset = 0;
                    CanLoadMore = false;
                    ChunkStatusText = string.Empty;
                    LogInfoText = "Size: 0 KB | 0 lines";
                    StatusMessage = $"Empty log file | Last modified: {fi.LastWriteTime:yyyy-MM-dd HH:mm:ss}";
                    return;
                }

                // Read tail chunk
                long readOffset = Math.Max(0, _totalFileLength - initialChunkBytes);
                int bytesToRead = (int)(_totalFileLength - readOffset);

                using var fs = new FileStream(LogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                byte[] buffer = new byte[bytesToRead];
                fs.Seek(readOffset, SeekOrigin.Begin);
                int bytesRead = fs.Read(buffer, 0, bytesToRead);

                int startByte = 0;
                if (readOffset > 0)
                {
                    // Find first newline and discard partial line before it to start at a complete line
                    for (int i = 0; i < bytesRead; i++)
                    {
                        if (buffer[i] == (byte)'\n')
                        {
                            startByte = i + 1;
                            break;
                        }
                    }
                }

                _actualStartOffset = readOffset + startByte;
                CanLoadMore = _actualStartOffset > 0;

                // The entries collected until now are in the file
                _liveAppender?.TakeEntries();
                string text = Encoding.UTF8.GetString(buffer, startByte, bytesRead - startByte);
                LogText = string.IsNullOrWhiteSpace(text) ? "(No log entries in chunk)" : text;

                UpdateLogDisplayMetrics(fi.LastWriteTime);

                // Scroll to end of tail
                LogTextBox?.ScrollToEnd();
            }
            catch (Exception ex)
            {
                Log.Error("Error reading log file", ex);
                string errTemplate = Texts.SelfService.LogviewerStatusError;
                LogText = string.Format(string.IsNullOrEmpty(errTemplate) ? "Error reading log file:\n{0}" : errTemplate, ex.Message);
                LogInfoText = "Read error";
                StatusMessage = $"Error: {ex.Message}";
                ChunkStatusText = string.Empty;
                CanLoadMore = false;
            }
        }

        public void LoadEarlierLines(int chunkBytes = DefaultChunkBytes)
        {
            if (_actualStartOffset <= 0)
            {
                CanLoadMore = false;
                return;
            }

            try
            {
                if (!File.Exists(LogFilePath)) return;

                var fi = new FileInfo(LogFilePath);
                long targetStartOffset = Math.Max(0, _actualStartOffset - chunkBytes);
                int bytesToRead = (int)(_actualStartOffset - targetStartOffset);

                using var fs = new FileStream(LogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                byte[] buffer = new byte[bytesToRead];
                fs.Seek(targetStartOffset, SeekOrigin.Begin);
                int bytesRead = fs.Read(buffer, 0, bytesToRead);

                int startByte = 0;
                if (targetStartOffset > 0)
                {
                    // Discard partial line at the start of the chunk
                    for (int i = 0; i < bytesRead; i++)
                    {
                        if (buffer[i] == (byte)'\n')
                        {
                            startByte = i + 1;
                            break;
                        }
                    }
                }

                _actualStartOffset = targetStartOffset + startByte;
                CanLoadMore = _actualStartOffset > 0;

                string earlierText = Encoding.UTF8.GetString(buffer, startByte, bytesRead - startByte);
                if (!string.IsNullOrEmpty(earlierText))
                {
                    LogText = earlierText + LogText;
                }

                UpdateLogDisplayMetrics(fi.LastWriteTime);
                StatusMessage = $"Loaded earlier chunk at {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                Log.Error("Error loading earlier log chunk", ex);
                StatusMessage = $"Error loading earlier chunk: {ex.Message}";
            }
        }

        public void LoadEntireLog()
        {
            if (_actualStartOffset <= 0) return;

            try
            {
                if (!File.Exists(LogFilePath)) return;

                var fi = new FileInfo(LogFilePath);
                int bytesToRead = (int)_actualStartOffset;

                using var fs = new FileStream(LogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                byte[] buffer = new byte[bytesToRead];
                fs.Seek(0, SeekOrigin.Begin);
                int bytesRead = fs.Read(buffer, 0, bytesToRead);

                _actualStartOffset = 0;
                CanLoadMore = false;

                string remainingText = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                if (!string.IsNullOrEmpty(remainingText))
                {
                    LogText = remainingText + LogText;
                }

                UpdateLogDisplayMetrics(fi.LastWriteTime);
                StatusMessage = $"Complete log loaded at {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                Log.Error("Error loading entire log", ex);
                StatusMessage = $"Error loading entire log: {ex.Message}";
            }
        }

        private void UpdateLogDisplayMetrics(DateTime lastWriteTime)
        {
            int lineCount = 0;
            int pos = 0;
            string text = LogText ?? string.Empty;
            while ((pos = text.IndexOf('\n', pos)) != -1)
            {
                lineCount++;
                pos++;
            }

            string totalSizeText = _totalFileLength >= 1024 * 1024
                ? $"{_totalFileLength / (1024.0 * 1024.0):F2} MB"
                : $"{_totalFileLength / 1024.0:F1} KB";

            long loadedBytes = Math.Max(0, _totalFileLength - _actualStartOffset);
            string loadedSizeText = loadedBytes >= 1024 * 1024
                ? $"{loadedBytes / (1024.0 * 1024.0):F2} MB"
                : $"{loadedBytes / 1024.0:F1} KB";

            if (_actualStartOffset > 0)
            {
                LogInfoText = $"Showing ~{lineCount:N0} lines ({loadedSizeText} of {totalSizeText})";
                ChunkStatusText = $"Showing tail {loadedSizeText} of {totalSizeText} total. Earlier log lines available.";
            }
            else
            {
                LogInfoText = $"Complete log: ~{lineCount:N0} lines ({totalSizeText})";
                ChunkStatusText = $"All {totalSizeText} loaded.";
            }

            StatusMessage = $"Loaded at {DateTime.Now:HH:mm:ss} | Last modified: {lastWriteTime:yyyy-MM-dd HH:mm:ss}";
        }

        private void OnLoadEarlierLinesClicked(object sender, RoutedEventArgs e)
        {
            LoadEarlierLines();
        }

        private void OnLoadEntireLogClicked(object sender, RoutedEventArgs e)
        {
            LoadEntireLog();
        }

        private void OnRefreshLogClicked(object sender, RoutedEventArgs e)
        {
            LoadLog();
        }

        private void OnCopyLogClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(LogText))
                {
                    ClipboardHelper.SetClipboardData(LogText);
                    string copiedTemplate = Texts.SelfService.LogviewerCopied;
                    StatusMessage = string.Format(string.IsNullOrEmpty(copiedTemplate) ? "Copied entire log content to clipboard ({0:N0} characters)" : copiedTemplate, LogText.Length);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Copy failed: {ex.Message}";
            }
        }

        private void OnThemeChanged()
        {
            ApplyImmersiveDarkMode();
            OnPropertyChanged(nameof(WindowBackgroundBrush));
            OnPropertyChanged(nameof(CardBackgroundBrush));
            OnPropertyChanged(nameof(CardBorderBrush));
            OnPropertyChanged(nameof(TextPrimaryBrush));
            OnPropertyChanged(nameof(TextSecondaryBrush));
            OnPropertyChanged(nameof(AccentBrush));
            OnPropertyChanged(nameof(BadgeBackgroundBrush));
            OnPropertyChanged(nameof(ThemeToggleIcon));
            OnPropertyChanged(nameof(ThemeToggleToolTip));
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            ApplyImmersiveDarkMode();

            // Follow the log of this Greenshot, another log file is shown as it is
            if (!string.IsNullOrEmpty(LogFilePath) && string.Equals(LogFilePath, GreenshotMain.LogFileLocation, StringComparison.OrdinalIgnoreCase))
            {
                _liveAppender = LogViewerAppender.Attach();
                _liveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
                _liveTimer.Tick += OnLiveTick;
                _liveTimer.Start();
            }
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            WpfThemeHelper.ThemeChanged -= OnThemeChanged;
            _searchTimer.Stop();
            _liveTimer?.Stop();
            _liveAppender?.Detach();
            _liveAppender = null;
        }

        private void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    Close();
                    e.Handled = true;
                    break;
                case Key.F5:
                    LoadLog();
                    e.Handled = true;
                    break;
                case Key.F when Keyboard.Modifiers == ModifierKeys.Control:
                    SearchTextBox.Focus();
                    SearchTextBox.SelectAll();
                    e.Handled = true;
                    break;
                // Not while typing a search text
                case Key.T when !Equals(e.OriginalSource, SearchTextBox):
                    WpfThemeHelper.ToggleTheme();
                    e.Handled = true;
                    break;
            }
        }

        private void ApplyImmersiveDarkMode()
        {
            // The title bar in the colors of the theme, it follows theme changes from now on
            WindowFrameTheme.Attach(this);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
