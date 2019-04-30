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

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autofac.Features.OwnedInstances;
using Dapplo.Addons;
using Dapplo.HttpExtensions;
using Dapplo.HttpExtensions.OAuth;
using Dapplo.Log;
using Dapplo.Utils;
using Dapplo.Windows.Clipboard;
using Greenshot.Addon.Box.Configuration;
using Greenshot.Addon.Box.Entities;
using Greenshot.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Controls;
using Greenshot.Addons.Core;
using Greenshot.Addons.Extensions;
using Greenshot.Addons.Interfaces;
using Greenshot.Gfx;

namespace Greenshot.Addon.Box
{
    /// <summary>
    /// This is the destination for the Box service
    /// </summary>
    [Destination("Box")]
    public class BoxDestination : AbstractDestination
	{
	    private static readonly LogSource Log = new LogSource();
	    private readonly ExportNotification _exportNotification;
	    private readonly IBoxConfiguration _boxConfiguration;
	    private readonly IBoxLanguage _boxLanguage;
	    private readonly Func<CancellationTokenSource, Owned<PleaseWaitForm>> _pleaseWaitFormFactory;
	    private readonly IHttpConfiguration _httpConfiguration;
	    private readonly IResourceProvider _resourceProvider;
	    private readonly OAuth2Settings _oauth2Settings;
        private static readonly Uri UploadFileUri = new Uri("https://upload.box.com/api/2.0/files/content");
        private static readonly Uri FilesUri = new Uri("https://www.box.com/api/2.0/files/");

        /// <summary>
        /// DI constructor
        /// </summary>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="greenshotLanguage">IGreenshotLanguage</param>
        /// <param name="exportNotification">ExportNotification</param>
        /// <param name="boxConfiguration">IBoxConfiguration</param>
        /// <param name="boxLanguage">IBoxLanguage</param>
        /// <param name="pleaseWaitFormFactory">Func factory for PleaseWaitForm</param>
        /// <param name="httpConfiguration">IHttpConfiguration</param>
        /// <param name="resourceProvider">IResourceProvider</param>
        public BoxDestination(
            ICoreConfiguration coreConfiguration,
            IGreenshotLanguage greenshotLanguage,
            ExportNotification exportNotification,
            IBoxConfiguration boxConfiguration,
            IBoxLanguage boxLanguage,
            Func<CancellationTokenSource, Owned<PleaseWaitForm>> pleaseWaitFormFactory,
            IHttpConfiguration httpConfiguration,
            IResourceProvider resourceProvider) : base(coreConfiguration, greenshotLanguage)
        {
	        _exportNotification = exportNotification;
	        _boxConfiguration = boxConfiguration;
	        _boxLanguage = boxLanguage;
	        _pleaseWaitFormFactory = pleaseWaitFormFactory;
	        _httpConfiguration = httpConfiguration;
	        _resourceProvider = resourceProvider;

	        _oauth2Settings = new OAuth2Settings
	        {
	            AuthorizationUri = new Uri("https://app.box.com").
	                AppendSegments("api", "oauth2", "authorize").
	                ExtendQuery(new Dictionary<string, string>
	                {
	                    {"response_type", "code"},
	                    {"client_id", "{ClientId}"},
	                    {"redirect_uri", "{RedirectUrl}"},
	                    {"state", "{State}"}
	                }),
	            TokenUrl = new Uri("https://api.box.com/oauth2/token"),
	            CloudServiceName = "Box",
	            ClientId = boxConfiguration.ClientId,
	            ClientSecret = boxConfiguration.ClientSecret,
	            RedirectUrl = "https://www.box.com/home/",
	            AuthorizeMode = AuthorizeModes.EmbeddedBrowser,
	            Token = boxConfiguration
            };
        }

        /// <inheritdoc />
        public override string Description => _boxLanguage.UploadMenuItem;

        /// <inheritdoc />
        public override IBitmapWithNativeSupport DisplayIcon
		{
			get
			{
                // TODO: Optimize this
			    using (var bitmapStream = _resourceProvider.ResourceAsStream(GetType().Assembly, "box.png"))
			    {
			        return BitmapHelper.FromStream(bitmapStream);
			    }
			}
		}

        /// <inheritdoc />
        public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);
			var uploadUrl = await UploadAsync(surface).ConfigureAwait(true);
			if (uploadUrl != null)
			{
				exportInformation.ExportMade = true;
				exportInformation.Uri = uploadUrl;
			}
            _exportNotification.NotifyOfExport(this, exportInformation, surface);
			return exportInformation;
		}

	    /// <summary>
	    ///     This will be called when the menu item in the Editor is clicked
	    /// </summary>
	    private async Task<string> UploadAsync(ISurface surfaceToUpload)
	    {
	        try
	        {
	            string url;
	            using (var ownedPleaseWaitForm = _pleaseWaitFormFactory(default))
	            {
	                ownedPleaseWaitForm.Value.SetDetails("Box", _boxLanguage.CommunicationWait);
                    ownedPleaseWaitForm.Value.Show();
	                try
	                {
	                    url = await UploadToBoxAsync(surfaceToUpload).ConfigureAwait(true);
	                }
	                finally
	                {
	                    ownedPleaseWaitForm.Value.Close();
	                }
	            }

	            if (url != null && _boxConfiguration.AfterUploadLinkToClipBoard)
	            {
	                using (var clipboardAccessToken = ClipboardNative.Access())
	                {
	                    clipboardAccessToken.ClearContents();
                        clipboardAccessToken.SetAsUrl(url);
	                }
                }

	            return url;
	        }
	        catch (Exception ex)
	        {
	            Log.Error().WriteLine(ex, "Error uploading.");
	            MessageBox.Show(_boxLanguage.UploadFailure + @" " + ex.Message);
	            return null;
	        }
	    }

        /// <summary>
        ///     Do the actual upload to Box
        ///     For more details on the available parameters, see:
        ///     http://developers.box.net/w/page/12923951/ApiFunction_Upload%20and%20Download
        /// </summary>
        /// <param name="surface">ICapture</param>
        /// <param name="progress">IProgress</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>url to uploaded image</returns>
        public async Task<string> UploadToBoxAsync(ISurface surface, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            string filename = surface.GenerateFilename(CoreConfiguration, _boxConfiguration);

            var oauthHttpBehaviour = HttpBehaviour.Current.ShallowClone();
            // Use the network settings
            oauthHttpBehaviour.HttpSettings = _httpConfiguration;
            // Use UploadProgress
            if (progress != null)
            {
                oauthHttpBehaviour.UploadProgress = percent => { UiContext.RunOn(() => progress.Report((int)(percent * 100))); };
            }

            oauthHttpBehaviour.OnHttpMessageHandlerCreated = httpMessageHandler => new OAuth2HttpMessageHandler(_oauth2Settings, oauthHttpBehaviour, httpMessageHandler);

            // TODO: See if the PostAsync<Bitmap> can be used? Or at least the HttpContentFactory?
            using (var imageStream = new MemoryStream())
            {
                var multiPartContent = new MultipartFormDataContent();
                var parentIdContent = new StringContent(_boxConfiguration.FolderId);
                parentIdContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                {
                    Name = "\"parent_id\""
                };
                multiPartContent.Add(parentIdContent);
                surface.WriteToStream(imageStream, CoreConfiguration, _boxConfiguration);
                imageStream.Position = 0;

                BoxFile response;
                using (var streamContent = new StreamContent(imageStream))
                {
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream"); //"image/" + outputSettings.Format);
                    streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                    {
                        Name = "\"file\"",
                        FileName = "\"" + filename + "\""
                    }; // the extra quotes are important here
                    multiPartContent.Add(streamContent);

                    oauthHttpBehaviour.MakeCurrent();
                    response = await UploadFileUri.PostAsync<BoxFile>(multiPartContent, cancellationToken).ConfigureAwait(false);
                }

                if (response == null)
                {
                    return null;
                }

                if (_boxConfiguration.UseSharedLink)
                {
                    if (response.SharedLink?.Url == null)
                    {
                        var uriForSharedLink = FilesUri.AppendSegments(response.Id);
                        var updateAccess = new
                        {
                            shared_link = new
                            {
                                access = "open"
                            }
                        };
                        oauthHttpBehaviour.MakeCurrent();
                        response = await uriForSharedLink.PostAsync<BoxFile>(updateAccess, cancellationToken).ConfigureAwait(false);
                    }
                    
                    return response.SharedLink.Url;
                }
                return $"http://www.box.com/files/0/f/0/1/f_{response.Id}";
            }
        }

    }
}