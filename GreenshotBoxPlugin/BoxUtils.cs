/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Xml;
using Greenshot.IniFile;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;

namespace GreenshotBoxPlugin {
	/// <summary>
	/// Description of ImgurUtils.
	/// </summary>
	public class BoxUtils {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(BoxUtils));
		private static BoxConfiguration config = IniConfig.GetIniSection<BoxConfiguration>();

		private BoxUtils() {
		}
		
		private static string ParseTicket(string response) {
			try {
				XmlDocument doc = new XmlDocument();
				doc.LoadXml(response);
				XmlNodeList nodes = doc.GetElementsByTagName("ticket");
				if(nodes.Count > 0) {
					return nodes.Item(0).InnerText;
				}
			} catch (Exception ex) {
				LOG.Error("Error parsing Box Response.", ex);
			}
			return null;
		}		

		private static bool Authorize() {
			string ticketUrl = string.Format("https://www.box.com/api/1.0/rest?action=get_ticket&api_key={0}", BoxCredentials.API_KEY);
			string ticketXML = NetworkHelper.GetAsString(new Uri(ticketUrl));
			string ticket = ParseTicket(ticketXML);
			string authorizeUrl = string.Format("https://www.box.com/api/1.0/auth/{0}", ticket);
			OAuthLoginForm loginForm = new OAuthLoginForm("Box Authorize", new Size(1060,600), authorizeUrl, "http://getgreenshot.org");
			loginForm.ShowDialog();
			if (loginForm.isOk) {
				if (loginForm.CallbackParameters != null && loginForm.CallbackParameters.ContainsKey("auth_token")) {
					config.BoxToken = loginForm.CallbackParameters["auth_token"];
					IniConfig.Save();
					return true;
				}
			}
			return false;
		}
	

		/// <summary>
		/// Upload file by post
		/// </summary>
		/// <param name="url"></param>
		/// <param name="parameters"></param>
		/// <returns>response</returns>
		public static string HttpUploadFile(string url, Dictionary<string, object> parameters) {
			HttpWebRequest webRequest = (HttpWebRequest)NetworkHelper.CreateWebRequest(url);
			webRequest.Method = "POST";
			webRequest.KeepAlive = true;
			webRequest.Credentials = System.Net.CredentialCache.DefaultCredentials;
			NetworkHelper.WriteMultipartFormData(webRequest, parameters);
			
			return NetworkHelper.GetResponse(webRequest);
		}

		/// <summary>
		/// Do the actual upload to Box
		/// For more details on the available parameters, see: http://developers.box.net/w/page/12923951/ApiFunction_Upload%20and%20Download
		/// </summary>
		/// <param name="imageData">byte[] with image data</param>
		/// <returns>url to uploaded image</returns>
		public static string UploadToBox(SurfaceContainer image, string title, string filename) {
			string folderId = "0";
			if (string.IsNullOrEmpty(config.BoxToken)) {
				if (!Authorize()) {
					return null;
				}
			}

			string strUrl = string.Format("https://upload.box.net/api/1.0/upload/{0}/{1}?file_name={2}&new_copy=1", config.BoxToken, folderId, filename);

			Dictionary<string, object> parameters = new Dictionary<string, object>();
			parameters.Add("share", "1");
			parameters.Add("new_file", image);
			
			string response = HttpUploadFile(strUrl, parameters);
			// Check if the token is wrong
			if ("wrong auth token".Equals(response)) {
				config.BoxToken = null;
				IniConfig.Save();
				return UploadToBox(image, title, filename);
			}
			LOG.DebugFormat("Box response: {0}", response);
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(response);
			XmlNodeList nodes = doc.GetElementsByTagName("status");
			if(nodes.Count > 0) {
				if ("upload_ok".Equals(nodes.Item(0).InnerText)) {
					nodes = doc.GetElementsByTagName("file");
					if (nodes.Count > 0) {
						long id = long.Parse(nodes.Item(0).Attributes["id"].Value);
						return string.Format("http://www.box.com/files/0/f/0/1/f_{0}", id);
					}
				}
			}
			return null;
		}
	}
}
