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

using System.Text;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Makes text which goes into the log, or is shown from it, harmless: window titles, file names, URLs or clipboard text can contain anything.
    /// Line breaks are handled by <see cref="EntryLayout"/>, which indents every line of an entry after the first one.
    /// </summary>
    public static class LogSanitizer
    {
        /// <summary>
        /// The longest log entry which is written as it is, the rest is left out:
        /// one entry can't push everything else out of the log (it rolls over at 1 MB)
        /// </summary>
        public const int MaxEntryLength = 64 * 1024;

        /// <summary>
        /// Control characters and the invisible characters which change how text is shown (e.g. a right-to-left override, which shows text
        /// in a different order than it was written) become <c>\uXXXX</c>, so the log shows what really was there.
        /// Line breaks and tabs stay. Returns the same string when there is nothing to escape.
        /// </summary>
        public static string EscapeUnsafeCharacters(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            StringBuilder result = null;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (IsUnsafe(c))
                {
                    result ??= new StringBuilder(text, 0, i, text.Length + 16);
                    result.Append("\\u").Append(((int) c).ToString("X4"));
                }
                else
                {
                    result?.Append(c);
                }
            }
            return result?.ToString() ?? text;
        }

        /// <summary>
        /// The text cut after <paramref name="maxLength"/> characters, with a note how much was left out
        /// </summary>
        public static string Limit(string text, int maxLength = MaxEntryLength)
        {
            if (text == null || text.Length <= maxLength)
            {
                return text;
            }
            int length = maxLength;
            // Don't cut a surrogate pair (e.g. an emoji) in half
            if (char.IsHighSurrogate(text[length - 1]))
            {
                length--;
            }
            return text.Substring(0, length) + $" ... ({text.Length - length:N0} more characters)";
        }

        /// <summary>
        /// Characters which are escaped: control characters except tab and line breaks, the line and paragraph separators,
        /// and the invisible formatting characters which change the order or the look of text
        /// </summary>
        internal static bool IsUnsafe(char c)
        {
            if (c == '\t' || c == '\r' || c == '\n')
            {
                return false;
            }
            if (c < 0x20 || (c >= 0x7F && c <= 0x9F))
            {
                return true;
            }
            switch ((int) c)
            {
                case 0x061C: // Arabic letter mark
                case 0x200B: // zero width space
                case 0x200C: // zero width non-joiner
                case 0x200D: // zero width joiner
                case 0x200E: // left-to-right mark
                case 0x200F: // right-to-left mark
                case 0x2028: // line separator
                case 0x2029: // paragraph separator
                case 0xFEFF: // zero width no-break space (byte order mark)
                    return true;
            }
            // Embeddings and overrides (U+202A to U+202E) and isolates (U+2066 to U+2069)
            return (c >= 0x202A && c <= 0x202E) || (c >= 0x2066 && c <= 0x2069);
        }
    }
}
