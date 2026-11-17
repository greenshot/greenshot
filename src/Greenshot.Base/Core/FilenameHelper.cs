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
using System.Buffers;
using System.Globalization;
using System.IO;
using System.Text;

namespace Greenshot.Base.Core
{
    public static class FilenameHelper
    {
        // File names and folders from templates: see Greenshot.Base.Expressions.OutputTemplates
        private const string UnsafeReplacement = "_";

        /// <summary>
        /// Remove invalid characters from the fully qualified filename
        /// </summary>
        /// <param name="fullPath">string with the full path to a file</param>
        /// <returns>string with the full path to a file, without invalid characters</returns>
        public static string MakeFqFilenameSafe(string fullPath)
        {
            string path = MakePathSafe(Path.GetDirectoryName(fullPath));
            string filename = MakeFilenameSafe(Path.GetFileName(fullPath));
            // Make the fullpath again and return
            return Path.Combine(path, filename);
        }

        /// <summary>
        /// A whole file name made safe, see <see cref="ToSafeNamePart"/>; also without dots and spaces at the end
        /// (Windows drops them) and with a _ before a reserved device name like CON or NUL
        /// </summary>
        /// <param name="filename">name of a file, without folder</param>
        /// <returns>the safe file name, null for null</returns>
        public static string MakeFilenameSafe(string filename)
        {
            if (filename == null)
            {
                return null;
            }

            string safe = ToSafeNamePart(filename).TrimEnd('.', ' ');
            return IsReservedDeviceName(safe) ? UnsafeReplacement + safe : safe;
        }

        /// <summary>
        /// One part of a file or folder name from text that comes from outside, like a window title or a web page, in one pass:
        /// <list type="bullet">
        /// <item>characters Windows doesn't allow in a name (control characters, line separators and &lt;&gt;:"/\|?*) become _</item>
        /// <item>invisible format characters are left out: direction marks and overrides (U+202E can make "fdp.exe" look like "exe.pdf"),
        /// zero width space, BOM, soft hyphen and the like (BUG-2164)</item>
        /// <item>the zero width (non-)joiner stays only between two characters which aren't ASCII, as emoji and some scripts
        /// (e.g. Persian) need it, so it can't hide inside a plain word</item>
        /// <item>tag characters stay only in a flag emoji (black flag, tags, cancel tag), else they could carry hidden text</item>
        /// <item>NFC normalized, no white space at the start or end, at most <paramref name="maxLength"/> characters, only dots become _</item>
        /// </list>
        /// Text that is already safe is returned as it is, without a new string.
        /// </summary>
        /// <param name="value">the text, may be null</param>
        /// <param name="maxLength">most characters to keep, a surrogate pair is never cut</param>
        /// <returns>the safe text, empty for null</returns>
        public static string ToSafeNamePart(string value, int maxLength = int.MaxValue)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            value = NormalizeToFormC(value);
            char[] rented = null;
            Span<char> buffer = value.Length <= StackBufferLength ? stackalloc char[value.Length] : (rented = ArrayPool<char>.Shared.Rent(value.Length));
            try
            {
                int length = 0;
                bool changed = false;
                int previous = -1;
                for (int i = 0; i < value.Length;)
                {
                    int width = char.IsSurrogatePair(value, i) ? 2 : 1;
                    int codePoint = width == 2 ? char.ConvertToUtf32(value[i], value[i + 1]) : value[i];
                    if (IsTag(codePoint))
                    {
                        // A run of tags: kept only as the black flag, tag letters and the cancel tag of e.g. the England flag
                        int end = i;
                        int last = codePoint;
                        while (end < value.Length && char.IsSurrogatePair(value, end))
                        {
                            int tag = char.ConvertToUtf32(value[end], value[end + 1]);
                            if (!IsTag(tag))
                            {
                                break;
                            }
                            last = tag;
                            end += 2;
                        }
                        bool flag = previous == BlackFlag && last == CancelTag && end - i >= 4;
                        if (flag)
                        {
                            value.AsSpan(i, end - i).CopyTo(buffer.Slice(length));
                            length += end - i;
                            previous = CancelTag;
                        }
                        else
                        {
                            changed = true;
                        }
                        i = end;
                        continue;
                    }

                    var category = CharUnicodeInfo.GetUnicodeCategory(value, i);
                    if (char.IsSurrogate(value[i]) && width == 1 || category == UnicodeCategory.Control || category == UnicodeCategory.LineSeparator
                        || category == UnicodeCategory.ParagraphSeparator || NotInNames.IndexOf(value[i]) >= 0)
                    {
                        buffer[length++] = UnsafeReplacementChar;
                        previous = UnsafeReplacementChar;
                        changed = true;
                    }
                    else if (category == UnicodeCategory.Format && !(IsJoiner(codePoint) && JoinsTwoNonAsciiCharacters(value, i, previous)))
                    {
                        changed = true;
                    }
                    else
                    {
                        value.AsSpan(i, width).CopyTo(buffer.Slice(length));
                        length += width;
                        previous = codePoint;
                    }
                    i += width;
                }

                ReadOnlySpan<char> safe = buffer.Slice(0, length);
                safe = safe.Trim();
                if (safe.Length > maxLength)
                {
                    int cut = char.IsHighSurrogate(safe[maxLength - 1]) ? maxLength - 1 : maxLength;
                    safe = safe.Slice(0, cut).TrimEnd();
                }
                // A joiner at the end after cutting joins nothing
                while (safe.Length > 0 && IsJoiner(safe[safe.Length - 1]))
                {
                    safe = safe.Slice(0, safe.Length - 1);
                }
                if (safe.Length > 0 && safe.Trim('.').IsEmpty)
                {
                    // Only dots would point to this or the parent folder
                    return new string(UnsafeReplacementChar, safe.Length);
                }

                return !changed && safe.Length == value.Length ? value : safe.ToString();
            }
            finally
            {
                if (rented != null)
                {
                    ArrayPool<char>.Shared.Return(rented);
                }
            }
        }

        /// <summary>
        /// Is the name, or its part before the first dot, a device name Windows reserves (CON, PRN, AUX, NUL, COM1-9, LPT1-9, also with ¹²³)
        /// </summary>
        public static bool IsReservedDeviceName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            int dot = name.IndexOf('.');
            var stem = (dot < 0 ? name.AsSpan() : name.AsSpan(0, dot)).TrimEnd(' ');
            if (stem.Length == 3)
            {
                return stem.Equals("CON".AsSpan(), StringComparison.OrdinalIgnoreCase) || stem.Equals("PRN".AsSpan(), StringComparison.OrdinalIgnoreCase)
                    || stem.Equals("AUX".AsSpan(), StringComparison.OrdinalIgnoreCase) || stem.Equals("NUL".AsSpan(), StringComparison.OrdinalIgnoreCase);
            }
            if (stem.Length == 4 && (stem.StartsWith("COM".AsSpan(), StringComparison.OrdinalIgnoreCase) || stem.StartsWith("LPT".AsSpan(), StringComparison.OrdinalIgnoreCase)))
            {
                char number = stem[3];
                return number >= '1' && number <= '9' || number == '\u00B9' || number == '\u00B2' || number == '\u00B3';
            }
            return stem.Equals("CONIN$".AsSpan(), StringComparison.OrdinalIgnoreCase) || stem.Equals("CONOUT$".AsSpan(), StringComparison.OrdinalIgnoreCase);
        }

        private const char UnsafeReplacementChar = '_';
        private const int StackBufferLength = 256;
        private const string NotInNames = "<>:\"/\\|?*";
        private const int ZeroWidthNonJoiner = 0x200C;
        private const int ZeroWidthJoiner = 0x200D;
        private const int BlackFlag = 0x1F3F4;
        private const int CancelTag = 0xE007F;

        private static bool IsJoiner(int codePoint) => codePoint == ZeroWidthJoiner || codePoint == ZeroWidthNonJoiner;

        private static bool IsTag(int codePoint) => codePoint >= 0xE0000 && codePoint <= CancelTag;

        /// <summary>
        /// The joiner at index stands between the last kept character and the next one, and both aren't ASCII nor invisible
        /// </summary>
        private static bool JoinsTwoNonAsciiCharacters(string value, int index, int previous)
        {
            if (previous < 0x80 || IsJoiner(previous) || index + 1 >= value.Length || value[index + 1] < 0x80)
            {
                return false;
            }
            var next = CharUnicodeInfo.GetUnicodeCategory(value, index + 1);
            return next != UnicodeCategory.Format && next != UnicodeCategory.Control && next != UnicodeCategory.Surrogate
                && next != UnicodeCategory.LineSeparator && next != UnicodeCategory.ParagraphSeparator && next != UnicodeCategory.SpaceSeparator;
        }

        private static string NormalizeToFormC(string value)
        {
            try
            {
                return value.IsNormalized(NormalizationForm.FormC) ? value : value.Normalize(NormalizationForm.FormC);
            }
            catch (ArgumentException)
            {
                // A lone surrogate can't be normalized, it becomes _ anyway
                return value;
            }
        }

        /// <summary>
        /// Remove invalid characters from the path
        /// </summary>
        /// <param name="path">string with the full path to a file</param>
        /// <returns>string with the full path to a file, without invalid characters</returns>
        public static string MakePathSafe(string path)
        {
            // Make the path save!
            if (path != null)
            {
                foreach (char disallowed in Path.GetInvalidPathChars())
                {
                    path = path.Replace(disallowed.ToString(), UnsafeReplacement);
                }
            }

            return path;
        }

        /// <summary>
        /// Checks whether a directory name is valid in the current file system
        /// </summary>
        /// <param name="directoryName">directory name (not path!)</param>
        /// <returns>true if directory name is valid</returns>
        public static bool IsDirectoryNameValid(string directoryName)
        {
            var forbiddenChars = Path.GetInvalidPathChars();
            foreach (var forbiddenChar in forbiddenChars)
            {
                if (directoryName == null || directoryName.Contains(forbiddenChar.ToString()))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks whether a filename is valid in the current file system
        /// </summary>
        /// <param name="filename">name of the file</param>
        /// <returns>true if filename is valid</returns>
        public static bool IsFilenameValid(string filename)
        {
            var forbiddenChars = Path.GetInvalidFileNameChars();
            foreach (var forbiddenChar in forbiddenChars)
            {
                if (filename == null || filename.Contains(forbiddenChar.ToString()))
                {
                    return false;
                }
            }

            return true;
        }
    }
}