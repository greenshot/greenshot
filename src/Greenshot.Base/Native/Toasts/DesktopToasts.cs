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
