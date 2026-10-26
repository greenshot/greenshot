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
/// Starting Greenshot with the session: a desktop entry in $XDG_CONFIG_HOME/autostart, the Linux "Launch Greenshot on startup".
/// GNOME, KDE, Xfce and others read it; on Sway or Hyprland use exec / exec-once in the config instead.
/// </summary>
public static class Autostart
{
    public static string EntryPath => Path.Combine(XdgDirectories.ConfigHome, "autostart", "greenshot.desktop");

    public static bool IsEnabled
    {
        get
        {
            if (!File.Exists(EntryPath))
            {
                return false;
            }

            // An entry can be switched off without removing it
            return !File.ReadLines(EntryPath).Any(line => line.Trim().Equals("Hidden=true", StringComparison.OrdinalIgnoreCase)
                || line.Trim().Equals("X-GNOME-Autostart-enabled=false", StringComparison.OrdinalIgnoreCase));
        }
    }

    public static void SetEnabled(bool enabled)
    {
        if (!enabled)
        {
            if (File.Exists(EntryPath))
            {
                File.Delete(EntryPath);
            }

            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(EntryPath)!);
        File.WriteAllText(EntryPath, CreateEntry(Executable));
    }

    /// <summary>
    /// The program to start: the apphost "greenshot" next to the assembly, or what runs now
    /// </summary>
    public static string Executable
    {
        get
        {
            var appHost = Path.Combine(AppContext.BaseDirectory, "greenshot");
            return File.Exists(appHost) ? appHost : Environment.ProcessPath ?? "greenshot";
        }
    }

    internal static string CreateEntry(string executable) => $"""
        [Desktop Entry]
        Type=Application
        Name=Greenshot
        Comment=Screenshot tool
        Exec="{executable.Replace("\"", "\\\"")}"
        Icon=greenshot
        Terminal=false
        X-GNOME-Autostart-enabled=true

        """;
}
