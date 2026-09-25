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
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using Bitmap = System.Drawing.Bitmap;
using Dapplo.Windows.Clipboard;
using Dapplo.Windows.User32;
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
        public override string Title => "Clipboard Diagnostics";
        public override string Subtitle => "Active formats and live clipboard locker detection";
        public override string Icon => "📋";

        // Win32 APIs for lock testing
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetOpenClipboardWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint EnumClipboardFormats(uint format);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetClipboardFormatName(uint format, [Out] StringBuilder lpszFormatName, int cchMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetClipboardData(uint uFormat);

        [DllImport("kernel32.dll")]
        private static extern UIntPtr GlobalSize(IntPtr hMem);

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
        private string _monitorToggleText = "▶ Start Monitoring Loop";
        private int _blockedOccurrenceCount;
        private DispatcherTimer _monitorTimer;
        private IDisposable _clipboardSubscription;

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

        public string BlockerProcessName { get => _blockerProcessName; private set { _blockerProcessName = value; OnPropertyChanged(); } }
        public int BlockerProcessId { get => _blockerProcessId; private set { _blockerProcessId = value; OnPropertyChanged(); } }
        public string BlockerWindowTitle { get => _blockerWindowTitle; private set { _blockerWindowTitle = value; OnPropertyChanged(); } }
        public string BlockerExecutablePath { get => _blockerExecutablePath; private set { _blockerExecutablePath = value; OnPropertyChanged(); } }

        public string OwnerProcessName { get => _ownerProcessName; private set { _ownerProcessName = value; OnPropertyChanged(); } }
        public int OwnerProcessId { get => _ownerProcessId; private set { _ownerProcessId = value; OnPropertyChanged(); } }
        public string OwnerWindowTitle { get => _ownerWindowTitle; private set { _ownerWindowTitle = value; OnPropertyChanged(); } }
        public string OwnerExecutablePath { get => _ownerExecutablePath; private set { _ownerExecutablePath = value; OnPropertyChanged(); } }
        public string CurrentOwnerInfo { get => _currentOwnerInfo; private set { _currentOwnerInfo = value; OnPropertyChanged(); } }

        public bool IsMonitoring
        {
            get => _isMonitoring;
            set
            {
                if (_isMonitoring != value)
                {
                    _isMonitoring = value;
                    OnPropertyChanged();
                    MonitorToggleText = value ? "⏹ Stop Monitoring Loop" : "▶ Start Monitoring Loop";
                }
            }
        }

        public string MonitorToggleText { get => _monitorToggleText; private set { _monitorToggleText = value; OnPropertyChanged(); } }

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
            RunOnUIThread(() =>
            {
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
            });
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
                    bool canOpen = OpenClipboard(IntPtr.Zero);

                    if (canOpen)
                    {
                        CloseClipboard();
                    }

                    // If OpenClipboard failed OR GetOpenClipboardWindow returned a non-zero handle, it is blocked!
                    bool blocked = !canOpen || openHwnd != IntPtr.Zero;

                    if (blocked)
                    {
                        IntPtr targetHwnd = openHwnd != IntPtr.Zero ? openHwnd : GetOpenClipboardWindow();
                        ExtractProcessDetails(targetHwnd, out string procName, out int pid, out string title, out string path);

                        IsBlocked = true;
                        BlockerProcessName = procName;
                        BlockerProcessId = pid;
                        BlockerWindowTitle = title;
                        BlockerExecutablePath = path;

                        StatusHeader = $"Clipboard is BLOCKED by {procName} (PID: {pid})";
                        StatusDetails = $"Window: \"{title}\"\nExecutable: {path}";
                        StatusBrush = WpfThemeHelper.ErrorText;
                        BadgeText = "BLOCKED";
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
                                    Message = $"BLOCKED by {procName} (PID {pid})",
                                    ProcessDetails = $"Window: \"{title}\" | Path: {path}"
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

                        StatusHeader = "Clipboard is Accessible (Unlocked)";
                        StatusDetails = $"Last data set by: {CurrentOwnerInfo}";
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
                                    Message = "Accessible (Clipboard is unlocked and available)",
                                    ProcessDetails = $"Data owner: {CurrentOwnerInfo} | Path: {OwnerExecutablePath}"
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
                    // Enumerate formats using Win32 API
                    if (OpenClipboard(IntPtr.Zero))
                    {
                        try
                        {
                            uint format = 0;
                            while ((format = EnumClipboardFormats(format)) != 0)
                            {
                                string formatName = GetFormatName(format);
                                string typeDescription = GetFormatDescription(format, formatName);
                                string details = "Native clipboard data";
                                string sizeText = "N/A";

                                try
                                {
                                    IntPtr hData = GetClipboardData(format);
                                    if (hData != IntPtr.Zero)
                                    {
                                        UIntPtr size = GlobalSize(hData);
                                        if (size.ToUInt64() > 0)
                                        {
                                            sizeText = $"{size.ToUInt64():N0} bytes";
                                        }
                                    }
                                }
                                catch
                                {
                                    // Ignore
                                }

                                items.Add(new ClipboardFormatItemViewModel
                                {
                                    Name = formatName,
                                    TypeDescription = typeDescription,
                                    Details = details,
                                    SizeText = sizeText
                                });
                            }
                        }
                        finally
                        {
                            CloseClipboard();
                        }
                    }

                    // Complement with managed IDataObject preview details
                    try
                    {
                        var dataObj = System.Windows.Forms.Clipboard.GetDataObject();
                        if (dataObj != null)
                        {
                            if (dataObj.GetDataPresent(System.Windows.Forms.DataFormats.UnicodeText))
                            {
                                string text = dataObj.GetData(System.Windows.Forms.DataFormats.UnicodeText) as string;
                                UpdateFormatDetailsInList(items, "CF_UNICODETEXT", $"Text snippet: \"{TruncateString(text, 60)}\" ({text?.Length ?? 0} chars)");
                            }
                            if (dataObj.GetDataPresent(System.Windows.Forms.DataFormats.Bitmap))
                            {
                                if (dataObj.GetData(System.Windows.Forms.DataFormats.Bitmap) is Bitmap bmp)
                                {
                                    UpdateFormatDetailsInList(items, "CF_BITMAP", $"Bitmap: {bmp.Width}x{bmp.Height} ({bmp.PixelFormat})");
                                    UpdateFormatDetailsInList(items, "CF_DIB", $"DIB: {bmp.Width}x{bmp.Height}");
                                }
                            }
                            if (dataObj.GetDataPresent(System.Windows.Forms.DataFormats.FileDrop))
                            {
                                if (dataObj.GetData(System.Windows.Forms.DataFormats.FileDrop) is string[] files)
                                {
                                    UpdateFormatDetailsInList(items, "CF_HDROP", $"Files ({files.Length}): {string.Join(", ", files)}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Debug("Error inspecting managed IDataObject", ex);
                    }

                    if (items.Count == 0)
                    {
                        items.Add(new ClipboardFormatItemViewModel
                        {
                            Name = "(Empty)",
                            TypeDescription = "Clipboard contains no data",
                            Details = "No formats currently active",
                            SizeText = "-"
                        });
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Error querying clipboard formats", ex);
                    items.Add(new ClipboardFormatItemViewModel
                    {
                        Name = "Error querying formats",
                        TypeDescription = ex.Message,
                        Details = "Clipboard may be locked by another application",
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

        private static void UpdateFormatDetailsInList(List<ClipboardFormatItemViewModel> list, string formatName, string newDetails)
        {
            var item = list.FirstOrDefault(f => string.Equals(f.Name, formatName, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                item.Details = newDetails;
            }
        }

        private void UpdateFormatDetails(string formatName, string newDetails)
        {
            lock (_formatsLock)
            {
                foreach (var item in Formats)
                {
                    if (item.Name.Equals(formatName, StringComparison.OrdinalIgnoreCase))
                    {
                        item.Details = newDetails;
                    }
                }
            }
        }

        private string GetFormatName(uint format)
        {
            switch (format)
            {
                case 1: return "CF_TEXT";
                case 2: return "CF_BITMAP";
                case 3: return "CF_METAFILEPICT";
                case 4: return "CF_SYLK";
                case 5: return "CF_DIF";
                case 6: return "CF_TIFF";
                case 7: return "CF_OEMTEXT";
                case 8: return "CF_DIB";
                case 9: return "CF_PALETTE";
                case 13: return "CF_UNICODETEXT";
                case 14: return "CF_ENHMETAFILE";
                case 15: return "CF_HDROP";
                case 16: return "CF_LOCALE";
                case 17: return "CF_DIBV5";
                default:
                    var sb = new StringBuilder(256);
                    if (GetClipboardFormatName(format, sb, 256) > 0)
                    {
                        return sb.ToString();
                    }
                    return $"Format_0x{format:X4}";
            }
        }

        private string GetFormatDescription(uint format, string name)
        {
            switch (format)
            {
                case 1: return "Standard ANSI text";
                case 2: return "Windows Bitmap";
                case 8: return "Device-Independent Bitmap (DIB)";
                case 13: return "Unicode Text";
                case 15: return "File Drop List";
                case 17: return "DIB Version 5 (Alpha transparency)";
                default:
                    if (name.IndexOf("PNG", StringComparison.OrdinalIgnoreCase) >= 0) return "PNG compressed image";
                    if (name.IndexOf("HTML", StringComparison.OrdinalIgnoreCase) >= 0) return "HTML formatted text";
                    if (name.IndexOf("Rich Text", StringComparison.OrdinalIgnoreCase) >= 0) return "Rich Text Format (RTF)";
                    return "Custom / Registered format";
            }
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
                    lock (_monitorLogLock)
                    {
                        MonitorLog.Insert(0, new ClipboardMonitorEvent
                        {
                            Timestamp = DateTime.Now.ToString("HH:mm:ss.fff"),
                            IsBlocked = false,
                            Message = "Started continuous clipboard monitoring loop (interval: 600ms)",
                            ProcessDetails = "Monitoring for clipboard locks..."
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
                    lock (_monitorLogLock)
                    {
                        MonitorLog.Insert(0, new ClipboardMonitorEvent
                        {
                            Timestamp = DateTime.Now.ToString("HH:mm:ss.fff"),
                            IsBlocked = false,
                            Message = "Stopped clipboard monitoring loop",
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
