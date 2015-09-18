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
using GreenshotPlugin.Extensions;
using GreenshotPlugin.OAuth;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GreenshotBoxPlugin
{

	/// <summary>
	/// Description of ImgurUtils.
	/// </summary>
	public static class BoxUtils {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(BoxUtils));
		private static readonly IBoxConfiguration _config = IniConfig.Current.Get<IBoxConfiguration>();
		private static readonly Uri UploadFileUri = new Uri("https://upload.box.com/api/2.0/files/content");
		private const string FilesUri = "https://www.box.com/api/2.0/files/{0}";

		/// <summary>
		/// Do the actual upload to Box
		/// For more details on the available parameters, see: http://developers.box.net/w/page/12923951/ApiFunction_Upload%20and%20Download
		/// </summary>
		/// <param name="image">Image for box upload</param>
		/// <param name="title">Title of box upload</param>
		/// <param name="filename">Filename of box upload</param>
		/// <returns>url to uploaded image</returns>
		public static async Task<string> UploadToBoxAsync(ISurface surfaceToUpload, ICaptureDetails captureDetails, IProgress<int> progress, CancellationToken token = default(CancellationToken)) {
			string filename = Path.GetFileName(FilenameHelper.GetFilename(_config.UploadFormat, captureDetails));
			var outputSettings = new SurfaceOutputSettings(_config.UploadFormat, _config.UploadJpegQuality, false);

			// Fill the OAuth2Settings
			OAuth2Settings settings = new OAuth2Settings();

			settings.AuthUrlPattern = "https://app.box.com/api/oauth2/authorize?client_id={ClientId}&response_type=code&state={State}&redirect_uri={RedirectUrl}";
			settings.TokenUrl = new Uri("https://api.box.com/oauth2/token");
			settings.CloudServiceName = "Box";
			settings.ClientId = BoxCredentials.ClientId;
			settings.ClientSecret = BoxCredentials.ClientSecret;
			settings.RedirectUrl = "https://www.box.com/home";
			settings.BrowserSize = new Size(1060, 600);
			settings.AuthorizeMode = OAuth2AuthorizeMode.EmbeddedBrowser;

			// Copy the settings from the config, which is kept in memory and on the disk
			settings.RefreshToken = _config.RefreshToken;
			settings.AccessToken = _config.AccessToken;
			settings.AccessTokenExpires = _config.AccessTokenExpires;

			try {
				string response;
				using (var httpClient = await OAuth2Helper.CreateOAuth2HttpClientAsync(UploadFileUri, settings, token)) {

					using (var stream = new MemoryStream()) {
						var multiPartContent = new MultipartFormDataContent();
						var formData = new Dictionary<string, string>();
						formData.Add("parent_id", _config.FolderId);
						formData.Add("filename", filename);
						var formContent = new FormUrlEncodedContent(formData);
						multiPartContent.Add(formContent);

						ImageOutput.SaveToStream(surfaceToUpload, stream, outputSettings);
						stream.Position = 0;
						using (var uploadStream = new ProgressStream(stream, progress)) {
							using (var streamContent = new StreamContent(uploadStream)) {
								streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/" + outputSettings.Format);
								multiPartContent.Add(streamContent);
								using (var responseMessage = await httpClient.PostAsync(UploadFileUri, multiPartContent, token)) {
									response = await responseMessage.GetAsStringAsync(token);
								}
							}
						}
					}
					LOG.DebugFormat("Box response: {0}", response);

					var upload = JsonSerializer.Deserialize<Upload>(response);
					if (upload == null || upload.Entries == null || upload.Entries.Count == 0) {
						return null;
					}

					if (_config.UseSharedLink) {
						Uri uri = new Uri(string.Format(FilesUri, upload.Entries[0].Id));
						var content = new StringContent("{\"shared_link\": {\"access\": \"open\"}}", Encoding.UTF8);
						using (var responseMessage = await httpClient.PutAsync(uri, content, token)) {
							var file = await responseMessage.GetAsJsonAsync(token);
							return file.SharedLink.Url;
						}
					}
					return string.Format("http://www.box.com/files/0/f/0/1/f_{0}", upload.Entries[0].Id);

				}
			} finally {
				// Copy the settings back to the config, so they are stored.
				_config.RefreshToken = settings.RefreshToken;
				_config.AccessToken = settings.AccessToken;
				_config.AccessTokenExpires = settings.AccessTokenExpires;
				// TODO: Save? IniConfig.Save();
			}
		}
	}
	/// <summary>
	/// A simple helper class for the DataContractJsonSerializer
	/// </summary>
	internal static class JsonSerializer {
		/// <summary>
		/// Helper method to serialize object to JSON
		/// </summary>
		/// <param name="jsonObject">JSON object</param>
		/// <returns>string</returns>
		public static string Serialize(object jsonObject) {
			var serializer = new DataContractJsonSerializer(jsonObject.GetType());
			using (MemoryStream stream = new MemoryStream()) {
				serializer.WriteObject(stream, jsonObject);
				return Encoding.UTF8.GetString(stream.ToArray());
			}
		}

		/// <summary>
		/// Helper method to parse JSON to object
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="jsonString"></param>
		/// <returns></returns>
		public static T Deserialize<T>(string jsonString) {
			var deserializer = new DataContractJsonSerializer(typeof(T));
			using (MemoryStream stream = new MemoryStream()) {
				byte[] content = Encoding.UTF8.GetBytes(jsonString);
				stream.Write(content, 0, content.Length);
				stream.Seek(0, SeekOrigin.Begin);
				return (T)deserializer.ReadObject(stream);
			}
		}
	}
}
