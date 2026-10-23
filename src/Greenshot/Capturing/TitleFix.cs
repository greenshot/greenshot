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

using System.Text.RegularExpressions;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using log4net;

namespace Greenshot.Capturing
{
    /// <summary>
    /// Rewrites the title of a capture with the title fixes of the settings, e.g. to remove the browser name
    /// </summary>
    internal static class TitleFix
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(TitleFix));
        private static readonly ICoreConfiguration Config = IniConfigRegistry.GetSection<ICoreConfiguration>();

        /// <summary>
        /// Apply the active title fixes to the title of the capture
        /// </summary>
        public static void Apply(ICaptureDetails captureDetails)
        {
            string title = captureDetails?.Title?.Trim();
            if (string.IsNullOrEmpty(title))
            {
                return;
            }
            foreach (string key in Config.ActiveTitleFixes)
            {
                if (!Config.TitleFixMatcher.TryGetValue(key, out var pattern) || string.IsNullOrEmpty(pattern))
                {
                    Log.WarnFormat("Title fix {0} has no pattern, it is skipped", key);
                    continue;
                }
                Config.TitleFixReplacer.TryGetValue(key, out var replacement);
                title = Regex.Replace(title, pattern, replacement ?? string.Empty);
            }
            captureDetails.Title = title;
        }
    }
}
