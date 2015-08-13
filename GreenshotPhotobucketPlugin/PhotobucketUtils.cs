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

using Dapplo.Config.Ini;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using GreenshotPlugin.OAuth;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

namespace GreenshotPhotobucketPlugin {
	/// <summary>
	/// Description of PhotobucketUtils.
	/// </summary>
	public static class PhotobucketUtils {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(PhotobucketUtils));
		private static readonly PhotobucketConfiguration config = IniConfig.Get("Greenshot", "greenshot").Get<PhotobucketConfiguration>();
		private static List<string> albumsCache = null;

		/// <summary>
		/// Do the actual upload to Photobucket
		/// For more details on the available parameters, see: http://api.Photobucket.com/resources_anon
		/// </summary>
		/// <returns>PhotobucketResponse</returns>
		public static async Task<PhotobucketInfo> UploadToPhotobucket(ISurface surfaceToUpload, SurfaceOutputSettings outputSettings, string albumPath, string title, string filename) {
			string responseString;
			
			if (string.IsNullOrEmpty(albumPath)) {
				albumPath = "!";
			}

			OAuthSession oAuth = await createSession(true);
			if (oAuth == null) {
				return null;
			}
			IDictionary<string, object> signedParameters = new Dictionary<string, object>();
			// add album
			if (albumPath == null) {
				signedParameters.Add("id", config.Username);
			} else {
				signedParameters.Add("id", albumPath);
			}
			// add type
			signedParameters.Add("type", "image");
			// add title
			if (title != null) {
				signedParameters.Add("title", title);
			}
			// add filename
			if (filename != null) {
				signedParameters.Add("filename", filename);
			}
			IDictionary<string, object> unsignedParameters = new Dictionary<string, object>();
			// Add image
			unsignedParameters.Add("uploadfile", new SurfaceContainer(surfaceToUpload, outputSettings, filename));
			try {
				var signUri = new Uri("http://api.photobucket.com/album/!/upload");
				var requestUri = new Uri("http://api.photobucket.com/album/!/upload".Replace("api.photobucket.com", config.SubDomain));
				responseString = await oAuth.MakeOAuthRequest(HttpMethod.Post, signUri, requestUri, null, signedParameters, unsignedParameters);
			} catch (Exception ex) {
				LOG.Error("Error uploading to Photobucket.", ex);
				throw;
			} finally {
				if (!string.IsNullOrEmpty(oAuth.Token)) {
					config.Token = oAuth.Token;
				}
				if (!string.IsNullOrEmpty(oAuth.TokenSecret)) {
					config.TokenSecret = oAuth.TokenSecret;
				}
			}
			if (responseString == null) {
				return null;
			}
			LOG.Info(responseString);
			PhotobucketInfo PhotobucketInfo = PhotobucketInfo.FromUploadResponse(responseString);
			LOG.Debug("Upload to Photobucket was finished");
			return PhotobucketInfo;
		}
		
		/// <summary>
		/// Helper method to create an OAuth session object for contacting the Photobucket API
		/// </summary>
		/// <returns>OAuthSession</returns>
		private static async Task<OAuthSession> createSession(bool autoLogin) {
			OAuthSession oAuth = new OAuthSession(PhotobucketCredentials.ConsumerKey, PhotobucketCredentials.ConsumerSecret);
			oAuth.AutoLogin = autoLogin;
			oAuth.CheckVerifier = false;
			// This url is configured in the Photobucket API settings in the Photobucket site!!
			oAuth.AccessTokenUrl = new Uri("http://api.photobucket.com/login/access");
			oAuth.AuthorizeUrl = new Uri("http://photobucket.com/apilogin/login");
			oAuth.RequestTokenUrl = new Uri("http://api.photobucket.com/login/request");
			oAuth.BrowserSize = new Size(1010, 400);
			// Photobucket is very particular about the used methods!
			oAuth.RequestTokenMethod = HttpMethod.Post;
			oAuth.AccessTokenMethod = HttpMethod.Post;

			oAuth.LoginTitle = "Photobucket authorization";
			if (string.IsNullOrEmpty(config.SubDomain) || string.IsNullOrEmpty(config.Token) || string.IsNullOrEmpty(config.Username)) {
				if (!autoLogin) {
					return null;
				}
				if (!await oAuth.AuthorizeAsync()) {
					return null;
				}
				if (!string.IsNullOrEmpty(oAuth.Token)) {
					config.Token = oAuth.Token;
				}
				if (!string.IsNullOrEmpty(oAuth.TokenSecret)) {
					config.TokenSecret = oAuth.TokenSecret;
				}
				if (oAuth.AccessTokenResponseParameters != null && oAuth.AccessTokenResponseParameters["subdomain"] != null) {
					config.SubDomain = oAuth.AccessTokenResponseParameters["subdomain"];
				}
				if (oAuth.AccessTokenResponseParameters != null && oAuth.AccessTokenResponseParameters["username"] != null) {
					config.Username = oAuth.AccessTokenResponseParameters["username"];
				}
			}
			oAuth.Token = config.Token;
			oAuth.TokenSecret = config.TokenSecret;
			return oAuth;
		}

		/// <summary>
		/// Get list of photobucket albums
		/// </summary>
		/// <returns>List of strings</returns>
		public static async Task<List<string>> RetrievePhotobucketAlbums() {
			if (albumsCache != null) {
				return albumsCache;
			}
			string responseString;

			OAuthSession oAuth = await createSession(false);
			if (oAuth == null) {
				return null;
			}
			IDictionary<string, object> signedParameters = new Dictionary<string, object>();
			try {
				var signUri = new Uri(string.Format("http://api.photobucket.com/album/{0}", config.Username));
				var requestUri = new Uri(string.Format("http://api.photobucket.com/album/{0}".Replace("api.photobucket.com", config.SubDomain), config.Username));
				responseString = await oAuth.MakeOAuthRequest(HttpMethod.Get, signUri, requestUri, null, signedParameters);
			} catch (Exception ex) {
				LOG.Error("Error uploading to Photobucket.", ex);
				throw;
			} finally {
				if (!string.IsNullOrEmpty(oAuth.Token)) {
					config.Token = oAuth.Token;
				}
				if (!string.IsNullOrEmpty(oAuth.TokenSecret)) {
					config.TokenSecret = oAuth.TokenSecret;
				}
			}
			if (responseString == null) {
				return null;
			}
			try {
				XmlDocument doc = new XmlDocument();
				doc.LoadXml(responseString);
				List<string> albums = new List<string>();
				recurseAlbums(albums, null, doc.GetElementsByTagName("content").Item(0).ChildNodes);
				LOG.DebugFormat("Albums: {0}", string.Join(",", albums.ToArray()));
				albumsCache = albums;
				return albums;
			} catch(Exception e) {
				LOG.Error("Error while Reading albums: ", e);
			}

			LOG.Debug("Upload to Photobucket was finished");
			return null;
		}
		
		/// <summary>
		/// Parse the album nodes recursively
		/// </summary>
		/// <param name="albums"></param>
		/// <param name="path"></param>
		/// <param name="nodes"></param>
		private static void recurseAlbums(List<string>albums, string path, XmlNodeList nodes) {
			foreach(XmlNode node in nodes) {
				if (node.Name != "album") {
					continue;
				}
				string currentAlbum = node.Attributes["name"].Value;
				string currentPath = currentAlbum;
				if (path != null && path.Length > 0) {
					currentPath = string.Format("{0}/{1}", path, currentAlbum);
				}
				
				albums.Add(currentPath);
				if (node.Attributes["subalbum_count"] != null && node.Attributes["subalbum_count"].Value != "0") {
					recurseAlbums(albums, currentPath, node.ChildNodes);
				}
			}
		}
	}
}
