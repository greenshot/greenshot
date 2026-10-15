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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Greenshot.Base.Core;
using Greenshot.Base.Languages;
using Greenshot.Base.Wpf;
using log4net;
using Microsoft.Win32;

namespace Greenshot.Helpers
{
    /// <summary>
    /// Offers to remove an older Greenshot which the user installed "for me only" (in the profile), when this Greenshot is the one installed for all users.
    /// The setup for all users doesn't see per-user installations (they are registered in HKCU), so both would stay installed:
    /// two Greenshots in "Apps", autostart entries for the wrong one, and the old uninstaller removing the autostart of this one later.
    /// Done here and not in the setup, because only Greenshot runs as every user: also as the user who is logged on while an administrator installs,
    /// and as every other user at their next logon. The user decides, see docs/autostart.md.
    /// </summary>
    public static class PerUserInstallCleanup
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PerUserInstallCleanup));

        internal const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\Greenshot_is1";

        /// <summary>
        /// How long Greenshot waits for the old uninstaller
        /// </summary>
        private static readonly TimeSpan UninstallTimeout = TimeSpan.FromSeconds(60);

        /// <summary>
        /// Ask the user to remove an older Greenshot installed for them only, if there is one they didn't decide to keep.
        /// Runs on the UI thread, after the start.
        /// </summary>
        /// <param name="configuration">Keeps the folders the user wants to keep</param>
        /// <param name="releaseMutex">The old uninstaller refuses to run while a Greenshot holds the single instance mutex (AppMutex of its setup)</param>
        /// <param name="acquireMutex">Takes the mutex again</param>
        public static async Task OfferRemovalAsync(ICoreConfiguration configuration, Action releaseMutex, Action acquireMutex)
        {
            string oldInstallation;
            string uninstaller;
            string version;
            using (var machine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Default))
            {
                oldInstallation = FindOldPerUserInstallation(Registry.CurrentUser, machine, AppContext.BaseDirectory, out uninstaller, out version);
            }
            if (oldInstallation == null || IsKept(configuration.KeptOlderInstallations, oldInstallation))
            {
                return;
            }

            var texts = Texts.Core;
            Log.Info($"Found an older Greenshot {version} installed for this user only in {oldInstallation}, this Greenshot is installed for all users.");
            int choice = ThemedMessageBox.ShowChoice(null, texts.OlderGreenshotTitle,
                string.Format(texts.OlderGreenshotText, version, oldInstallation, AppContext.BaseDirectory),
                MessageBoxImage.Question, new[] { texts.OlderGreenshotRemove, texts.OlderGreenshotKeep }, defaultIndex: 0, cancelIndex: 1);
            if (choice != 0)
            {
                Log.Info("The user keeps the older Greenshot, they aren't asked again for it.");
                configuration.KeptOlderInstallations = (configuration.KeptOlderInstallations ?? new List<string>()).Append(oldInstallation).ToList();
                return;
            }

            bool hadAutostart = HasRunValue(Registry.CurrentUser);
            object printScreenForSnipping = GetValue(Registry.CurrentUser, KeyboardKey, PrintScreenForSnippingValue);
            bool removed;
            releaseMutex();
            try
            {
                removed = await UninstallAsync(uninstaller);
            }
            finally
            {
                acquireMutex();
            }

            if (!removed)
            {
                ThemedMessageBox.Show(string.Format(texts.OlderGreenshotFailed, oldInstallation), texts.OlderGreenshotTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // The old uninstaller removes the Greenshot value from the Run key, also when it starts this Greenshot already
            if (hadAutostart && !HasRunValue(Registry.CurrentUser))
            {
                Log.Info("The start with Windows was removed by the old uninstaller, it starts this Greenshot again.");
                StartupHelper.EnableAutostart();
            }

            // The setup of 1.3 switched the Print key of the Snipping Tool off for the user (component "disablesnippingtool", the default on Windows 11),
            // its uninstaller removes that value again: the Print key would open the Snipping Tool instead of this Greenshot
            if (printScreenForSnipping != null && GetValue(Registry.CurrentUser, KeyboardKey, PrintScreenForSnippingValue) == null)
            {
                Log.Info("The old uninstaller gave the Print key back to the Snipping Tool, it's set as it was.");
                using var keyboardKey = Registry.CurrentUser.CreateSubKey(KeyboardKey);
                keyboardKey.SetValue(PrintScreenForSnippingValue, printScreenForSnipping, RegistryValueKind.DWord);
            }
        }

        private const string KeyboardKey = @"Control Panel\Keyboard";
        private const string PrintScreenForSnippingValue = "PrintScreenKeyForSnippingEnabled";

        private static object GetValue(RegistryKey root, string keyPath, string valueName)
        {
            using var key = root.OpenSubKey(keyPath, false);
            return key?.GetValue(valueName);
        }

        /// <summary>
        /// The folder of a Greenshot installed for the current user only, when the running Greenshot is the one installed for all users
        /// </summary>
        /// <param name="currentUser">HKEY_CURRENT_USER</param>
        /// <param name="localMachine">HKEY_LOCAL_MACHINE, 64-bit view</param>
        /// <param name="runningDirectory">The folder of the running Greenshot.exe</param>
        /// <param name="uninstaller">The path of its uninstaller</param>
        /// <param name="version">Its version</param>
        /// <returns>The folder, or null when there's nothing to remove</returns>
        internal static string FindOldPerUserInstallation(RegistryKey currentUser, RegistryKey localMachine, string runningDirectory, out string uninstaller, out string version)
        {
            uninstaller = null;
            version = null;
            using var machineKey = localMachine.OpenSubKey(UninstallKey, false);
            string machineLocation = machineKey?.GetValue("InstallLocation") as string;
            // Only the Greenshot installed for all users offers to remove another one, not a portable one or one started from a build folder
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
            version = userKey.GetValue("DisplayVersion") as string;
            return userLocation;
        }

        internal static bool IsKept(IEnumerable<string> keptInstallations, string installation) =>
            keptInstallations?.Any(kept => IsSameDirectory(kept, installation)) == true;

        /// <summary>
        /// Runs the uninstaller silently. It waits for its copy in the temp folder, which does the work, and ends with its exit code.
        /// </summary>
        private static async Task<bool> UninstallAsync(string uninstaller)
        {
            Log.Info($"Removing the older Greenshot with {uninstaller}");
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo(uninstaller, "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART")
                {
                    UseShellExecute = false,
                    WorkingDirectory = Path.GetTempPath()
                },
                EnableRaisingEvents = true
            };
            var exited = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            process.Exited += (sender, args) => exited.TrySetResult(true);
            try
            {
                process.Start();
            }
            catch (Exception ex)
            {
                Log.Warn("Couldn't start the uninstaller of the older Greenshot", ex);
                return false;
            }

            if (await Task.WhenAny(exited.Task, Task.Delay(UninstallTimeout)) != exited.Task)
            {
                Log.Warn("The uninstaller of the older Greenshot didn't finish in time.");
                return false;
            }

            using var userKey = Registry.CurrentUser.OpenSubKey(UninstallKey, false);
            if (process.ExitCode != 0 || userKey != null)
            {
                Log.Warn($"The uninstaller of the older Greenshot ended with {process.ExitCode}.");
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
