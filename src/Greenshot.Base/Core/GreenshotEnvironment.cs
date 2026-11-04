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
using System.IO;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Holds runtime environment state for Greenshot that is determined at startup
    /// and shared across the application.
    /// </summary>
    public static class GreenshotEnvironment
    {
        /// <summary>
        /// The folder of the PortableApps.com package (GreenshotPortable) when Greenshot.exe runs from its <c>App\Greenshot</c>,
        /// detected at startup by <c>App\AppInfo\appinfo.ini</c> in that folder; null otherwise.
        /// </summary>
        public static string PortableAppsDirectory { get; set; }

        /// <summary>
        /// Finds the folder of the PortableApps.com package for Greenshot.exe in <paramref name="applicationDirectory"/>:
        /// two folders up (from App\Greenshot), when it has App\AppInfo\appinfo.ini.
        /// </summary>
        /// <param name="applicationDirectory">The directory of Greenshot.exe</param>
        /// <returns>The package folder, or null when Greenshot.exe isn't in a PortableApps.com package</returns>
        public static string FindPortableAppsDirectory(string applicationDirectory)
        {
            var packageDirectory = Path.GetFullPath(Path.Combine(applicationDirectory, @"..\.."));
            return File.Exists(Path.Combine(packageDirectory, @"App\AppInfo\appinfo.ini")) ? packageDirectory : null;
        }

        /// <summary>
        /// Gets the full path of the active <c>greenshot.ini</c> configuration file.
        /// Resolved lazily from <see cref="Dapplo.Ini.IniConfigRegistry"/> so that
        /// it is correct even when read before the initial <c>Load()</c> call completes.
        /// </summary>
        public static string ConfigLocation => Dapplo.Ini.IniConfigRegistry.Get()?.LoadedFromPath ?? string.Empty;

        /// <summary>
        /// The directory for the files Greenshot keeps on this computer: %LocalAppData%\Greenshot
        /// </summary>
        public static string LocalDataDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Greenshot");

        /// <summary>
        /// True when Greenshot.exe runs from the PortableApps.com package (<see cref="PortableAppsDirectory"/>)
        /// </summary>
        public static bool IsPortableApps => PortableAppsDirectory != null;

        /// <summary>
        /// The Data\Settings folder of the PortableApps.com package, where it keeps greenshot.ini; null outside the package
        /// </summary>
        public static string PortableAppsSettingsDirectory => IsPortableApps ? Path.Combine(PortableAppsDirectory, @"Data\Settings") : null;

        /// <summary>
        /// The default folder for saved captures: Documents\Pictures\Greenshots on the drive of the PortableApps.com package
        /// (the PortableApps.com folders), otherwise the desktop
        /// </summary>
        public static string DefaultOutputDirectory => IsPortableApps
            ? Path.GetFullPath(Path.Combine(PortableAppsDirectory, @"..\..\Documents\Pictures\Greenshots"))
            : Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        /// <summary>
        /// True when greenshot.ini was loaded from the folder of Greenshot.exe, like the .zip does with the greenshot.ini it ships.
        /// Only known after greenshot.ini was loaded.
        /// </summary>
        public static bool UsesSettingsNextToExecutable
        {
            get
            {
                string configLocation = ConfigLocation;
                return !string.IsNullOrEmpty(configLocation)
                    && string.Equals(Path.GetDirectoryName(configLocation)?.TrimEnd('\\'), AppContext.BaseDirectory.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// True when Greenshot may only write to its own folders: the PortableApps.com package and settings next to Greenshot.exe (the .zip).
        /// Then nothing that is optional, like the toast registration or the JIT profile, is written to the system.
        /// </summary>
        public static bool LeavesNoTraces => IsPortableApps || UsesSettingsNextToExecutable;

        /// <summary>
        /// How this Greenshot runs, for the version in the log and the bug report: " PortableApps", " Portable" or empty
        /// </summary>
        public static string PortableSuffix => IsPortableApps ? " PortableApps" : UsesSettingsNextToExecutable ? " Portable" : string.Empty;
    }
}
