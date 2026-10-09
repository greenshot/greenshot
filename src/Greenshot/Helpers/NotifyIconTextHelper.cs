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

using System.Text.RegularExpressions;

namespace Greenshot.Helpers
{
    /// <summary>
    /// Makes text safe for NotifyIcon.Text, which throws an ArgumentOutOfRangeException on .NET Framework
    /// when the text is 64 characters or longer (see #1425).
    /// </summary>
    internal static class NotifyIconTextHelper
    {
        /// <summary>
        /// The maximum length NotifyIcon.Text accepts on .NET Framework
        /// </summary>
        internal const int MaxLength = 63;

        private const string Fallback = "Greenshot";
        private const char Ellipsis = '…';
        private static readonly Regex WhitespaceRegex = new Regex(@"\s+", RegexOptions.Compiled);

        /// <summary>
        /// Collapse whitespace and shorten the text, at a word boundary with an ellipsis, so it fits in MaxLength
        /// </summary>
        /// <param name="text">text to show as the tray icon tooltip</param>
        /// <returns>text of at most MaxLength characters, never empty</returns>
        internal static string ToNotifyIconText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return Fallback;
            }

            text = WhitespaceRegex.Replace(text, " ").Trim();
            if (text.Length <= MaxLength)
            {
                return text;
            }

            // Leave room for the ellipsis and don't split a surrogate pair
            int length = MaxLength - 1;
            if (char.IsHighSurrogate(text[length - 1]))
            {
                length--;
            }

            string shortened = text.Substring(0, length);
            // Prefer cutting at a word boundary, unless that would throw away more than half of the text
            int lastSpace = shortened.LastIndexOf(' ');
            if (lastSpace > length / 2)
            {
                shortened = shortened.Substring(0, lastSpace);
            }

            return shortened.TrimEnd(' ', '-', '–', '—', ',', ';', ':') + Ellipsis;
        }
    }
}
