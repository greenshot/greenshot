/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Linq;
using Dapplo.Ini;
using log4net;

namespace Greenshot.Helpers
{
    /// <summary>
    /// Decides where greenshot.ini (and its defaults / fixed companion files) are read from and written to.
    /// </summary>
    internal static class IniLocation
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(IniLocation));

        internal const string DefaultsFileName = "greenshot-defaults.ini";
        internal const string ConstantsFileName = "greenshot-fixed.ini";

        /// <summary>
        /// Adds the search paths, defaults file and constants file to the builder.
        /// <para>
        /// Without <paramref name="iniDirectory"/> the ini is searched in <paramref name="appDataDirectory"/>
        /// and then <paramref name="startupDirectory"/>, and written to the AppData directory when not found.
        /// </para>
        /// <para>
        /// With <paramref name="iniDirectory"/> the ini is read from and written to that directory only
        /// (it is created when missing), so an existing AppData or startup-path ini can never take over.
        /// Dapplo.Ini returns the first search path where the file exists, so the override directory must be
        /// the only search path. The defaults and fixed files are therefore resolved here to absolute paths,
        /// looking in the override directory first and then the startup and AppData directories.
        /// When the override directory cannot be created, a warning is logged and the normal locations are used.
        /// </para>
        /// </summary>
        /// <returns>The directory used for greenshot.ini when the override is active, otherwise <c>null</c>.</returns>
        internal static string Configure(IniConfigBuilder builder, string iniDirectory, string appDataDirectory, string startupDirectory)
        {
            var overrideDirectory = PrepareOverrideDirectory(iniDirectory);
            if (overrideDirectory == null)
            {
                Directory.CreateDirectory(appDataDirectory);
                builder.AddSearchPath(appDataDirectory)
                       .AddSearchPath(startupDirectory)
                       .AddDefaultsFile(DefaultsFileName)
                       .AddConstantsFile(ConstantsFileName);
                return null;
            }

            string[] companionDirectories = [overrideDirectory, startupDirectory, appDataDirectory];
            builder.AddSearchPath(overrideDirectory)
                   .AddDefaultsFile(ResolveCompanionFile(DefaultsFileName, companionDirectories))
                   .AddConstantsFile(ResolveCompanionFile(ConstantsFileName, companionDirectories));
            return overrideDirectory;
        }

        /// <summary>
        /// Returns the full path of the requested ini directory after making sure it exists,
        /// or <c>null</c> when no directory was requested or it cannot be used.
        /// </summary>
        private static string PrepareOverrideDirectory(string iniDirectory)
        {
            if (string.IsNullOrWhiteSpace(iniDirectory))
            {
                return null;
            }

            try
            {
                var fullPath = Path.GetFullPath(iniDirectory);
                Directory.CreateDirectory(fullPath);
                return fullPath;
            }
            catch (Exception ex)
            {
                Log.Warn($"The ini-directory '{iniDirectory}' can't be used, falling back to the default location: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Returns the first existing <paramref name="fileName"/> in <paramref name="directories"/>, or the
        /// (non-existing) path in the first directory so Dapplo.Ini simply skips it.
        /// </summary>
        private static string ResolveCompanionFile(string fileName, string[] directories)
        {
            var candidates = directories.Where(d => !string.IsNullOrEmpty(d)).Select(d => Path.Combine(d, fileName)).ToList();
            return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
        }
    }
}
