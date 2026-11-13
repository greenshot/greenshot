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

namespace Greenshot.Editor.Standalone.Settings
{
    /// <summary>
    /// Where the editor finds Greenshot and keeps its own files. Installed next to Greenshot the editor lives in the folder
    /// "Editor" of Greenshot's folder; it also runs alone, e.g. on another operating system.
    /// </summary>
    public static class EditorPaths
    {
        public const string ApplicationFolder = "Greenshot";
        public const string GreenshotExecutableName = "Greenshot.exe";
        public const string GreenshotIniName = "greenshot.ini";
        public const string SettingsFileName = "greenshot-editor.ini";
        public const string LogFileName = "greenshot-editor.log";

        private static string _settingsDirectory;

        /// <summary>
        /// The folder of Greenshot when the editor was installed with it (the folder above the editor), else null
        /// </summary>
        public static string GreenshotDirectory { get; } = FindGreenshot();

        /// <summary>
        /// Greenshot.exe next to which the editor was installed, null when the editor runs alone
        /// </summary>
        public static string GreenshotExecutable => GreenshotDirectory == null ? null : Path.Combine(GreenshotDirectory, GreenshotExecutableName);

        /// <summary>
        /// True when Greenshot keeps its settings next to its executable (portable): the editor then keeps its files there too
        /// and leaves nothing in the profile of the user
        /// </summary>
        public static bool IsPortable => GreenshotDirectory != null && File.Exists(Path.Combine(GreenshotDirectory, GreenshotIniName));

        /// <summary>
        /// The folder of greenshot-editor.ini (and of greenshot.ini, which the editor only reads). Tests set another one.
        /// </summary>
        public static string SettingsDirectory
        {
            get => _settingsDirectory ??= IsPortable ? GreenshotDirectory : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ApplicationFolder);
            set => _settingsDirectory = value;
        }

        /// <summary>
        /// greenshot.ini, the settings of Greenshot. The editor reads the language from it and never writes it.
        /// </summary>
        public static string GreenshotIni => Path.Combine(SettingsDirectory, GreenshotIniName);

        /// <summary>
        /// The log of the editor, next to the log of Greenshot
        /// </summary>
        public static string LogFile => Path.Combine(IsPortable || _settingsDirectory != null ? SettingsDirectory : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ApplicationFolder), LogFileName);

        /// <summary>
        /// The stamps folder: the configured one, else "Stamps" next to the settings (the same folder the stamps of Greenshot use)
        /// </summary>
        public static string StampsDirectory(string configured) => string.IsNullOrWhiteSpace(configured) ? Path.Combine(SettingsDirectory, "Stamps") : Environment.ExpandEnvironmentVariables(configured);

        /// <summary>
        /// The folder through which Greenshot and the editor hand documents to each other, as files. It is the same folder
        /// in both programs: "GreenshotEditor" in the temporary folder of the user. A file in it lives only until the other
        /// side has read it.
        /// </summary>
        public static string HandoverDirectory => Path.Combine(Path.GetTempPath(), "GreenshotEditor");

        /// <summary>
        /// True for a document in the handover folder. Only such a file is removed after it was read: a path from the
        /// command line can never make the editor delete another file.
        /// </summary>
        public static bool IsHandoverFile(string path)
        {
            try
            {
                string full = Path.GetFullPath(path);
                string folder = Path.GetFullPath(HandoverDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                return full.StartsWith(folder, comparison) && string.Equals(Path.GetExtension(full), ".greenshot", StringComparison.OrdinalIgnoreCase)
                       && string.Equals(Path.GetDirectoryName(full) + Path.DirectorySeparatorChar, folder, comparison);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string FindGreenshot()
        {
            try
            {
                var parent = Directory.GetParent(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                return parent != null && File.Exists(Path.Combine(parent.FullName, GreenshotExecutableName)) ? parent.FullName : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
