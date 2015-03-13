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
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Text;
using Greenshot.IniFile;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using System.Runtime.Serialization.Json;
using System.IO;

namespace GreenshotBoxPlugin {

	/// <summary>
	/// Description of ImgurUtils.
	/// </summary>
	public static class BoxUtils {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(BoxUtils));
		private static readonly BoxConfiguration Config = IniConfig.GetIniSection<BoxConfiguration>();
		private const string RedirectUri = "https://www.box.com/home/";
		private const string UploadFileUri = "https://upload.box.com/api/2.0/files/content";
		private const string AuthorizeUri = "https://www.box.com/api/oauth2/authorize";
		private const string TokenUri = "https://www.box.com/api/oauth2/token";
		private const string FilesUri = "https://www.box.com/api/2.0/files/{0}";

		private static bool Authorize() {
			string authorizeUrl = string.Format("{0}?client_id={1}&response_type=code&state=dropboxplugin&redirect_uri={2}", AuthorizeUri, BoxCredentials.ClientId, RedirectUri);

			OAuthLoginForm loginForm = new OAuthLoginForm("Box Authorize", new Size(1060, 600), authorizeUrl, RedirectUri);
			loginForm.ShowDialog();
			if (!loginForm.isOk) {
				return false;
			}
			var callbackParameters = loginForm.CallbackParameters;
			if (callbackParameters == null || !callbackParameters.ContainsKey("code")) {
				return false;
			}

			string authorizationResponse = PostAndReturn(new Uri(TokenUri), string.Format("grant_type=authorization_code&code={0}&client_id={1}&client_secret={2}", callbackParameters["code"], BoxCredentials.ClientId, BoxCredentials.ClientSecret));
			var authorization = JsonSerializer.Deserialize<Authorization>(authorizationResponse);

			Config.BoxToken = authorization.AccessToken;
			IniConfig.Save();
			return true;
		}

		/// <summary>
		/// Download a url response as string
		/// </summary>
		/// <param name="url">An Uri to specify the download location</param>
		/// <param name="postMessage"></param>
		/// <returns>string with the file content</returns>
		public static string PostAndReturn(Uri url, string postMessage) {
			HttpWebRequest webRequest = NetworkHelper.CreateWebRequest(url);
			webRequest.Method = "POST";
			webRequest.KeepAlive = true;
			webRequest.Credentials = CredentialCache.DefaultCredentials;
			webRequest.ContentType = "application/x-www-form-urlencoded";
			byte[] data = Encoding.UTF8.GetBytes(postMessage);
			using (var requestStream = webRequest.GetRequestStream()) {
				requestStream.Write(data, 0, data.Length);
			}
			return NetworkHelper.GetResponse(webRequest);
		}

		/// <summary>
		/// Upload parameters by post
		/// </summary>
		/// <param name="url"></param>
		/// <param name="parameters"></param>
		/// <returns>response</returns>
		public static string HttpPost(string url, IDictionary<string, object> parameters) {
			var webRequest = (HttpWebRequest)NetworkHelper.CreateWebRequest(url);
			webRequest.Method = "POST";
			webRequest.KeepAlive = true;
			webRequest.Credentials = CredentialCache.DefaultCredentials;
			webRequest.Headers.Add("Authorization", "Bearer " + Config.BoxToken);
			NetworkHelper.WriteMultipartFormData(webRequest, parameters);

			return NetworkHelper.GetResponse(webRequest);
		}

		/// <summary>
		/// Upload file by PUT
		/// </summary>
		/// <param name="url"></param>
		/// <param name="content"></param>
		/// <returns>response</returns>
		public static string HttpPut(string url, string content) {
			var webRequest = (HttpWebRequest)NetworkHelper.CreateWebRequest(url);
			webRequest.Method = "PUT";
			webRequest.KeepAlive = true;
			webRequest.Credentials = CredentialCache.DefaultCredentials;
			webRequest.Headers.Add("Authorization", "Bearer " + Config.BoxToken);
			byte[] data = Encoding.UTF8.GetBytes(content);
			using (var requestStream = webRequest.GetRequestStream()) {
				requestStream.Write(data, 0, data.Length);
			}
			return NetworkHelper.GetResponse(webRequest);
		}


		/// <summary>
		/// Get REST request
		/// </summary>
		/// <param name="url"></param>
		/// <returns>response</returns>
		public static string HttpGet(string url) {
			var webRequest = (HttpWebRequest)NetworkHelper.CreateWebRequest(url);
			webRequest.Method = "GET";
			webRequest.KeepAlive = true;
			webRequest.Credentials = CredentialCache.DefaultCredentials;
			webRequest.Headers.Add("Authorization", "Bearer " + Config.BoxToken);
			return NetworkHelper.GetResponse(webRequest);
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
			while (true) {
				const string folderId = "0";
				if (string.IsNullOrEmpty(Config.BoxToken)) {
					if (!Authorize()) {
						return null;
					}
				}

				IDictionary<string, object> parameters = new Dictionary<string, object>();
				parameters.Add("filename", image);
				parameters.Add("parent_id", folderId);

				var response = "";
				try {
					response = HttpPost(UploadFileUri, parameters);
				} catch (WebException ex) {
					if (ex.Status == WebExceptionStatus.ProtocolError) {
						Config.BoxToken = null;
						continue;
					}
				}

				LOG.DebugFormat("Box response: {0}", response);

				// Check if the token is wrong
				if ("wrong auth token".Equals(response)) {
					Config.BoxToken = null;
					IniConfig.Save();
					continue;
				}
				var upload = JsonSerializer.Deserialize<Upload>(response);
				if (upload == null || upload.Entries == null || upload.Entries.Count == 0) return null;

				if (Config.UseSharedLink) {
					string filesResponse = HttpPut(string.Format(FilesUri, upload.Entries[0].Id), "{\"shared_link\": {\"access\": \"open\"}}");
					var file = JsonSerializer.Deserialize<FileEntry>(filesResponse);
					return file.SharedLink.Url;
				}
				return string.Format("http://www.box.com/files/0/f/0/1/f_{0}", upload.Entries[0].Id);
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
