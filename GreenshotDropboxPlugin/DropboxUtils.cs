/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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

using Dapplo.Config.Ini;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using GreenshotPlugin.OAuth;
using System;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;

namespace GreenshotDropboxPlugin {
	/// <summary>
	/// Description of DropboxUtils.
	/// </summary>
	public class DropboxUtils {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(DropboxUtils));
		private static DropboxPluginConfiguration config = IniConfig.Get("Greenshot", "greenshot").Get<DropboxPluginConfiguration>();

		private DropboxUtils() {
		}

		public static async Task<string> UploadToDropbox(ISurface surfaceToUpload, SurfaceOutputSettings outputSettings, string filename) {
			OAuthSession oAuth = new OAuthSession(DropBoxCredentials.CONSUMER_KEY, DropBoxCredentials.CONSUMER_SECRET);
			oAuth.BrowserSize = new Size(1080, 650);
			oAuth.CheckVerifier = false;
			oAuth.AccessTokenUrl = new Uri("https://api.dropbox.com/1/oauth/access_token");
			oAuth.AuthorizeUrl = new Uri("https://api.dropbox.com/1/oauth/authorize");
			oAuth.RequestTokenUrl = new Uri("https://api.dropbox.com/1/oauth/request_token");
			oAuth.LoginTitle = "Dropbox authorization";
			oAuth.Token = config.DropboxToken;
			oAuth.TokenSecret = config.DropboxTokenSecret;

			try {
				SurfaceContainer imageToUpload = new SurfaceContainer(surfaceToUpload, outputSettings, filename);
				string uploadResponse = await oAuth.MakeOAuthRequest(HttpMethod.Post, new Uri("https://api-content.dropbox.com/1/files_put/sandbox/" + Uri.EscapeDataString(filename)), null, null, imageToUpload);
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
				string responseString = await oAuth.MakeOAuthRequest(HttpMethod.Get, new Uri("https://api.dropbox.com/1/shares/sandbox/" + Uri.EscapeDataString(filename)), null, null, null);
				if (responseString != null) {
					LOG.DebugFormat("Parsing output: {0}", responseString);
					var result = DynamicJson.Parse(responseString);
					return result.url;
				}
			} catch (Exception ex) {
				LOG.Error("Can't parse response.", ex);
			}
			return null;
 		}
	}
}
