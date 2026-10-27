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

namespace Greenshot.Linux.Recipes.Ipc;

/// <summary>
/// Checks a path a caller passes to Greenshot (the IpcSecurityDispatcher of Windows): no control or invisible Unicode
/// characters, resolved against the caller's working directory, and no paths into /proc, /sys or /dev from callers which
/// aren't the user (AI tools, links)
/// </summary>
public static class PathSanitizer
{
    private static readonly string[] SystemFolders = ["/proc", "/sys", "/dev"];

    public static bool TrySanitizeAndResolve(string rawPath, string? cwd, string? source, out string? fullPath, out string? errorMessage)
    {
        fullPath = null;
        errorMessage = null;
        if (string.IsNullOrWhiteSpace(rawPath))
        {
            errorMessage = "Path is empty or whitespace.";
            return false;
        }

        foreach (char c in rawPath)
        {
            if (c < 0x20 || c == 0x7F)
            {
                errorMessage = "Path contains illegal control characters.";
                return false;
            }

            // Zero-width characters and bidirectional overrides, which make a path look like another one
            if (c is >= '​' and <= '‏' or >= '‪' and <= '‮' or >= '⁦' and <= '⁩' or '﻿')
            {
                errorMessage = "Path contains forbidden Unicode formatting or bidirectional override characters.";
                return false;
            }
        }

        string path = rawPath;
        if (path == "~" || path.StartsWith("~/", StringComparison.Ordinal))
        {
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), path.TrimStart('~').TrimStart('/'));
        }

        try
        {
            fullPath = Path.GetFullPath(!string.IsNullOrWhiteSpace(cwd) && !Path.IsPathRooted(path) ? Path.Combine(cwd, path) : path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            errorMessage = $"Path resolution error: {ex.Message}";
            return false;
        }

        bool trusted = string.Equals(source, "cli", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(source);
        string resolved = fullPath;
        if (!trusted && SystemFolders.Any(f => resolved == f || resolved.StartsWith(f + "/", StringComparison.Ordinal)))
        {
            errorMessage = "Paths of the system (/proc, /sys, /dev) are not permitted from this source.";
            fullPath = null;
            return false;
        }

        return true;
    }
}
