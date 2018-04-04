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
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Dapplo.Ini;
using Dapplo.HttpExtensions;
using Dapplo.HttpExtensions.Factory;
using Dapplo.HttpExtensions.JsonNet;
using Dapplo.HttpExtensions.OAuth;
using Dapplo.Log;
using Dapplo.Utils;
using Greenshot.Addon.Imgur.Entities;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;
using OAuth2Settings = Dapplo.HttpExtensions.OAuth.OAuth2Settings;

#endregion

namespace Greenshot.Addon.Imgur
{
	/// <summary>
	///     Description of ImgurUtils.
	/// </summary>
	public static class ImgurUtils
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly IImgurConfiguration _imgurConfiguration = IniConfig.Current.Get<IImgurConfiguration>();

		private static readonly HttpBehaviour Behaviour = new HttpBehaviour
		{
            JsonSerializer = new JsonNetJsonSerializer(),
			OnHttpClientCreated = httpClient =>
			{
				httpClient.SetAuthorization("Client-ID", _imgurConfiguration.ClientId);
				httpClient.DefaultRequestHeaders.ExpectContinue = false;
			}
		};

		private static async Task<ImgurImage> AnnonymousUploadToImgurAsync(ISurface surfaceToUpload, SurfaceOutputSettings outputSettings, IDictionary<string, string> otherParameters, IProgress<int> progress = null, CancellationToken token = default)
		{
			var uploadUri = new Uri(_imgurConfiguration.ApiUrl).AppendSegments("upload.json").ExtendQuery(otherParameters);
			var localBehaviour = Behaviour.ShallowClone();
		    if (progress != null)
		    {
		        localBehaviour.UploadProgress = percent => { UiContext.RunOn(() => progress.Report((int)(percent * 100)), token); };
            }

            using (var imageStream = new MemoryStream())
			{
			    ImageOutput.SaveToStream(surfaceToUpload, imageStream, outputSettings);
				imageStream.Position = 0;
				using (var content = new StreamContent(imageStream))
				{
					content.Headers.Add("Content-Type", "image/" + outputSettings.Format);
					localBehaviour.MakeCurrent();
					return await uploadUri.PostAsync<ImgurImage>(content, token).ConfigureAwait(false);
				}
			}
		}

        /// <summary>
        ///     Do the actual upload to Picasa
        /// </summary>
        /// <param name="oAuth2Settings">OAuth2Settings</param>
        /// <param name="surfaceToUpload">ICapture</param>
        /// <param name="outputSettings">SurfaceOutputSettings</param>
        /// <param name="otherParameters">IDictionary</param>
        /// <param name="progress">IProgress</param>
        /// <param name="token"></param>
        /// <returns>PicasaResponse</returns>
        public static async Task<ImgurImage> AuthenticatedUploadToImgurAsync(OAuth2Settings oAuth2Settings, ISurface surfaceToUpload, SurfaceOutputSettings outputSettings, IDictionary<string, string> otherParameters, IProgress<int> progress = null, CancellationToken token = default)
		{
			var uploadUri = new Uri(_imgurConfiguration.ApiUrl).AppendSegments("upload.json").ExtendQuery(otherParameters);
			var localBehaviour = Behaviour.ShallowClone();
		    if (progress != null)
		    {
		        localBehaviour.UploadProgress = percent => { UiContext.RunOn(() => progress.Report((int)(percent * 100)), token); };
            }
            var oauthHttpBehaviour = OAuth2HttpBehaviourFactory.Create(oAuth2Settings, localBehaviour);
			using (var imageStream = new MemoryStream())
			{
			    ImageOutput.SaveToStream(surfaceToUpload, imageStream, outputSettings);
				imageStream.Position = 0;
				using (var content = new StreamContent(imageStream))
				{
					content.Headers.Add("Content-Type", "image/" + outputSettings.Format);
					oauthHttpBehaviour.MakeCurrent();
					return await uploadUri.PostAsync<ImgurImage>(content, token);
				}
			}
		}


		/// <summary>
		///     Delete an imgur image, this is done by specifying the delete hash
		/// </summary>
		/// <param name="imgurImage"></param>
		/// <param name="token"></param>
		public static async Task<string> DeleteImgurImageAsync(ImgurImage imgurImage, CancellationToken token = default)
		{
			Log.Info().WriteLine("Deleting Imgur image for {0}", imgurImage.Data.Deletehash);
			var deleteUri = new Uri(string.Format(_imgurConfiguration.ApiUrl + "/image/{0}", imgurImage.Data.Deletehash));
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
			_imgurConfiguration.RuntimeImgurHistory.Remove(imgurImage.Data.Id);
			_imgurConfiguration.ImgurUploadHistory.Remove(imgurImage.Data.Id);

			// dispose is called inside the imgurInfo object
			imgurImage.Image = null;
			return responseString;
		}

		/// <summary>
		///     Retrieve the thumbnail of an imgur image
		/// </summary>
		/// <param name="token"></param>
		public static async Task RetrieveImgurCredits(CancellationToken token = default)
		{
			var creditsUri = new Uri($"{_imgurConfiguration.ApiUrl}/credits.json");
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
					_imgurConfiguration.Credits = credits;
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
		public static async Task<ImgurImage> RetrieveImgurInfoAsync(string id, string deleteHash, CancellationToken token = default)
		{
			var imageUri = new Uri(string.Format(_imgurConfiguration.ApiUrl + "/image/{0}.json", id));
			Log.Info().WriteLine("Retrieving Imgur info for {0} with url {1}", id, imageUri);

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
				return await response.GetAsAsync<ImgurImage>(token).ConfigureAwait(false);
			}
		}

        /// <summary>
        ///     Retrieve the thumbnail of an imgur image
        /// </summary>
        /// <param name="imgurImage">ImgurImage</param>
        /// <param name="token">CancellationToken</param>
        public static async Task RetrieveImgurThumbnailAsync(ImgurImage imgurImage, CancellationToken token = default)
		{
		    if (imgurImage.Image != null)
		    {
		        Log.Info().WriteLine("Imgur thumbnail already available.");
		        return;
		    }
            if (imgurImage.Data.LinkThumbnail == null)
			{
				Log.Info().WriteLine("Imgur URL was null, not retrieving thumbnail.");
				return;
			}
			Log.Info().WriteLine("Retrieving Imgur image for {0} with url {1}", imgurImage.Data.Id, imgurImage.Data.LinkThumbnail);
			Behaviour.MakeCurrent();
			using (var client = HttpClientFactory.Create(imgurImage.Data.LinkThumbnail))
			{
				using (var response = await client.GetAsync(imgurImage.Data.LinkThumbnail, token).ConfigureAwait(false))
				{
					await response.HandleErrorAsync().ConfigureAwait(false);
				    imgurImage.Image = await response.GetAsAsync<BitmapSource>(token).ConfigureAwait(false);
				}
			}
		}

        /// <summary>
        ///     Do the actual upload to Imgur
        ///     For more details on the available parameters, see: http://api.imgur.com/resources_anon
        /// </summary>
        /// <param name="oAuth2Settings">OAuth2Settings</param>
        /// <param name="surfaceToUpload">ISurface to upload</param>
        /// <param name="outputSettings">OutputSettings for the image file format</param>
        /// <param name="title">Title</param>
        /// <param name="filename">Filename</param>
        /// <param name="progress">IProgress</param>
        /// <param name="token">CancellationToken</param>
        /// <returns>ImgurInfo with details</returns>
        public static async Task<ImgurImage> UploadToImgurAsync(OAuth2Settings oAuth2Settings, ISurface surfaceToUpload, SurfaceOutputSettings outputSettings, string title, string filename, IProgress<int> progress = null, CancellationToken token = default)
		{
			IDictionary<string, string> otherParameters = new Dictionary<string, string>();
			// add title
			if ((title != null) && _imgurConfiguration.AddTitle)
			{
				otherParameters.Add("title", title);
			}
			// add filename
			if ((filename != null) && _imgurConfiguration.AddFilename)
			{
				otherParameters.Add("name", filename);
			}
			ImgurImage imgurImage;
			if (_imgurConfiguration.AnonymousAccess)
			{
				imgurImage = await AnnonymousUploadToImgurAsync(surfaceToUpload, outputSettings, otherParameters, progress, token).ConfigureAwait(false);
			}
			else
			{
				imgurImage = await AuthenticatedUploadToImgurAsync(oAuth2Settings, surfaceToUpload, outputSettings, otherParameters, progress, token);
			}
            return imgurImage;
		}
	}
}