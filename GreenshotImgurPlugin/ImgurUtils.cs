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
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Linq;
using System.Threading;

namespace GreenshotImgurPlugin {
	/// <summary>
	/// Description of ImgurUtils.
	/// </summary>
	public static class ImgurUtils {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ImgurUtils));
		private const string IMGUR_ANONYMOUS_API_KEY = "8116a978913f3cf5dfc8e1117a055056";
		private static ImgurConfiguration config = IniConfig.GetIniSection<ImgurConfiguration>();
		private static readonly Uri IMGUR_IMAGES_URI = new Uri("http://api.imgur.com/2/account/images.xml");

		/// <summary>
		/// Load the complete history of the imgur uploads, with the corresponding information
		/// </summary>
		public static async Task LoadHistory(CancellationToken token = default(CancellationToken)) {
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
					ImgurInfo imgurInfo = await RetrieveImgurInfoAsync(hash, config.ImgurUploadHistory[hash]).ConfigureAwait(false);
					if (imgurInfo != null) {
						await ImgurUtils.RetrieveImgurThumbnailAsync(imgurInfo).ConfigureAwait(false);
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
		/// <param name="surfaceToUpload">ISurface to upload</param>
		/// <param name="outputSettings">OutputSettings for the image file format</param>
		/// <param name="title">Title</param>
		/// <param name="filename">Filename</param>
		/// <returns>ImgurInfo with details</returns>
		public static async Task<ImgurInfo> UploadToImgurAsync(ISurface surfaceToUpload, SurfaceOutputSettings outputSettings, string title, string filename, CancellationToken token = default(CancellationToken)) {
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

				var uploadUri = new Uri(config.ImgurApiUrl + "/upload.xml?" + NetworkHelper.GenerateQueryParameters(otherParameters));
				using (var client = uploadUri.CreateHttpClient()) {
					client.SetAuthorization("Client-ID", ImgurCredentials.CONSUMER_KEY);
					client.DefaultRequestHeaders.ExpectContinue = false;
					using (var uploadStream = new MemoryStream()) {
						ImageOutput.SaveToStream(surfaceToUpload, uploadStream, outputSettings);
						using (var content = new StreamContent(uploadStream)) {
							content.Headers.Add("Content-Type", "image/" + outputSettings.Format.ToString());
							var response = await client.PostAsync(uploadUri, content, token).ConfigureAwait(false);
							await response.HandleErrorAsync(token).ConfigureAwait(false);
							LogRateLimitInfo(response.Headers);
						}
					}
				}

			} else {
				OAuthSession oAuth = new OAuthSession(ImgurCredentials.CONSUMER_KEY, ImgurCredentials.CONSUMER_SECRET);
				oAuth.BrowserSize = new Size(650, 500);
				oAuth.CallbackUrl = new Uri("http://getgreenshot.org");
				oAuth.AccessTokenUrl = new Uri("http://api.imgur.com/oauth/access_token");
				oAuth.AuthorizeUrl = new Uri("http://api.imgur.com/oauth/authorize");
				oAuth.RequestTokenUrl = new Uri("http://api.imgur.com/oauth/request_token");
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
					responseString = oAuth.MakeOAuthRequest(HttpMethod.Post, IMGUR_IMAGES_URI, uploadParameters, otherParameters, null);
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

		/// <summary>
		/// Retrieve the thumbnail of an imgur image
		/// </summary>
		/// <param name="imgurInfo"></param>
		public static async Task RetrieveImgurThumbnailAsync(ImgurInfo imgurInfo, CancellationToken token = default(CancellationToken)) {
			if (imgurInfo.SmallSquare == null) {
				LOG.Info("RetrieveImgurThumbnailAsync: Imgur URL was null, not retrieving thumbnail.");
				return;
			}
			LOG.InfoFormat("Retrieving Imgur image for {0} with url {1}", imgurInfo.Hash, imgurInfo.SmallSquare);
			using (var client = imgurInfo.SmallSquare.CreateHttpClient()) {
				using (var response = await client.GetAsync(imgurInfo.SmallSquare, token).ConfigureAwait(false)) {
					await response.HandleErrorAsync(token).ConfigureAwait(false);
					LogRateLimitInfo(response.Headers);
					using (var stream = await response.GetAsMemoryStreamAsync(true, token).ConfigureAwait(false)) {
						using (var tmpImage = Image.FromStream(stream)) {
							if (tmpImage != null) {
								imgurInfo.Image = ImageHelper.Clone(tmpImage);
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Retrieve information on an imgur image
		/// </summary>
		/// <param name="hash"></param>
		/// <param name="deleteHash"></param>
		/// <returns>ImgurInfo</returns>
		public static async Task<ImgurInfo> RetrieveImgurInfoAsync(string hash, string deleteHash, CancellationToken token = default(CancellationToken)) {
			Uri imageUri = new Uri(config.ImgurApiUrl + "/image/" + hash);
			LOG.InfoFormat("Retrieving Imgur info for {0} with url {1}", hash, imageUri);

			string responseString;
			using (var client = imageUri.CreateHttpClient()) {
				client.SetAuthorization("Client-ID", ImgurCredentials.CONSUMER_KEY);
				client.DefaultRequestHeaders.ExpectContinue = false;
				var response = await client.GetAsync(imageUri, token).ConfigureAwait(false);
				if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Redirect) {
					return null;
				}
				await response.HandleErrorAsync(token).ConfigureAwait(false);
				LogRateLimitInfo(response.Headers);
				responseString = await response.GetAsStringAsync().ConfigureAwait(false);
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
		public static async Task<string> DeleteImgurImageAsync(ImgurInfo imgurInfo, CancellationToken token = default(CancellationToken)) {
			LOG.InfoFormat("Deleting Imgur image for {0}", imgurInfo.DeleteHash);
			Uri deleteUri = new Uri(config.ImgurApiUrl + "/delete/" + imgurInfo.DeleteHash);
			string responseString;
			using (var client = deleteUri.CreateHttpClient()) {
				client.SetAuthorization("Client-ID", ImgurCredentials.CONSUMER_KEY);
				client.DefaultRequestHeaders.ExpectContinue = false;
				var response = await client.GetAsync(deleteUri, token).ConfigureAwait(false);
				if (response.StatusCode != HttpStatusCode.NotFound && response.StatusCode != HttpStatusCode.BadRequest) {
					await response.HandleErrorAsync(token).ConfigureAwait(false);
				}
				LogRateLimitInfo(response.Headers);
				responseString = await response.GetAsStringAsync().ConfigureAwait(false);
				LOG.InfoFormat("Delete result: {0}", responseString);
			}
			// Make sure we remove it from the history, if no error occured
			config.runtimeImgurHistory.Remove(imgurInfo.Hash);
			config.ImgurUploadHistory.Remove(imgurInfo.Hash);

			// dispose is called inside the imgurInfo object
			imgurInfo.Image = null;
			return responseString;
		}

		/// <summary>
		/// Helper for logging
		/// </summary>
		/// <param name="nameValues"></param>
		/// <param name="key"></param>
		private static void LogHeader(HttpResponseHeaders headers, string key) {
			IEnumerable<string> values;
			if (headers.TryGetValues(key, out values)) {
				LOG.InfoFormat("{0}={1}", key, string.Join(",", values));
			}
		}

		/// <summary>
		/// Log the current rate-limit information
		/// </summary>
		/// <param name="response"></param>
		private static void LogRateLimitInfo(HttpResponseHeaders headers) {
			LogHeader(headers, "X-RateLimit-Limit");
			LogHeader(headers, "X-RateLimit-Remaining");
			LogHeader(headers, "X-RateLimit-UserLimit");
			LogHeader(headers, "X-RateLimit-UserRemaining");
			LogHeader(headers, "X-RateLimit-UserReset");
			LogHeader(headers, "X-RateLimit-ClientLimit");
			LogHeader(headers, "X-RateLimit-ClientRemaining");

			// Update the credits in the config, this is shown in a form
			int credits = 0;
			IEnumerable<string> values;
			if (headers.TryGetValues("X-RateLimit-Remaining", out values)) {
				if (int.TryParse(values.First(), out credits)) {
					config.Credits = credits;
				}
			}
		}
	}
}
