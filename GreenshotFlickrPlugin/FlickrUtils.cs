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
using log4net;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Dapplo.HttpExtensions;

namespace GreenshotFlickrPlugin
{
	/// <summary>
	/// Description of FlickrUtils.
	/// </summary>
	public static class FlickrUtils {
		private static readonly ILog LOG = LogManager.GetLogger(typeof(FlickrUtils));
		private static IFlickrConfiguration config = IniConfig.Current.Get<IFlickrConfiguration>();
		private static readonly Uri FLICKR_API_BASE_URL = new Uri("https://api.flickr.com/services");
		private static readonly Uri FLICKR_UPLOAD_URI = FLICKR_API_BASE_URL.AppendSegments("upload");
		// OAUTH
		private static readonly Uri FLICKR_OAUTH_BASE_URL = FLICKR_API_BASE_URL.AppendSegments("oauth");
		private static readonly Uri FLICKR_ACCESS_TOKEN_URL = FLICKR_OAUTH_BASE_URL.AppendSegments("access_token");
		private static readonly Uri FLICKR_AUTHORIZE_URL = FLICKR_OAUTH_BASE_URL.AppendSegments("authorize");
		private static readonly Uri FLICKR_REQUEST_TOKEN_URL = FLICKR_OAUTH_BASE_URL.AppendSegments("request_token");
		private const string FLICKR_FARM_URL = "https://farm{0}.staticflickr.com/{1}/{2}_{3}.jpg";
		// REST
		private static readonly Uri FLICKR_REST_URL = FLICKR_API_BASE_URL.AppendSegments("rest");
		private static readonly Uri FLICKR_GET_INFO_URL = FLICKR_REST_URL.ExtendQuery(new Dictionary<string, object> { { "method", "flickr.photos.getInfo" }});

		/// <summary>
		/// Do the actual upload to Flickr
		/// For more details on the available parameters, see: http://flickrnet.codeplex.com
		/// </summary>
		/// <param name="surfaceToUpload"></param>
		/// <param name="outputSettings"></param>
		/// <param name="title"></param>
		/// <param name="filename"></param>
		/// <returns>url to image</returns>
		public static async Task<string> UploadToFlickrAsync(ISurface surfaceToUpload, SurfaceOutputSettings outputSettings, string title, string filename, IProgress<int> progress = null, CancellationToken token = default(CancellationToken)) {
			var oAuth = new OAuthSession(FlickrCredentials.ConsumerKey, FlickrCredentials.ConsumerSecret);
			oAuth.BrowserSize = new Size(520, 800);
			oAuth.CheckVerifier = false;
			oAuth.AccessTokenUrl = FLICKR_ACCESS_TOKEN_URL;
			oAuth.AuthorizeUrl = FLICKR_AUTHORIZE_URL;
			oAuth.RequestTokenUrl = FLICKR_REQUEST_TOKEN_URL;
			oAuth.LoginTitle = "Flickr authorization";
			oAuth.Token = config.FlickrToken;
			oAuth.TokenSecret = config.FlickrTokenSecret;
			if (string.IsNullOrEmpty(oAuth.Token)) {
				if (!await oAuth.AuthorizeAsync()) {
					return null;
				}
				if (!string.IsNullOrEmpty(oAuth.Token)) {
					config.FlickrToken = oAuth.Token;
				}
				if (!string.IsNullOrEmpty(oAuth.TokenSecret)) {
					config.FlickrTokenSecret = oAuth.TokenSecret;
				}
				// TODO: IniConfig.Save();
			}
			try {
				IDictionary<string, object> signedParameters = new Dictionary<string, object>();
				signedParameters.Add("content_type", "2");	// content = screenshot
				signedParameters.Add("tags", "Greenshot");
				signedParameters.Add("is_public", config.IsPublic ? "1" : "0");
				signedParameters.Add("is_friend", config.IsFriend ? "1" : "0");
				signedParameters.Add("is_family", config.IsFamily ? "1" : "0");
				signedParameters.Add("safety_level", string.Format("{0}", (int)config.SafetyLevel));
				signedParameters.Add("hidden", config.HiddenFromSearch ? "1" : "2");
				signedParameters.Add("format", "json");	// Doesn't work... :(
				signedParameters.Add("nojsoncallback", "1");
				 
				var unsignedParameters = new Dictionary<string, object>();
				string response, photoId;
				using (var stream = new MemoryStream()) {
					ImageOutput.SaveToStream(surfaceToUpload, stream, outputSettings);
					stream.Position = 0;
					using (var uploadStream = new ProgressStream(stream, progress)) {
						using (var streamContent = new StreamContent(uploadStream)) {
							streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/" + outputSettings.Format);
							streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") {
								Name = "\"photo\"",
								FileName = "\"" + filename + "\"",
							};
							unsignedParameters.Add("photo", streamContent);
							response = await oAuth.MakeOAuthRequest(HttpMethod.Post, FLICKR_UPLOAD_URI, FLICKR_UPLOAD_URI, null, signedParameters, unsignedParameters);
							photoId = GetPhotoId(response);
						}
					}
				}

				// Get Photo Info
				signedParameters = new Dictionary<string, object> { { "photo_id", photoId }, { "format", "json" }, { "nojsoncallback", "1" } };
				response = await oAuth.MakeOAuthRequest(HttpMethod.Post, FLICKR_GET_INFO_URL, FLICKR_GET_INFO_URL, null, signedParameters);
				var photoInfo = DynamicJson.Parse(response);
				if (config.UsePageLink) {
					return photoInfo.photo.urls.url[0]._content;
				}
				return string.Format(FLICKR_FARM_URL, photoInfo.photo.farm, photoInfo.photo.server, photoId, photoInfo.photo.secret);
			} catch (Exception ex) {
				LOG.Error("Upload error: ", ex);
				throw;
			} finally {
				if (!string.IsNullOrEmpty(oAuth.Token)) {
					config.FlickrToken = oAuth.Token;
				}
				if (!string.IsNullOrEmpty(oAuth.TokenSecret)) {
					config.FlickrTokenSecret = oAuth.TokenSecret;
				}
			}
		}

		private static string GetPhotoId(string response) {
			try {
				var doc = new XmlDocument();
				doc.LoadXml(response);
				var nodes = doc.GetElementsByTagName("photoid");
				if (nodes.Count > 0) {
					var xmlNode = nodes.Item(0);
					if (xmlNode != null) {
						return xmlNode.InnerText;
					}
				}
			} catch (Exception ex) {
				LOG.Error("Error parsing Flickr Response.", ex);
			}
			return null;
		}
	}
}
