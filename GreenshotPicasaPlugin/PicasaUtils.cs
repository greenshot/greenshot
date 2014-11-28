/*
 * A Picasa Plugin for Greenshot
 * Copyright (C) 2011  Francis Noel
 * 
 * For more information see: http://getgreenshot.org/
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
using System.Xml;
using Greenshot.IniFile;
using Greenshot.Plugin;
using GreenshotPlugin.Core;

namespace GreenshotPicasaPlugin {
	/// <summary>
	/// Description of PicasaUtils.
	/// </summary>
	public class PicasaUtils {
		private const string GoogleAccountUri = "https://www.google.com/accounts/";
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(PicasaUtils));
		private static readonly PicasaConfiguration Config = IniConfig.GetIniSection<PicasaConfiguration>();

		private PicasaUtils() {
		}

		/// <summary>
		/// Do the actual upload to Picasa
		/// </summary>
		/// <param name="surfaceToUpload">Image to upload</param>
		/// <param name="outputSettings"></param>
		/// <param name="title"></param>
		/// <param name="filename"></param>
		/// <returns>PicasaResponse</returns>
		public static string UploadToPicasa(ISurface surfaceToUpload, SurfaceOutputSettings outputSettings, string title, string filename) {
			OAuthSession oAuth = new OAuthSession(PicasaCredentials.ConsumerKey, PicasaCredentials.ConsumerSecret);
			oAuth.BrowserSize = new Size(1020, 590);
			oAuth.AccessTokenUrl =  GoogleAccountUri + "OAuthGetAccessToken";
			oAuth.AuthorizeUrl =	GoogleAccountUri + "OAuthAuthorizeToken";
			oAuth.RequestTokenUrl = GoogleAccountUri + "OAuthGetRequestToken";
			oAuth.LoginTitle = "Picasa authorization";
			oAuth.Token = Config.PicasaToken;
			oAuth.TokenSecret = Config.PicasaTokenSecret;
			oAuth.RequestTokenParameters.Add("scope", "https://picasaweb.google.com/data/");
			oAuth.RequestTokenParameters.Add("xoauth_displayname", "Greenshot");
			if (string.IsNullOrEmpty(oAuth.Token)) {
				if (!oAuth.Authorize()) {
					return null;
				}
				if (!string.IsNullOrEmpty(oAuth.Token)) {
					Config.PicasaToken = oAuth.Token;
				}
				if (!string.IsNullOrEmpty(oAuth.TokenSecret)) {
					Config.PicasaTokenSecret = oAuth.TokenSecret;
				}
				IniConfig.Save();
			}
			try {
				IDictionary<string, string> headers = new Dictionary<string, string>();
				headers.Add("slug", OAuthSession.UrlEncode3986(filename));
				string response = oAuth.MakeOAuthRequest(HTTPMethod.POST, "https://picasaweb.google.com/data/feed/api/user/default/albumid/default", headers, null, null, new SurfaceContainer(surfaceToUpload, outputSettings, filename));
				return ParseResponse(response);
			} catch (Exception ex) {
				LOG.Error("Upload error: ", ex);
				throw;
			} finally {
				if (!string.IsNullOrEmpty(oAuth.Token)) {
					Config.PicasaToken = oAuth.Token;
				}
				if (!string.IsNullOrEmpty(oAuth.TokenSecret)) {
					Config.PicasaTokenSecret = oAuth.TokenSecret;
				}
			}
		}
		
		public static string ParseResponse(string response) {
			if (response == null) {
				return null;
			}
			try {
				XmlDocument doc = new XmlDocument();
				doc.LoadXml(response);
				XmlNodeList nodes = doc.GetElementsByTagName("link", "*");
				if(nodes.Count > 0) {
					string url = null;
					foreach(XmlNode node in nodes) {
						if (node.Attributes != null) {
							url = node.Attributes["href"].Value;
							string rel = node.Attributes["rel"].Value;
							// Pictures with rel="http://schemas.google.com/photos/2007#canonical" are the direct link
							if (rel != null && rel.EndsWith("canonical")) {
								break;
							}
						}
					}
					return url;
				}
			} catch(Exception e) {
				LOG.ErrorFormat("Could not parse Picasa response due to error {0}, response was: {1}", e.Message, response);
			}
			return null;
		}
	}
}
