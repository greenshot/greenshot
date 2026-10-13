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
using System.IO;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace Greenshot.Shell
{
    /// <summary>
    /// Windows 11 keeps a new notification icon in the overflow until the user turns it on.
    /// The choice is stored per executable and icon id. An icon id Windows has never seen
    /// has no choice yet; that icon is shown on the taskbar. A choice the user already made
    /// (including turning the icon off) is left as it is.
    /// </summary>
    internal static class TrayIconPromotion
    {
        /// <summary>
        /// Sets IsPromoted for this process's icon when Windows has not stored a choice yet.
        /// Returns true when a value was written. The icon has to be added again afterwards,
        /// Explorer reads the value when the icon is created.
        /// </summary>
        internal static bool PromoteIfUndecided(uint iconId)
        {
            string exePath = CurrentExecutablePath();
            if (exePath == null)
            {
                return false;
            }

            using (var settings = Registry.CurrentUser.OpenSubKey(@"Control Panel\NotifyIconSettings", writable: true))
            {
                if (settings == null)
                {
                    return false;
                }

                foreach (var name in settings.GetSubKeyNames())
                {
                    using (var iconKey = settings.OpenSubKey(name, writable: true))
                    {
                        if (iconKey == null ||
                            !SameExecutable(iconKey.GetValue("ExecutablePath") as string, exePath) ||
                            !SameId(iconKey.GetValue("UID"), iconId) ||
                            iconKey.GetValue("IsPromoted") != null)
                        {
                            continue;
                        }

                        iconKey.SetValue("IsPromoted", 1, RegistryValueKind.DWord);
                        return true;
                    }
                }
            }

            return false;
        }

        private static string CurrentExecutablePath()
        {
            try
            {
                return System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool SameId(object registeredId, uint iconId)
        {
            switch (registeredId)
            {
                case int id:
                    return unchecked((uint)id) == iconId;
                case long id:
                    return unchecked((uint)id) == iconId;
                default:
                    return false;
            }
        }

        private static bool SameExecutable(string registeredPath, string exePath)
        {
            if (string.IsNullOrEmpty(registeredPath))
            {
                return false;
            }

            return string.Equals(ExpandKnownFolder(registeredPath), exePath, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Explorer stores a path under Program Files as "{known-folder-guid}\Greenshot\Greenshot.exe".
        /// </summary>
        private static string ExpandKnownFolder(string path)
        {
            if (path.Length < 3 || path[0] != '{')
            {
                return path;
            }

            int end = path.IndexOf('}');
            if (end <= 1 || !Guid.TryParse(path.Substring(1, end - 1), out Guid folderId))
            {
                return path;
            }

            string folder = KnownFolderPath(folderId);
            if (folder == null)
            {
                return path;
            }

            string rest = path.Substring(end + 1).TrimStart('\\');
            return rest.Length == 0 ? folder : Path.Combine(folder, rest);
        }

        private static string KnownFolderPath(Guid folderId)
        {
            if (SHGetKnownFolderPath(ref folderId, 0, IntPtr.Zero, out IntPtr pointer) != 0 || pointer == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                return Marshal.PtrToStringUni(pointer);
            }
            finally
            {
                Marshal.FreeCoTaskMem(pointer);
            }
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHGetKnownFolderPath(ref Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);
    }
}
