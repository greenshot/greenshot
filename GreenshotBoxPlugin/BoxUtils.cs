/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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

using GreenshotPlugin.Core;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using GreenshotPlugin.IniFile;

namespace GreenshotBoxPlugin {

    /// <summary>
    /// Description of BoxUtils.
    /// </summary>
    public static class BoxUtils {
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(BoxUtils));
		private static readonly BoxConfiguration Config = IniConfig.GetIniSection<BoxConfiguration>();
		private const string UploadFileUri = "https://upload.box.com/api/2.0/files/content";
		private const string FilesUri = "https://www.box.com/api/2.0/files/{0}";

		/// <summary>
		/// Put string
		/// </summary>
		/// <param name="url"></param>
		/// <param name="content"></param>
		/// <param name="settings">OAuth2Settings</param>
		/// <returns>response</returns>
		public static string HttpPut(string url, string content, OAuth2Settings settings) {
			var webRequest= OAuth2Helper.CreateOAuth2WebRequest(HTTPMethod.PUT, url, settings);

			byte[] data = Encoding.UTF8.GetBytes(content);
			using (var requestStream = webRequest.GetRequestStream()) {
				requestStream.Write(data, 0, data.Length);
			}
			return NetworkHelper.GetResponseAsString(webRequest);
		}

		/// <summary>
		/// Do the actual upload to Box
		/// For more details on the available parameters, see: http://developers.box.net/w/page/12923951/ApiFunction_Upload%20and%20Download
		/// </summary>
		/// <param name="image">Image for box upload</param>
		/// <param name="title">Title of box upload</param>
		/// <param name="filename">Filename of box upload</param>
		/// <returns>url to uploaded image</returns>
		public static string UploadToBox(SurfaceContainer image, string title, string filename) {

			// Fill the OAuth2Settings
			var settings = new OAuth2Settings
			{
				AuthUrlPattern = "https://app.box.com/api/oauth2/authorize?client_id={ClientId}&response_type=code&state={State}&redirect_uri={RedirectUrl}",
				TokenUrl = "https://api.box.com/oauth2/token",
				CloudServiceName = "Box",
				ClientId = BoxCredentials.ClientId,
				ClientSecret = BoxCredentials.ClientSecret,
				RedirectUrl = "https://www.box.com/home/",
				BrowserSize = new Size(1060, 600),
				AuthorizeMode = OAuth2AuthorizeMode.EmbeddedBrowser,
				RefreshToken = Config.RefreshToken,
				AccessToken = Config.AccessToken,
				AccessTokenExpires = Config.AccessTokenExpires
			};


			// Copy the settings from the config, which is kept in memory and on the disk

			try {
				var webRequest = OAuth2Helper.CreateOAuth2WebRequest(HTTPMethod.POST, UploadFileUri, settings);
				IDictionary<string, object> parameters = new Dictionary<string, object>
				{
					{ "file", image },
					{ "parent_id", Config.FolderId }
				};

				NetworkHelper.WriteMultipartFormData(webRequest, parameters);

				var response = NetworkHelper.GetResponseAsString(webRequest);

				Log.DebugFormat("Box response: {0}", response);

				var upload = JsonSerializer.Deserialize<Upload>(response);
				if (upload?.Entries == null || upload.Entries.Count == 0) return null;

				if (Config.UseSharedLink) {
					string filesResponse = HttpPut(string.Format(FilesUri, upload.Entries[0].Id), "{\"shared_link\": {\"access\": \"open\"}}", settings);
					var file = JsonSerializer.Deserialize<FileEntry>(filesResponse);
					return file.SharedLink.Url;
				}
				return $"http://www.box.com/files/0/f/0/1/f_{upload.Entries[0].Id}";
			} finally {
				// Copy the settings back to the config, so they are stored.
				Config.RefreshToken = settings.RefreshToken;
				Config.AccessToken = settings.AccessToken;
				Config.AccessTokenExpires = settings.AccessTokenExpires;
				Config.IsDirty = true;
				IniConfig.Save();
			}
		}
	}
	/// <summary>
	/// A simple helper class for the DataContractJsonSerializer
	/// </summary>
	internal static class JsonSerializer {
        /// <summary>
		/// Helper method to parse JSON to object
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="jsonString"></param>
		/// <returns></returns>
		public static T Deserialize<T>(string jsonString) {
			var deserializer = new DataContractJsonSerializer(typeof(T));
            using var stream = new MemoryStream();
            byte[] content = Encoding.UTF8.GetBytes(jsonString);
            stream.Write(content, 0, content.Length);
            stream.Seek(0, SeekOrigin.Begin);
            return (T)deserializer.ReadObject(stream);
        }
	}
}
