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
using Greenshot.Plugin;

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
		public static PhotobucketInfo UploadToPhotobucket(Image image, OutputSettings outputSettings, string title, string filename) {
			string responseString;

			OAuthSession oAuth = new OAuthSession("149833145", "ebd828180b11103c010c7e71c66f6bcb");
			oAuth.CheckVerifier = false;
			// This url is configured in the Photobucket API settings in the Photobucket site!!
			oAuth.CallbackUrl = "http://getgreenshot.org";
			oAuth.AccessTokenUrl = "http://api.photobucket.com/login/access";
			oAuth.AuthorizeUrl = "http://photobucket.com/apilogin/login";
			oAuth.RequestTokenUrl = "http://api.photobucket.com/login/request";
			oAuth.BrowserSize = new Size(1010, 400);
			oAuth.LoginTitle = "Photobucket authorization";
			if (string.IsNullOrEmpty(config.SubDomain) || string.IsNullOrEmpty(config.Token)) {
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
				IniConfig.Save();
			}
			oAuth.Token = config.Token;
			oAuth.TokenSecret = config.TokenSecret;

			Dictionary<string, object> parameters = new Dictionary<string, object>();
			// add album
			parameters.Add("id", "Apex75/greenshot");
			// add type
			parameters.Add("type", "base64");
			// add title
			if (title != null) {
				parameters.Add("title", title);
			}
			// add filename
			if (filename != null) {
				parameters.Add("filename", filename);
			}
			try {
				string apiUrl = "http://api.photobucket.com/album/!/upload";
				oAuth.Sign(HTTPMethod.POST, apiUrl, parameters);
				apiUrl = apiUrl.Replace("api.photobucket.com", config.SubDomain);
				// Add image
				parameters.Add("uploadfile", new ImageContainer(image, outputSettings, filename));
				responseString = oAuth.MakeRequest(HTTPMethod.POST, apiUrl, parameters, null);
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
