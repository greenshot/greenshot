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

namespace Greenshot.Linux.Core.Configuration;

/// <summary>
/// The XDG base and user directories, see https://specifications.freedesktop.org/basedir-spec/
/// </summary>
public static class XdgDirectories
{
    private static string Home => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public static string ConfigHome => FromEnvironment("XDG_CONFIG_HOME", Path.Combine(Home, ".config"));

    public static string StateHome => FromEnvironment("XDG_STATE_HOME", Path.Combine(Home, ".local", "state"));

    /// <summary>
    /// $XDG_RUNTIME_DIR, which only the user can read; falls back to a per user folder in /tmp
    /// </summary>
    public static string RuntimeDir => FromEnvironment("XDG_RUNTIME_DIR", Path.Combine(Path.GetTempPath(), $"greenshot-{Environment.UserName}"));

    /// <summary>
    /// The pictures folder of xdg-user-dirs (XDG_PICTURES_DIR in user-dirs.dirs), ~/Pictures when it is not set
    /// </summary>
    public static string Pictures => GetUserDir("XDG_PICTURES_DIR") ?? Path.Combine(Home, "Pictures");

    private static string FromEnvironment(string variable, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(variable);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    internal static string? GetUserDir(string name)
    {
        var file = Path.Combine(ConfigHome, "user-dirs.dirs");
        if (!File.Exists(file))
        {
            return null;
        }

        try
        {
            return ParseUserDir(File.ReadAllLines(file), name, Home);
        }
        catch (IOException)
        {
            return null;
        }
    }

    /// <summary>
    /// Read a line like XDG_PICTURES_DIR="$HOME/Pictures" from user-dirs.dirs
    /// </summary>
    internal static string? ParseUserDir(IEnumerable<string> lines, string name, string home)
    {
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith(name + "=", StringComparison.Ordinal))
            {
                continue;
            }

            var value = trimmed[(name.Length + 1)..].Trim().Trim('"');
            value = value.Replace("$HOME", home, StringComparison.Ordinal);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        return null;
    }
}
