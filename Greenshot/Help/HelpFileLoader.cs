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
		
		private HelpFileLoader()
		{
		}
		
		public static void LoadHelp() {
			string uri = findOnlineHelpUrl(Language.GetInstance().CurrentLanguage);
			if(uri == null) {
				uri = Language.GetInstance().GetHelpFilePath();
			}
			Process.Start(uri);			
		}
		
		/// <returns>URL of help file in selected ietf, or (if not present) default ietf, or null (if not present, too. probably indicating that there is no internet connection)</returns>
		private static string findOnlineHelpUrl(string currentIETF) {
			string ret = null;
			
			string extHelpUrlForCurrrentIETF = EXT_HELP_URL;
			
			if(!currentIETF.Equals("en_US")) {
				extHelpUrlForCurrrentIETF += currentIETF.ToLower() + "/";
			}
			
			HttpStatusCode? s = getHttpStatus(extHelpUrlForCurrrentIETF);
			if(s == HttpStatusCode.OK) {
				ret = extHelpUrlForCurrrentIETF;
			} else if(s != null && !extHelpUrlForCurrrentIETF.Equals(EXT_HELP_URL)) {
				LOG.Debug(String.Format("Localized online help not found at %s, will try %s as fallback", extHelpUrlForCurrrentIETF, EXT_HELP_URL));
				s = getHttpStatus(EXT_HELP_URL);
				if(s == HttpStatusCode.OK) {
					ret = EXT_HELP_URL;
				} else {
					LOG.Warn(String.Format("%s returned status %s", EXT_HELP_URL, s));
				}
			} else if(s == null){
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
				HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(url);
				HttpWebResponse res = (HttpWebResponse)req.GetResponse();
				return res.StatusCode;
			} catch(WebException e) {
				if(e.Response != null) return ((HttpWebResponse)e.Response).StatusCode;
				else return null;
			}
		}
	}
}
