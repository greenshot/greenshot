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

namespace GreenshotImgurPlugin {
	/// <summary>
	/// Description of ImgurUtils.
	/// </summary>
	public class ImgurUtils {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ImgurUtils));
		private const string IMGUR_ANONYMOUS_API_KEY = "8116a978913f3cf5dfc8e1117a055056";
		private static ImgurConfiguration config = IniConfig.GetIniSection<ImgurConfiguration>();

		private ImgurUtils() {
		}

		public static void LoadHistory() {
			if (config.runtimeImgurHistory.Count == config.ImgurUploadHistory.Count) {
				return;
			}
			// Load the ImUr history
			List<string> hashes = new List<string>();
			foreach(string hash in config.ImgurUploadHistory.Keys) {
				hashes.Add(hash);
			}
			
			bool saveNeeded = false;

			foreach(string hash in hashes) {
				if (config.runtimeImgurHistory.ContainsKey(hash)) {
					// Already loaded
					continue;
				}
				try {
					ImgurInfo imgurInfo = ImgurUtils.RetrieveImgurInfo(hash, config.ImgurUploadHistory[hash]);
					if (imgurInfo != null) {
						ImgurUtils.RetrieveImgurThumbnail(imgurInfo);
						config.runtimeImgurHistory.Add(hash, imgurInfo);
					} else {
						LOG.DebugFormat("Deleting not found ImgUr {0} from config.", hash);
						config.ImgurUploadHistory.Remove(hash);
						saveNeeded = true;
					}
				} catch (WebException wE) {
					bool redirected = false;
					if (wE.Status == WebExceptionStatus.ProtocolError) {
						HttpWebResponse response = ((HttpWebResponse)wE.Response);
						// Image no longer available
						if (response.StatusCode == HttpStatusCode.Redirect) {
							LOG.InfoFormat("ImgUr image for hash {0} is no longer available", hash);
							config.ImgurUploadHistory.Remove(hash);
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
		/// Do the actual upload to Imgur
		/// For more details on the available parameters, see: http://api.imgur.com/resources_anon
		/// </summary>
		/// <param name="imageData">byte[] with image data</param>
		/// <returns>ImgurResponse</returns>
		public static ImgurInfo UploadToImgur(byte[] imageData, int dataLength, string title, string filename) {
			IDictionary<string, string> uploadParameters = new Dictionary<string, string>();
			// Add image
			uploadParameters.Add("image", System.Convert.ToBase64String(imageData, 0, dataLength));
			// add type
			uploadParameters.Add("type", "base64");

			// add title
			if (title != null) {
				uploadParameters.Add("title", title);
			}
			// add filename
			if (filename != null) {
				uploadParameters.Add("name", filename);
			}

			string responseString = null;
			if (config.AnonymousAccess) {
				// add key
				uploadParameters.Add("key", IMGUR_ANONYMOUS_API_KEY);
				HttpWebRequest webRequest = (HttpWebRequest)NetworkHelper.CreateWebRequest(config.ImgurApiUrl + "/upload");

				webRequest.Method = "POST";
				webRequest.ContentType = "application/x-www-form-urlencoded";
				webRequest.ServicePoint.Expect100Continue = false;
	
				using(StreamWriter streamWriter = new StreamWriter(webRequest.GetRequestStream())) {
					string urloadText = NetworkHelper.GenerateQueryParameters(uploadParameters);
					streamWriter.Write(urloadText);
				}
				using (WebResponse response = webRequest.GetResponse()) {
					LogCredits(response);
					Stream responseStream = response.GetResponseStream();
					StreamReader responseReader = new StreamReader(responseStream);
					responseString = responseReader.ReadToEnd();
				}

			} else {
				OAuthSession oAuth = new OAuthSession();
				oAuth.BrowserWidth = 650;
				oAuth.BrowserHeight = 500;
				oAuth.CallbackUrl = "http://getgreenshot.org";
				oAuth.AccessTokenUrl = "http://api.imgur.com/oauth/access_token";
				oAuth.AuthorizeUrl = "http://api.imgur.com/oauth/authorize";
				oAuth.RequestTokenUrl = "http://api.imgur.com/oauth/request_token";
				oAuth.ConsumerKey = ImgurCredentials.CONSUMER_KEY;
				oAuth.ConsumerSecret = ImgurCredentials.CONSUMER_SECRET;
				oAuth.UserAgent = "Greenshot";
				oAuth.LoginTitle = "Imgur authorization";
				//oAuth.UseHTTPHeadersForAuthorization = false;
				oAuth.Token = config.ImgurToken;
				oAuth.TokenSecret = config.ImgurTokenSecret;
				try {
					LOG.DebugFormat("Test: {0}", oAuth.oAuthWebRequest(HTTPMethod.GET, "http://api.imgur.com/2/account", null));
					responseString = oAuth.oAuthWebRequest(HTTPMethod.POST, "http://api.imgur.com/2/account/images.xml", uploadParameters);
				} catch (Exception ex) {
					LOG.Error("Upload to imgur gave an exeption: ", ex);
					throw ex;
				} finally {
					if (oAuth.Token != null) {
						config.ImgurToken = oAuth.Token;
					}
					if (oAuth.TokenSecret != null) {
						config.ImgurTokenSecret = oAuth.TokenSecret;
					}
					IniConfig.Save();
				}
			}
			LOG.Info(responseString);
			ImgurInfo imgurInfo = ImgurInfo.ParseResponse(responseString);
			return imgurInfo;
		}

		public static void RetrieveImgurThumbnail(ImgurInfo imgurInfo) {
			if (imgurInfo.SmallSquare == null) {
				LOG.Warn("Imgur URL was null, not retrieving thumbnail.");
				return;
			}
			LOG.InfoFormat("Retrieving Imgur image for {0} with url {1}", imgurInfo.Hash, imgurInfo.SmallSquare);
			HttpWebRequest webRequest = (HttpWebRequest)NetworkHelper.CreateWebRequest(imgurInfo.SmallSquare);
			webRequest.Method = "GET";
			webRequest.ServicePoint.Expect100Continue = false;

			using (WebResponse response = webRequest.GetResponse()) {
				LogCredits(response);
				Stream responseStream = response.GetResponseStream();
				imgurInfo.Image = Image.FromStream(responseStream);
			}
			return;
		}

		public static ImgurInfo RetrieveImgurInfo(string hash, string deleteHash) {
			string url = config.ImgurApiUrl + "/image/" + hash;
			LOG.InfoFormat("Retrieving Imgur info for {0} with url {1}", hash, url);
			HttpWebRequest webRequest = (HttpWebRequest)NetworkHelper.CreateWebRequest(url);
			webRequest.Method = "GET";
			webRequest.ServicePoint.Expect100Continue = false;
			string responseString;
			try {
				using (WebResponse response = webRequest.GetResponse()) {
					LogCredits(response);
					Stream responseStream = response.GetResponseStream();
					StreamReader responseReader = new StreamReader(responseStream);
					responseString = responseReader.ReadToEnd();
				}
			} catch (WebException wE) {
				if (wE.Status == WebExceptionStatus.ProtocolError) {
					if (((HttpWebResponse)wE.Response).StatusCode == HttpStatusCode.NotFound) {
						return null;
					}
				}
				throw wE;
			}
			LOG.Info(responseString);
			ImgurInfo imgurInfo = ImgurInfo.ParseResponse(responseString);
			imgurInfo.DeleteHash = deleteHash;
			return imgurInfo;
		}

		public static void DeleteImgurImage(ImgurInfo imgurInfo) {
			LOG.InfoFormat("Deleting Imgur image for {0}", imgurInfo.DeleteHash);
			
			try {
				string url = config.ImgurApiUrl + "/delete/" + imgurInfo.DeleteHash;
				HttpWebRequest webRequest = (HttpWebRequest)NetworkHelper.CreateWebRequest(url);
	
				//webRequest.Method = "DELETE";
				webRequest.Method = "GET";
				webRequest.ServicePoint.Expect100Continue = false;
	
				string responseString;
				using (WebResponse response = webRequest.GetResponse()) {
					LogCredits(response);
					Stream responseStream = response.GetResponseStream();
					StreamReader responseReader = new StreamReader(responseStream);
					responseString = responseReader.ReadToEnd();
				}
				LOG.InfoFormat("Delete result: {0}", responseString);
			} catch (WebException wE) {
				// Allow "Bad request" this means we already deleted it
				if (wE.Status == WebExceptionStatus.ProtocolError) {
					if (((HttpWebResponse)wE.Response).StatusCode != HttpStatusCode.BadRequest) {
						throw wE;
					}
				}
			}
			// Make sure we remove it from the history, if no error occured
			config.runtimeImgurHistory.Remove(imgurInfo.Hash);
			config.ImgurUploadHistory.Remove(imgurInfo.Hash);
			imgurInfo.Image = null;
		}
		
		private static void LogCredits(WebResponse response) {
			try {
				int credits = 0;
				if (int.TryParse(response.Headers["X-RateLimit-Remaining"], out credits)) {
					config.Credits = credits;
				}
				LOG.InfoFormat("X-RateLimit-Limit={0}", response.Headers["X-RateLimit-Limit"]);
				LOG.InfoFormat("X-RateLimit-Remaining={0}", response.Headers["X-RateLimit-Remaining"]);
				
			} catch {}
		}
	}
}
