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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Wpf;
using Greenshot.Helpers;
using log4net;
using Microsoft.Win32;
using Greenshot.Base.Languages;

namespace Greenshot.SelfService.ViewModels
{
    public class HotkeyStatusItemViewModel
    {
        public string ActionName { get; set; }
        public string ConfigKey { get; set; }
        public string HotkeyText { get; set; }
        public bool IsRegistered { get; set; }
        public string StatusText => IsRegistered 
            ? (Texts.SelfService.HotkeysStatusOk ?? "Registered") 
            : (Texts.SelfService.HotkeysStatusFailed ?? "Not Registered / Conflict");
        public Brush StatusBrush => IsRegistered ? WpfThemeHelper.Accent : WpfThemeHelper.WarningText;
    }

    public class ContenderAppViewModel
    {
        public string Name { get; set; }
        public string ProcessName { get; set; }
        public bool IsRunning { get; set; }
        public int ProcessId { get; set; }
        public string Description { get; set; }
        public string StatusText
        {
            get
            {
                if (IsRunning)
                {
                    string runningTemplate = Texts.SelfService.HotkeysRunning;
                    return string.Format(string.IsNullOrEmpty(runningTemplate) ? "Running (PID {0})" : runningTemplate, ProcessId);
                }
                string notRunning = Texts.SelfService.HotkeysNotrunning;
                return string.IsNullOrEmpty(notRunning) ? "Not Running" : notRunning;
            }
        }
        public Brush StatusBrush => IsRunning ? WpfThemeHelper.WarningText : WpfThemeHelper.TextSecondary;
    }

    public class HotkeySectionViewModel : SelfServiceSectionViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(HotkeySectionViewModel));
        private static readonly ICoreConfiguration Config = IniConfigRegistry.GetSection<ICoreConfiguration>();

        public override string Id => "hotkeys";
        public override string Title
        {
            get
            {
                string title = Texts.SelfService.CategoryHotkeys;
                return string.IsNullOrEmpty(title) ? "Hotkeys & App Conflicts" : title;
            }
        }
        public override string Subtitle
        {
            get
            {
                string sub = Texts.SelfService.CategoryHotkeysSub;
                return string.IsNullOrEmpty(sub) ? "Greenshot shortcuts, conflict detection and OneDrive / Windows fixes" : sub;
            }
        }
        public override string Icon => "⌨️";

        // OneDrive status
        private bool _isOneDriveRunning;
        private bool _isOneDriveBlocking;
        private string _oneDriveStatusText;
        private Brush _oneDriveStatusBrush;
        private Visibility _oneDriveFixButtonVisibility = Visibility.Collapsed;

        // Snipping Tool status
        private bool _isSnippingToolHijackEnabled;
        private string _snippingToolStatusText;
        private Brush _snippingToolStatusBrush;
        private Visibility _snippingToolFixButtonVisibility = Visibility.Collapsed;

        // Dropbox status
        private bool _isDropboxRunning;
        private string _dropboxStatusText;
        private Brush _dropboxStatusBrush;

        // General status
        private string _statusMessage;

        public ObservableCollection<HotkeyStatusItemViewModel> Hotkeys { get; } = new ObservableCollection<HotkeyStatusItemViewModel>();
        public ObservableCollection<ContenderAppViewModel> Contenders { get; } = new ObservableCollection<ContenderAppViewModel>();

        public bool IsOneDriveRunning { get => _isOneDriveRunning; private set { _isOneDriveRunning = value; OnPropertyChanged(); } }
        public bool IsOneDriveBlocking { get => _isOneDriveBlocking; private set { _isOneDriveBlocking = value; OnPropertyChanged(); } }
        public string OneDriveStatusText { get => _oneDriveStatusText; private set { _oneDriveStatusText = value; OnPropertyChanged(); } }
        public Brush OneDriveStatusBrush { get => _oneDriveStatusBrush; private set { _oneDriveStatusBrush = value; OnPropertyChanged(); } }
        public Visibility OneDriveFixButtonVisibility { get => _oneDriveFixButtonVisibility; private set { _oneDriveFixButtonVisibility = value; OnPropertyChanged(); } }

        public bool IsSnippingToolHijackEnabled { get => _isSnippingToolHijackEnabled; private set { _isSnippingToolHijackEnabled = value; OnPropertyChanged(); } }
        public string SnippingToolStatusText { get => _snippingToolStatusText; private set { _snippingToolStatusText = value; OnPropertyChanged(); } }
        public Brush SnippingToolStatusBrush { get => _snippingToolStatusBrush; private set { _snippingToolStatusBrush = value; OnPropertyChanged(); } }
        public Visibility SnippingToolFixButtonVisibility { get => _snippingToolFixButtonVisibility; private set { _snippingToolFixButtonVisibility = value; OnPropertyChanged(); } }

        public bool IsDropboxRunning { get => _isDropboxRunning; private set { _isDropboxRunning = value; OnPropertyChanged(); } }
        public string DropboxStatusText { get => _dropboxStatusText; private set { _dropboxStatusText = value; OnPropertyChanged(); } }
        public Brush DropboxStatusBrush { get => _dropboxStatusBrush; private set { _dropboxStatusBrush = value; OnPropertyChanged(); } }

        public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }

        public HotkeySectionViewModel()
        {
            Refresh();
        }

        public override void OnNavigatedTo()
        {
            Refresh();
        }

        public override void OnLanguageChanged()
        {
            base.OnLanguageChanged();
            RefreshGreenshotHotkeys();
            CheckOneDriveStatus();
            CheckSnippingToolStatus();
            CheckDropboxStatus();
            CheckOtherContenders();
        }

        public override void Refresh()
        {
            RefreshGreenshotHotkeys();
            CheckOneDriveStatus();
            CheckSnippingToolStatus();
            CheckDropboxStatus();
            CheckOtherContenders();

            int totalIssues = 0;
            if (IsOneDriveBlocking) totalIssues++;
            if (IsSnippingToolHijackEnabled) totalIssues++;
            if (Hotkeys.Any(h => !h.IsRegistered && !string.Equals(h.HotkeyText, "None", StringComparison.OrdinalIgnoreCase))) totalIssues++;

            if (totalIssues > 0)
            {
                BadgeText = $"{totalIssues} Issue{(totalIssues > 1 ? "s" : "")}";
                BadgeBrush = WpfThemeHelper.WarningBackground;
            }
            else
            {
                BadgeText = null;
                BadgeBrush = null;
            }

            string updatedTemplate = Texts.SelfService.HotkeysUpdatedAt;
            StatusMessage = string.Format(string.IsNullOrEmpty(updatedTemplate) ? "Updated at {0}" : updatedTemplate, DateTime.Now.ToString("HH:mm:ss"));
        }

        public void RefreshGreenshotHotkeys()
        {
            Hotkeys.Clear();

            AddHotkeyItem(Texts.Core.ContextmenuCapturearea ?? "Capture Region", "RegionHotkey", Config?.RegionHotkey);
            AddHotkeyItem(Texts.Core.ContextmenuCapturewindow ?? "Capture Window", "WindowHotkey", Config?.WindowHotkey);
            AddHotkeyItem(Texts.Core.ContextmenuCapturefullscreen ?? "Capture Fullscreen", "FullscreenHotkey", Config?.FullscreenHotkey);
            AddHotkeyItem(Texts.Core.ContextmenuCapturelastregion ?? "Capture Last Region", "LastregionHotkey", Config?.LastregionHotkey);
            AddHotkeyItem(Texts.Core.ContextmenuCaptureclipboard ?? "Capture Clipboard", "ClipboardHotkey", Config?.ClipboardHotkey);
        }

        private void AddHotkeyItem(string actionName, string configKey, string hotkeyValue)
        {
            string displayValue = string.IsNullOrWhiteSpace(hotkeyValue) ? "None" : hotkeyValue;
            bool isRegistered = false;

            if (!string.Equals(displayValue, "None", StringComparison.OrdinalIgnoreCase))
            {
                // In Greenshot, if HotkeyManager registered sequences exist, assume registered unless error
                // Test parsing sequence
                try
                {
                    var seq = HotkeySequence.Parse(displayValue);
                    isRegistered = seq != null;
                }
                catch
                {
                    isRegistered = false;
                }
            }

            Hotkeys.Add(new HotkeyStatusItemViewModel
            {
                ActionName = actionName,
                ConfigKey = configKey,
                HotkeyText = displayValue,
                IsRegistered = isRegistered
            });
        }

        public void ReRegisterHotkeys()
        {
            try
            {
                HotkeyManager.UnregisterHotkeys();
                bool ok = HotkeyHelper.RegisterHotkeys(true);
                RefreshGreenshotHotkeys();

                StatusMessage = ok 
                    ? (Texts.SelfService.HotkeysReregisterSuccess ?? "Successfully re-registered Greenshot hotkeys!") 
                    : (Texts.SelfService.HotkeysReregisterConflict ?? "Hotkeys re-registered (some keys may have conflicts).");
            }
            catch (Exception ex)
            {
                Log.Error("Error re-registering hotkeys", ex);
                string failedTemplate = Texts.SelfService.HotkeysReregisterFailed;
                StatusMessage = string.Format(string.IsNullOrEmpty(failedTemplate) ? "Failed to re-register hotkeys: {0}" : failedTemplate, ex.Message);
            }
        }

        public void CheckOneDriveStatus()
        {
            try
            {
                var oneDriveProcs = Process.GetProcessesByName("OneDrive");
                IsOneDriveRunning = oneDriveProcs.Length > 0;

                // Check screenshot intercept settings in OneDrive
                bool blocking = false;
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var oneDriveSettingsBase = Path.Combine(localAppData, @"Microsoft\OneDrive\settings");

                if (Directory.Exists(oneDriveSettingsBase))
                {
                    var screenshotFiles = Directory.GetFiles(oneDriveSettingsBase, "*_screenshot.dat", SearchOption.AllDirectories);
                    foreach (var file in screenshotFiles)
                    {
                        var lines = File.ReadAllLines(file);
                        if (lines.Length >= 2 && "2".Equals(lines[1]))
                        {
                            blocking = true;
                            break;
                        }
                    }
                }

                // Check registry
                try
                {
                    using var regKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\OneDrive");
                    if (regKey != null)
                    {
                        var autoSave = regKey.GetValue("AutoSaveScreenshots");
                        if (autoSave != null && (autoSave.ToString() == "1" || autoSave.ToString().Equals("true", StringComparison.OrdinalIgnoreCase)))
                        {
                            blocking = true;
                        }
                    }
                }
                catch
                {
                    // Ignore
                }

                IsOneDriveBlocking = blocking;

                if (blocking)
                {
                    OneDriveStatusText = Texts.SelfService.HotkeysOnedriveStatusEnabled ?? "⚠️ OneDrive is currently intercepting screenshot hotkeys!";
                    OneDriveStatusBrush = WpfThemeHelper.ErrorText;
                    OneDriveFixButtonVisibility = Visibility.Visible;
                }
                else if (IsOneDriveRunning)
                {
                    OneDriveStatusText = Texts.SelfService.HotkeysOnedriveStatusDisabled ?? "OneDrive is running, but screenshot hotkey capture is disabled. (OK)";
                    OneDriveStatusBrush = WpfThemeHelper.Accent;
                    OneDriveFixButtonVisibility = Visibility.Collapsed;
                }
                else
                {
                    OneDriveStatusText = Texts.SelfService.HotkeysOnedriveStatusNotdetected ?? "OneDrive is not running.";
                    OneDriveStatusBrush = WpfThemeHelper.TextSecondary;
                    OneDriveFixButtonVisibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error checking OneDrive status", ex);
                OneDriveStatusText = Texts.SelfService.HotkeysOnedriveStatusNotdetected ?? "Could not verify OneDrive status.";
                OneDriveStatusBrush = WpfThemeHelper.TextSecondary;
                OneDriveFixButtonVisibility = Visibility.Collapsed;
            }
        }

        public void DisableOneDriveHotkey()
        {
            try
            {
                int modifiedCount = 0;
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var oneDriveSettingsBase = Path.Combine(localAppData, @"Microsoft\OneDrive\settings");

                if (Directory.Exists(oneDriveSettingsBase))
                {
                    var screenshotFiles = Directory.GetFiles(oneDriveSettingsBase, "*_screenshot.dat", SearchOption.AllDirectories);
                    foreach (var file in screenshotFiles)
                    {
                        var lines = File.ReadAllLines(file);
                        if (lines.Length >= 2 && "2".Equals(lines[1]))
                        {
                            lines[1] = "1";
                            File.WriteAllLines(file, lines);
                            modifiedCount++;
                        }
                    }
                }

                // Registry update
                try
                {
                    using var regKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\OneDrive", true);
                    if (regKey != null)
                    {
                        regKey.SetValue("AutoSaveScreenshots", 0, RegistryValueKind.DWord);
                    }
                }
                catch
                {
                    // Ignore
                }

                CheckOneDriveStatus();
                ReRegisterHotkeys();

                StatusMessage = Texts.SelfService.HotkeysOnedriveSuccess ?? "Disabled OneDrive screenshot capture! Greenshot hotkeys re-registered.";
            }
            catch (Exception ex)
            {
                Log.Error("Error disabling OneDrive hotkey", ex);
                string errTemplate = Texts.SelfService.HotkeysOnedriveError;
                StatusMessage = string.Format(string.IsNullOrEmpty(errTemplate) ? "Could not modify OneDrive configuration: {0}" : errTemplate, ex.Message);
            }
        }

        public void CheckSnippingToolStatus()
        {
            try
            {
                // In Windows 10/11, check HKCU\Control Panel\Keyboard\PrintScreenKeyForSnippingEnabled
                using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Keyboard");
                if (key != null)
                {
                    var val = key.GetValue("PrintScreenKeyForSnippingEnabled");
                    if (val != null)
                    {
                        int intVal = Convert.ToInt32(val);
                        IsSnippingToolHijackEnabled = intVal == 1;
                    }
                    else
                    {
                        // On Windows 11 default is 1 if not set
                        IsSnippingToolHijackEnabled = WindowsVersion.IsWindows11OrLater;
                    }
                }
                else
                {
                    IsSnippingToolHijackEnabled = false;
                }

                if (IsSnippingToolHijackEnabled)
                {
                    SnippingToolStatusText = Texts.SelfService.HotkeysSnippingStatusEnabled ?? "⚠️ Windows Snipping Tool is set to open on PrintScreen!";
                    SnippingToolStatusBrush = WpfThemeHelper.WarningText;
                    SnippingToolFixButtonVisibility = Visibility.Visible;
                }
                else
                {
                    SnippingToolStatusText = Texts.SelfService.HotkeysSnippingStatusDisabled ?? "Windows Snipping Tool PrintScreen takeover is disabled. (OK)";
                    SnippingToolStatusBrush = WpfThemeHelper.Accent;
                    SnippingToolFixButtonVisibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error checking Snipping Tool registry", ex);
                SnippingToolStatusText = Texts.SelfService.HotkeysSnippingStatusNa ?? "Could not check Windows Snipping Tool status.";
                SnippingToolStatusBrush = WpfThemeHelper.TextSecondary;
                SnippingToolFixButtonVisibility = Visibility.Collapsed;
            }
        }

        public void DisableSnippingToolTakeover()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Keyboard", true);
                if (key != null)
                {
                    key.SetValue("PrintScreenKeyForSnippingEnabled", 0, RegistryValueKind.DWord);
                }

                CheckSnippingToolStatus();
                ReRegisterHotkeys();

                StatusMessage = Texts.SelfService.HotkeysSnippingSuccess ?? "Disabled Windows Snipping Tool takeover! Greenshot hotkeys re-registered.";
            }
            catch (Exception ex)
            {
                Log.Error("Error disabling Snipping Tool takeover", ex);
                StatusMessage = $"Could not update registry: {ex.Message}";
            }
        }

        public void OpenWindowsKeyboardSettings()
        {
            try
            {
                Process.Start(new ProcessStartInfo("ms-settings:easeofaccess-keyboard") { UseShellExecute = true });
                StatusMessage = Texts.SelfService.HotkeysOpenedSettings ?? "Opened Windows Keyboard Settings.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Could not open Windows Settings: {ex.Message}";
            }
        }

        public void CheckDropboxStatus()
        {
            try
            {
                var dropboxProcs = Process.GetProcessesByName("Dropbox");
                IsDropboxRunning = dropboxProcs.Length > 0;

                if (IsDropboxRunning)
                {
                    DropboxStatusText = Texts.SelfService.HotkeysDropboxRunning ?? "Dropbox is running.";
                    DropboxStatusBrush = WpfThemeHelper.WarningText;
                }
                else
                {
                    DropboxStatusText = Texts.SelfService.HotkeysDropboxNotrunning ?? "Dropbox is not running.";
                    DropboxStatusBrush = WpfThemeHelper.TextSecondary;
                }
            }
            catch (Exception)
            {
                DropboxStatusText = Texts.SelfService.HotkeysDropboxNotrunning ?? "Could not check Dropbox status.";
                DropboxStatusBrush = WpfThemeHelper.TextSecondary;
            }
        }

        public void CheckOtherContenders()
        {
            Contenders.Clear();

            CheckContender("ShareX", "ShareX", Texts.SelfService.HotkeysContenderSharex ?? "Full screen capture utility with global hotkeys");
            CheckContender("Snagit", "Snagit32", Texts.SelfService.HotkeysContenderSnagit ?? "TechSmith Snagit screen capture application");
            CheckContender("Snagit 64-bit", "Snagit64", Texts.SelfService.HotkeysContenderSnagit64 ?? "TechSmith Snagit 64-bit screen capture application");
            CheckContender("Lightshot", "Lightshot", Texts.SelfService.HotkeysContenderLightshot ?? "Lightshot screenshot tool (claims PrintScreen)");
            CheckContender("PicPick", "picpick", Texts.SelfService.HotkeysContenderPicpick ?? "PicPick graphic design and screen capture tool");
            CheckContender("Windows Snipping Tool", "SnippingTool", Texts.SelfService.HotkeysContenderSnipping ?? "Built-in Windows Snipping Tool process");
            CheckContender("Screen Clipping Host", "ScreenClippingHost", Texts.SelfService.HotkeysContenderScreenclipping ?? "Windows Snip & Sketch overlay process");
        }

        private void CheckContender(string name, string processName, string description)
        {
            try
            {
                var procs = Process.GetProcessesByName(processName);
                bool running = procs.Length > 0;
                int pid = running ? procs[0].Id : 0;

                Contenders.Add(new ContenderAppViewModel
                {
                    Name = name,
                    ProcessName = processName,
                    IsRunning = running,
                    ProcessId = pid,
                    Description = description
                });
            }
            catch
            {
                // Ignore
            }
        }
    }
}
