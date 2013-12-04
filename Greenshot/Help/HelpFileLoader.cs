/*
 * Created by SharpDevelop.
 * User: jens
 * Date: 09.04.2012
 * Time: 19:24
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using Greenshot.Configuration;
using GreenshotPlugin.Core;
using System;
using System.Diagnostics;
using System.Net;
using System.Windows.Forms;

namespace Greenshot.Help
{
	/// <summary>
	/// Description of HelpFileLoader.
	/// </summary>
	public sealed class HelpFileLoader
	{
		
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(HelpFileLoader));
		
		private const string EXT_HELP_URL = @"http://getgreenshot.org/help/";
		
		private HelpFileLoader() {
		}
		
		public static void LoadHelp() {
			string uri = findOnlineHelpUrl(Language.CurrentLanguage);
			if(uri == null) {
				uri = Language.HelpFilePath;
			}
			Process.Start(uri);			
		}
		
		/// <returns>URL of help file in selected ietf, or (if not present) default ietf, or null (if not present, too. probably indicating that there is no internet connection)</returns>
		private static string findOnlineHelpUrl(string currentIETF) {
			string ret = null;
			
			string extHelpUrlForCurrrentIETF = EXT_HELP_URL;
			
			if(!currentIETF.Equals("en-US")) {
				extHelpUrlForCurrrentIETF += currentIETF.ToLower() + "/";
			}
			
			HttpStatusCode? httpStatusCode = getHttpStatus(extHelpUrlForCurrrentIETF);
			if(httpStatusCode == HttpStatusCode.OK) {
				ret = extHelpUrlForCurrrentIETF;
			} else if(httpStatusCode != null && !extHelpUrlForCurrrentIETF.Equals(EXT_HELP_URL)) {
				LOG.DebugFormat("Localized online help not found at {0}, will try {1} as fallback", extHelpUrlForCurrrentIETF, EXT_HELP_URL);
				httpStatusCode = getHttpStatus(EXT_HELP_URL);
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
		private static HttpStatusCode? getHttpStatus(string url) {
			try {
				HttpWebRequest req = (HttpWebRequest)NetworkHelper.CreateWebRequest(url);
				HttpWebResponse res = (HttpWebResponse)req.GetResponse();
				return res.StatusCode;
			} catch(WebException e) {
				if(e.Response != null) return ((HttpWebResponse)e.Response).StatusCode;
				else return null;
			}
		}
	}
}
