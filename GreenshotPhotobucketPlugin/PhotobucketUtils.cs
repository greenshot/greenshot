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
using Greenshot.IniFile;
using Greenshot.Plugin;
using GreenshotPlugin.Core;

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
		/// <returns>PhotobucketResponse</returns>
		public static PhotobucketInfo UploadToPhotobucket(ISurface surfaceToUpload, SurfaceOutputSettings outputSettings, string title, string filename) {
			string responseString;

			OAuthSession oAuth = new OAuthSession(PhotobucketCredentials.ConsumerKey, PhotobucketCredentials.ConsumerSecret);
			oAuth.CheckVerifier = false;
			// This url is configured in the Photobucket API settings in the Photobucket site!!
			oAuth.CallbackUrl = "http://getgreenshot.org";
			oAuth.AccessTokenUrl = "http://api.photobucket.com/login/access";
			oAuth.AuthorizeUrl = "http://photobucket.com/apilogin/login";
			oAuth.RequestTokenUrl = "http://api.photobucket.com/login/request";
			oAuth.BrowserSize = new Size(1010, 400);
			// Photobucket is very particular about the used methods!
			oAuth.RequestTokenMethod = HTTPMethod.POST;
			oAuth.AccessTokenMethod = HTTPMethod.POST;

			oAuth.LoginTitle = "Photobucket authorization";
			if (string.IsNullOrEmpty(config.SubDomain) || string.IsNullOrEmpty(config.Token) || string.IsNullOrEmpty(config.Username)) {
				if (!oAuth.Authorize()) {
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
				IniConfig.Save();
			}
			oAuth.Token = config.Token;
			oAuth.TokenSecret = config.TokenSecret;

			IDictionary<string, object> signedParameters = new Dictionary<string, object>();
			// add album
			signedParameters.Add("id", config.Username);
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
				string apiUrl = "http://api.photobucket.com/album/!/upload";
				responseString = oAuth.MakeOAuthRequest(HTTPMethod.POST, apiUrl, apiUrl.Replace("api.photobucket.com", config.SubDomain), signedParameters, unsignedParameters, null);
			} catch (Exception ex) {
				LOG.Error("Error uploading to Photobucket.", ex);
				throw ex;
			} finally {
				if (!string.IsNullOrEmpty(oAuth.Token)) {
					config.Token = oAuth.Token;
				}
				if (!string.IsNullOrEmpty(oAuth.TokenSecret)) {
					config.TokenSecret = oAuth.TokenSecret;
				}
			}
			LOG.Info(responseString);
			PhotobucketInfo PhotobucketInfo = PhotobucketInfo.ParseResponse(responseString);
			LOG.Debug("Upload to Photobucket was finished");
			return PhotobucketInfo;
		}
	}
}
