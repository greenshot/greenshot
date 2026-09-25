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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using Dapplo.Windows.Clipboard;
using Dapplo.Windows.User32;
using Greenshot.Base.Core;
using Greenshot.Base.Wpf;
using log4net;

namespace Greenshot.UI.SelfService
{
    public class ClipboardFormatItemViewModel
    {
        public string Name { get; set; }
        public string TypeDescription { get; set; }
        public string Details { get; set; }
        public string SizeText { get; set; }
    }

    public class ClipboardMonitorEvent
    {
        public string Timestamp { get; set; }
        public bool IsBlocked { get; set; }
        public string Message { get; set; }
        public string ProcessDetails { get; set; }
        public Brush StatusBrush => IsBlocked ? WpfThemeHelper.ErrorText : WpfThemeHelper.Accent;
    }

    public class ClipboardSectionViewModel : SelfServiceSectionViewModel, IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ClipboardSectionViewModel));

        public override string Id => "clipboard";
        public override string Title
        {
            get
            {
                string title = Language.GetString("selfservice_category_clipboard");
                return string.IsNullOrEmpty(title) ? "Clipboard Diagnostics" : title;
            }
        }
        public override string Subtitle
        {
            get
            {
                string sub = Language.GetString("selfservice_category_clipboard_sub");
                return string.IsNullOrEmpty(sub) ? "Active formats and live clipboard locker detection" : sub;
            }
        }
        public override string Icon => "📋";

        // Win32 API to find which window currently holds the clipboard open (locker detection)
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetOpenClipboardWindow();

        // State properties
        private bool _isBlocked;
        private string _statusHeader;
        private string _statusDetails;
        private Brush _statusBrush;

        // Blocker Process Details
        private string _blockerProcessName;
        private int _blockerProcessId;
        private string _blockerWindowTitle;
        private string _blockerExecutablePath;

        // Owner Process Details (last application that set data)
        private string _ownerProcessName;
        private int _ownerProcessId;
        private string _ownerWindowTitle;
        private string _ownerExecutablePath;
        private string _currentOwnerInfo;

        // Loop Monitor
        private bool _isMonitoring;
        private string _monitorToggleText;
        private int _blockedOccurrenceCount;
        private DispatcherTimer _monitorTimer;
        private IDisposable _clipboardSubscription;
        private int _isProcessingUpdate;

        private readonly Dispatcher _dispatcher;
        private readonly object _formatsLock = new object();
        private readonly object _monitorLogLock = new object();

        public ObservableCollection<ClipboardFormatItemViewModel> Formats { get; } = new ObservableCollection<ClipboardFormatItemViewModel>();
        public ObservableCollection<ClipboardMonitorEvent> MonitorLog { get; } = new ObservableCollection<ClipboardMonitorEvent>();

        public bool IsBlocked
        {
            get => _isBlocked;
            private set
            {
                if (_isBlocked != value)
                {
                    _isBlocked = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsBlockedVisibility));
                    OnPropertyChanged(nameof(IsAccessibleVisibility));
                }
            }
        }

        public Visibility IsBlockedVisibility => IsBlocked ? Visibility.Visible : Visibility.Collapsed;
        public Visibility IsAccessibleVisibility => IsBlocked ? Visibility.Collapsed : Visibility.Visible;

        public string StatusHeader { get => _statusHeader; private set { _statusHeader = value; OnPropertyChanged(); } }
        public string StatusDetails { get => _statusDetails; private set { _statusDetails = value; OnPropertyChanged(); } }
        public Brush StatusBrush { get => _statusBrush; private set { _statusBrush = value; OnPropertyChanged(); } }

        public string BlockerProcessName { get => _blockerProcessName; set { _blockerProcessName = value; OnPropertyChanged(); } }
        public int BlockerProcessId { get => _blockerProcessId; set { _blockerProcessId = value; OnPropertyChanged(); } }
        public string BlockerWindowTitle { get => _blockerWindowTitle; set { _blockerWindowTitle = value; OnPropertyChanged(); } }
        public string BlockerExecutablePath { get => _blockerExecutablePath; set { _blockerExecutablePath = value; OnPropertyChanged(); } }

        public string OwnerProcessName { get => _ownerProcessName; set { _ownerProcessName = value; OnPropertyChanged(); } }
        public int OwnerProcessId { get => _ownerProcessId; set { _ownerProcessId = value; OnPropertyChanged(); } }
        public string OwnerWindowTitle { get => _ownerWindowTitle; set { _ownerWindowTitle = value; OnPropertyChanged(); } }
        public string OwnerExecutablePath { get => _ownerExecutablePath; set { _ownerExecutablePath = value; OnPropertyChanged(); } }
        public string CurrentOwnerInfo { get => _currentOwnerInfo; set { _currentOwnerInfo = value; OnPropertyChanged(); } }

        public bool IsMonitoring
        {
            get => _isMonitoring;
            set
            {
                if (_isMonitoring != value)
                {
                    _isMonitoring = value;
                    OnPropertyChanged();
                    UpdateMonitorToggleText();
                }
            }
        }

        public string MonitorToggleText { get => _monitorToggleText; private set { _monitorToggleText = value; OnPropertyChanged(); } }

        private void UpdateMonitorToggleText()
        {
            string startText = Language.GetString("selfservice_clipboard_btn_start_monitor");
            string stopText = Language.GetString("selfservice_clipboard_btn_stop_monitor");
            MonitorToggleText = _isMonitoring 
                ? $"⏹ {(string.IsNullOrEmpty(stopText) ? "Stop Monitor Loop" : stopText)}" 
                : $"▶ {(string.IsNullOrEmpty(startText) ? "Start Monitor Loop" : startText)}";
        }

        public override void OnLanguageChanged()
        {
            base.OnLanguageChanged();
            UpdateMonitorToggleText();
            Refresh();
        }

        public ClipboardSectionViewModel()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            try
            {
                BindingOperations.EnableCollectionSynchronization(Formats, _formatsLock);
                BindingOperations.EnableCollectionSynchronization(MonitorLog, _monitorLogLock);
            }
            catch (Exception ex)
            {
                Log.Debug("BindingOperations.EnableCollectionSynchronization not available or failed", ex);
            }

            _monitorTimer = new DispatcherTimer(DispatcherPriority.Normal, _dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(600)
            };
            _monitorTimer.Tick += OnMonitorTimerTick;

            UpdateMonitorToggleText();
            StartListeningToClipboardUpdates();
            Refresh();
        }

        private void RunOnUIThread(Action action)
        {
            if (_dispatcher != null && !_dispatcher.HasShutdownStarted)
            {
                if (_dispatcher.CheckAccess())
                {
                    action();
                }
                else
                {
                    _dispatcher.BeginInvoke(action);
                }
            }
            else if (Application.Current?.Dispatcher != null && !Application.Current.Dispatcher.HasShutdownStarted)
            {
                if (Application.Current.Dispatcher.CheckAccess())
                {
                    action();
                }
                else
                {
                    Application.Current.Dispatcher.BeginInvoke(action);
                }
            }
            else
            {
                action();
            }
        }

        private void StartListeningToClipboardUpdates()
        {
            try
            {
                _clipboardSubscription ??= ClipboardNative.OnUpdate
                    .Throttle(TimeSpan.FromMilliseconds(150))
                    .Subscribe(updateInfo =>
                    {
                        RunOnUIThread(() => OnClipboardUpdateReceived(updateInfo));
                    }, ex => Log.Debug("Non-critical notification in ClipboardNative.OnUpdate", ex));
            }
            catch (Exception ex)
            {
                Log.Debug("Could not subscribe to ClipboardNative.OnUpdate", ex);
            }
        }

        private void OnClipboardUpdateReceived(ClipboardUpdateInformation updateInfo)
        {
            if (Interlocked.CompareExchange(ref _isProcessingUpdate, 1, 0) != 0)
            {
                return;
            }

            try
            {
                // Auto-refresh formats whenever clipboard is updated!
                QueryClipboardFormats();

                // Resolve owner process details
                IntPtr ownerHwnd = updateInfo.OwnerHandle != IntPtr.Zero ? updateInfo.OwnerHandle : ClipboardNative.CurrentOwner;
                ResolveOwnerDetails(ownerHwnd);

                // Update lock status
                CheckClipboardStatus(logToMonitor: IsMonitoring);
            }
            catch (Exception ex)
            {
                Log.Debug("Error handling clipboard update event", ex);
            }
            finally
            {
                Interlocked.Exchange(ref _isProcessingUpdate, 0);
            }
        }

        public override void OnNavigatedTo()
        {
            StartListeningToClipboardUpdates();
            Refresh();
        }

        public override void OnNavigatedFrom()
        {
            StopMonitoring();
        }

        public override void Refresh()
        {
            QueryClipboardFormats();
            ResolveOwnerDetails(ClipboardNative.CurrentOwner);
            CheckClipboardStatus(logToMonitor: false);
        }

        private void ResolveOwnerDetails(IntPtr ownerHwnd)
        {
            if (ownerHwnd != IntPtr.Zero)
            {
                ExtractProcessDetails(ownerHwnd, out string ownerProc, out int ownerPid, out string ownerTitle, out string ownerPath);
                OwnerProcessName = ownerProc;
                OwnerProcessId = ownerPid;
                OwnerWindowTitle = ownerTitle;
                OwnerExecutablePath = ownerPath;
                CurrentOwnerInfo = $"{ownerProc} (PID: {ownerPid}) - \"{ownerTitle}\"";
            }
            else
            {
                OwnerProcessName = "None";
                OwnerProcessId = 0;
                OwnerWindowTitle = "None";
                OwnerExecutablePath = "(No owner or system clipboard)";
                CurrentOwnerInfo = "None (or system clipboard)";
            }
        }

        public void CheckClipboardStatus(bool logToMonitor = false)
        {
            RunOnUIThread(() =>
            {
                try
                {
                    IntPtr openHwnd = GetOpenClipboardWindow();
                    bool canAccess = false;

                    try
                    {
                        // Use Dapplo.Windows.Clipboard to safely test access
                        using var token = ClipboardNative.Access(IntPtr.Zero, retries: 0, retryInterval: TimeSpan.Zero, timeout: TimeSpan.FromMilliseconds(40));
                        canAccess = token.CanAccess;
                    }
                    catch
                    {
                        canAccess = false;
                    }

                    // If Access failed or GetOpenClipboardWindow returned non-zero, clipboard is locked!
                    bool isBlocked = !canAccess || openHwnd != IntPtr.Zero;

                    if (isBlocked)
                    {
                        IntPtr targetHwnd = openHwnd != IntPtr.Zero ? openHwnd : GetOpenClipboardWindow();
                        ExtractProcessDetails(targetHwnd, out string procName, out int pid, out string title, out string path);

                        IsBlocked = true;
                        BlockerProcessName = procName;
                        BlockerProcessId = pid;
                        BlockerWindowTitle = title;
                        BlockerExecutablePath = path;

                        string blockedTemplate = Language.GetString("selfservice_clipboard_status_blocked_by");
                        string winLabel = Language.GetString("selfservice_clipboard_window_label");
                        string exeLabel = Language.GetString("selfservice_clipboard_executable_label");

                        StatusHeader = string.Format(string.IsNullOrEmpty(blockedTemplate) ? "Clipboard is BLOCKED by {0} (PID: {1})" : blockedTemplate, procName, pid);
                        StatusDetails = $"{string.Format(string.IsNullOrEmpty(winLabel) ? "Window: \"{0}\"" : winLabel, title)}\n{string.Format(string.IsNullOrEmpty(exeLabel) ? "Executable: {0}" : exeLabel, path)}";
                        StatusBrush = WpfThemeHelper.ErrorText;
                        string badge = Language.GetString("selfservice_clipboard_badge_blocked");
                        BadgeText = string.IsNullOrEmpty(badge) ? "BLOCKED" : badge;
                        BadgeBrush = WpfThemeHelper.ErrorBackground;

                        if (logToMonitor)
                        {
                            _blockedOccurrenceCount++;
                            lock (_monitorLogLock)
                            {
                                MonitorLog.Insert(0, new ClipboardMonitorEvent
                                {
                                    Timestamp = DateTime.Now.ToString("HH:mm:ss.fff"),
                                    IsBlocked = true,
                                    Message = StatusHeader,
                                    ProcessDetails = $"{string.Format(string.IsNullOrEmpty(winLabel) ? "Window: \"{0}\"" : winLabel, title)} | {string.Format(string.IsNullOrEmpty(exeLabel) ? "Executable: {0}" : exeLabel, path)}"
                                });
                                TrimMonitorLog();
                            }
                        }
                    }
                    else
                    {
                        IsBlocked = false;
                        BlockerProcessName = null;
                        BlockerProcessId = 0;
                        BlockerWindowTitle = null;
                        BlockerExecutablePath = null;

                        string accessibleText = Language.GetString("selfservice_clipboard_status_accessible");
                        string lastOwnerTemplate = Language.GetString("selfservice_clipboard_status_last_owner");
                        string exeLabel = Language.GetString("selfservice_clipboard_executable_label");

                        StatusHeader = string.IsNullOrEmpty(accessibleText) ? "Clipboard is Accessible (Unlocked)" : accessibleText;
                        StatusDetails = string.Format(string.IsNullOrEmpty(lastOwnerTemplate) ? "Last data set by: {0}" : lastOwnerTemplate, CurrentOwnerInfo);
                        StatusBrush = WpfThemeHelper.Accent;
                        BadgeText = null;
                        BadgeBrush = null;

                        if (logToMonitor && (_blockedOccurrenceCount > 0 || MonitorLog.Count == 0))
                        {
                            lock (_monitorLogLock)
                            {
                                MonitorLog.Insert(0, new ClipboardMonitorEvent
                                {
                                    Timestamp = DateTime.Now.ToString("HH:mm:ss.fff"),
                                    IsBlocked = false,
                                    Message = StatusHeader,
                                    ProcessDetails = $"{string.Format(string.IsNullOrEmpty(lastOwnerTemplate) ? "Last data set by: {0}" : lastOwnerTemplate, CurrentOwnerInfo)} | {string.Format(string.IsNullOrEmpty(exeLabel) ? "Executable: {0}" : exeLabel, OwnerExecutablePath)}"
                                });
                                TrimMonitorLog();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Error checking clipboard status", ex);
                    StatusHeader = "Error checking clipboard status";
                    StatusDetails = ex.Message;
                    StatusBrush = WpfThemeHelper.WarningText;
                }
            });
        }

        private void ExtractProcessDetails(IntPtr hWnd, out string processName, out int processId, out string windowTitle, out string executablePath)
        {
            processName = "Unknown Process";
            processId = 0;
            windowTitle = "(No Title)";
            executablePath = "Unknown Path";

            if (hWnd == IntPtr.Zero)
            {
                return;
            }

            try
            {
                // Use Dapplo.Windows.User32.User32Api
                windowTitle = User32Api.GetText(hWnd) ?? "(No Title)";

                User32Api.GetWindowThreadProcessId(hWnd, out var pid);
                processId = (int)pid;

                if (pid > 0)
                {
                    using var proc = Process.GetProcessById((int)pid);
                    processName = proc.ProcessName;
                    try
                    {
                        executablePath = proc.MainModule?.FileName ?? "Access Denied";
                    }
                    catch
                    {
                        executablePath = "(Protected or Elevated Process)";
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug($"Could not resolve process details for hWnd {hWnd}: {ex.Message}");
            }
        }

        public void QueryClipboardFormats()
        {
            RunOnUIThread(() =>
            {
                var items = new List<ClipboardFormatItemViewModel>();
                try
                {
                    // Access clipboard safely via Dapplo.Windows.Clipboard
                    using var token = ClipboardNative.Access(IntPtr.Zero, retries: 1, retryInterval: TimeSpan.Zero, timeout: TimeSpan.FromMilliseconds(50));
                    if (token.CanAccess)
                    {
                        var formats = token.AvailableFormats()?.ToList() ?? new List<string>();
                        foreach (var formatName in formats)
                        {
                            string desc = GetFormatDescription(formatName);
                            string details = "Native clipboard data";
                            string sizeText = "-";

                            try
                            {
                                if (string.Equals(formatName, "CF_UNICODETEXT", StringComparison.OrdinalIgnoreCase) ||
                                    string.Equals(formatName, "CF_TEXT", StringComparison.OrdinalIgnoreCase))
                                {
                                    string text = token.GetAsUnicodeString(formatName);
                                    if (text != null)
                                    {
                                        details = $"Text: \"{TruncateString(text, 60)}\"";
                                        sizeText = $"{text.Length:N0} chars";
                                    }
                                }
                                else if (string.Equals(formatName, "CF_HDROP", StringComparison.OrdinalIgnoreCase))
                                {
                                    var files = token.GetFileNames()?.ToList();
                                    if (files != null && files.Count > 0)
                                    {
                                        string preview = string.Join(", ", files.Take(2));
                                        if (files.Count > 2) preview += $" (+{files.Count - 2} more)";
                                        details = $"Files: {preview}";
                                        sizeText = $"{files.Count} files";
                                    }
                                }
                                else if (string.Equals(formatName, "CF_BITMAP", StringComparison.OrdinalIgnoreCase) ||
                                         string.Equals(formatName, "CF_DIB", StringComparison.OrdinalIgnoreCase) ||
                                         string.Equals(formatName, "CF_DIBV5", StringComparison.OrdinalIgnoreCase))
                                {
                                    details = "Raster bitmap image data";
                                    sizeText = "Image";
                                }
                                else if (formatName.IndexOf("PNG", StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    details = "PNG compressed image data";
                                    sizeText = "Image";
                                }
                                else if (formatName.StartsWith("HTML", StringComparison.OrdinalIgnoreCase))
                                {
                                    details = "HTML formatted text fragment";
                                }
                                else if (formatName.StartsWith("Rich Text", StringComparison.OrdinalIgnoreCase))
                                {
                                    details = "Rich Text Format (RTF)";
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Debug($"Could not read preview for format {formatName}", ex);
                            }

                            items.Add(new ClipboardFormatItemViewModel
                            {
                                Name = formatName,
                                TypeDescription = desc,
                                Details = details,
                                SizeText = sizeText
                            });
                        }
                    }

                    if (items.Count == 0)
                    {
                        items.Add(new ClipboardFormatItemViewModel
                        {
                            Name = Language.GetString("selfservice_clipboard_empty") ?? "(Empty)",
                            TypeDescription = Language.GetString("selfservice_clipboard_no_data") ?? "Clipboard contains no data",
                            Details = Language.GetString("selfservice_clipboard_no_formats") ?? "No formats currently active",
                            SizeText = "-"
                        });
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug("Error querying clipboard formats via Dapplo", ex);
                    items.Add(new ClipboardFormatItemViewModel
                    {
                        Name = Language.GetString("selfservice_clipboard_query_error") ?? "Error querying formats",
                        TypeDescription = ex.Message,
                        Details = Language.GetString("selfservice_clipboard_busy") ?? "Clipboard may be busy or locked",
                        SizeText = "-"
                    });
                }

                lock (_formatsLock)
                {
                    Formats.Clear();
                    foreach (var item in items)
                    {
                        Formats.Add(item);
                    }
                }
            });
        }

        private string GetFormatDescription(string name)
        {
            if (string.Equals(name, "CF_TEXT", StringComparison.OrdinalIgnoreCase)) return Language.GetString("selfservice_clipformat_ansi") ?? "Standard ANSI text string";
            if (string.Equals(name, "CF_BITMAP", StringComparison.OrdinalIgnoreCase)) return Language.GetString("selfservice_clipformat_bitmap") ?? "Device-dependent bitmap (GDI)";
            if (string.Equals(name, "CF_DIB", StringComparison.OrdinalIgnoreCase)) return Language.GetString("selfservice_clipformat_dib") ?? "Device-independent bitmap (DIB)";
            if (string.Equals(name, "CF_DIBV5", StringComparison.OrdinalIgnoreCase)) return Language.GetString("selfservice_clipformat_dibv5") ?? "DIB version 5 bitmap";
            if (string.Equals(name, "CF_UNICODETEXT", StringComparison.OrdinalIgnoreCase)) return Language.GetString("selfservice_clipformat_unicode") ?? "Standard Unicode (UTF-16) text string";
            if (string.Equals(name, "CF_ENHMETAFILE", StringComparison.OrdinalIgnoreCase)) return Language.GetString("selfservice_clipformat_metafile") ?? "Enhanced Windows Metafile";
            if (string.Equals(name, "CF_HDROP", StringComparison.OrdinalIgnoreCase)) return Language.GetString("selfservice_clipformat_hdrop") ?? "List of files dragged or copied (HDROP)";
            if (string.Equals(name, "CF_LOCALE", StringComparison.OrdinalIgnoreCase)) return Language.GetString("selfservice_clipformat_locale") ?? "Locale identifier for clipboard text";
            if (string.Equals(name, "CF_OEMTEXT", StringComparison.OrdinalIgnoreCase)) return Language.GetString("selfservice_clipformat_oem") ?? "OEM text string";
            if (string.Equals(name, "CF_TIFF", StringComparison.OrdinalIgnoreCase)) return Language.GetString("selfservice_clipformat_tiff") ?? "TIFF image data";
            if (name.StartsWith("HTML", StringComparison.OrdinalIgnoreCase)) return Language.GetString("selfservice_clipformat_html") ?? "Hypertext Markup Language (HTML)";
            if (name.StartsWith("Rich Text", StringComparison.OrdinalIgnoreCase)) return Language.GetString("selfservice_clipformat_rtf") ?? "Rich Text Format (RTF)";
            if (name.IndexOf("PNG", StringComparison.OrdinalIgnoreCase) >= 0) return Language.GetString("selfservice_clipformat_png") ?? "Portable Network Graphics (PNG)";
            if (name.IndexOf("Bitmap", StringComparison.OrdinalIgnoreCase) >= 0) return Language.GetString("selfservice_clipformat_bmp") ?? "Bitmap image format";
            return Language.GetString("selfservice_clipformat_custom") ?? "Custom registered format";
        }

        public void ToggleMonitoring()
        {
            if (IsMonitoring)
            {
                StopMonitoring();
            }
            else
            {
                StartMonitoring();
            }
        }

        public void StartMonitoring()
        {
            RunOnUIThread(() =>
            {
                if (!IsMonitoring)
                {
                    _blockedOccurrenceCount = 0;
                    IsMonitoring = true;
                    string startMsg = Language.GetString("selfservice_clipboard_monitor_started_msg");
                    lock (_monitorLogLock)
                    {
                        MonitorLog.Insert(0, new ClipboardMonitorEvent
                        {
                            Timestamp = DateTime.Now.ToString("HH:mm:ss.fff"),
                            IsBlocked = false,
                            Message = string.IsNullOrEmpty(startMsg) ? "Started continuous clipboard monitoring loop (interval: 600ms)" : startMsg,
                            ProcessDetails = "..."
                        });
                    }
                    _monitorTimer.Start();
                }
            });
        }

        public void StopMonitoring()
        {
            RunOnUIThread(() =>
            {
                if (IsMonitoring)
                {
                    _monitorTimer.Stop();
                    IsMonitoring = false;
                    string stopMsg = Language.GetString("selfservice_clipboard_monitor_stopped_msg");
                    lock (_monitorLogLock)
                    {
                        MonitorLog.Insert(0, new ClipboardMonitorEvent
                        {
                            Timestamp = DateTime.Now.ToString("HH:mm:ss.fff"),
                            IsBlocked = false,
                            Message = string.IsNullOrEmpty(stopMsg) ? "Stopped clipboard monitoring loop" : stopMsg,
                            ProcessDetails = "Idle"
                        });
                    }
                }
            });
        }

        private void OnMonitorTimerTick(object sender, EventArgs e)
        {
            CheckClipboardStatus(logToMonitor: true);
        }

        private void TrimMonitorLog()
        {
            lock (_monitorLogLock)
            {
                while (MonitorLog.Count > 100)
                {
                    MonitorLog.RemoveAt(MonitorLog.Count - 1);
                }
            }
        }

        public void ClearLog()
        {
            RunOnUIThread(() =>
            {
                lock (_monitorLogLock)
                {
                    MonitorLog.Clear();
                }
            });
        }

        public void Dispose()
        {
            StopMonitoring();
            _clipboardSubscription?.Dispose();
            _clipboardSubscription = null;
        }

        private static string TruncateString(string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            string singleLine = input.Replace("\r", " ").Replace("\n", " ");
            return singleLine.Length <= maxLength ? singleLine : singleLine.Substring(0, maxLength) + "...";
        }
    }
}
