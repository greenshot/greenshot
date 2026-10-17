/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Text;

namespace Greenshot.SelfService
{
    /// <summary>
    /// Filters the log on a search text, per log entry instead of per line:
    /// an entry starts with the date of the log layout (%date{ISO8601}), the lines after it (e.g. an exception) belong to it.
    /// </summary>
    internal static class LogSearch
    {
        /// <summary>
        /// The log entries that contain the search text (ignoring case), in their original order
        /// </summary>
        /// <param name="logText">The log</param>
        /// <param name="search">The text to look for, empty returns the log as it is</param>
        /// <param name="matchCount">The number of entries that contain the text</param>
        /// <param name="entryCount">The number of entries in the log</param>
        public static string Filter(string logText, string search, out int matchCount, out int entryCount)
        {
            matchCount = 0;
            entryCount = 0;
            logText ??= string.Empty;
            if (string.IsNullOrEmpty(search))
            {
                return logText;
            }

            var result = new StringBuilder();
            int entryStart = 0;
            int lineStart = 0;
            while (lineStart < logText.Length)
            {
                int lineEnd = logText.IndexOf('\n', lineStart);
                int nextLineStart = lineEnd < 0 ? logText.Length : lineEnd + 1;
                if (nextLineStart >= logText.Length || StartsEntry(logText, nextLineStart))
                {
                    AddEntry(logText, entryStart, nextLineStart, search, result, ref matchCount, ref entryCount);
                    entryStart = nextLineStart;
                }
                lineStart = nextLineStart;
            }
            return result.ToString();
        }

        private static void AddEntry(string logText, int start, int end, string search, StringBuilder result, ref int matchCount, ref int entryCount)
        {
            if (end <= start)
            {
                return;
            }
            entryCount++;
            int length = end - start;
            if (logText.IndexOf(search, start, length, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }
            matchCount++;
            result.Append(logText, start, length);
            if (logText[end - 1] != '\n')
            {
                result.AppendLine();
            }
        }

        /// <summary>
        /// True when the line at the index starts with a date like 2026-10-16
        /// </summary>
        private static bool StartsEntry(string text, int index)
        {
            if (index + 10 > text.Length)
            {
                return false;
            }
            for (int i = 0; i < 10; i++)
            {
                char c = text[index + i];
                bool ok = i is 4 or 7 ? c == '-' : c is >= '0' and <= '9';
                if (!ok)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
