/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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

using System.Runtime.InteropServices;

namespace Greenshot.Base.Native
{
    /// <summary>
    /// The app id (AppUserModelID) of the process: the taskbar groups its windows by it, and Windows files its tray icon and
    /// notifications under it. Without one, Windows makes one up for the tray icon and registers it for the user
    /// (AppUserModelId\NotifyIconGeneratedAumid_…, with an icon in Temp), also for the .zip, which should leave nothing behind.
    /// </summary>
    public static class AppUserModelId
    {
        /// <summary>
        /// The app id of Greenshot, the same as the one of the start menu shortcut (see tasks-icons-reg.iss) and of the toasts
        /// </summary>
        public const string Greenshot = "Greenshot";

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SetCurrentProcessExplicitAppUserModelID(string appId);

        /// <summary>
        /// Gives this process the app id, before it shows a window or its tray icon. It writes nothing, the registration of the
        /// toasts is separate (see Toasts.DesktopToasts.Register).
        /// </summary>
        public static void SetForCurrentProcess(string appId) => SetCurrentProcessExplicitAppUserModelID(appId);
    }
}
