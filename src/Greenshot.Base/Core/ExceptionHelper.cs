/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Utility class for exception handling and analysis
    /// </summary>
    public static class ExceptionHelper
    {
        /// <summary>
        /// Generates a stable hash from an exception's stacktrace, excluding framework code and line numbers.
        /// </summary>
        /// <param name="ex">The exception to hash</param>
        /// <returns>A SHA256 hash string</returns>
        public static string GetStacktraceHash(Exception ex)
        {
            if (ex == null)
            {
                return null;
            }

            string normalizedStacktrace = GetNormalizedStacktrace(ex);
            if (string.IsNullOrEmpty(normalizedStacktrace))
            {
                // Fallback to message if stacktrace is empty
                normalizedStacktrace = ex.GetType().FullName + ":" + ex.Message;
            }

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(normalizedStacktrace);
                byte[] hash = sha256.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant().Substring(0, 12);
            }
        }

        /// <summary>
        /// Normalizes a stacktrace by keeping only relevant (non-framework) lines and removing line numbers/file paths.
        /// </summary>
        /// <param name="ex">The exception</param>
        /// <returns>A normalized stacktrace string</returns>
        public static string GetNormalizedStacktrace(Exception ex)
        {
            if (ex == null)
            {
                return string.Empty;
            }

            StringBuilder sb = new StringBuilder();
            Exception currentEx = ex;
            while (currentEx != null)
            {
                sb.Append(currentEx.GetType().FullName).Append('\n');
                if (!string.IsNullOrEmpty(currentEx.StackTrace))
                {
                    // Split by any newline sequence to be robust
                    string[] lines = currentEx.StackTrace.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in lines)
                    {
                        string trimmedLine = line.Trim();
                        // Keep only Greenshot or Dapplo (related to Greenshot) lines
                        if (trimmedLine.StartsWith("at Greenshot.") || trimmedLine.StartsWith("at Dapplo."))
                        {
                            // Remove line numbers and file paths to make it more stable across builds
                            // Matches: " in C:\path\to\file.cs:line 123"
                            string normalizedLine = Regex.Replace(trimmedLine, @" in .*?:line \d+", string.Empty);
                            sb.Append(normalizedLine).Append('\n');
                        }
                    }
                }
                currentEx = currentEx.InnerException;
            }
            return sb.ToString().Trim();
        }
    }
}
