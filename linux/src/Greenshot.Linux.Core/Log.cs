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

using Greenshot.Linux.Core.Configuration;

namespace Greenshot.Linux.Core;

/// <summary>
/// Logging to $XDG_STATE_HOME/greenshot/greenshot.log and to stderr.
/// </summary>
public static class Log
{
    private static readonly object Lock = new();
    private static string? _file;

    public static string FilePath => _file ??= Path.Combine(XdgDirectories.StateHome, "greenshot", "greenshot.log");

    public static bool DebugEnabled { get; set; } = Environment.GetEnvironmentVariable("GREENSHOT_DEBUG") == "1";

    public static void Debug(string message)
    {
        if (DebugEnabled)
        {
            Write("DEBUG", message, null);
        }
    }

    public static void Info(string message) => Write("INFO ", message, null);

    public static void Warn(string message, Exception? ex = null) => Write("WARN ", message, ex);

    public static void Error(string message, Exception? ex = null) => Write("ERROR", message, ex);

    private static void Write(string level, string message, Exception? ex)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {level} {message}";
        if (ex != null)
        {
            line += Environment.NewLine + ex;
        }

        lock (Lock)
        {
            Console.Error.WriteLine(line);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.AppendAllText(FilePath, line + Environment.NewLine);
            }
            catch (IOException)
            {
                // Logging must never break a capture
            }
            catch (UnauthorizedAccessException)
            {
                // Logging must never break a capture
            }
        }
    }
}
