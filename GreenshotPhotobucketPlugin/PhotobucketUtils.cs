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
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Xml;

namespace GreenshotPhotobucketPlugin
{
	/// <summary>
	/// Description of PhotobucketUtils.
	/// </summary>
	public static class PhotobucketUtils {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(PhotobucketUtils));
		private static readonly IPhotobucketConfiguration Config = IniConfig.Current.Get<IPhotobucketConfiguration>();
		private static List<string> _albumsCache;

		/// <summary>
		/// Do the actual upload to Photobucket
		/// For more details on the available parameters, see: http://api.Photobucket.com/resources_anon
		/// </summary>
		/// <returns>PhotobucketResponse</returns>
		public static async Task<PhotobucketInfo> UploadToPhotobucket(ISurface surfaceToUpload, SurfaceOutputSettings outputSettings, string albumPath, string title, string filename, IProgress<int> progress = null) {
			string responseString;

			var uploadUri = new Uri("http://api.photobucket.com/album/!/upload");
			var oAuth = await CreateSession(true);
			if (oAuth == null) {
				return null;
			}
			var signedParameters = new Dictionary<string, object>();
			// add username if the albumpath is for the user
			// see "Alternative Identifier Specifications" here: https://pic.photobucket.com/dev_help/WebHelpPublic/Content/Getting%20Started/Conventions.htm#ObjectIdentifiers
			if (string.IsNullOrEmpty(albumPath) && oAuth.AccessTokenResponseParameters != null && oAuth.AccessTokenResponseParameters.ContainsKey("username")) {
				signedParameters.Add("id", oAuth.AccessTokenResponseParameters["username"]);
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
			var unsignedParameters = new Dictionary<string, object>();
			// Add image

			using (var stream = new MemoryStream())
			{
				ImageOutput.SaveToStream(surfaceToUpload, stream, outputSettings);
				stream.Position = 0;
				using (var uploadStream = new ProgressStream(stream, progress))
				{
					using (var streamContent = new StreamContent(uploadStream))
					{
						streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/" + outputSettings.Format);
						streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") {
							Name = "\"uploadfile\"",
							FileName = "\"" + filename + "\"",
						};
						unsignedParameters.Add("uploadfile", streamContent);

						try
						{
							var signUri = uploadUri;
							var requestUri = string.IsNullOrEmpty(Config.SubDomain) ? uploadUri : new Uri(uploadUri.AbsoluteUri.Replace("api.photobucket.com", Config.SubDomain));
							responseString = await oAuth.MakeOAuthRequest(HttpMethod.Post, signUri, requestUri, null, signedParameters, unsignedParameters);
						}
						catch (Exception ex)
						{
							LOG.Error("Error uploading to Photobucket.", ex);
							throw;
						}
						finally
						{
							if (!string.IsNullOrEmpty(oAuth.Token))
							{
								Config.Token = oAuth.Token;
							}
							if (!string.IsNullOrEmpty(oAuth.TokenSecret))
							{
								Config.TokenSecret = oAuth.TokenSecret;
							}
						}
					}
				}
			}

			if (responseString == null) {
				return null;
			}
			LOG.Info(responseString);
			var photobucketInfo = PhotobucketInfo.FromUploadResponse(responseString);
			LOG.Debug("Upload to Photobucket was finished");
			return photobucketInfo;
		}
		
		/// <summary>
		/// Helper method to create an OAuth session object for contacting the Photobucket API
		/// </summary>
		/// <returns>OAuthSession</returns>
		private static async Task<OAuthSession> CreateSession(bool autoLogin) {
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
			if (string.IsNullOrEmpty(Config.SubDomain) || string.IsNullOrEmpty(Config.Token) || string.IsNullOrEmpty(Config.Username)) {
				if (!autoLogin) {
					return null;
				}
				if (!await oAuth.AuthorizeAsync()) {
					return null;
				}
				if (!string.IsNullOrEmpty(oAuth.Token)) {
					Config.Token = oAuth.Token;
				}
				if (!string.IsNullOrEmpty(oAuth.TokenSecret)) {
					Config.TokenSecret = oAuth.TokenSecret;
				}
				if (oAuth.AccessTokenResponseParameters != null && oAuth.AccessTokenResponseParameters["subdomain"] != null) {
					Config.SubDomain = oAuth.AccessTokenResponseParameters["subdomain"];
				}
				if (oAuth.AccessTokenResponseParameters != null && oAuth.AccessTokenResponseParameters["username"] != null) {
					Config.Username = oAuth.AccessTokenResponseParameters["username"];
				}
			}
			oAuth.Token = Config.Token;
			oAuth.TokenSecret = Config.TokenSecret;
			return oAuth;
		}

		/// <summary>
		/// Get list of photobucket albums
		/// </summary>
		/// <returns>List of strings</returns>
		public static async Task<List<string>> RetrievePhotobucketAlbums() {
			if (_albumsCache != null) {
				return _albumsCache;
			}
			string responseString;

			var oAuth = await CreateSession(false);
			if (oAuth == null) {
				return null;
			}
			var signedParameters = new Dictionary<string, object>();
			try {
				var signUri = new Uri(string.Format("http://api.photobucket.com/album/{0}", Config.Username));
				var requestUri = new Uri(string.Format("http://api.photobucket.com/album/{0}".Replace("api.photobucket.com", Config.SubDomain), Config.Username));
				responseString = await oAuth.MakeOAuthRequest(HttpMethod.Get, signUri, requestUri, null, signedParameters);
			} catch (Exception ex) {
				LOG.Error("Error uploading to Photobucket.", ex);
				throw;
			} finally {
				if (!string.IsNullOrEmpty(oAuth.Token)) {
					Config.Token = oAuth.Token;
				}
				if (!string.IsNullOrEmpty(oAuth.TokenSecret)) {
					Config.TokenSecret = oAuth.TokenSecret;
				}
			}
			if (responseString == null) {
				return null;
			}
			try {
				var doc = new XmlDocument();
				doc.LoadXml(responseString);
				var albums = new List<string>();
				var xmlNode = doc.GetElementsByTagName("content").Item(0);
				if (xmlNode != null)
				{
					RecurseAlbums(albums, null, xmlNode.ChildNodes);
				}
				LOG.DebugFormat("Albums: {0}", string.Join(",", albums.ToArray()));
				_albumsCache = albums;
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
		private static void RecurseAlbums(List<string>albums, string path, IEnumerable nodes) {
			foreach(XmlNode node in nodes) {
				if (node.Name != "album") {
					continue;
				}
				if (node.Attributes != null)
				{
					string currentAlbum = node.Attributes["name"].Value;
					string currentPath = currentAlbum;
					if (!string.IsNullOrEmpty(path)) {
						currentPath = string.Format("{0}/{1}", path, currentAlbum);
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
