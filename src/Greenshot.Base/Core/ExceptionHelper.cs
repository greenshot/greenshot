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
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using log4net;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Utility class for parsing exceptions, normalizing stack traces across different Windows UI languages,
    /// generating stable stack trace hashes to detect duplicate issues, and opening issue tracker URLs.
    /// </summary>
    public static class ExceptionHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ExceptionHelper));

        public const string StackTraceHashMarker = "[stack-trace-hash:";
        public const string GitHubRepoOwner = "greenshot";
        public const string GitHubRepoName = "greenshot";

        // Regex to extract exception type from "Exception: <Full.Type.Name>: message"
        private static readonly Regex ExceptionTypeRegex = new(@"^Exception:\s+([^\s:]+)", RegexOptions.Compiled);

        // Regex to match and normalize stack frame method calls across Windows locales (en: "at", de: "bei", fr: "à", es: "en", zh: "在", etc.)
        // Matches any leading localized word (or none), followed by method signature starting with Greenshot. or Dapplo. up to closing parenthesis
        private static readonly Regex StackFrameRegex = new(@"^(?:[^\s(]+\s+)?((?:Greenshot|Dapplo)\.[^(]+(?:\([^)]*\))?)", RegexOptions.Compiled);

        /// <summary>
        /// Normalizes raw stack trace or EnvironmentInfo report text into a stable, locale-independent representation.
        /// Extracts the root exception type name and Greenshot/Dapplo stack frames without file paths or line numbers.
        /// </summary>
        /// <param name="rawReportOrStackTrace">Raw exception report or stack trace text</param>
        /// <returns>Normalized stack trace text</returns>
        public static string NormalizeStackTrace(string rawReportOrStackTrace)
        {
            if (string.IsNullOrWhiteSpace(rawReportOrStackTrace))
            {
                return string.Empty;
            }

            var lines = rawReportOrStackTrace.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var normalizedLines = new List<string>();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }

                if (trimmed.StartsWith("Exception: ", StringComparison.OrdinalIgnoreCase))
                {
                    var match = ExceptionTypeRegex.Match(trimmed);
                    if (match.Success)
                    {
                        normalizedLines.Add(match.Groups[1].Value);
                    }
                    continue;
                }

                var frameMatch = StackFrameRegex.Match(trimmed);
                if (frameMatch.Success)
                {
                    normalizedLines.Add("at " + frameMatch.Groups[1].Value);
                }
            }

            return string.Join("\n", normalizedLines).Trim();
        }

        /// <summary>
        /// Normalizes an Exception instance into a stable, locale-independent representation.
        /// </summary>
        /// <param name="ex">The Exception to normalize</param>
        /// <returns>Normalized stack trace text</returns>
        public static string NormalizeException(Exception ex)
        {
            if (ex == null)
            {
                return string.Empty;
            }

            // Use EnvironmentInfo's ExceptionToString to format inner exceptions and details, then normalize
            string raw = EnvironmentInfo.ExceptionToString(ex);
            return NormalizeStackTrace(raw);
        }

        /// <summary>
        /// Computes a 12-character lowercase hexadecimal hash for normalized stack trace text.
        /// Returns empty string if no valid stack frames / exception types were present.
        /// </summary>
        /// <param name="normalizedText">Normalized stack trace text from NormalizeStackTrace</param>
        /// <returns>12-character hexadecimal hash</returns>
        public static string ComputeHash(string normalizedText)
        {
            if (string.IsNullOrWhiteSpace(normalizedText))
            {
                return string.Empty;
            }

            byte[] bytes = Encoding.UTF8.GetBytes(normalizedText.Trim());
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(bytes);
                var sb = new StringBuilder(12);
                for (int i = 0; i < 6; i++) // 6 bytes = 12 hex characters
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Generates a GitHub search URL to check for existing issues with the specified stack trace hash.
        /// </summary>
        /// <param name="hash">12-char stack trace hash</param>
        /// <returns>Search URL</returns>
        public static string GetGitHubSearchUrl(string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
            {
                return $"https://github.com/{GitHubRepoOwner}/{GitHubRepoName}/issues";
            }

            // Search GitHub issues containing the hash marker or hash
            string query = $"is:issue \"{StackTraceHashMarker} {hash}]\"";
            string encodedQuery = Uri.EscapeDataString(query);
            return $"https://github.com/{GitHubRepoOwner}/{GitHubRepoName}/issues?q={encodedQuery}";
        }

        /// <summary>
        /// Generates a GitHub URL to open a new bug report with the stack trace hash and exception pre-filled.
        /// </summary>
        /// <param name="hash">12-char stack trace hash</param>
        /// <param name="exceptionType">Optional exception type name</param>
        /// <returns>New issue URL</returns>
        public static string GetNewIssueUrl(string hash, string exceptionType = null)
        {
            var title = !string.IsNullOrWhiteSpace(exceptionType)
                ? $"[{hash}] {exceptionType}"
                : $"[{hash}] Exception report";

            string encodedTitle = Uri.EscapeDataString(title);
            return $"https://github.com/{GitHubRepoOwner}/{GitHubRepoName}/issues/new?template=bug_report.yml&title={encodedTitle}";
        }

        /// <summary>
        /// Opens a URL using Process.Start with UseShellExecute set to true, compatible with .NET 4.8 and modern .NET runtimes.
        /// </summary>
        /// <param name="url">The URL to launch</param>
        /// <returns>True if successfully launched, false otherwise</returns>
        public static bool OpenUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to open URL: {url}", ex);
                return false;
            }
        }
    }
}
