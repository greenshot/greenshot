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

using Greenshot.IniFile;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GreenshotImgurPlugin {
	/// <summary>
	/// Description of ImgurUtils.
	/// </summary>
	public static class ImgurUtils {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ImgurUtils));
		private static ImgurConfiguration config = IniConfig.GetIniSection<ImgurConfiguration>();
		private static readonly Uri IMGUR_IMAGES_URI = new Uri("http://api.imgur.com/2/account/images.json");
		private const string PAGE_URL_PATTERN = "http://imgur.com/{0}";
		private const string IMAGE_URL_PATTERN = "http://i.imgur.com/{0}.png";
		private const string SMALL_URL_PATTERN = "http://i.imgur.com/{0}s.png";
		private const string AuthUrlPattern = "https://api.imgur.com/oauth2/authorize?response_type=code&client_id={ClientId}&redirect_uri={RedirectUrl}&state={State}";
		private static readonly Uri TokenUrl = new Uri("https://api.imgur.com/oauth2/token");

		/// <summary>
		/// Load the complete history of the imgur uploads, with the corresponding information
		/// </summary>
		public static async Task LoadHistory(CancellationToken token = default(CancellationToken)) {
			if (config.runtimeImgurHistory.Count == config.ImgurUploadHistory.Count) {
				return;
			}
			// Load the ImUr history
			IList<string> hashes = new List<string>();
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
					var imgurInfo = await RetrieveImgurInfoAsync(hash, config.ImgurUploadHistory[hash]).ConfigureAwait(false);
					if (imgurInfo != null) {
						await RetrieveImgurThumbnailAsync(imgurInfo).ConfigureAwait(false);
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
		/// Do the actual upload to Picasa
		/// </summary>
		/// <param name="surfaceToUpload">Image to upload</param>
		/// <param name="outputSettings"></param>
		/// <param name="title"></param>
		/// <param name="filename"></param>
		/// <returns>PicasaResponse</returns>
		public static async Task<ImageInfo> AuthenticatedUploadToImgurAsync(ISurface surfaceToUpload, SurfaceOutputSettings outputSettings, IDictionary<string, string> otherParameters, CancellationToken token = default(CancellationToken)) {
			// Fill the OAuth2Settings
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
			oauth2Settings.RefreshToken = config.RefreshToken;
			oauth2Settings.AccessToken = config.AccessToken;
			oauth2Settings.AccessTokenExpires = config.AccessTokenExpires;

			try {
				DynamicJson imageJson;
				var uploadUri = new Uri(config.ApiUrl + "/upload.json").ExtendQuery(otherParameters);
				using (var httpClient = await OAuth2Helper.CreateOAuth2HttpClientAsync(uploadUri, oauth2Settings)) {
					using (var uploadStream = new MemoryStream()) {
						ImageOutput.SaveToStream(surfaceToUpload, uploadStream, outputSettings);
						uploadStream.Seek(0, SeekOrigin.Begin);
						using (var content = new StreamContent(uploadStream)) {
							content.Headers.Add("Content-Type", "image/" + outputSettings.Format);
							var responseMessage = await httpClient.PostAsync(uploadUri, content, token).ConfigureAwait(false);
							await responseMessage.HandleErrorAsync(token).ConfigureAwait(false);
							imageJson = await responseMessage.GetAsJsonAsync().ConfigureAwait(false);
						}
					}
				}
				return (ImageInfo)CreateImageInfo(imageJson);
			} finally {
				// Copy the settings back to the config, so they are stored.
				config.RefreshToken = oauth2Settings.RefreshToken;
				config.AccessToken = oauth2Settings.AccessToken;
				config.AccessTokenExpires = oauth2Settings.AccessTokenExpires;
				config.IsDirty = true;
				IniConfig.Save();
			}
		}

		private static async Task<ImageInfo> AnnonymousUploadToImgurAsync(ISurface surfaceToUpload, SurfaceOutputSettings outputSettings, IDictionary<string, string> otherParameters, CancellationToken token = default(CancellationToken)) {
			dynamic imageJson = null;
			var uploadUri = new Uri(config.ApiUrl + "/upload.json").ExtendQuery(otherParameters);
			using (var client = uploadUri.CreateHttpClient()) {
				client.SetAuthorization("Client-ID", ImgurCredentials.CONSUMER_KEY);
				client.DefaultRequestHeaders.ExpectContinue = false;
				using (var uploadStream = new MemoryStream()) {
					ImageOutput.SaveToStream(surfaceToUpload, uploadStream, outputSettings);
					uploadStream.Seek(0, SeekOrigin.Begin);
					using (var content = new StreamContent(uploadStream)) {
						content.Headers.Add("Content-Type", "image/" + outputSettings.Format);
						var response = await client.PostAsync(uploadUri, content, token).ConfigureAwait(false);
						await response.HandleErrorAsync(token).ConfigureAwait(false);
						imageJson = await response.GetAsJsonAsync().ConfigureAwait(false);
					}
				}
				return (ImageInfo)CreateImageInfo(imageJson);
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
		public static async Task<ImageInfo> UploadToImgurAsync(ISurface surfaceToUpload, SurfaceOutputSettings outputSettings, string title, string filename, CancellationToken token = default(CancellationToken)) {
			IDictionary<string, string> otherParameters = new Dictionary<string, string>();
			// add title
			if (title != null && config.AddTitle) {
				otherParameters.Add("title", title);
			}
			// add filename
			if (filename != null && config.AddFilename) {
				otherParameters.Add("name", filename);
			}
			ImageInfo imageInfo = null;
			if (config.AnonymousAccess) {
				imageInfo = await AnnonymousUploadToImgurAsync(surfaceToUpload, outputSettings, otherParameters, token).ConfigureAwait(false);
			} else {
				imageInfo = await AuthenticatedUploadToImgurAsync(surfaceToUpload, outputSettings, otherParameters, token).ConfigureAwait(false);
			}

			if (imageInfo != null && config.TrackHistory) {
				config.ImgurUploadHistory.Add(imageInfo.Id, imageInfo.DeleteHash);
				config.runtimeImgurHistory.Add(imageInfo.Id, imageInfo);
				using (Image tmpImage = surfaceToUpload.GetImageForExport()) {
					imageInfo.Image = ImageHelper.CreateThumbnail(tmpImage, 90, 90);
				}
			}

			return imageInfo;
		}

		/// <summary>
		/// Retrieve the thumbnail of an imgur image
		/// </summary>
		/// <param name="imgurInfo"></param>
		public static async Task RetrieveImgurThumbnailAsync(ImageInfo imgurInfo, CancellationToken token = default(CancellationToken)) {
			if (imgurInfo.SmallSquare == null) {
				LOG.Info("RetrieveImgurThumbnailAsync: Imgur URL was null, not retrieving thumbnail.");
				return;
			}
			LOG.InfoFormat("Retrieving Imgur image for {0} with url {1}", imgurInfo.Id, imgurInfo.SmallSquare);
			using (var client = imgurInfo.SmallSquare.CreateHttpClient()) {
				using (var response = await client.GetAsync(imgurInfo.SmallSquare, token).ConfigureAwait(false)) {
					await response.HandleErrorAsync(token).ConfigureAwait(false);
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
		/// <param name="id"></param>
		/// <param name="deleteHash"></param>
		/// <returns>ImgurInfo</returns>
		public static async Task<ImageInfo> RetrieveImgurInfoAsync(string id, string deleteHash, CancellationToken token = default(CancellationToken)) {
			Uri imageUri = new Uri(string.Format(config.ApiUrl + "/image/{0}.json", id));
			LOG.InfoFormat("Retrieving Imgur info for {0} with url {1}", id, imageUri);

			dynamic imageJson;
			using (var client = imageUri.CreateHttpClient()) {
				client.SetAuthorization("Client-ID", ImgurCredentials.CONSUMER_KEY);
				client.DefaultRequestHeaders.ExpectContinue = false;
				var response = await client.GetAsync(imageUri, token).ConfigureAwait(false);
				// retrieving image data seems to throw a 403 (Unauthorized) if it has been deleted
				if (response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Redirect || response.StatusCode == HttpStatusCode.Unauthorized) {
					return null;
				}
				await response.HandleErrorAsync(token).ConfigureAwait(false);
				imageJson = await response.GetAsJsonAsync().ConfigureAwait(false);
			}

			return (ImageInfo)CreateImageInfo(imageJson, deleteHash);
		}

		private static DateTimeOffset FromUnixTime(double secondsSince) {
			var epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
			return epoch.AddSeconds(secondsSince);
		}

		/// <summary>
		/// Helper method to parse the Json into a ImageInfo
		/// </summary>
		/// <param name="imageJson"></param>
		/// <param name="deleteHash"></param>
		/// <returns>filled ImageInfo</returns>
		private static ImageInfo CreateImageInfo(dynamic imageJson, string deleteHash = null) {
			ImageInfo imageInfo = null;
			if (imageJson != null && imageJson.IsDefined("data")) {
				dynamic data = imageJson.data;
				imageInfo = new ImageInfo {
					Id = data.IsDefined("id") ? data.id : null,
					DeleteHash = data.IsDefined("deletehash") ? data.deletehash : null,
					Title = data.IsDefined("title") ? data.title : null,
					Timestamp = data.IsDefined("datetime") ? FromUnixTime(data.datetime) : default(DateTimeOffset),
					Original = data.IsDefined("link") ? new Uri(data.link) : null,
					Page = data.IsDefined("id") ? new Uri(string.Format(PAGE_URL_PATTERN, data.id)) : null,
					SmallSquare = data.IsDefined("id") ? new Uri(string.Format(SMALL_URL_PATTERN, data.id)) : null
				};
				if (string.IsNullOrEmpty(imageInfo.DeleteHash)) {
					imageInfo.DeleteHash = deleteHash;
				}
			}
			return imageInfo;
		}

		/// <summary>
		/// Delete an imgur image, this is done by specifying the delete hash
		/// </summary>
		/// <param name="imgurInfo"></param>
		public static async Task<string> DeleteImgurImageAsync(ImageInfo imgurInfo, CancellationToken token = default(CancellationToken)) {
			LOG.InfoFormat("Deleting Imgur image for {0}", imgurInfo.DeleteHash);
			Uri deleteUri = new Uri(string.Format(config.ApiUrl + "/image/{0}", imgurInfo.DeleteHash));
			string responseString;
			using (var client = deleteUri.CreateHttpClient()) {
				client.SetAuthorization("Client-ID", ImgurCredentials.CONSUMER_KEY);
				client.DefaultRequestHeaders.ExpectContinue = false;
				var response = await client.DeleteAsync(deleteUri, token).ConfigureAwait(false);
				if (response.StatusCode != HttpStatusCode.NotFound && response.StatusCode != HttpStatusCode.BadRequest) {
					await response.HandleErrorAsync(token).ConfigureAwait(false);
				}
				responseString = await response.GetAsStringAsync().ConfigureAwait(false);
				LOG.InfoFormat("Delete result: {0}", responseString);
			}
			// Make sure we remove it from the history, if no error occured
			config.runtimeImgurHistory.Remove(imgurInfo.Id);
			config.ImgurUploadHistory.Remove(imgurInfo.Id);

			// dispose is called inside the imgurInfo object
			imgurInfo.Image = null;
			return responseString;
		}

		/// <summary>
		/// Retrieve the thumbnail of an imgur image
		/// </summary>
		/// <param name="imgurInfo"></param>
		public static async Task RetrieveImgurCredits(CancellationToken token = default(CancellationToken)) {
			Uri creditsUri = new Uri(string.Format("{0}/credits.json", config.ApiUrl));

			using (var client = creditsUri.CreateHttpClient()) {
				client.SetAuthorization("Client-ID", ImgurCredentials.CONSUMER_KEY);
				client.DefaultRequestHeaders.ExpectContinue = false;
				var response = await client.GetAsync(creditsUri, token).ConfigureAwait(false);
				await response.HandleErrorAsync(token).ConfigureAwait(false);
				var creditsJson = await response.GetAsJsonAsync().ConfigureAwait(false);
				if (creditsJson != null && creditsJson.IsDefined("data")) {
					dynamic data = creditsJson.data;
					int credits = 0;
					if (data.IsDefined("ClientRemaining")) {
						credits = (int)data.ClientRemaining;
						LOG.InfoFormat("{0}={1}", "ClientRemaining", (int)data.ClientRemaining);
					}
					if (data.IsDefined("UserRemaining")) {
						credits = Math.Min(credits, (int)data.UserRemaining);
						LOG.InfoFormat("{0}={1}", "UserRemaining", (int)data.UserRemaining);
					}
					config.Credits = credits;
				}
			}
		}
	}
}
