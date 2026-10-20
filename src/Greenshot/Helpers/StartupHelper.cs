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

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using log4net;
using Microsoft.Win32;

namespace Greenshot.Helpers
{
    /// <summary>
    /// Starting Greenshot with Windows: the "Greenshot" value in the Run key of the current user.
    /// Older installers wrote it to the Run key of the machine (all users), that one is still recognized, see docs/vNext/autostart.md
    /// </summary>
    public static class StartupHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(StartupHelper));

        internal const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        /// <summary>
        /// Windows (Task Manager, Settings > Apps > Startup) keeps here if a Run entry is switched off, the entry itself stays
        /// </summary>
        internal const string StartupApprovedRunKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

        /// <summary>
        /// The same for the entries of the 32-bit Run key of the machine, it's in the 64-bit view
        /// </summary>
        internal const string StartupApprovedRun32Key = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run32";

        internal const string ValueName = "Greenshot";

        /// <summary>
        /// The Run value: the quoted path with --autostart, so a second start at the logon ends silently (see GreenshotApplication.Start).
        /// The same as autostart.inf of the installer writes.
        /// </summary>
        internal static string CreateRunValue(string executablePath) => $"\"{executablePath}\" --autostart";

        /// <summary>
        /// Does this Greenshot start with Windows? An entry for another Greenshot.exe doesn't count, nor one switched off in Windows.
        /// </summary>
        public static bool IsAutostartEnabled()
        {
            try
            {
                using var locations = RunLocations.Open();
                return IsAutostartEnabled(locations, Application.ExecutablePath);
            }
            catch (Exception ex)
            {
                Log.Error("Couldn't check if Greenshot starts with Windows", ex);
                return false;
            }
        }

        /// <summary>
        /// Let this Greenshot start with Windows, for the current user.
        /// The installer writes the same value with autostart.inf, also for every user through Active Setup.
        /// </summary>
        public static void EnableAutostart()
        {
            try
            {
                using var locations = RunLocations.Open();
                EnableAutostart(locations, Application.ExecutablePath);
            }
            catch (Exception ex)
            {
                Log.Error("Couldn't let Greenshot start with Windows", ex);
            }
        }

        /// <summary>
        /// Don't start this Greenshot with Windows anymore
        /// </summary>
        public static void DisableAutostart()
        {
            try
            {
                using var locations = RunLocations.Open();
                DisableAutostart(locations, Application.ExecutablePath);
            }
            catch (Exception ex)
            {
                Log.Error("Couldn't stop Greenshot from starting with Windows", ex);
            }
        }

        internal static bool IsAutostartEnabled(RunLocations locations, string executablePath)
        {
            foreach (var location in locations.All)
            {
                if (location.StartsExecutable(executablePath) && location.IsApproved())
                {
                    return true;
                }
            }
            return false;
        }

        internal static void EnableAutostart(RunLocations locations, string executablePath)
        {
            // An entry for all users, from an older installer, already starts it: a second one would start Greenshot twice
            foreach (var machineLocation in locations.Machine)
            {
                if (machineLocation.StartsExecutable(executablePath) && machineLocation.IsApproved())
                {
                    Log.Info("Greenshot already starts with Windows for all users.");
                    return;
                }
            }

            var user = locations.User;
            using (var runKey = user.Root.CreateSubKey(user.RunKeyPath))
            {
                runKey.SetValue(ValueName, CreateRunValue(executablePath));
            }

            // Switched off in the Task Manager earlier: without the value Windows starts it
            using (var approvedKey = user.ApprovedRoot.OpenSubKey(user.ApprovedKeyPath, true))
            {
                if (approvedKey?.GetValue(ValueName) != null && !user.IsApproved())
                {
                    approvedKey.DeleteValue(ValueName, false);
                }
            }
            Log.InfoFormat("Greenshot starts with Windows: {0}", executablePath);
        }

        internal static void DisableAutostart(RunLocations locations, string executablePath)
        {
            foreach (var location in locations.All)
            {
                if (!location.StartsExecutable(executablePath))
                {
                    continue;
                }

                try
                {
                    using var runKey = location.Root.OpenSubKey(location.RunKeyPath, true);
                    runKey?.DeleteValue(ValueName, false);
                    Log.InfoFormat("Removed the start with Windows from {0}", location);
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException)
                {
                    // The entry for all users of an older installer, only an administrator can remove it
                    Log.Warn($"Couldn't remove the start with Windows from {location}, that needs administrator rights.", ex);
                }
            }
        }

        /// <summary>
        /// The executable a Run value starts: the part in quotes, or up to ".exe" without them
        /// </summary>
        internal static string GetExecutable(string runValue)
        {
            if (string.IsNullOrWhiteSpace(runValue))
            {
                return null;
            }

            string value = Environment.ExpandEnvironmentVariables(runValue.Trim());
            if (value.StartsWith("\"", StringComparison.Ordinal))
            {
                int end = value.IndexOf('"', 1);
                return end > 1 ? value.Substring(1, end - 1) : null;
            }

            int exeEnd = value.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            return exeEnd > 0 ? value.Substring(0, exeEnd + 4) : value;
        }

        internal static bool IsSameExecutable(string path, string executablePath)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(executablePath))
            {
                return false;
            }

            try
            {
                return string.Equals(Path.GetFullPath(path), Path.GetFullPath(executablePath), StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                return false;
            }
        }

        /// <summary>
        /// Test if there is a link in the Startup folder
        /// </summary>
        /// <returns>bool</returns>
        public static bool IsInStartupFolder()
        {
            try
            {
                string lnkName = Path.GetFileNameWithoutExtension(Application.ExecutablePath) + ".lnk";
                string startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                if (Directory.Exists(startupPath))
                {
                    Log.DebugFormat("Startup path: {0}", startupPath);
                    if (File.Exists(Path.Combine(startupPath, lnkName)))
                    {
                        return true;
                    }
                }

                string startupAll = Environment.GetEnvironmentVariable("ALLUSERSPROFILE") + @"\Microsoft\Windows\Start Menu\Programs\Startup";
                if (Directory.Exists(startupAll))
                {
                    Log.DebugFormat("Startup all path: {0}", startupAll);
                    if (File.Exists(Path.Combine(startupAll, lnkName)))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // ignored
            }

            return false;
        }
    }

    /// <summary>
    /// One Run key Windows starts programs from, with the key where Windows keeps if its entries are switched off
    /// </summary>
    internal sealed class RunLocation
    {
        public RunLocation(string name, RegistryKey root, string runKeyPath, RegistryKey approvedRoot, string approvedKeyPath)
        {
            Name = name;
            Root = root;
            RunKeyPath = runKeyPath;
            ApprovedRoot = approvedRoot;
            ApprovedKeyPath = approvedKeyPath;
        }

        public string Name { get; }
        public RegistryKey Root { get; }
        public string RunKeyPath { get; }
        public RegistryKey ApprovedRoot { get; }
        public string ApprovedKeyPath { get; }

        public string GetRunValue()
        {
            using var runKey = Root.OpenSubKey(RunKeyPath, false);
            return runKey?.GetValue(StartupHelper.ValueName) as string;
        }

        public bool StartsExecutable(string executablePath) =>
            StartupHelper.IsSameExecutable(StartupHelper.GetExecutable(GetRunValue()), executablePath);

        /// <summary>
        /// Not switched off in Windows: the first byte of the StartupApproved value is even (2 or 6), odd (3 or 7) when switched off.
        /// No value means it's on.
        /// </summary>
        public bool IsApproved()
        {
            using var approvedKey = ApprovedRoot.OpenSubKey(ApprovedKeyPath, false);
            return approvedKey?.GetValue(StartupHelper.ValueName) is not byte[] { Length: > 0 } state || (state[0] & 1) == 0;
        }

        public override string ToString() => Name;
    }

    /// <summary>
    /// The Run keys Greenshot uses or used: the current user's, and the machine's in the 64-bit and the 32-bit view
    /// </summary>
    internal sealed class RunLocations : IDisposable
    {
        private readonly IList<RegistryKey> _ownedKeys;

        public RunLocations(RegistryKey currentUser, RegistryKey machine64, RegistryKey machine32, IList<RegistryKey> ownedKeys = null)
        {
            _ownedKeys = ownedKeys ?? new List<RegistryKey>();
            User = new RunLocation("the Run key of the current user", currentUser, StartupHelper.RunKey, currentUser, StartupHelper.StartupApprovedRunKey);
            Machine = new[]
            {
                new RunLocation("the Run key of the machine", machine64, StartupHelper.RunKey, machine64, StartupHelper.StartupApprovedRunKey),
                new RunLocation("the 32-bit Run key of the machine", machine32, StartupHelper.RunKey, machine64, StartupHelper.StartupApprovedRun32Key)
            };
        }

        public RunLocation User { get; }

        public IReadOnlyList<RunLocation> Machine { get; }

        public IEnumerable<RunLocation> All
        {
            get
            {
                yield return User;
                foreach (var location in Machine)
                {
                    yield return location;
                }
            }
        }

        /// <summary>
        /// The real registry. A 32-bit Greenshot would only see the 32-bit view of the machine without RegistryView.Registry64.
        /// </summary>
        public static RunLocations Open()
        {
            var machine64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Default);
            var machine32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            return new RunLocations(Registry.CurrentUser, machine64, machine32, new[] { machine64, machine32 });
        }

        public void Dispose()
        {
            foreach (var key in _ownedKeys)
            {
                key.Dispose();
            }
        }
    }
}
