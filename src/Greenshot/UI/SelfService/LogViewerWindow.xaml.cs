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
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Greenshot.Base.Wpf;
using log4net;

namespace Greenshot.UI.SelfService
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

        public LogViewerWindow(string logFilePath = null)
        {
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

        public void LoadLog(int initialChunkBytes = DefaultChunkBytes)
        {
            try
            {
                if (string.IsNullOrEmpty(LogFilePath) || !File.Exists(LogFilePath))
                {
                    LogText = $"Log file does not currently exist at:\n{LogFilePath}";
                    LogInfoText = "File not found";
                    StatusMessage = "Log file not found.";
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
                    LogText = "(Log file is empty)";
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

                string text = Encoding.UTF8.GetString(buffer, startByte, bytesRead - startByte);
                LogText = string.IsNullOrWhiteSpace(text) ? "(No log entries in chunk)" : text;

                UpdateLogDisplayMetrics(fi.LastWriteTime);

                // Scroll to end of tail
                LogTextBox?.ScrollToEnd();
            }
            catch (Exception ex)
            {
                Log.Error("Error reading log file", ex);
                LogText = $"Error reading log file:\n{ex.Message}";
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
                    Clipboard.SetText(LogText);
                    StatusMessage = "Log copied to clipboard!";
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
            System.Windows.Forms.Integration.ElementHost.EnableModelessKeyboardInterop(this);
        }

        private void OnWindowClosed(object sender, EventArgs e)
        {
            WpfThemeHelper.ThemeChanged -= OnThemeChanged;
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
                case Key.T:
                    WpfThemeHelper.ToggleTheme();
                    e.Handled = true;
                    break;
            }
        }

        private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount == 2)
                {
                    WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                }
                else
                {
                    DragMove();
                }
            }
        }

        private void OnThemeToggleClicked(object sender, RoutedEventArgs e)
        {
            WpfThemeHelper.ToggleTheme();
        }

        private void OnMinimizeClicked(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void OnMaximizeRestoreClicked(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void OnCloseClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ApplyImmersiveDarkMode()
        {
            try
            {
                var helper = new WindowInteropHelper(this);
                if (helper.Handle != IntPtr.Zero)
                {
                    int useImmersiveDarkMode = WpfThemeHelper.IsDarkMode ? 1 : 0;
                    int hr = DwmSetWindowAttribute(helper.Handle, 20, ref useImmersiveDarkMode, sizeof(int));
                    if (hr != 0)
                    {
                        DwmSetWindowAttribute(helper.Handle, 19, ref useImmersiveDarkMode, sizeof(int));
                    }
                }
            }
            catch
            {
                // Ignore if unsupported
            }
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
