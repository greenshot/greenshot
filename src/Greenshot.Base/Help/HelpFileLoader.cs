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

using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using log4net;

namespace Greenshot.Base.Help
{
    /// <summary>
    /// Opens the online help (localized when available) or the local help file.
    /// </summary>
    public static class HelpFileLoader
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(HelpFileLoader));

        private const string ExtHelpUrl = @"https://getgreenshot.org/help/";

        /// <summary>
        /// Open the help, checks (without blocking the UI) if the online help is reachable.
        /// </summary>
        public static async Task LoadHelpAsync()
        {
            string uri = await FindOnlineHelpUrlAsync(Language.CurrentLanguage, CancellationToken.None).ConfigureAwait(false) ?? Language.HelpFilePath;
            using (Process.Start(uri))
            {
                // Only started
            }
        }

        private static async Task<string> FindOnlineHelpUrlAsync(string currentIETF, CancellationToken cancellationToken)
        {
            string ret = null;

            string extHelpUrlForCurrrentIETF = ExtHelpUrl;

            if (!currentIETF.Equals("en-US"))
            {
                extHelpUrlForCurrrentIETF += currentIETF.ToLower() + "/";
            }

            HttpStatusCode? httpStatusCode = await GetHttpStatusAsync(extHelpUrlForCurrrentIETF, cancellationToken).ConfigureAwait(false);
            if (httpStatusCode == HttpStatusCode.OK)
            {
                ret = extHelpUrlForCurrrentIETF;
            }
            else if (httpStatusCode != null && !extHelpUrlForCurrrentIETF.Equals(ExtHelpUrl))
            {
                Log.DebugFormat("Localized online help not found at {0}, will try {1} as fallback", extHelpUrlForCurrrentIETF, ExtHelpUrl);
                httpStatusCode = await GetHttpStatusAsync(ExtHelpUrl, cancellationToken).ConfigureAwait(false);
                if (httpStatusCode == HttpStatusCode.OK)
                {
                    ret = ExtHelpUrl;
                }
                else
                {
                    Log.WarnFormat("{0} returned status {1}", ExtHelpUrl, httpStatusCode);
                }
            }
            else if (httpStatusCode == null)
            {
                Log.Info("Internet connection does not seem to be available, will load help from file system.");
            }

            return ret;
        }

        private static async Task<HttpStatusCode?> GetHttpStatusAsync(string url, CancellationToken cancellationToken)
        {
            try
            {
                using var response = await NetworkHelper.HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                return response.StatusCode;
            }
            catch (HttpRequestException)
            {
                return null;
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout
                return null;
            }
        }
    }
}
