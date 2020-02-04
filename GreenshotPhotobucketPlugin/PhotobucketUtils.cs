/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Xml;
using Greenshot.IniFile;
using Greenshot.Plugin;
using GreenshotPlugin.Core;

namespace GreenshotPhotobucketPlugin {
	/// <summary>
	/// Description of PhotobucketUtils.
	/// </summary>
	public static class PhotobucketUtils {
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(PhotobucketUtils));
		private static readonly PhotobucketConfiguration PhotobucketConfig = IniConfig.GetIniSection<PhotobucketConfiguration>();
		private static List<string> _albumsCache;

		/// <summary>
		/// Do the actual upload to Photobucket
		/// For more details on the available parameters, see: http://api.Photobucket.com/resources_anon
		/// </summary>
		/// <returns>PhotobucketResponse</returns>
		public static PhotobucketInfo UploadToPhotobucket(ISurface surfaceToUpload, SurfaceOutputSettings outputSettings, string albumPath, string title, string filename) {
			string responseString;
			
			var oAuth = CreateSession(true);
			if (oAuth == null) {
				return null;
			}
			IDictionary<string, object> signedParameters = new Dictionary<string, object>();
			// add album
			if (string.IsNullOrEmpty(albumPath))
			{
				signedParameters.Add("id", string.IsNullOrEmpty(PhotobucketConfig.Username) ? "!" : PhotobucketConfig.Username);
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
			IDictionary<string, object> unsignedParameters = new Dictionary<string, object>
			{
				// Add image
				{ "uploadfile", new SurfaceContainer(surfaceToUpload, outputSettings, filename) }
			};
			try {
				string apiUrl = "http://api.photobucket.com/album/!/upload";
				responseString = oAuth.MakeOAuthRequest(HTTPMethod.POST, apiUrl, apiUrl.Replace("api.photobucket.com", PhotobucketConfig.SubDomain), signedParameters, unsignedParameters, null);
			} catch (Exception ex) {
				Log.Error("Error uploading to Photobucket.", ex);
				throw;
			} finally {
				if (!string.IsNullOrEmpty(oAuth.Token)) {
					PhotobucketConfig.Token = oAuth.Token;
				}
				if (!string.IsNullOrEmpty(oAuth.TokenSecret)) {
					PhotobucketConfig.TokenSecret = oAuth.TokenSecret;
				}
			}
			if (responseString == null) {
				return null;
			}
			Log.Info(responseString);
			var photobucketInfo = PhotobucketInfo.FromUploadResponse(responseString);
			Log.Debug("Upload to Photobucket was finished");
			return photobucketInfo;
		}
		
		/// <summary>
		/// Helper method to create an OAuth session object for contacting the Photobucket API
		/// </summary>
		/// <returns>OAuthSession</returns>
		private static OAuthSession CreateSession(bool autoLogin) {
			var oAuth = new OAuthSession(PhotobucketCredentials.ConsumerKey, PhotobucketCredentials.ConsumerSecret)
			{
				AutoLogin = autoLogin,
				CheckVerifier = false,
				CallbackUrl = "http://getgreenshot.org",
				AccessTokenUrl = "http://api.photobucket.com/login/access",
				AuthorizeUrl = "http://photobucket.com/apilogin/login",
				RequestTokenUrl = "http://api.photobucket.com/login/request",
				BrowserSize = new Size(1010, 400),
				RequestTokenMethod = HTTPMethod.POST,
				AccessTokenMethod = HTTPMethod.POST,
				LoginTitle = "Photobucket authorization"
			};
			// This url is configured in the Photobucket API settings in the Photobucket site!!
			// Photobucket is very particular about the used methods!

			if (string.IsNullOrEmpty(PhotobucketConfig.SubDomain) || string.IsNullOrEmpty(PhotobucketConfig.Token) || string.IsNullOrEmpty(PhotobucketConfig.Username)) {
				if (!autoLogin) {
					return null;
				}
				if (!oAuth.Authorize()) {
					return null;
				}
				if (!string.IsNullOrEmpty(oAuth.Token)) {
					PhotobucketConfig.Token = oAuth.Token;
				}
				if (!string.IsNullOrEmpty(oAuth.TokenSecret)) {
					PhotobucketConfig.TokenSecret = oAuth.TokenSecret;
				}
				if (oAuth.AccessTokenResponseParameters?["subdomain"] != null) {
					PhotobucketConfig.SubDomain = oAuth.AccessTokenResponseParameters["subdomain"];
				}
				if (oAuth.AccessTokenResponseParameters?["username"] != null) {
					PhotobucketConfig.Username = oAuth.AccessTokenResponseParameters["username"];
				}
				IniConfig.Save();
			}
			oAuth.Token = PhotobucketConfig.Token;
			oAuth.TokenSecret = PhotobucketConfig.TokenSecret;
			return oAuth;
		}

		/// <summary>
		/// Get list of photobucket albums
		/// </summary>
		/// <returns>List of string</returns>
		public static IList<string> RetrievePhotobucketAlbums() {
			if (_albumsCache != null) {
				return _albumsCache;
			}
			string responseString;

			OAuthSession oAuth = CreateSession(false);
			if (oAuth == null) {
				return null;
			}
			IDictionary<string, object> signedParameters = new Dictionary<string, object>();
			try {
				string apiUrl = $"http://api.photobucket.com/album/{PhotobucketConfig.Username}";
				responseString = oAuth.MakeOAuthRequest(HTTPMethod.GET, apiUrl, apiUrl.Replace("api.photobucket.com", PhotobucketConfig.SubDomain), signedParameters, null, null);
			} catch (Exception ex) {
				Log.Error("Error uploading to Photobucket.", ex);
				throw;
			} finally {
				if (!string.IsNullOrEmpty(oAuth.Token)) {
					PhotobucketConfig.Token = oAuth.Token;
				}
				if (!string.IsNullOrEmpty(oAuth.TokenSecret)) {
					PhotobucketConfig.TokenSecret = oAuth.TokenSecret;
				}
			}
			if (responseString == null) {
				return null;
			}
			try {
				XmlDocument doc = new XmlDocument();
				doc.LoadXml(responseString);
				List<string> albums = new List<string>();
				var xmlNode = doc.GetElementsByTagName("content").Item(0);
				if (xmlNode != null)
				{
					RecurseAlbums(albums, null, xmlNode.ChildNodes);
				}
				Log.DebugFormat("Albums: {0}", string.Join(",", albums.ToArray()));
				_albumsCache = albums;
				return albums;
			} catch(Exception e) {
				Log.Error("Error while Reading albums: ", e);
			}

			Log.Debug("Upload to Photobucket was finished");
			return null;
		}
		
		/// <summary>
		/// Parse the album nodes recursively
		/// </summary>
		/// <param name="albums"></param>
		/// <param name="path"></param>
		/// <param name="nodes"></param>
		private static void RecurseAlbums(ICollection<string> albums, string path, IEnumerable nodes) {
			foreach(XmlNode node in nodes) {
				if (node.Name != "album") {
					continue;
				}
				if (node.Attributes != null)
				{
					string currentAlbum = node.Attributes["name"].Value;
					string currentPath = currentAlbum;
					if (!string.IsNullOrEmpty(path)) {
						currentPath = $"{path}/{currentAlbum}";
					}
				
					albums.Add(currentPath);
					if (node.Attributes["subalbum_count"] != null && node.Attributes["subalbum_count"].Value != "0") {
						RecurseAlbums(albums, currentPath, node.ChildNodes);
					}
				}
			}
		}
	}
}
