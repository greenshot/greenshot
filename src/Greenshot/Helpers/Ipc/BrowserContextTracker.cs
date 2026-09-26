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
using System.IO;
using System.Text.RegularExpressions;
using log4net;

namespace Greenshot.Helpers.Ipc
{
    /// <summary>
    /// Tracks active browser tab metadata reported by the browser extension.
    /// Can be utilized by QA workflows to dynamically route captures into ticket-specific folders.
    /// </summary>
    public class BrowserContextTracker
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(BrowserContextTracker));
        private static readonly Lazy<BrowserContextTracker> LazyInstance = new Lazy<BrowserContextTracker>(() => new BrowserContextTracker());

        private static readonly Regex TicketPattern = new Regex(@"\b([A-Z]{2,10}-[0-9]{1,6})\b", RegexOptions.Compiled);
        private readonly object _syncLock = new object();

        public static BrowserContextTracker Instance => LazyInstance.Value;

        public string CurrentUrl { get; private set; } = string.Empty;
        public string CurrentTitle { get; private set; } = string.Empty;
        public string CurrentDomain { get; private set; } = string.Empty;
        public string CurrentTicket { get; private set; } = string.Empty;

        public void UpdateContext(string url, string title)
        {
            lock (_syncLock)
            {
                CurrentUrl = Sanitize(url, 2048);
                CurrentTitle = Sanitize(title, 512);
                CurrentDomain = ExtractDomain(CurrentUrl);
                CurrentTicket = ExtractTicket(CurrentUrl, CurrentTitle);

                Log.Debug($"Browser context updated: Domain='{CurrentDomain}', Ticket='{CurrentTicket}', Title='{CurrentTitle}, Url='{CurrentUrl}'");
            }
        }

        private static string Sanitize(string input, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            // Strip control characters
            char[] buffer = new char[Math.Min(input.Length, maxLength)];
            int idx = 0;

            for (int i = 0; i < input.Length && idx < maxLength; ++i)
            {
                char c = input[i];
                if (!char.IsControl(c))
                {
                    buffer[idx++] = c;
                }
            }

            return new string(buffer, 0, idx);
        }

        private static string ExtractDomain(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return string.Empty;
            }

            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return uri.Host;
            }

            return string.Empty;
        }

        private static string ExtractTicket(string url, string title)
        {
            // First check title for ticket key (e.g. "PROJ-1234: Bug description")
            if (!string.IsNullOrEmpty(title))
            {
                var match = TicketPattern.Match(title);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            // Next check URL path (e.g. "/browse/PROJ-1234")
            if (!string.IsNullOrEmpty(url))
            {
                var match = TicketPattern.Match(url);
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Returns a sanitized path segment based on the active ticket or domain, safe for folder names.
        /// </summary>
        public string GetSafeDestinationSubfolder()
        {
            lock (_syncLock)
            {
                string candidate = !string.IsNullOrEmpty(CurrentTicket) ? CurrentTicket : CurrentDomain;
                if (string.IsNullOrEmpty(candidate))
                {
                    return string.Empty;
                }

                foreach (char c in Path.GetInvalidFileNameChars())
                {
                    candidate = candidate.Replace(c, '_');
                }

                return candidate;
            }
        }
    }
}
