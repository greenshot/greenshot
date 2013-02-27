/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
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
		/// <param name="surfaceToUpload">ISurface to upload</param>
		/// <param name="outputSettings">OutputSettings for the image file format</param>
		/// <param name="title">Title</param>
		/// <param name="filename">Filename</param>
		/// <returns>ImgurInfo with details</returns>
		public static ImgurInfo UploadToImgur(ISurface surfaceToUpload, SurfaceOutputSettings outputSettings, string title, string filename) {
			IDictionary<string, object> uploadParameters = new Dictionary<string, object>();
			IDictionary<string, object> otherParameters = new Dictionary<string, object>();
			// add title
			if (title != null && config.AddTitle) {
				otherParameters.Add("title", title);
			}
			// add filename
			if (filename != null && config.AddFilename) {
				otherParameters.Add("name", filename);
			}
			string responseString = null;
			if (config.AnonymousAccess) {
				// add key, we only use the other parameters for the AnonymousAccess
				otherParameters.Add("key", IMGUR_ANONYMOUS_API_KEY);
				HttpWebRequest webRequest = (HttpWebRequest)NetworkHelper.CreateWebRequest(config.ImgurApiUrl + "/upload.xml?" + NetworkHelper.GenerateQueryParameters(otherParameters));
				webRequest.Method = "POST";
				webRequest.ContentType = "image/" + outputSettings.Format.ToString();
				webRequest.ServicePoint.Expect100Continue = false;
				try {
					using (var requestStream = webRequest.GetRequestStream()) {
						ImageOutput.SaveToStream(surfaceToUpload, requestStream, outputSettings);
					}
		
					using (WebResponse response = webRequest.GetResponse()) {
						using (StreamReader reader = new StreamReader(response.GetResponseStream(), true)) {
							responseString = reader.ReadToEnd();
						}
						LogCredits(response);
					}
				} catch (Exception ex) {
					LOG.Error("Upload to imgur gave an exeption: ", ex);
					throw;
				}
			} else {
				OAuthSession oAuth = new OAuthSession(ImgurCredentials.CONSUMER_KEY, ImgurCredentials.CONSUMER_SECRET);
				oAuth.BrowserSize = new Size(650, 500);
				oAuth.CallbackUrl = "http://getgreenshot.org";
				oAuth.AccessTokenUrl = "http://api.imgur.com/oauth/access_token";
				oAuth.AuthorizeUrl = "http://api.imgur.com/oauth/authorize";
				oAuth.RequestTokenUrl = "http://api.imgur.com/oauth/request_token";
				oAuth.LoginTitle = "Imgur authorization";
				oAuth.Token = config.ImgurToken;
				oAuth.TokenSecret = config.ImgurTokenSecret;
				if (string.IsNullOrEmpty(oAuth.Token)) {
					if (!oAuth.Authorize()) {
						return null;
					}
					if (!string.IsNullOrEmpty(oAuth.Token)) {
						config.ImgurToken = oAuth.Token;
					}
					if (!string.IsNullOrEmpty(oAuth.TokenSecret)) {
						config.ImgurTokenSecret = oAuth.TokenSecret;
					}
					IniConfig.Save();
				}
				try {
					otherParameters.Add("image", new SurfaceContainer(surfaceToUpload, outputSettings, filename));
					responseString = oAuth.MakeOAuthRequest(HTTPMethod.POST, "http://api.imgur.com/2/account/images.xml", uploadParameters, otherParameters, null);
				} catch (Exception ex) {
					LOG.Error("Upload to imgur gave an exeption: ", ex);
					throw;
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
			return ImgurInfo.ParseResponse(responseString);
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
