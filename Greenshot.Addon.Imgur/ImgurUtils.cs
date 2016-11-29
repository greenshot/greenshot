//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Config.Ini;
using Dapplo.HttpExtensions;
using Dapplo.HttpExtensions.Factory;
using Dapplo.HttpExtensions.OAuth;
using Dapplo.Log;
using Dapplo.Utils;
using Greenshot.Addon.Core;
using Greenshot.Addon.Extensions;
using Greenshot.Addon.Interfaces;
using Greenshot.CaptureCore.Extensions;
using Greenshot.Core;
using Greenshot.Core.Gfx;
using Greenshot.Core.Interfaces;
using Greenshot.Legacy.Extensions;

#endregion

namespace Greenshot.Addon.Imgur
{
	/// <summary>
	///     Description of ImgurUtils.
	/// </summary>
	public static class ImgurUtils
	{
		private const string PageUrlPattern = "http://imgur.com/{0}";
		private const string SmallUrlPattern = "http://i.imgur.com/{0}s.png";
		private static readonly LogSource Log = new LogSource();
		private static readonly IImgurConfiguration Config = IniConfig.Current.Get<IImgurConfiguration>();

		private static readonly HttpBehaviour Behaviour = new HttpBehaviour
		{
			OnHttpClientCreated = httpClient =>
			{
				httpClient.SetAuthorization("Client-ID", Config.ClientId);
				httpClient.DefaultRequestHeaders.ExpectContinue = false;
			}
		};

		private static async Task<ImageInfo> AnnonymousUploadToImgurAsync(ICapture surfaceToUpload, SurfaceOutputSettings outputSettings, IDictionary<string, string> otherParameters, IProgress<int> progress, CancellationToken token = default(CancellationToken))
		{
			var uploadUri = new Uri(Config.ApiUrl).AppendSegments("upload.json").ExtendQuery(otherParameters);
			dynamic imageJson;
			var localBehaviour = Behaviour.ShallowClone();
			localBehaviour.UploadProgress = percent => { UiContext.RunOn(() => progress.Report((int) (percent*100))); };

			using (var imageStream = new MemoryStream())
			{
				surfaceToUpload.SaveToStream(imageStream, outputSettings);
				imageStream.Position = 0;
				using (var content = new StreamContent(imageStream))
				{
					content.Headers.Add("Content-Type", "image/" + outputSettings.Format);
					localBehaviour.MakeCurrent();
					imageJson = await uploadUri.PostAsync<dynamic>(content, token).ConfigureAwait(false);
				}
			}
			return CreateImageInfo(imageJson);
		}

		/// <summary>
		///     Do the actual upload to Picasa
		/// </summary>
		/// <param name="oAuth2Settings">OAuth2Settings</param>
		/// <param name="surfaceToUpload">ICapture</param>
		/// <param name="outputSettings">SurfaceOutputSettings</param>
		/// <param name="otherParameters">IDictionary</param>
		/// <param name="progress"></param>
		/// <param name="token"></param>
		/// <returns>PicasaResponse</returns>
		public static async Task<ImageInfo> AuthenticatedUploadToImgurAsync(OAuth2Settings oAuth2Settings, ICapture surfaceToUpload, SurfaceOutputSettings outputSettings, IDictionary<string, string> otherParameters, IProgress<int> progress, CancellationToken token = default(CancellationToken))
		{
			dynamic imageJson;
			var uploadUri = new Uri(Config.ApiUrl).AppendSegments("upload.json").ExtendQuery(otherParameters);
			var localBehaviour = Behaviour.ShallowClone();
			localBehaviour.UploadProgress = percent => { UiContext.RunOn(() => progress.Report((int) (percent*100))); };
			var oauthHttpBehaviour = OAuth2HttpBehaviourFactory.Create(oAuth2Settings, localBehaviour);
			using (var imageStream = new MemoryStream())
			{
				surfaceToUpload.SaveToStream(imageStream, outputSettings);
				imageStream.Position = 0;
				using (var content = new StreamContent(imageStream))
				{
					content.Headers.Add("Content-Type", "image/" + outputSettings.Format);
					oauthHttpBehaviour.MakeCurrent();
					imageJson = await uploadUri.PostAsync<dynamic>(content, token);
				}
			}
			return CreateImageInfo(imageJson);
		}

		/// <summary>
		///     Helper method to parse the Json into a ImageInfo
		/// </summary>
		/// <param name="imageJson"></param>
		/// <param name="deleteHash"></param>
		/// <returns>filled ImageInfo</returns>
		private static ImageInfo CreateImageInfo(dynamic imageJson, string deleteHash = null)
		{
			ImageInfo imageInfo = null;
			if (imageJson == null)
			{
				return null;
			}
			if (imageJson.ContainsKey("data"))
			{
				var data = imageJson.data;
				imageInfo = new ImageInfo
				{
					Id = data.ContainsKey("id") ? data.id : null,
					DeleteHash = data.ContainsKey("deletehash") ? data.deletehash : null,
					Title = data.ContainsKey("title") ? data.title : null,
					Timestamp = data.ContainsKey("datetime") ? FromUnixTime(data.datetime) : default(DateTimeOffset),
					Original = data.ContainsKey("link") ? new Uri(data.link) : null,
					Page = data.ContainsKey("id") ? new Uri(string.Format(PageUrlPattern, data.id)) : null,
					SmallSquare = data.ContainsKey("id") ? new Uri(string.Format(SmallUrlPattern, data.id)) : null
				};
				if (string.IsNullOrEmpty(imageInfo.DeleteHash))
				{
					imageInfo.DeleteHash = deleteHash;
				}
			}
			return imageInfo;
		}

		/// <summary>
		///     Delete an imgur image, this is done by specifying the delete hash
		/// </summary>
		/// <param name="imgurInfo"></param>
		/// <param name="token"></param>
		public static async Task<string> DeleteImgurImageAsync(ImageInfo imgurInfo, CancellationToken token = default(CancellationToken))
		{
			Log.Info().WriteLine("Deleting Imgur image for {0}", imgurInfo.DeleteHash);
			Uri deleteUri = new Uri(string.Format(Config.ApiUrl + "/image/{0}", imgurInfo.DeleteHash));
			string responseString;
			Behaviour.MakeCurrent();
			using (var client = HttpClientFactory.Create(deleteUri))
			{
				var response = await client.DeleteAsync(deleteUri, token).ConfigureAwait(false);
				if ((response.StatusCode != HttpStatusCode.NotFound) && (response.StatusCode != HttpStatusCode.BadRequest))
				{
					await response.HandleErrorAsync().ConfigureAwait(false);
				}
				responseString = await response.GetAsAsync<string>(token).ConfigureAwait(false);
				Log.Info().WriteLine("Delete result: {0}", responseString);
			}
			// Make sure we remove it from the history, if no error occured
			Config.RuntimeImgurHistory.Remove(imgurInfo.Id);
			Config.ImgurUploadHistory.Remove(imgurInfo.Id);

			// dispose is called inside the imgurInfo object
			imgurInfo.Image = null;
			return responseString;
		}

		private static DateTimeOffset FromUnixTime(double secondsSince)
		{
			var epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);
			return epoch.AddSeconds(secondsSince);
		}

		/// <summary>
		///     Retrieve the thumbnail of an imgur image
		/// </summary>
		/// <param name="token"></param>
		public static async Task RetrieveImgurCredits(CancellationToken token = default(CancellationToken))
		{
			var creditsUri = new Uri(string.Format("{0}/credits.json", Config.ApiUrl));
			Behaviour.MakeCurrent();
			using (var client = HttpClientFactory.Create(creditsUri))
			{
				var response = await client.GetAsync(creditsUri, token).ConfigureAwait(false);
				await response.HandleErrorAsync().ConfigureAwait(false);
				var creditsJson = await response.GetAsAsync<dynamic>(token).ConfigureAwait(false);
				if ((creditsJson != null) && creditsJson.ContainsKey("data"))
				{
					dynamic data = creditsJson.data;
					int credits = 0;
					if (data.ContainsKey("ClientRemaining"))
					{
						credits = (int) data.ClientRemaining;
						Log.Info().WriteLine("{0}={1}", "ClientRemaining", (int) data.ClientRemaining);
					}
					if (data.ContainsKey("UserRemaining"))
					{
						credits = Math.Min(credits, (int) data.UserRemaining);
						Log.Info().WriteLine("{0}={1}", "UserRemaining", (int) data.UserRemaining);
					}
					Config.Credits = credits;
				}
			}
		}

		/// <summary>
		///     Retrieve information on an imgur image
		/// </summary>
		/// <param name="id"></param>
		/// <param name="deleteHash"></param>
		/// <param name="token"></param>
		/// <returns>ImgurInfo</returns>
		public static async Task<ImageInfo> RetrieveImgurInfoAsync(string id, string deleteHash, CancellationToken token = default(CancellationToken))
		{
			var imageUri = new Uri(string.Format(Config.ApiUrl + "/image/{0}.json", id));
			Log.Info().WriteLine("Retrieving Imgur info for {0} with url {1}", id, imageUri);

			dynamic imageJson;
			Behaviour.MakeCurrent();
			using (var client = HttpClientFactory.Create(imageUri))
			{
				var response = await client.GetAsync(imageUri, token).ConfigureAwait(false);
				// retrieving image data seems to throw a 403 (Unauthorized) if it has been deleted
				if ((response.StatusCode == HttpStatusCode.NotFound) || (response.StatusCode == HttpStatusCode.Redirect) || (response.StatusCode == HttpStatusCode.Unauthorized))
				{
					return null;
				}
				await response.HandleErrorAsync().ConfigureAwait(false);
				imageJson = await response.GetAsAsync<dynamic>(token).ConfigureAwait(false);
			}

			return CreateImageInfo(imageJson, deleteHash);
		}

		/// <summary>
		///     Retrieve the thumbnail of an imgur image
		/// </summary>
		/// <param name="imgurInfo"></param>
		/// <param name="token"></param>
		public static async Task RetrieveImgurThumbnailAsync(ImageInfo imgurInfo, CancellationToken token = default(CancellationToken))
		{
			if (imgurInfo.SmallSquare == null)
			{
				Log.Info().WriteLine("RetrieveImgurThumbnailAsync: Imgur URL was null, not retrieving thumbnail.");
				return;
			}
			Log.Info().WriteLine("Retrieving Imgur image for {0} with url {1}", imgurInfo.Id, imgurInfo.SmallSquare);
			Behaviour.MakeCurrent();
			using (var client = HttpClientFactory.Create(imgurInfo.SmallSquare))
			{
				using (var response = await client.GetAsync(imgurInfo.SmallSquare, token).ConfigureAwait(false))
				{
					await response.HandleErrorAsync().ConfigureAwait(false);
					using (var stream = await response.GetAsAsync<MemoryStream>(token).ConfigureAwait(false))
					{
						using (var tmpImage = Image.FromStream(stream))
						{
							imgurInfo.Image = ImageHelper.Clone(tmpImage);
						}
					}
				}
			}
		}

		/// <summary>
		///     Do the actual upload to Imgur
		///     For more details on the available parameters, see: http://api.imgur.com/resources_anon
		/// </summary>
		/// <param name="oAuth2Settings"></param>
		/// <param name="surfaceToUpload">ISurface to upload</param>
		/// <param name="outputSettings">OutputSettings for the image file format</param>
		/// <param name="title">Title</param>
		/// <param name="filename">Filename</param>
		/// <param name="progress"></param>
		/// <param name="token"></param>
		/// <returns>ImgurInfo with details</returns>
		public static async Task<ImageInfo> UploadToImgurAsync(OAuth2Settings oAuth2Settings, ICapture surfaceToUpload, SurfaceOutputSettings outputSettings, string title, string filename, IProgress<int> progress, CancellationToken token = default(CancellationToken))
		{
			IDictionary<string, string> otherParameters = new Dictionary<string, string>();
			// add title
			if ((title != null) && Config.AddTitle)
			{
				otherParameters.Add("title", title);
			}
			// add filename
			if ((filename != null) && Config.AddFilename)
			{
				otherParameters.Add("name", filename);
			}
			ImageInfo imageInfo;
			if (Config.AnonymousAccess)
			{
				imageInfo = await AnnonymousUploadToImgurAsync(surfaceToUpload, outputSettings, otherParameters, progress, token).ConfigureAwait(false);
			}
			else
			{
				imageInfo = await AuthenticatedUploadToImgurAsync(oAuth2Settings, surfaceToUpload, outputSettings, otherParameters, progress, token).ConfigureAwait(false);
			}

			if ((imageInfo != null) && Config.TrackHistory)
			{
				Config.ImgurUploadHistory.Add(imageInfo.Id, imageInfo.DeleteHash);
				Config.RuntimeImgurHistory.Add(imageInfo.Id, imageInfo);
				using (var tmpImage = surfaceToUpload.GetImageForExport())
				{
					imageInfo.Image = ImageHelper.CreateThumbnail(tmpImage, 90, 90);
				}
			}

			return imageInfo;
		}
	}
}