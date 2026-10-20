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
        /// Gets a value indicating whether Greenshot is running as PortableApps.com app (PAF), see <see cref="PortableAppsDirectory"/>.
        /// </summary>
        public static bool IsPortable => PortableAppsDirectory != null;

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
        /// True for the PortableApp and the .zip (greenshot.ini next to Greenshot.exe), these leave nothing on the system
        /// </summary>
        public static bool LeavesNoTraces
        {
            get
            {
                string configLocation = ConfigLocation;
                return IsPortable || (!string.IsNullOrEmpty(configLocation)
                    && string.Equals(Path.GetDirectoryName(configLocation)?.TrimEnd('\\'), AppContext.BaseDirectory.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase));
            }
        }
    }
}
