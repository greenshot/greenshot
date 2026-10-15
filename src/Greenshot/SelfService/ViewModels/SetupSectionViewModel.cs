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
using System.Diagnostics;
using System.Windows;
using Greenshot.Base.Languages;
using Greenshot.Base.Wpf;
using Greenshot.Helpers;
using log4net;
using Microsoft.Win32;

namespace Greenshot.SelfService.ViewModels
{
    /// <summary>
    /// What Greenshot needs to work as expected: its icon next to the clock, the Print key and the start with Windows.
    /// The "Your setup" step of the welcome window and the section of the Self-Service window show the same checks.
    /// </summary>
    public class SetupSectionViewModel : SelfServiceSectionViewModel
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SetupSectionViewModel));
        // Hardcodet.NotifyIcon.Wpf adds every icon with id 0, see Shell/TrayIcon.cs
        private const int TrayIconId = 0;
        private readonly HotkeySectionViewModel _hotkeyChecks = new();

        public override string Id => "setup";
        public override string Title => Texts.Welcome.SetupTitle;
        public override string Subtitle => Texts.Welcome.SetupSub;
        public override string Icon => "✅";

        public bool IsTrayIconHidden { get; private set; }

        public bool IsSnippingToolTakingPrintKey => _hotkeyChecks.IsSnippingToolHijackEnabled;

        public bool IsOneDriveTakingPrintKey => _hotkeyChecks.IsOneDriveBlocking;

        public Visibility TrayIconOkVisibility => IsTrayIconHidden ? Visibility.Collapsed : Visibility.Visible;
        public Visibility TrayIconHiddenVisibility => IsTrayIconHidden ? Visibility.Visible : Visibility.Collapsed;
        public Visibility PrintKeyOkVisibility => IsSnippingToolTakingPrintKey || IsOneDriveTakingPrintKey ? Visibility.Collapsed : Visibility.Visible;
        public Visibility SnippingToolVisibility => IsSnippingToolTakingPrintKey ? Visibility.Visible : Visibility.Collapsed;
        public Visibility OneDriveVisibility => IsOneDriveTakingPrintKey ? Visibility.Visible : Visibility.Collapsed;

        /// <summary>
        /// Does this Greenshot start with Windows for the current user? Changing it changes the user's own entry, like the settings do.
        /// </summary>
        public bool IsAutostartEnabled
        {
            get => StartupHelper.IsAutostartEnabled();
            set
            {
                if (value == StartupHelper.IsAutostartEnabled())
                {
                    return;
                }

                if (value)
                {
                    StartupHelper.EnableAutostart();
                }
                else
                {
                    StartupHelper.DisableAutostart();
                }

                OnPropertyChanged();
            }
        }

        public override void OnNavigatedTo()
        {
            Refresh();
        }

        /// <summary>
        /// Check everything again, e.g. when the user comes back from the Windows settings
        /// </summary>
        public override void Refresh()
        {
            try
            {
                IsTrayIconHidden = IsHiddenInOverflow(Registry.CurrentUser, System.Windows.Forms.Application.ExecutablePath, TrayIconId);
            }
            catch (Exception ex)
            {
                Log.Warn("Couldn't check if the notification area icon is shown", ex);
                IsTrayIconHidden = false;
            }

            _hotkeyChecks.CheckSnippingToolStatus();
            _hotkeyChecks.CheckOneDriveStatus();

            int issues = (IsTrayIconHidden ? 1 : 0) + (IsSnippingToolTakingPrintKey ? 1 : 0) + (IsOneDriveTakingPrintKey ? 1 : 0);
            BadgeText = issues > 0 ? issues.ToString() : null;
            BadgeBrush = issues > 0 ? WpfThemeHelper.WarningBackground : null;

            OnPropertyChanged(nameof(IsTrayIconHidden));
            OnPropertyChanged(nameof(IsSnippingToolTakingPrintKey));
            OnPropertyChanged(nameof(IsOneDriveTakingPrintKey));
            OnPropertyChanged(nameof(TrayIconOkVisibility));
            OnPropertyChanged(nameof(TrayIconHiddenVisibility));
            OnPropertyChanged(nameof(PrintKeyOkVisibility));
            OnPropertyChanged(nameof(SnippingToolVisibility));
            OnPropertyChanged(nameof(OneDriveVisibility));
            OnPropertyChanged(nameof(IsAutostartEnabled));
        }

        /// <summary>
        /// Show the icon next to the clock, or open the taskbar settings when Windows has no settings for it yet
        /// </summary>
        public void ShowTrayIcon()
        {
            try
            {
                if (ShowNextToClock(Registry.CurrentUser, System.Windows.Forms.Application.ExecutablePath, TrayIconId))
                {
                    Refresh();
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Warn("Couldn't show the notification area icon next to the clock", ex);
            }

            // Windows wouldn't take it, the user can still switch it on in the taskbar settings
            try
            {
                Process.Start(new ProcessStartInfo("ms-settings:taskbar") { UseShellExecute = true })?.Dispose();
            }
            catch (Exception ex)
            {
                Log.Warn("Couldn't open the taskbar settings", ex);
            }
        }

        public void FixSnippingTool()
        {
            _hotkeyChecks.DisableSnippingToolTakeover();
            Refresh();
        }

        public void FixOneDrive()
        {
            _hotkeyChecks.DisableOneDriveHotkey();
            Refresh();
        }

        /// <summary>
        /// Windows 11 remembers per notification icon if it's shown next to the clock (IsPromoted 1) or hidden in the overflow,
        /// in HKCU\Control Panel\NotifyIconSettings\{a number of Windows}, one key per executable and icon id (UID).
        /// A new icon is hidden until the user shows it. Windows 10 has no such key, there it's never reported as hidden.
        /// </summary>
        /// <param name="currentUser">HKEY_CURRENT_USER</param>
        /// <param name="executablePath">The path of Greenshot.exe</param>
        /// <param name="iconId">The id Greenshot adds its icon with</param>
        /// <returns>True when Windows has the icon and hides it</returns>
        internal static bool IsHiddenInOverflow(RegistryKey currentUser, string executablePath, int iconId)
        {
            using var iconKey = OpenIconSettings(currentUser, executablePath, iconId, false);
            return iconKey != null && !(iconKey.GetValue("IsPromoted") is int isPromoted && isPromoted == 1);
        }

        /// <summary>
        /// Show the icon next to the clock, like the switch of the icon in the taskbar settings does: Windows applies it right away.
        /// This isn't documented and only done when the user asks for it.
        /// </summary>
        /// <returns>False when Windows has no settings for the icon</returns>
        internal static bool ShowNextToClock(RegistryKey currentUser, string executablePath, int iconId)
        {
            using var iconKey = OpenIconSettings(currentUser, executablePath, iconId, true);
            if (iconKey == null)
            {
                return false;
            }

            iconKey.SetValue("IsPromoted", 1, RegistryValueKind.DWord);
            return true;
        }

        /// <summary>
        /// The key Windows keeps for the icon with this id of this executable, null when there is none
        /// </summary>
        private static RegistryKey OpenIconSettings(RegistryKey currentUser, string executablePath, int iconId, bool writable)
        {
            using var notifyIconSettings = currentUser.OpenSubKey(@"Control Panel\NotifyIconSettings");
            if (notifyIconSettings == null)
            {
                return null;
            }

            foreach (var iconKeyName in notifyIconSettings.GetSubKeyNames())
            {
                var iconKey = notifyIconSettings.OpenSubKey(iconKeyName, writable);
                if (iconKey?.GetValue("ExecutablePath") is string registeredPath && IsSameExecutable(registeredPath, executablePath)
                    && iconKey.GetValue("UID") is int uid && uid == iconId)
                {
                    return iconKey;
                }
                iconKey?.Dispose();
            }

            return null;
        }

        /// <summary>
        /// The path of an executable in a known folder starts with the GUID of the folder,
        /// e.g. {6D809377-6AF0-444B-8957-A3773F02200E}\Greenshot\Greenshot.exe for C:\Program Files\Greenshot\Greenshot.exe
        /// </summary>
        internal static bool IsSameExecutable(string registeredPath, string executablePath)
        {
            if (string.IsNullOrEmpty(registeredPath) || string.IsNullOrEmpty(executablePath))
            {
                return false;
            }

            int knownFolderEnd = registeredPath.StartsWith("{", StringComparison.Ordinal) ? registeredPath.IndexOf('}') : -1;
            if (knownFolderEnd > 0)
            {
                return executablePath.EndsWith(registeredPath.Substring(knownFolderEnd + 1), StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(registeredPath, executablePath, StringComparison.OrdinalIgnoreCase);
        }
    }
}
