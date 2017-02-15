/*
 * A Google Photos Plugin for Greenshot
 * Copyright (C) 2011  Francis Noel
 * 
 * For more information see: http://getgreenshot.org/
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

using GreenshotPlugin.Core;
using System;
using System.Linq;
using System.Xml.Linq;
using GreenshotPlugin.IniFile;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;

namespace GreenshotGooglePhotosPlugin {
	/// <summary>
	/// Description of GooglePhotosUtils.
	/// </summary>
	public static class GooglePhotosUtils {
		private const string PicasaScope = "https://picasaweb.google.com/data/";
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(GooglePhotosUtils));
		private static readonly GooglePhotosConfiguration Config = IniConfig.GetIniSection<GooglePhotosConfiguration>();
		private const string AuthUrl = "https://accounts.google.com/o/oauth2/auth?response_type=code&client_id={ClientId}&redirect_uri={RedirectUrl}&state={State}&scope=" + PicasaScope;
		private const string TokenUrl = "https://www.googleapis.com/oauth2/v3/token";
		private const string UploadUrl = "https://picasaweb.google.com/data/feed/api/user/{0}/albumid/{1}";

		/// <summary>
		/// Do the actual upload to Google Photos
		/// </summary>
		/// <param name="surfaceToUpload">Image to upload</param>
		/// <param name="outputSettings"></param>
		/// <param name="title"></param>
		/// <param name="filename"></param>
		/// <returns>GooglePhotos Response</returns>
		public static string UploadToGooglePhotos(ISurface surfaceToUpload, SurfaceOutputSettings outputSettings, string title, string filename) {
			// Fill the OAuth2Settings
			var settings = new OAuth2Settings
			{
				AuthUrlPattern = AuthUrl,
				TokenUrl = TokenUrl,
				CloudServiceName = "Google Photos",
				ClientId = GooglePhotosCredentials.ClientId,
				ClientSecret = GooglePhotosCredentials.ClientSecret,
				AuthorizeMode = OAuth2AuthorizeMode.LocalServer,
				RefreshToken = Config.RefreshToken,
				AccessToken = Config.AccessToken,
				AccessTokenExpires = Config.AccessTokenExpires
			};

			// Copy the settings from the config, which is kept in memory and on the disk

			try {
				var webRequest = OAuth2Helper.CreateOAuth2WebRequest(HTTPMethod.POST, string.Format(UploadUrl, Config.UploadUser, Config.UploadAlbum), settings);
				if (Config.AddFilename) {
					webRequest.Headers.Add("Slug", NetworkHelper.EscapeDataString(filename));
				}
				SurfaceContainer container = new SurfaceContainer(surfaceToUpload, outputSettings, filename);
				container.Upload(webRequest);
				
				string response = NetworkHelper.GetResponseAsString(webRequest);

				return ParseResponse(response);
			} finally {
				// Copy the settings back to the config, so they are stored.
				Config.RefreshToken = settings.RefreshToken;
				Config.AccessToken = settings.AccessToken;
				Config.AccessTokenExpires = settings.AccessTokenExpires;
				Config.IsDirty = true;
				IniConfig.Save();
			}
		}
		
		/// <summary>
		/// Parse the upload URL from the response
		/// </summary>
		/// <param name="response"></param>
		/// <returns></returns>
		public static string ParseResponse(string response) {
			if (response == null) {
				return null;
			}
			try
			{
				return XElement.Parse(response).Elements()
					.Where(element => element.Name.LocalName == "content" && element.Attribute("src") != null)
					.Select(element => (string)element.Attribute("src")).FirstOrDefault();
			} catch(Exception e) {
				Log.ErrorFormat("Could not parse Google Photos response due to error {0}, response was: {1}", e.Message, response);
			}
			return null;
		}
	}
}
