/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

using GreenshotPlugin.Core;
using Greenshot.IniFile;

namespace GreenshotPhotobucketPlugin {
	/// <summary>
	/// Description of PhotobucketUtils.
	/// </summary>
	public class PhotobucketUtils {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(PhotobucketUtils));
		private static PhotobucketConfiguration config = IniConfig.GetIniSection<PhotobucketConfiguration>();

		private PhotobucketUtils() {
		}

		/// <summary>
		/// Do the actual upload to Photobucket
		/// For more details on the available parameters, see: http://api.Photobucket.com/resources_anon
		/// </summary>
		/// <param name="imageData">byte[] with image data</param>
		/// <returns>PhotobucketResponse</returns>
		public static PhotobucketInfo UploadToPhotobucket(byte[] imageData, int dataLength, string title, string filename) {
			StringBuilder uploadRequest = new StringBuilder();
			uploadRequest.Append("identifier=greenshot");
			// add type
			uploadRequest.Append("&type=base64");
			// Add image
			uploadRequest.Append("&uploadfile=");
			uploadRequest.Append(OAuthHelper.UrlEncode3986(System.Convert.ToBase64String(imageData, 0, dataLength)));
			// add title
			if (title != null) {
				uploadRequest.Append("&title=");
				uploadRequest.Append(OAuthHelper.UrlEncode3986(title));
			}
			// add filename
			if (filename != null) {
				uploadRequest.Append("&filename=");
				uploadRequest.Append(OAuthHelper.UrlEncode3986(filename));
			}
			string url = "http://api.photobucket.com/album/greenshot/upload";
			string responseString;

			OAuthHelper oAuth = new OAuthHelper();
			// This url is configured in the Photobucket API settings in the Photobucket site!!
			oAuth.CallbackUrl = "http://getgreenshot.org";
			oAuth.AccessTokenUrl = "http://api.photobucket.com/login/access";
			oAuth.AuthorizeUrl = "http://photobucket.com/apilogin/login";
			oAuth.RequestTokenUrl = "http://api.photobucket.com/login/request";
			oAuth.ConsumerKey = ;
			oAuth.ConsumerSecret = ;
			oAuth.UserAgent = "Greenshot";
			oAuth.BrowserWidth = 1010;
			oAuth.BrowserHeight = 400;
			oAuth.CheckVerifier = false;
			if (string.IsNullOrEmpty(config.PhotobucketToken)) {
				LOG.Debug("Creating Photobucket Token");
				try {
					oAuth.getRequestToken();
				} catch (Exception ex) {
					LOG.Error(ex);
					throw new NotSupportedException("Photobucket is not available: " + ex.Message);
				}
				LOG.Debug("Authorizing Photobucket");
				if (string.IsNullOrEmpty(oAuth.authorizeToken("Photobucket authorization"))) {
					return null;
				}
				string accessToken = oAuth.getAccessToken();
				config.PhotobucketToken = oAuth.Token;
				config.PhotobucketTokenSecret = oAuth.TokenSecret;
			} else {
				LOG.Debug("Using stored Photobucket Token");
				oAuth.Token = config.PhotobucketToken;
				oAuth.TokenSecret = config.PhotobucketTokenSecret;
			}
			responseString = oAuth.oAuthWebRequest(OAuthHelper.Method.POST, url, uploadRequest.ToString());
			LOG.Info(responseString);
			PhotobucketInfo PhotobucketInfo = PhotobucketInfo.ParseResponse(responseString);
			LOG.Debug("Upload to Photobucket was finished");
			return PhotobucketInfo;
		}
	}
}
