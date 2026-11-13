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
using System.Globalization;
using System.IO;
using System.Text;
using Greenshot.Editor.Standalone.Settings;

namespace Greenshot.Editor.Standalone.Diagnostics
{
    /// <summary>
    /// The log file of the editor (greenshot-editor.log). Values which are logged can never break a line of the log:
    /// line breaks and other control characters in them are escaped here, once for every caller.
    /// </summary>
    public static class EditorLog
    {
        private const long MaxSize = 2 * 1024 * 1024;
        private static readonly object Lock = new();
        private static bool _failed;

        /// <summary>
        /// False to keep debug lines out of the log
        /// </summary>
        public static bool IsDebugEnabled { get; set; }
#if DEBUG
            = true;
#endif

        public static void Debug(string message)
        {
            if (IsDebugEnabled)
            {
                Write("DEBUG", message, null);
            }
        }

        public static void Info(string message) => Write("INFO", message, null);

        public static void Warn(string message, Exception exception = null) => Write("WARN", message, exception);

        public static void Error(string message, Exception exception = null) => Write("ERROR", message, exception);

        /// <summary>
        /// The text with every control character escaped, so it stays on its line
        /// </summary>
        public static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            StringBuilder escaped = null;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (!char.IsControl(c) && c != '\u2028' && c != '\u2029')
                {
                    escaped?.Append(c);
                    continue;
                }

                escaped ??= new StringBuilder(text, 0, i, text.Length + 8);
                escaped.Append(c switch
                {
                    '\r' => "\\r",
                    '\n' => "\\n",
                    '\t' => "\\t",
                    _ => "\\u" + ((int)c).ToString("x4", CultureInfo.InvariantCulture)
                });
            }

            return escaped?.ToString() ?? text;
        }

        private static void Write(string level, string message, Exception exception)
        {
            if (_failed)
            {
                return;
            }

            string line = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss,fff", CultureInfo.InvariantCulture)} {level} {Escape(message)}";
            if (exception != null)
            {
                line += " | " + Escape(exception.ToString());
            }

            lock (Lock)
            {
                try
                {
                    string path = EditorPaths.LogFile;
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    var file = new FileInfo(path);
                    if (file.Exists && file.Length > MaxSize)
                    {
                        // One older log is kept
                        File.Move(path, path + ".1", true);
                    }

                    File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);
                }
                catch (Exception)
                {
                    // Without a log the editor still works, and there is nowhere to tell
                    _failed = true;
                }
            }
        }
    }
}
