// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Diagnostics;
using System.Net;
using Dapplo.HttpExtensions;
using Dapplo.Log;

namespace Greenshot.Help
{
	/// <summary>
	///     Description of HelpFileLoader.
	/// </summary>
	public static class HelpFileLoader
	{
		private const string ExtHelpUrl = @"http://getgreenshot.org/help/";
		private static readonly LogSource Log = new LogSource();

		public static void LoadHelp()
		{
            var uri = FindOnlineHelpUrl("en-US");// Language.CurrentLanguage);// ?? Language.HelpFilePath;
            var processStartInfo = new ProcessStartInfo(uri)
            {
                CreateNoWindow = true,
                UseShellExecute = true
            };
            Process.Start(processStartInfo);
		}

		/// <returns>
		///     URL of help file in selected ietf, or (if not present) default ietf, or null (if not present, too. probably
		///     indicating that there is no internet connection)
		/// </returns>
		private static string FindOnlineHelpUrl(string currentIetf)
		{
			string ret = null;

			var extHelpUrlForCurrrentIetf = ExtHelpUrl;

			if (!currentIetf.Equals("en-US"))
			{
				extHelpUrlForCurrrentIetf += currentIetf.ToLower() + "/";
			}

			var httpStatusCode = GetHttpStatus(extHelpUrlForCurrrentIetf);
			if (httpStatusCode == HttpStatusCode.OK)
			{
				ret = extHelpUrlForCurrrentIetf;
			}
			else if (httpStatusCode != null && !extHelpUrlForCurrrentIetf.Equals(ExtHelpUrl))
			{
				Log.Debug().WriteLine("Localized online help not found at {0}, will try {1} as fallback", extHelpUrlForCurrrentIetf, ExtHelpUrl);
				httpStatusCode = GetHttpStatus(ExtHelpUrl);
				if (httpStatusCode == HttpStatusCode.OK)
				{
					ret = ExtHelpUrl;
				}
				else
				{
					Log.Warn().WriteLine("{0} returned status {1}", ExtHelpUrl, httpStatusCode);
				}
			}
			else if (httpStatusCode == null)
			{
				Log.Info().WriteLine("Internet connection does not seem to be available, will load help from file system.");
			}

			return ret;
		}

		/// <summary>
		///     Retrieves HTTP status for a given url.
		/// </summary>
		/// <param name="url">URL for which the HTTP status is to be checked</param>
		/// <returns>
		///     An HTTP status code, or null if there is none (probably indicating that there is no internet connection
		///     available
		/// </returns>
		private static HttpStatusCode? GetHttpStatus(string url)
		{
			try
			{
			    var uri = new Uri(url);
			    var head = uri.HeadAsync().Result;
			    return HttpStatusCode.OK;
			}
			catch (Exception)
			{
			    return HttpStatusCode.NotFound;
			}
		}
	}
}