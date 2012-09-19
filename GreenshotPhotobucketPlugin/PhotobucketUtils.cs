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
			string responseString;

			OAuthSession oAuth = new OAuthSession();
			// This url is configured in the Photobucket API settings in the Photobucket site!!
			oAuth.CallbackUrl = "http://getgreenshot.org";
			oAuth.AccessTokenUrl = "http://api.photobucket.com/login/access";
			oAuth.AuthorizeUrl = "http://photobucket.com/apilogin/login";
			oAuth.RequestTokenUrl = "http://api.photobucket.com/login/request";
			oAuth.ConsumerKey = "149833145";
			oAuth.ConsumerSecret = "ebd828180b11103c010c7e71c66f6bcb";
			oAuth.UserAgent = "Greenshot";
			oAuth.BrowserWidth = 1010;
			oAuth.BrowserHeight = 400;
			oAuth.CheckVerifier = false;
			oAuth.LoginTitle = "Photobucket authorization";
			oAuth.Token = config.PhotobucketToken;
			oAuth.TokenSecret = config.PhotobucketTokenSecret;
			Dictionary<string ,string> parameters = new Dictionary<string, string>();
			// add album
			parameters.Add("id", "Apex75");
			// add type
			parameters.Add("type", "base64");
			// Add image
			parameters.Add("uploadfile", System.Convert.ToBase64String(imageData, 0, dataLength));
			// add title
			if (title != null) {
				//parameters.Add("title", title);
			}
			// add filename
			if (filename != null) {
				parameters.Add("filename", filename);
			}
			try {
				LOG.DebugFormat("Album info", oAuth.oAuthWebRequest(HTTPMethod.GET, "http://api.photobucket.com/album/Apex75", null, null, null));
				responseString = oAuth.oAuthWebRequest(HTTPMethod.POST, "http://api.photobucket.com/album/!/upload", parameters, null, null);
			} catch (Exception ex) {
				LOG.Error("Error uploading to Photobucket.", ex);
				throw ex;
			} finally {
				if (!string.IsNullOrEmpty(oAuth.Token)) {
					config.PhotobucketToken = oAuth.Token;
				}
				if (!string.IsNullOrEmpty(oAuth.TokenSecret)) {
					config.PhotobucketTokenSecret = oAuth.TokenSecret;
				}
			}
			LOG.Info(responseString);
			PhotobucketInfo PhotobucketInfo = PhotobucketInfo.ParseResponse(responseString);
			LOG.Debug("Upload to Photobucket was finished");
			return PhotobucketInfo;
		}
	}
}
