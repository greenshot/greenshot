/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2010  Thomas Braun, Jens Klingen, Robin Krom
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
using System;
using System.Drawing;
using System.Net;

using Greenshot.Core;

namespace GreenshotCore.Helpers {
	/// <summary>
	/// Description of NetworkHelper.
	/// </summary>
	public class NetworkHelper {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(NetworkHelper));
		private static CoreConfiguration config = IniConfig.GetIniSection<CoreConfiguration>();

		/// <summary>
		/// Download the FavIcon as a Bitmap
		/// </summary>
		/// <param name="baseUri"></param>
		/// <returns>Bitmap with the FavIcon</returns>
		public static Bitmap DownloadFavIcon(Uri baseUri) {
			Uri url = new Uri(baseUri, new Uri("favicon.ico"));
			try {
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
				request.Proxy = NetworkHelper.CreateProxy(url);
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				if (request.HaveResponse) {
					Image image = Image.FromStream(response.GetResponseStream());
					return (image.Height > 16 && image.Width > 16) ? new Bitmap(image, 16, 16) : new Bitmap(image);
				}
				
			} catch (Exception e) {
				LOG.Error("Problem downloading the FavIcon from: " + baseUri.ToString(), e);
			}
			return null;
		}

		/// <summary>
		/// Create a IWebProxy Object which can be used to access the Internet
		/// This method will check the configuration if the proxy is allowed to be used.
		/// Usages can be found in the DownloadFavIcon or Jira and Confluence plugins
		/// </summary>
		/// <param name="url"></param>
		/// <returns>IWebProxy filled with all the proxy details or null if none is set/wanted</returns>
		public static IWebProxy CreateProxy(Uri uri) {
			IWebProxy proxyToUse = null;
			if (config.UseProxy) {
				proxyToUse = WebRequest.DefaultWebProxy;
				if (proxyToUse != null) {
					proxyToUse.Credentials = CredentialCache.DefaultCredentials;
					if (LOG.IsDebugEnabled) {
						// check the proxy for the Uri
						if (!proxyToUse.IsBypassed(uri)) {
							Uri proxyUri = proxyToUse.GetProxy(uri);
							if (proxyUri != null) {
								LOG.Debug("Using proxy: " + proxyUri.ToString() + " for " + uri.ToString());
							} else {
								LOG.Debug("No proxy found!");
							}
						} else {
							LOG.Debug("Proxy bypass for: " + uri.ToString());
						}
					}
				} else {
					LOG.Debug("No proxy found!");
				}
			}
			return proxyToUse;
		}
	}
}
