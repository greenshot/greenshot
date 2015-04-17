/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using GreenshotPlugin.Core;
using System.Diagnostics;
using System.Net;
using log4net;

namespace Greenshot.Help
{
	/// <summary>
	/// Description of HelpFileLoader.
	/// </summary>
	public sealed class HelpFileLoader
	{
		
		private static readonly ILog LOG = LogManager.GetLogger(typeof(HelpFileLoader));
		
		private const string EXT_HELP_URL = @"http://getgreenshot.org/help/";
		
		private HelpFileLoader() {
		}
		
		public static void LoadHelp() {
			string uri = FindOnlineHelpUrl(Language.CurrentLanguage);
			if(uri == null) {
				uri = Language.HelpFilePath;
			}
			Process.Start(uri);			
		}
		
		/// <returns>URL of help file in selected ietf, or (if not present) default ietf, or null (if not present, too. probably indicating that there is no internet connection)</returns>
		private static string FindOnlineHelpUrl(string currentIETF) {
			string ret = null;
			
			string extHelpUrlForCurrrentIETF = EXT_HELP_URL;
			
			if(!currentIETF.Equals("en-US")) {
				extHelpUrlForCurrrentIETF += currentIETF.ToLower() + "/";
			}
			
			HttpStatusCode? httpStatusCode = GetHttpStatus(extHelpUrlForCurrrentIETF);
			if(httpStatusCode == HttpStatusCode.OK) {
				ret = extHelpUrlForCurrrentIETF;
			} else if(httpStatusCode != null && !extHelpUrlForCurrrentIETF.Equals(EXT_HELP_URL)) {
				LOG.DebugFormat("Localized online help not found at {0}, will try {1} as fallback", extHelpUrlForCurrrentIETF, EXT_HELP_URL);
				httpStatusCode = GetHttpStatus(EXT_HELP_URL);
				if(httpStatusCode == HttpStatusCode.OK) {
					ret = EXT_HELP_URL;
				} else {
					LOG.WarnFormat("{0} returned status {1}", EXT_HELP_URL, httpStatusCode);
				}
			} else if(httpStatusCode == null){
				LOG.Info("Internet connection does not seem to be available, will load help from file system.");
			}
			
			return ret;
		}
		
		/// <summary>
		/// Retrieves HTTP status for a given url.
		/// </summary>
		/// <param name="url">URL for which the HTTP status is to be checked</param>
		/// <returns>An HTTP status code, or null if there is none (probably indicating that there is no internet connection available</returns>
		private static HttpStatusCode? GetHttpStatus(string url) {
			try {
				HttpWebRequest req = NetworkHelper.CreateWebRequest(url);
				HttpWebResponse res = (HttpWebResponse)req.GetResponse();
				return res.StatusCode;
			} catch(WebException e) {
				if(e.Response != null) return ((HttpWebResponse)e.Response).StatusCode;
				return null;
			}
		}
	}
}
