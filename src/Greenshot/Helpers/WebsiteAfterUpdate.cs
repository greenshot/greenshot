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
using System.Diagnostics;
using System.Security.Principal;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Threading;
using log4net;

namespace Greenshot.Helpers
{
    /// <summary>
    /// Opens the thank-you page of the website once per user for every new version, after an install or update.
    /// This was done by the installer, which opened it as the admin user when installed elevated (rollout tools)
    /// and before its last page. Greenshot runs as the user who works with it, in their session and their browser,
    /// whatever installed it (installer, silent rollout, portable zip).
    /// </summary>
    internal static class WebsiteAfterUpdate
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WebsiteAfterUpdate));
        private const string ThankYouPage = "https://getgreenshot.org/thank-you/";

        /// <summary>
        /// Wait after the start, so a capture started with Greenshot isn't disturbed by the browser taking the focus
        /// </summary>
        private static readonly TimeSpan Delay = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Open the thank-you page in the background when this version didn't show it yet
        /// </summary>
        public static void ShowIfNewVersion()
        {
            ShowIfNewVersionAsync().FireAndLog("Open the website after an install or update", Log);
        }

        private static async Task ShowIfNewVersionAsync()
        {
            var config = IniConfigRegistry.GetSection<ICoreConfiguration>();
            string version = EnvironmentInfo.GetGreenshotVersion(true);
            string shownForVersion = config.WebsiteShownForVersion;
            if (!ShouldShow(shownForVersion, version) || !IsInteractiveUser())
            {
                return;
            }

            await Task.Delay(Delay).ConfigureAwait(false);

            var uri = BuildUri(version, config.Language, EditionInfo.IsFull ? null : EditionInfo.Name);
            Log.InfoFormat("Opening {0} for version {1} (shown before for {2})", uri, version, shownForVersion ?? "none");
            // Remember it first: a browser which fails to start shouldn't make this happen on every start
            try
            {
                config.WebsiteShownForVersion = version;
            }
            catch (Exception ex)
            {
                // Pinned in greenshot-fixed.ini, Dapplo.Ini refuses the change: the page opens anyway
                Log.Warn("Couldn't remember the version the website was shown for", ex);
            }
            using (Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }))
            {
            }
        }

        /// <summary>
        /// True when the page wasn't shown for this version yet: a new install, an update from a version
        /// before this existed, an update or a downgrade. Any other remembered version than the running one counts,
        /// so a version pinned in the future (e.g. 99.0 in greenshot-defaults.ini) can't suppress it.
        /// </summary>
        internal static bool ShouldShow(string shownForVersion, string currentVersion)
        {
            if (!Version.TryParse(currentVersion, out var current))
            {
                return false;
            }

            return !Version.TryParse(shownForVersion, out var shown) || current != shown;
        }

        /// <summary>
        /// The thank-you page with the version, the language (as the installer named it) and, for any other edition than Full, the edition
        /// </summary>
        internal static Uri BuildUri(string version, string ietf, string edition)
        {
            var query = $"?language={Uri.EscapeDataString(WebsiteLanguage(ietf))}&version={Uri.EscapeDataString(version)}";
            if (!string.IsNullOrEmpty(edition))
            {
                query += $"&edition={Uri.EscapeDataString(edition.ToLowerInvariant())}";
            }

            return new Uri(ThankYouPage + query);
        }

        /// <summary>
        /// The language as the installer passed it to the website: the language without the region, except "ptBR" and "cn"
        /// </summary>
        internal static string WebsiteLanguage(string ietf)
        {
            if (string.IsNullOrWhiteSpace(ietf))
            {
                return "en";
            }

            if (ietf.Equals("pt-BR", StringComparison.OrdinalIgnoreCase))
            {
                return "ptBR";
            }

            if (ietf.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            {
                return "cn";
            }

            int dash = ietf.IndexOf('-');
            return (dash > 0 ? ietf.Substring(0, dash) : ietf).ToLowerInvariant();
        }

        /// <summary>
        /// Not for the system account (no desktop to show it on) and not while debugging Greenshot
        /// </summary>
        private static bool IsInteractiveUser()
        {
            if (Debugger.IsAttached || !Environment.UserInteractive)
            {
                return false;
            }

            using var identity = WindowsIdentity.GetCurrent();
            return !identity.IsSystem;
        }
    }
}
