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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Greenshot.Base.Native.Toasts
{
    /// <summary>
    /// Toasts of an app which isn't packaged (no MSIX): the app id (AppUserModelID) is registered for the current user,
    /// with the name and icon of the toasts and the COM class which receives the clicks (<see cref="ToastActivator"/>).
    /// </summary>
    public static class DesktopToasts
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SetCurrentProcessExplicitAppUserModelID(string appId);

        /// <summary>
        /// Gives this process the app id, registers it and receives the clicks. Call it before the app shows a window,
        /// the taskbar groups the windows by the app id.
        /// </summary>
        /// <param name="appId">The app id, the same as the one of the start menu shortcut</param>
        /// <param name="displayName">The name shown on the toasts</param>
        /// <param name="iconPath">A png file with the icon shown on the toasts, null for none</param>
        /// <param name="clickCommandLine">What Windows starts for a click when the app isn't running</param>
        public static void Register(string appId, string displayName, string iconPath, string clickCommandLine)
        {
            SetCurrentProcessExplicitAppUserModelID(appId);
            using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\AppUserModelId\{appId}"))
            {
                key.SetValue("DisplayName", displayName);
                if (iconPath != null)
                {
                    key.SetValue("IconUri", iconPath);
                }
                else
                {
                    key.DeleteValue("IconUri", false);
                }
                key.SetValue("CustomActivator", $"{{{ToastActivator.Clsid}}}");
            }
            using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Classes\CLSID\{{{ToastActivator.Clsid}}}\LocalServer32"))
            {
                key.SetValue(null, clickCommandLine);
            }
            ToastActivator.RegisterForClicks();
        }

        /// <summary>
        /// The values the Windows Community Toolkit wrote next to its CustomActivator, which nothing else uses
        /// </summary>
        private static readonly string[] ToolkitValues = { "CustomActivator", "IconBackgroundColor", "Has7.0.1Fix" };

        /// <summary>
        /// The values of a key which the Toolkit created for an app id of its own, the key is removed when only these are left
        /// </summary>
        private static readonly string[] ToolkitKeyValues = { "DisplayName", "IconUri", "HasSentNotification" };

        /// <summary>
        /// Removes the toast registrations whose clicks start another COM class in an exe with the same file name, like the ones the
        /// Windows Community Toolkit wrote for Greenshot up to 1.4: one per exe path (Windows lists each of them as an app in its
        /// notification settings), a CustomActivator of its own for <paramref name="appId"/>, and sometimes its CustomActivator in the
        /// app id of another app. Removed are the COM class, the values the Toolkit wrote (the whole key of an app id it created) and
        /// its icon folder. Call it before <see cref="Register"/>, which then writes the own registration.
        /// </summary>
        /// <param name="appId">The own app id: it is kept, only the Toolkit's values are removed from it</param>
        /// <param name="exeFileName">The file name of the exe, e.g. Greenshot.exe</param>
        /// <returns>The app ids whose registration was removed</returns>
        public static IReadOnlyCollection<string> RemoveOldRegistrations(string appId, string exeFileName)
        {
            var removed = new List<string>();
            using var appIds = Registry.CurrentUser.OpenSubKey(@"Software\Classes\AppUserModelId", true);
            if (appIds == null)
            {
                return removed;
            }
            foreach (var otherAppId in appIds.GetSubKeyNames())
            {
                using var key = appIds.OpenSubKey(otherAppId, true);
                var clsid = key?.GetValue("CustomActivator") as string;
                if (string.IsNullOrEmpty(clsid) || string.Equals(clsid.Trim('{', '}'), ToastActivator.Clsid, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                string commandLine;
                using (var server = Registry.CurrentUser.OpenSubKey($@"Software\Classes\CLSID\{clsid}\LocalServer32"))
                {
                    commandLine = server?.GetValue(null) as string;
                }
                if (!string.Equals(GetExeFileName(commandLine), exeFileName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\CLSID\{clsid}", false);
                var iconPath = key.GetValue("IconUri") as string;
                foreach (var value in ToolkitValues)
                {
                    key.DeleteValue(value, false);
                }
                bool ownAppId = string.Equals(otherAppId, appId, StringComparison.OrdinalIgnoreCase);
                if (!ownAppId && key.SubKeyCount == 0 && key.GetValueNames().All(name => ToolkitKeyValues.Contains(name, StringComparer.OrdinalIgnoreCase)))
                {
                    key.Dispose();
                    appIds.DeleteSubKeyTree(otherAppId, false);
                }
                DeleteToolkitIconDirectory(iconPath);
                removed.Add(otherAppId);
            }
            return removed;
        }

        /// <summary>
        /// The file name of the exe a command line starts, e.g. Greenshot.exe for "C:\Program Files\Greenshot\Greenshot.exe" -ToastActivated
        /// </summary>
        internal static string GetExeFileName(string commandLine)
        {
            var trimmed = commandLine?.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                return null;
            }
            string exePath;
            if (trimmed[0] == '"')
            {
                int end = trimmed.IndexOf('"', 1);
                exePath = end < 0 ? trimmed.Substring(1) : trimmed.Substring(1, end - 1);
            }
            else
            {
                int end = trimmed.IndexOf(' ');
                exePath = end < 0 ? trimmed : trimmed.Substring(0, end);
            }
            try
            {
                return Path.GetFileName(exePath);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>
        /// The Toolkit kept the icon in a folder of its own: %LocalAppData%\ToastNotificationManagerCompat\Apps\{id}\Icon.png
        /// </summary>
        private static void DeleteToolkitIconDirectory(string iconPath)
        {
            var iconDirectory = string.IsNullOrEmpty(iconPath) ? null : Path.GetDirectoryName(iconPath);
            var appsDirectory = iconDirectory == null ? null : Path.GetDirectoryName(iconDirectory);
            if (appsDirectory == null || !string.Equals(Path.GetFileName(appsDirectory), "Apps", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(Path.GetFileName(Path.GetDirectoryName(appsDirectory)), "ToastNotificationManagerCompat", StringComparison.OrdinalIgnoreCase)
                || !Directory.Exists(iconDirectory))
            {
                return;
            }
            Directory.Delete(iconDirectory, true);
        }

        /// <summary>
        /// Whether Windows shows the toasts of the registered app id, or why not (turned off by the user or a group policy)
        /// </summary>
        public static NotificationSetting GetSetting(string appId) => ToastNotificationManager.CreateToastNotifier(appId).Setting;

        /// <summary>
        /// Shows the toast, it stays in the notification center until it expires or the user removes it
        /// </summary>
        /// <param name="appId">The registered app id</param>
        /// <param name="content">The content, see <see cref="ToastContent"/></param>
        /// <param name="tag">Identifies the toast, see <see cref="GetTags"/></param>
        /// <param name="expiration">When Windows removes it, null for the default (3 days)</param>
        public static ToastNotification Show(string appId, XmlDocument content, string tag, DateTimeOffset? expiration)
        {
            var toast = new ToastNotification(content)
            {
                Tag = tag,
                ExpirationTime = expiration
            };
            ToastNotificationManager.CreateToastNotifier(appId).Show(toast);
            return toast;
        }

        /// <summary>
        /// The tags of the toasts which are in the notification center
        /// </summary>
        public static IReadOnlyCollection<string> GetTags(string appId) => ToastNotificationManager.History.GetHistory(appId).Select(toast => toast.Tag).ToList();
    }
}
