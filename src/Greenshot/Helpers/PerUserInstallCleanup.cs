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
using System.IO;
using System.Threading;
using log4net;
using Microsoft.Win32;

namespace Greenshot.Helpers
{
    /// <summary>
    /// Removes an older Greenshot which the user installed "for me only" (in the profile), when this Greenshot is the one installed for all users.
    /// The setup for all users doesn't see per-user installations (they are registered in HKCU), so both would stay installed:
    /// two Greenshots in "Apps", autostart entries for the wrong one, and the old uninstaller removing the autostart of this one later.
    /// Done here and not in the setup, because only Greenshot runs as every user: also as the user who is logged on while an administrator installs,
    /// and as every other user at their next logon. See docs/autostart.md.
    /// </summary>
    public static class PerUserInstallCleanup
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PerUserInstallCleanup));

        internal const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\Greenshot_is1";

        /// <summary>
        /// How long the start of Greenshot waits for the old uninstaller, once
        /// </summary>
        private static readonly TimeSpan UninstallTimeout = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Before the single instance check: the old uninstaller refuses to run while a Greenshot holds the mutex (AppMutex of its setup).
        /// One registry read when there's nothing to do.
        /// </summary>
        /// <returns>true when an old copy was removed</returns>
        /// <param name="applicationMutexName">The mutex of the running Greenshot: while a Greenshot runs, the old uninstaller would refuse</param>
        public static bool RemoveOldPerUserInstallation(string applicationMutexName)
        {
            try
            {
                if (Mutex.TryOpenExisting(applicationMutexName, out var runningGreenshot))
                {
                    runningGreenshot.Dispose();
                    return false;
                }

                using var machine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Default);
                string oldInstallation = FindOldPerUserInstallation(Registry.CurrentUser, machine, AppContext.BaseDirectory, out string uninstaller);
                if (oldInstallation == null)
                {
                    return false;
                }

                bool hadAutostart = HasRunValue(Registry.CurrentUser);
                Log.Info($"Removing the older Greenshot installed for this user only in {oldInstallation}, this Greenshot is installed for all users.");
                if (!Uninstall(uninstaller))
                {
                    return false;
                }

                // The old uninstaller removes the Greenshot value from the Run key, also when it starts this Greenshot already
                if (hadAutostart && !HasRunValue(Registry.CurrentUser))
                {
                    Log.Info("The start with Windows was removed by the old uninstaller, it starts this Greenshot again.");
                    StartupHelper.EnableAutostart();
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn("Couldn't remove the older Greenshot installed for this user only", ex);
                return false;
            }
        }

        /// <summary>
        /// The folder of a Greenshot installed for the current user only, when the running Greenshot is the one installed for all users
        /// </summary>
        /// <param name="currentUser">HKEY_CURRENT_USER</param>
        /// <param name="localMachine">HKEY_LOCAL_MACHINE, 64-bit view</param>
        /// <param name="runningDirectory">The folder of the running Greenshot.exe</param>
        /// <param name="uninstaller">The path of its uninstaller</param>
        /// <returns>The folder, or null when there's nothing to remove</returns>
        internal static string FindOldPerUserInstallation(RegistryKey currentUser, RegistryKey localMachine, string runningDirectory, out string uninstaller)
        {
            uninstaller = null;
            using var machineKey = localMachine.OpenSubKey(UninstallKey, false);
            string machineLocation = machineKey?.GetValue("InstallLocation") as string;
            // Only the Greenshot installed for all users removes another one, not a portable one or one started from a build folder
            if (!IsSameDirectory(machineLocation, runningDirectory))
            {
                return null;
            }

            using var userKey = currentUser.OpenSubKey(UninstallKey, false);
            string userLocation = userKey?.GetValue("InstallLocation") as string;
            if (string.IsNullOrEmpty(userLocation) || IsSameDirectory(userLocation, runningDirectory))
            {
                return null;
            }

            uninstaller = StartupHelper.GetExecutable(userKey.GetValue("UninstallString") as string);
            if (string.IsNullOrEmpty(uninstaller) || !File.Exists(uninstaller))
            {
                Log.Info($"The older Greenshot in {userLocation} has no uninstaller anymore, it's left alone.");
                uninstaller = null;
                return null;
            }
            return userLocation;
        }

        private static bool Uninstall(string uninstaller)
        {
            using var process = Process.Start(new ProcessStartInfo(uninstaller, "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART")
            {
                UseShellExecute = false,
                WorkingDirectory = Path.GetTempPath()
            });
            // It waits for its copy in the temp folder, which does the work, and ends with its exit code
            if (process == null || !process.WaitForExit((int)UninstallTimeout.TotalMilliseconds))
            {
                Log.Warn("The uninstaller of the older Greenshot didn't finish in time, it's tried again at the next start.");
                return false;
            }

            using var userKey = Registry.CurrentUser.OpenSubKey(UninstallKey, false);
            if (process.ExitCode != 0 || userKey != null)
            {
                Log.Warn($"The uninstaller of the older Greenshot ended with {process.ExitCode}, it's tried again at the next start.");
                return false;
            }
            Log.Info("The older Greenshot was removed.");
            return true;
        }

        private static bool HasRunValue(RegistryKey currentUser)
        {
            using var runKey = currentUser.OpenSubKey(StartupHelper.RunKey, false);
            return runKey?.GetValue(StartupHelper.ValueName) != null;
        }

        internal static bool IsSameDirectory(string first, string second)
        {
            if (string.IsNullOrEmpty(first) || string.IsNullOrEmpty(second))
            {
                return false;
            }

            try
            {
                return string.Equals(Path.GetFullPath(first).TrimEnd('\\'), Path.GetFullPath(second).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                return false;
            }
        }
    }
}
