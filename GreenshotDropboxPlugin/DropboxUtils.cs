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
using Greenshot.IniFile;
using Greenshot.Plugin;
using GreenshotPlugin.Core;

namespace GreenshotDropboxPlugin {
	/// <summary>
	/// Description of DropboxUtils.
	/// </summary>
	public class DropboxUtils {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(DropboxUtils));
		private static DropboxPluginConfiguration config = IniConfig.GetIniSection<DropboxPluginConfiguration>();

		private DropboxUtils() {
		}

		public static string UploadToDropbox(ISurface surfaceToUpload, SurfaceOutputSettings outputSettings, string filename) {
			OAuthSession oAuth = new OAuthSession(DropBoxCredentials.CONSUMER_KEY, DropBoxCredentials.CONSUMER_SECRET);
			oAuth.BrowserSize = new Size(1080, 650);
			oAuth.CheckVerifier = false;
			oAuth.AccessTokenUrl = "https://api.dropbox.com/1/oauth/access_token";
			oAuth.AuthorizeUrl = "https://api.dropbox.com/1/oauth/authorize";
			oAuth.RequestTokenUrl = "https://api.dropbox.com/1/oauth/request_token";
			oAuth.LoginTitle = "Dropbox authorization";
			oAuth.Token = config.DropboxToken;
			oAuth.TokenSecret = config.DropboxTokenSecret;

			try {
				SurfaceContainer imageToUpload = new SurfaceContainer(surfaceToUpload, outputSettings, filename);
				string uploadResponse = oAuth.MakeOAuthRequest(HTTPMethod.POST, "https://api-content.dropbox.com/1/files_put/sandbox/" + OAuthSession.UrlEncode3986(filename), null, null, imageToUpload);
				LOG.DebugFormat("Upload response: {0}", uploadResponse);
			} catch (Exception ex) {
				LOG.Error("Upload error: ", ex);
				throw;
			} finally {
				if (!string.IsNullOrEmpty(oAuth.Token)) {
					config.DropboxToken = oAuth.Token;
				}
				if (!string.IsNullOrEmpty(oAuth.TokenSecret)) {
					config.DropboxTokenSecret = oAuth.TokenSecret;
				}
			}

			// Try to get a URL to the uploaded image
			try {
				string responseString = oAuth.MakeOAuthRequest(HTTPMethod.GET, "https://api.dropbox.com/1/shares/sandbox/" + OAuthSession.UrlEncode3986(filename), null, null, null);
				if (responseString != null) {
					LOG.DebugFormat("Parsing output: {0}", responseString);
					IDictionary<string, object> returnValues = JSONHelper.JsonDecode(responseString);
					if (returnValues.ContainsKey("url")) {
						return returnValues["url"] as string;
					}
				}
			} catch (Exception ex) {
				LOG.Error("Can't parse response.", ex);
			}
			return null;
 		}
	}
}
