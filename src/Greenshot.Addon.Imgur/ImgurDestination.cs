#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Dapplo.Addons.Bootstrapper.Resolving;
using Dapplo.HttpExtensions;
using Dapplo.HttpExtensions.Factory;
using Dapplo.HttpExtensions.JsonNet;
using Dapplo.HttpExtensions.OAuth;
using Dapplo.Log;
using Dapplo.Utils;
using Dapplo.Windows.Extensions;
using Greenshot.Addon.Imgur.Entities;
using Greenshot.Addon.Imgur.ViewModels;
using Greenshot.Addons.Addons;
using Greenshot.Addons.Controls;
using Greenshot.Addons.Core;
using Greenshot.Addons.Extensions;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;
using Greenshot.Gfx;

#endregion

namespace Greenshot.Addon.Imgur
{
    /// <summary>
    ///     Description of ImgurDestination.
    /// </summary>
    [Destination("Imgur")]
    public class ImgurDestination : AbstractDestination
	{
	    private static readonly LogSource Log = new LogSource();
        private readonly IImgurConfiguration _imgurConfiguration;
	    private readonly IImgurLanguage _imgurLanguage;
	    private readonly ImgurHistoryViewModel _imgurHistoryViewModel;
	    private readonly Dapplo.HttpExtensions.OAuth.OAuth2Settings _oauth2Settings;

	    private HttpBehaviour Behaviour { get; }

        [ImportingConstructor]
		public ImgurDestination(IImgurConfiguration imgurConfiguration, IImgurLanguage imgurLanguage, ImgurHistoryViewModel imgurHistoryViewModel)
		{
			_imgurConfiguration = imgurConfiguration;
		    _imgurLanguage = imgurLanguage;
		    _imgurHistoryViewModel = imgurHistoryViewModel;
		    // Configure the OAuth2 settings for Imgur communication
		    _oauth2Settings = new Dapplo.HttpExtensions.OAuth.OAuth2Settings
            {
		        AuthorizationUri = new Uri("https://api.imgur.com").AppendSegments("oauth2", "authorize").
		            ExtendQuery(new Dictionary<string, string>
		            {
		                {"response_type", "code"},
		                {"client_id", "{ClientId}"},
		                {"redirect_uri", "{RedirectUrl}"},
		                {"state", "{State}"}
		            }),
		        TokenUrl = new Uri("https://api.imgur.com/oauth2/token"),
		        CloudServiceName = "Imgur",
		        ClientId = imgurConfiguration.ClientId,
		        ClientSecret = imgurConfiguration.ClientSecret,
		        RedirectUrl = "http://getgreenshot.org",
		        AuthorizeMode = AuthorizeModes.EmbeddedBrowser,
		        Token = imgurConfiguration
            };

		    Behaviour = new HttpBehaviour
		    {
		        JsonSerializer = new JsonNetJsonSerializer(),
		        OnHttpClientCreated = httpClient =>
		        {
		            httpClient.SetAuthorization("Client-ID", _imgurConfiguration.ClientId);
		            httpClient.DefaultRequestHeaders.ExpectContinue = false;
		        }
		    };
        }

		public override string Description => _imgurLanguage.UploadMenuItem;

		public override Bitmap DisplayIcon
		{
			get
			{
			    // TODO: Optimize this
			    var embeddedResource = GetType().Assembly.FindEmbeddedResources(@".*Imgur\.png").FirstOrDefault();
			    using (var bitmapStream = GetType().Assembly.GetEmbeddedResourceAsStream(embeddedResource))
			    {
			        return BitmapHelper.FromStream(bitmapStream);
			    }
            }
		}

		public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
		    var uploadUrl = await Upload(captureDetails, surface).ConfigureAwait(true);

            var exportInformation = new ExportInformation(Designation, Description)
		    {
		        ExportMade = uploadUrl != null,
		        Uri = uploadUrl?.AbsoluteUri
		    };
		    ProcessExport(exportInformation, surface);
			return exportInformation;
		}

        /// <summary>
        /// Upload the capture to imgur
        /// </summary>
        /// <param name="captureDetails">ICaptureDetails</param>
        /// <param name="surfaceToUpload">ISurface</param>
        /// <returns>Uri</returns>
        private async Task<Uri> Upload(ICaptureDetails captureDetails, ISurface surfaceToUpload)
        {
            var outputSettings = new SurfaceOutputSettings(_imgurConfiguration.OutputFileFormat, _imgurConfiguration.OutputFileJpegQuality, _imgurConfiguration.OutputFileReduceColors);
            try
            {
                ImgurImage imgurImage;

                var cancellationTokenSource = new CancellationTokenSource();
                // TODO: Replace the form
                using (var pleaseWaitForm = new PleaseWaitForm("Imgur", _imgurLanguage.CommunicationWait, cancellationTokenSource))
                {
                    pleaseWaitForm.Show();
                    try
                    {
                        imgurImage = await UploadToImgurAsync(_oauth2Settings, surfaceToUpload, captureDetails.Title, null, cancellationTokenSource.Token).ConfigureAwait(true);
                        if (imgurImage != null)
                        {
                            // Create thumbnail
                            using (var tmpImage = surfaceToUpload.GetBitmapForExport())
                            using (var thumbnail = tmpImage.CreateThumbnail(90, 90))
                            {
                                imgurImage.Image = thumbnail.ToBitmapSource();
                            }
                            if (_imgurConfiguration.AnonymousAccess && _imgurConfiguration.TrackHistory)
                            {
                                Log.Info().WriteLine("Storing imgur upload for hash {0} and delete hash {1}", imgurImage.Data.Id, imgurImage.Data.Deletehash);
                                _imgurConfiguration.ImgurUploadHistory.Add(imgurImage.Data.Id, imgurImage.Data.Deletehash);
                                _imgurConfiguration.RuntimeImgurHistory.Add(imgurImage.Data.Id, imgurImage);

                                // Update history
                                _imgurHistoryViewModel.ImgurHistory.Add(imgurImage);
                            }
                        }
                    }
                    finally
                    {
                        pleaseWaitForm.Close();
                    }
                }

                if (imgurImage != null)
                {
                    var uploadUrl = _imgurConfiguration.UsePageLink ? imgurImage.Data.LinkPage: imgurImage.Data.Link;
                    if (uploadUrl == null || !_imgurConfiguration.CopyLinkToClipboard)
                    {
                        return uploadUrl;
                    }

                    try
                    {
                        ClipboardHelper.SetClipboardData(uploadUrl.AbsoluteUri);

                    }
                    catch (Exception ex)
                    {
                        Log.Error().WriteLine(ex, "Can't write to clipboard: ");
                        uploadUrl = null;
                    }
                    return uploadUrl;
                }
            }
            catch (Exception e)
            {
                Log.Error().WriteLine(e, "Error uploading.");
                MessageBox.Show(_imgurLanguage.UploadFailure + " " + e.Message);
            }
            return null;
        }

        // Used for image downloading
        private HttpBehaviour ImageHttpBehaviour { get; } = new HttpBehaviour{};

        private async Task<ImgurImage> AnnonymousUploadToImgurAsync(ISurface surfaceToUpload, SurfaceOutputSettings outputSettings, IDictionary<string, string> otherParameters, IProgress<int> progress = null, CancellationToken token = default)
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
        public async Task<ImgurImage> AuthenticatedUploadToImgurAsync(OAuth2Settings oAuth2Settings, ISurface surfaceToUpload, SurfaceOutputSettings outputSettings, IDictionary<string, string> otherParameters, IProgress<int> progress = null, CancellationToken token = default)
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
                    return await uploadUri.PostAsync<ImgurImage>(content, token).ConfigureAwait(false);
                }
            }
        }


        /// <summary>
        ///     Delete an imgur image, this is done by specifying the delete hash
        /// </summary>
        /// <param name="imgurImage"></param>
        /// <param name="token"></param>
        public async Task<string> DeleteImgurImageAsync(ImgurImage imgurImage, CancellationToken token = default)
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
        public async Task RetrieveImgurCredits(CancellationToken token = default)
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
                        credits = (int)data.ClientRemaining;
                        Log.Info().WriteLine("{0}={1}", "ClientRemaining", (int)data.ClientRemaining);
                    }
                    if (data.ContainsKey("UserRemaining"))
                    {
                        credits = Math.Min(credits, (int)data.UserRemaining);
                        Log.Info().WriteLine("{0}={1}", "UserRemaining", (int)data.UserRemaining);
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
        public async Task<ImgurImage> RetrieveImgurInfoAsync(string id, string deleteHash, CancellationToken token = default)
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
        public async Task RetrieveImgurThumbnailAsync(ImgurImage imgurImage, CancellationToken token = default)
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
            ImageHttpBehaviour.MakeCurrent();
            imgurImage.Image = await imgurImage.Data.LinkThumbnail.GetAsAsync<BitmapSource>(token).ConfigureAwait(true);
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
        public async Task<ImgurImage> UploadToImgurAsync(OAuth2Settings oAuth2Settings, ISurface surfaceToUpload, string title, IProgress<int> progress = null, CancellationToken token = default)
        {
            var filename = surfaceToUpload.GenerateFilename(_imgurConfiguration);
            var outputSettings = surfaceToUpload.GenerateOutputSettings(_imgurConfiguration);
            IDictionary<string, string> otherParameters = new Dictionary<string, string>();
            // add title
            if ((title != null) && _imgurConfiguration.AddTitle)
            {
                otherParameters.Add("title", title);
            }
            // add filename
            if (filename != null && _imgurConfiguration.AddFilename)
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