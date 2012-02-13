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
using IniFile;

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
		public static ImgurInfo UploadToImgur(byte[] imageData, string title, string filename) {
			StringBuilder uploadRequest = new StringBuilder();
			// Add image
			uploadRequest.Append("image=");
			uploadRequest.Append(HttpUtility.UrlEncode(System.Convert.ToBase64String(imageData)));
			// add key
			uploadRequest.Append("&");
			uploadRequest.Append("key=");
			uploadRequest.Append(IMGUR_ANONYMOUS_API_KEY);
			// add title
			if (title != null) {
				uploadRequest.Append("&");
				uploadRequest.Append("title=");
				uploadRequest.Append(HttpUtility.UrlEncode(title, Encoding.UTF8));
			}
			// add filename
			if (filename != null) {
				uploadRequest.Append("&");
				uploadRequest.Append("name=");
				uploadRequest.Append(HttpUtility.UrlEncode(filename, Encoding.UTF8));
			}
			string url = config.ImgurApiUrl + "/upload";
			HttpWebRequest webRequest = (HttpWebRequest)NetworkHelper.CreatedWebRequest(url);

			webRequest.Method = "POST";
			webRequest.ContentType = "application/x-www-form-urlencoded";
			webRequest.ServicePoint.Expect100Continue = false;
			
			using(StreamWriter streamWriter = new StreamWriter(webRequest.GetRequestStream())) {
				streamWriter.Write(uploadRequest.ToString());
			}
			string responseString;
			using (WebResponse response = webRequest.GetResponse()) {
				Stream responseStream = response.GetResponseStream();
				StreamReader responseReader = new StreamReader(responseStream);
				responseString = responseReader.ReadToEnd();
			}
			LOG.Info(responseString);
			ImgurInfo imgurInfo = ImgurInfo.ParseResponse(responseString);
			return imgurInfo;
        }

		public static void RetrieveImgurThumbnail(ImgurInfo imgurInfo) {
			LOG.InfoFormat("Retrieving Imgur image for {0} with url {1}", imgurInfo.Hash, imgurInfo);
			HttpWebRequest webRequest = (HttpWebRequest)NetworkHelper.CreatedWebRequest(imgurInfo.SmallSquare);
			webRequest.Method = "GET";
			webRequest.ServicePoint.Expect100Continue = false;

			using (WebResponse response = webRequest.GetResponse()) {
				Stream responseStream = response.GetResponseStream();
				imgurInfo.Image = Image.FromStream(responseStream);
			}
			return;
		}

		public static ImgurInfo RetrieveImgurInfo(string hash, string deleteHash) {
			string url = config.ImgurApiUrl + "/image/" + hash;
			LOG.InfoFormat("Retrieving Imgur info for {0} with url {1}", hash, url);
			HttpWebRequest webRequest = (HttpWebRequest)NetworkHelper.CreatedWebRequest(url);
			webRequest.Method = "GET";
			webRequest.ServicePoint.Expect100Continue = false;
			string responseString;
			try {
				using (WebResponse response = webRequest.GetResponse()) {
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
				HttpWebRequest webRequest = (HttpWebRequest)NetworkHelper.CreatedWebRequest(url);
	
				//webRequest.Method = "DELETE";
				webRequest.Method = "GET";
				webRequest.ServicePoint.Expect100Continue = false;
	
				string responseString;
				using (WebResponse response = webRequest.GetResponse()) {
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
	}
}
