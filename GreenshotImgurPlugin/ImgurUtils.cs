/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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
using System.IO;
using System.Net;
using Greenshot.IniFile;
using Greenshot.Plugin;
using GreenshotPlugin.Core;

namespace GreenshotImgurPlugin {
	/// <summary>
	/// Description of ImgurUtils.
	/// </summary>
	public static class ImgurUtils {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ImgurUtils));
		private const string SmallUrlPattern = "http://i.imgur.com/{0}s.jpg";
		private static readonly ImgurConfiguration Config = IniConfig.GetIniSection<ImgurConfiguration>();
		private const string AuthUrlPattern = "https://api.imgur.com/oauth2/authorize?response_type=code&client_id={ClientId}&redirect_uri={RedirectUrl}&state={State}";
		private const string TokenUrl = "https://api.imgur.com/oauth2/token";

		/// <summary>
		/// Load the complete history of the imgur uploads, with the corresponding information
		/// </summary>
		public static void LoadHistory() {
			if (Config.runtimeImgurHistory.Count == Config.ImgurUploadHistory.Count) {
				return;
			}
			// Load the ImUr history
			List<string> hashes = new List<string>();
			foreach(string hash in Config.ImgurUploadHistory.Keys) {
				hashes.Add(hash);
			}
			
			bool saveNeeded = false;

			foreach(string hash in hashes) {
				if (Config.runtimeImgurHistory.ContainsKey(hash)) {
					// Already loaded
					continue;
				}
				try {
					ImgurInfo imgurInfo = RetrieveImgurInfo(hash, Config.ImgurUploadHistory[hash]);
					if (imgurInfo != null) {
						RetrieveImgurThumbnail(imgurInfo);
						Config.runtimeImgurHistory.Add(hash, imgurInfo);
					} else {
						LOG.DebugFormat("Deleting not found ImgUr {0} from config.", hash);
						Config.ImgurUploadHistory.Remove(hash);
						saveNeeded = true;
					}
				} catch (WebException wE) {
					bool redirected = false;
					if (wE.Status == WebExceptionStatus.ProtocolError) {
						HttpWebResponse response = ((HttpWebResponse)wE.Response);
						// Image no longer available
						if (response.StatusCode == HttpStatusCode.Redirect) {
							LOG.InfoFormat("ImgUr image for hash {0} is no longer available", hash);
							Config.ImgurUploadHistory.Remove(hash);
							redirected = true;
						}
					}
					if (!redirected) {
						LOG.Error("Problem loading ImgUr history for hash " + hash, wE);
					}
				} catch (Exception e) {
					LOG.Error("Problem loading ImgUr history for hash " + hash, e);
				}
			}
			if (saveNeeded) {
				// Save needed changes
				IniConfig.Save();
			}
		}

		/// <summary>
		/// Use this to make sure Imgur knows from where the upload comes.
		/// </summary>
		/// <param name="webRequest"></param>
		private static void SetClientId(HttpWebRequest webRequest) {
			webRequest.Headers.Add("Authorization", "Client-ID " + ImgurCredentials.CONSUMER_KEY);
		}

		/// <summary>
		/// Do the actual upload to Imgur
		/// For more details on the available parameters, see: http://api.imgur.com/resources_anon
		/// </summary>
		/// <param name="surfaceToUpload">ISurface to upload</param>
		/// <param name="outputSettings">OutputSettings for the image file format</param>
		/// <param name="title">Title</param>
		/// <param name="filename">Filename</param>
		/// <returns>ImgurInfo with details</returns>
		public static ImgurInfo UploadToImgur(ISurface surfaceToUpload, SurfaceOutputSettings outputSettings, string title, string filename) {
			IDictionary<string, object> otherParameters = new Dictionary<string, object>();
			// add title
			if (title != null && Config.AddTitle) {
				otherParameters.Add("title", title);
			}
			// add filename
			if (filename != null && Config.AddFilename) {
				otherParameters.Add("name", filename);
			}
			string responseString;
			if (Config.AnonymousAccess) {
				// add key, we only use the other parameters for the AnonymousAccess
				//otherParameters.Add("key", IMGUR_ANONYMOUS_API_KEY);
				HttpWebRequest webRequest = NetworkHelper.CreateWebRequest(Config.ImgurApi3Url + "/upload.xml?" + NetworkHelper.GenerateQueryParameters(otherParameters), HTTPMethod.POST);
				webRequest.ContentType = "image/" + outputSettings.Format;
				webRequest.ServicePoint.Expect100Continue = false;

				SetClientId(webRequest);
				try {
					using (var requestStream = webRequest.GetRequestStream()) {
						ImageOutput.SaveToStream(surfaceToUpload, requestStream, outputSettings);
					}
		
					using (WebResponse response = webRequest.GetResponse()) {
						using (StreamReader reader = new StreamReader(response.GetResponseStream(), true)) {
							responseString = reader.ReadToEnd();
						}
						LogRateLimitInfo(response);
					}
				} catch (Exception ex) {
					LOG.Error("Upload to imgur gave an exeption: ", ex);
					throw;
				}
			} else {

				var oauth2Settings = new OAuth2Settings();
				oauth2Settings.AuthUrlPattern = AuthUrlPattern;
				oauth2Settings.TokenUrl = TokenUrl;
				oauth2Settings.RedirectUrl = "https://imgur.com";
				oauth2Settings.CloudServiceName = "Imgur";
				oauth2Settings.ClientId = ImgurCredentials.CONSUMER_KEY;
				oauth2Settings.ClientSecret = ImgurCredentials.CONSUMER_SECRET;
				oauth2Settings.AuthorizeMode = OAuth2AuthorizeMode.EmbeddedBrowser;
				oauth2Settings.BrowserSize = new Size(680, 880);

				// Copy the settings from the config, which is kept in memory and on the disk
				oauth2Settings.RefreshToken = Config.RefreshToken;
				oauth2Settings.AccessToken = Config.AccessToken;
				oauth2Settings.AccessTokenExpires = Config.AccessTokenExpires;

				try
				{
					var webRequest = OAuth2Helper.CreateOAuth2WebRequest(HTTPMethod.POST, Config.ImgurApi3Url + "upload.xml", oauth2Settings);
					otherParameters.Add("image", new SurfaceContainer(surfaceToUpload, outputSettings, filename));

					NetworkHelper.WriteMultipartFormData(webRequest, otherParameters);

					responseString = NetworkHelper.GetResponseAsString(webRequest);
				}
				finally
				{
					// Copy the settings back to the config, so they are stored.
					Config.RefreshToken = oauth2Settings.RefreshToken;
					Config.AccessToken = oauth2Settings.AccessToken;
					Config.AccessTokenExpires = oauth2Settings.AccessTokenExpires;
					Config.IsDirty = true;
					IniConfig.Save();
				}
			}
			return ImgurInfo.ParseResponse(responseString);
		}

		/// <summary>
		/// Retrieve the thumbnail of an imgur image
		/// </summary>
		/// <param name="imgurInfo"></param>
		public static void RetrieveImgurThumbnail(ImgurInfo imgurInfo) {
			if (imgurInfo.SmallSquare == null) {
				LOG.Warn("Imgur URL was null, not retrieving thumbnail.");
				return;
			}
			LOG.InfoFormat("Retrieving Imgur image for {0} with url {1}", imgurInfo.Hash, imgurInfo.SmallSquare);
			HttpWebRequest webRequest = NetworkHelper.CreateWebRequest(string.Format(SmallUrlPattern, imgurInfo.Hash), HTTPMethod.GET);
			webRequest.ServicePoint.Expect100Continue = false;
			SetClientId(webRequest);
			using (WebResponse response = webRequest.GetResponse()) {
				LogRateLimitInfo(response);
				Stream responseStream = response.GetResponseStream();
				if (responseStream != null)
				{
					imgurInfo.Image = Image.FromStream(responseStream);
				}
			}
		}

		/// <summary>
		/// Retrieve information on an imgur image
		/// </summary>
		/// <param name="hash"></param>
		/// <param name="deleteHash"></param>
		/// <returns>ImgurInfo</returns>
		public static ImgurInfo RetrieveImgurInfo(string hash, string deleteHash) {
			string url = Config.ImgurApiUrl + "/image/" + hash;
			LOG.InfoFormat("Retrieving Imgur info for {0} with url {1}", hash, url);
			HttpWebRequest webRequest = NetworkHelper.CreateWebRequest(url, HTTPMethod.GET);
			webRequest.ServicePoint.Expect100Continue = false;
			SetClientId(webRequest);
			string responseString;
			try {
				using (WebResponse response = webRequest.GetResponse()) {
					LogRateLimitInfo(response);
					using (StreamReader reader = new StreamReader(response.GetResponseStream(), true)) {
						responseString = reader.ReadToEnd();
					}
				}
			} catch (WebException wE) {
				if (wE.Status == WebExceptionStatus.ProtocolError) {
					if (((HttpWebResponse)wE.Response).StatusCode == HttpStatusCode.NotFound) {
						return null;
					}
				}
				throw;
			}
			LOG.Debug(responseString);
			ImgurInfo imgurInfo = ImgurInfo.ParseResponse(responseString);
			imgurInfo.DeleteHash = deleteHash;
			return imgurInfo;
		}

		/// <summary>
		/// Delete an imgur image, this is done by specifying the delete hash
		/// </summary>
		/// <param name="imgurInfo"></param>
		public static void DeleteImgurImage(ImgurInfo imgurInfo) {
			LOG.InfoFormat("Deleting Imgur image for {0}", imgurInfo.DeleteHash);
			
			try {
				string url = Config.ImgurApiUrl + "/delete/" + imgurInfo.DeleteHash;
				HttpWebRequest webRequest = NetworkHelper.CreateWebRequest(url, HTTPMethod.GET);
				webRequest.ServicePoint.Expect100Continue = false;
				SetClientId(webRequest);
				string responseString;
				using (WebResponse response = webRequest.GetResponse()) {
					LogRateLimitInfo(response);
					using (StreamReader reader = new StreamReader(response.GetResponseStream(), true)) {
						responseString = reader.ReadToEnd();
					}
				}
				LOG.InfoFormat("Delete result: {0}", responseString);
			} catch (WebException wE) {
				// Allow "Bad request" this means we already deleted it
				if (wE.Status == WebExceptionStatus.ProtocolError) {
					if (((HttpWebResponse)wE.Response).StatusCode != HttpStatusCode.BadRequest) {
						throw ;
					}
				}
			}
			// Make sure we remove it from the history, if no error occured
			Config.runtimeImgurHistory.Remove(imgurInfo.Hash);
			Config.ImgurUploadHistory.Remove(imgurInfo.Hash);
			imgurInfo.Image = null;
		}

		/// <summary>
		/// Helper for logging
		/// </summary>
		/// <param name="nameValues"></param>
		/// <param name="key"></param>
		private static void LogHeader(IDictionary<string, string> nameValues, string key) {
			if (nameValues.ContainsKey(key)) {
				LOG.InfoFormat("key={0}", nameValues[key]);
			}
		}

		/// <summary>
		/// Log the current rate-limit information
		/// </summary>
		/// <param name="response"></param>
		private static void LogRateLimitInfo(WebResponse response) {
			IDictionary<string, string> nameValues = new Dictionary<string, string>();
			foreach (string key in response.Headers.AllKeys) {
				if (!nameValues.ContainsKey(key)) {
					nameValues.Add(key, response.Headers[key]);
				}
			}
			LogHeader(nameValues, "X-RateLimit-Limit");
			LogHeader(nameValues, "X-RateLimit-Remaining");
			LogHeader(nameValues, "X-RateLimit-UserLimit");
			LogHeader(nameValues, "X-RateLimit-UserRemaining");
			LogHeader(nameValues, "X-RateLimit-UserReset");
			LogHeader(nameValues, "X-RateLimit-ClientLimit");
			LogHeader(nameValues, "X-RateLimit-ClientRemaining");

			// Update the credits in the config, this is shown in a form
			int credits;
			if (nameValues.ContainsKey("X-RateLimit-Remaining") && int.TryParse(nameValues["X-RateLimit-Remaining"], out credits)) {
				Config.Credits = credits;
			}
		}
	}
}
