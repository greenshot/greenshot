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
using System.Xml;
using Greenshot.IniFile;
using Greenshot.Plugin;
using GreenshotPlugin.Core;

namespace GreenshotFlickrPlugin {
	/// <summary>
	/// Description of FlickrUtils.
	/// </summary>
	public class FlickrUtils {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(FlickrUtils));
		private static FlickrConfiguration config = IniConfig.GetIniSection<FlickrConfiguration>();

		private FlickrUtils() {
		}


		/// <summary>
		/// Do the actual upload to Flickr
		/// For more details on the available parameters, see: http://flickrnet.codeplex.com
		/// </summary>
		/// <param name="imageData">byte[] with image data</param>
		/// <returns>url to image</returns>
		public static string UploadToFlickr(ISurface surfaceToUpload, SurfaceOutputSettings outputSettings, string title, string filename) {
			OAuthSession oAuth = new OAuthSession(FlickrCredentials.ConsumerKey, FlickrCredentials.ConsumerSecret);
			oAuth.BrowserSize = new Size(520, 800);
			oAuth.CheckVerifier = false;
			oAuth.AccessTokenUrl = "http://api.flickr.com/services/oauth/access_token";
			oAuth.AuthorizeUrl = "http://api.flickr.com/services/oauth/authorize";
			oAuth.RequestTokenUrl = "http://api.flickr.com/services/oauth/request_token";
			oAuth.LoginTitle = "Flickr authorization";
			oAuth.Token = config.FlickrToken;
			oAuth.TokenSecret = config.FlickrTokenSecret;
			if (string.IsNullOrEmpty(oAuth.Token)) {
				if (!oAuth.Authorize()) {
					return null;
				}
				if (!string.IsNullOrEmpty(oAuth.Token)) {
					config.FlickrToken = oAuth.Token;
				}
				if (!string.IsNullOrEmpty(oAuth.TokenSecret)) {
					config.FlickrTokenSecret = oAuth.TokenSecret;
				}
				IniConfig.Save();
			}
			try {
				IDictionary<string, object> signedParameters = new Dictionary<string, object>();
				signedParameters.Add("content_type","2");	// Screenshot
				signedParameters.Add("tags","Greenshot");
				signedParameters.Add("is_public", config.IsPublic?"1":"0");
				signedParameters.Add("is_friend", config.IsFriend?"1":"0");
				signedParameters.Add("is_family", config.IsFamily?"1":"0");
				signedParameters.Add("safety_level", string.Format("{0}", (int)config.SafetyLevel));
				signedParameters.Add("hidden", config.HiddenFromSearch?"1":"2");
				IDictionary<string, object> otherParameters = new Dictionary<string, object>();
				otherParameters.Add("photo", new SurfaceContainer(surfaceToUpload, outputSettings, filename));
				string response = oAuth.MakeOAuthRequest(HTTPMethod.POST, "http://api.flickr.com/services/upload/", signedParameters, otherParameters, null);
				string photoId = GetPhotoId(response);

				// Get Photo Info
				signedParameters = new Dictionary<string, object>();
				signedParameters.Add("photo_id", photoId);
				string photoInfo = oAuth.MakeOAuthRequest(HTTPMethod.POST, "http://api.flickr.com/services/rest/?method=flickr.photos.getInfo", signedParameters, null, null);
				return GetUrl(photoInfo);
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

		private static string GetUrl(string response) {
			try {
				XmlDocument doc = new XmlDocument();
				doc.LoadXml(response);
				XmlNodeList nodes = doc.GetElementsByTagName("url");
				if(nodes.Count > 0) {
					return nodes.Item(0).InnerText;
				}
			} catch (Exception ex) {
				LOG.Error("Error parsing Flickr Response.", ex);
			}
			return null;
		}

		private static string GetPhotoId(string response) {
			try {
				XmlDocument doc = new XmlDocument();
				doc.LoadXml(response);
				XmlNodeList nodes = doc.GetElementsByTagName("photoid");
				if(nodes.Count > 0) {
					return nodes.Item(0).InnerText;
				}
			} catch (Exception ex) {
				LOG.Error("Error parsing Flickr Response.", ex);
			}
			return null;
		}
	}
}
