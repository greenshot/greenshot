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
using System.Linq;
using Dapplo.Ini;
using Greenshot.Editor.Standalone.Diagnostics;

namespace Greenshot.Editor.Standalone.Settings
{
    /// <summary>
    /// Loads and saves greenshot-editor.ini, and reads single values of greenshot.ini
    /// </summary>
    public static class EditorSettings
    {
        private const int MaxRecentEmojis = 24;
        private static readonly object Lock = new();
        private static IniConfig _config;
        private static IEditorSettings _current;

        public static IEditorSettings Current
        {
            get
            {
                lock (Lock)
                {
                    return _current ??= Load();
                }
            }
        }

        private static IEditorSettings Load()
        {
            var section = new EditorSettingsImpl();
            try
            {
                Directory.CreateDirectory(EditorPaths.SettingsDirectory);
                _config = IniConfigRegistry.ForFile(EditorPaths.SettingsFileName)
                    .AddSearchPath(EditorPaths.SettingsDirectory)
                    .RegisterSection<IEditorSettings>(section)
                    .Build();
            }
            catch (Exception ex)
            {
                // The editor works with the defaults, they just aren't kept
                EditorLog.Warn("The settings of the editor could not be loaded", ex);
            }

            return section;
        }

        public static void Save()
        {
            lock (Lock)
            {
                try
                {
                    _config?.Save();
                }
                catch (Exception ex)
                {
                    EditorLog.Warn("The settings of the editor could not be saved", ex);
                }
            }
        }

        /// <summary>
        /// Forgets the loaded settings, for tests which use another folder
        /// </summary>
        internal static void Reset()
        {
            lock (Lock)
            {
                _config?.Dispose();
                IniConfigRegistry.Unregister(EditorPaths.SettingsFileName);
                _config = null;
                _current = null;
            }
        }

        public static IReadOnlyList<string> RecentEmojis =>
            (Current.RecentEmojis ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        /// <summary>
        /// Puts the emoji first in the list of the emoji used last
        /// </summary>
        public static void AddRecentEmoji(string emoji)
        {
            if (string.IsNullOrWhiteSpace(emoji))
            {
                return;
            }

            Current.RecentEmojis = string.Join(" ", new[] { emoji }.Concat(RecentEmojis.Where(known => known != emoji)).Take(MaxRecentEmojis));
        }

        /// <summary>
        /// A value of greenshot.ini, null when the file or the key is missing. Read only: the file belongs to Greenshot.
        /// </summary>
        public static string ReadGreenshotValue(string section, string key) => ReadValue(EditorPaths.GreenshotIni, section, key);

        internal static string ReadValue(string path, string section, string key)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }

                bool inSection = false;
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                using var reader = new StreamReader(stream);
                while (reader.ReadLine() is string line)
                {
                    line = line.Trim();
                    if (line.Length == 0 || line[0] == ';' || line[0] == '#')
                    {
                        continue;
                    }

                    if (line[0] == '[')
                    {
                        inSection = line.TrimEnd().Equals($"[{section}]", StringComparison.OrdinalIgnoreCase);
                        continue;
                    }

                    int separator = line.IndexOf('=');
                    if (inSection && separator > 0 && line.AsSpan(0, separator).Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        return line.Substring(separator + 1).Trim();
                    }
                }
            }
            catch (Exception ex)
            {
                EditorLog.Debug($"{path} could not be read: {ex.Message}");
            }

            return null;
        }
    }
}
