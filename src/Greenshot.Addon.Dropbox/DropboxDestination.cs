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
using Greenshot.Addon.Dropbox.Configuration;
using Greenshot.Addon.Dropbox.Entities;
using Greenshot.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Controls;
using Greenshot.Addons.Core;
using Greenshot.Addons.Extensions;
using Greenshot.Addons.Interfaces;
using Greenshot.Gfx;
using Newtonsoft.Json;

namespace Greenshot.Addon.Dropbox
{
    /// <summary>
    /// This is the destination implementation to export to dropbox
    /// </summary>
    [Destination("Dropbox")]
    public sealed class DropboxDestination : AbstractDestination
	{
	    private static readonly LogSource Log = new LogSource();
	    private static readonly Uri DropboxApiUri = new Uri("https://api.dropbox.com");
	    private static readonly Uri DropboxContentUri = new Uri("https://content.dropboxapi.com/2/files/upload");

        private readonly IDropboxConfiguration _dropboxPluginConfiguration;
	    private readonly IDropboxLanguage _dropboxLanguage;
	    private readonly IResourceProvider _resourceProvider;
	    private readonly ExportNotification _exportNotification;
	    private readonly Func<CancellationTokenSource, Owned<PleaseWaitForm>> _pleaseWaitFormFactory;
	    private OAuth2Settings _oAuth2Settings;
	    private IHttpBehaviour _oAuthHttpBehaviour;

        /// <summary>
        /// DI Coonstructor
        /// </summary>
        /// <param name="dropboxPluginConfiguration">IDropboxConfiguration</param>
        /// <param name="dropboxLanguage">IDropboxLanguage</param>
        /// <param name="httpConfiguration">IHttpConfiguration</param>
        /// <param name="resourceProvider">IResourceProvider</param>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="greenshotLanguage">IGreenshotLanguage</param>
        /// <param name="exportNotification">ExportNotification</param>
        /// <param name="pleaseWaitFormFactory">Func</param>
        public DropboxDestination(
	        IDropboxConfiguration dropboxPluginConfiguration,
	        IDropboxLanguage dropboxLanguage,
	        IHttpConfiguration httpConfiguration,
	        IResourceProvider resourceProvider,
	        ICoreConfiguration coreConfiguration,
	        IGreenshotLanguage greenshotLanguage,
	        ExportNotification exportNotification,
	        Func<CancellationTokenSource, Owned<PleaseWaitForm>> pleaseWaitFormFactory
        ) : base(coreConfiguration, greenshotLanguage)
        {
	        _dropboxPluginConfiguration = dropboxPluginConfiguration;
	        _dropboxLanguage = dropboxLanguage;
	        _resourceProvider = resourceProvider;
            _exportNotification = exportNotification;
            _pleaseWaitFormFactory = pleaseWaitFormFactory;

            _oAuth2Settings = new OAuth2Settings
	        {
	            AuthorizationUri = DropboxApiUri.
	                AppendSegments("1", "oauth2", "authorize").
	                ExtendQuery(new Dictionary<string, string>
	                {
	                    {"response_type", "code"},
	                    {"client_id", "{ClientId}"},
	                    {"redirect_uri", "{RedirectUrl}"},
	                    // TODO: Add version?
	                    {"state", "{State}"}
	                }),
	            TokenUrl = DropboxApiUri.AppendSegments("1", "oauth2", "token"),
	            CloudServiceName = "Dropbox",
	            ClientId = dropboxPluginConfiguration.ClientId,
	            ClientSecret = dropboxPluginConfiguration.ClientSecret,
	            AuthorizeMode = AuthorizeModes.OutOfBoundAuto,
	            RedirectUrl = "https://getgreenshot.org/oauth/dropbox",
	            Token = dropboxPluginConfiguration
            };

            var httpBehaviour = OAuth2HttpBehaviourFactory.Create(_oAuth2Settings);

	        _oAuthHttpBehaviour = httpBehaviour;
            // Use the default network settings
	        httpBehaviour.HttpSettings = httpConfiguration;
        }

        /// <inheritdoc />
        public override IBitmapWithNativeSupport DisplayIcon
		{
			get
			{
                // TODO: Optimize this by caching
			    using (var bitmapStream = _resourceProvider.ResourceAsStream(GetType().Assembly, "Dropbox.gif"))
			    {
			        return BitmapHelper.FromStream(bitmapStream);
			    }
            }
		}

        /// <inheritdoc />
        public override string Description => _dropboxLanguage.UploadMenuItem;

        /// <inheritdoc />
        public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);
		    var uploadUrl = await UploadAsync(surface).ConfigureAwait(true);
			if (uploadUrl != null)
			{
				exportInformation.Uri = uploadUrl;
				exportInformation.ExportMade = true;
				if (_dropboxPluginConfiguration.AfterUploadLinkToClipBoard)
				{
				    using (var clipboardAccessToken = ClipboardNative.Access())
				    {
				        clipboardAccessToken.ClearContents();
                        clipboardAccessToken.SetAsUrl(uploadUrl);
				    }
                }
			}
		    _exportNotification.NotifyOfExport(this, exportInformation, surface);
			return exportInformation;
		}

	    /// <summary>
	    ///     This will be called when the menu item in the Editor is clicked
	    /// </summary>
	    private async Task<string> UploadAsync(ISurface surfaceToUpload, CancellationToken cancellationToken = default)
	    {
	        string dropboxUrl = null;
	        try
	        {
	            var cancellationTokenSource = new CancellationTokenSource();
                using (var ownedPleaseWaitForm = _pleaseWaitFormFactory(cancellationTokenSource))
	            {
	                ownedPleaseWaitForm.Value.SetDetails("Dropbox", _dropboxLanguage.CommunicationWait);
                    ownedPleaseWaitForm.Value.Show();
	                try
	                {
	                    var filename = surfaceToUpload.GenerateFilename(CoreConfiguration, _dropboxPluginConfiguration);
	                    using (var imageStream = new MemoryStream())
	                    {
                            surfaceToUpload.WriteToStream(imageStream, CoreConfiguration, _dropboxPluginConfiguration);
	                        imageStream.Position = 0;
	                        using (var streamContent = new StreamContent(imageStream))
	                        {
	                            streamContent.Headers.ContentType = new MediaTypeHeaderValue(surfaceToUpload.GenerateMimeType(CoreConfiguration, _dropboxPluginConfiguration));
	                            dropboxUrl = await UploadAsync(filename, streamContent, null, cancellationToken).ConfigureAwait(false);
	                        }
	                    }
	                }
	                finally
	                {
	                    ownedPleaseWaitForm.Value.Close();
	                }
	            }
	        }
	        catch (Exception e)
	        {
	            Log.Error().WriteLine(e);
	            MessageBox.Show(_dropboxLanguage.UploadFailure + @" " + e.Message);
	        }
            return dropboxUrl;
	    }

        /// <summary>
        ///     Upload the HttpContent to dropbox
        /// </summary>
        /// <param name="filename">Name of the file</param>
        /// <param name="content">HttpContent</param>
        /// <param name="progress">IProgress</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Url as string</returns>
        private async Task<string> UploadAsync(string filename, HttpContent content, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            var oAuthHttpBehaviour = _oAuthHttpBehaviour.ShallowClone();
            // Use UploadProgress
            if (progress != null)
            {
                oAuthHttpBehaviour.UploadProgress = percent => { UiContext.RunOn(() => progress.Report((int)(percent * 100))); };
            }
            oAuthHttpBehaviour.MakeCurrent();

            // Build the upload content together
            var uploadContent = new Upload
            {
                Content = content
            };

            // This is needed
            if (!filename.StartsWith("/"))
            {
                filename = "/" + filename;
            }
            // Create the upload request parameters
            var parameters = new UploadRequest
            {
                Path = filename
            };

            // Add it to the headers
            uploadContent.Headers.Add("Dropbox-API-Arg", JsonConvert.SerializeObject(parameters, Formatting.None));
            _oAuthHttpBehaviour.MakeCurrent();
            // Post everything, and return the upload reply or an error
            var response = await DropboxContentUri.PostAsync<HttpResponse<UploadReply, Error>>(uploadContent, cancellationToken).ConfigureAwait(false);

            if (response.HasError)
            {
                throw new ApplicationException(response.ErrorResponse.Summary);
            }
            // Take the response from the upload, and use the information to request dropbox to create a link
            var createLinkRequest = new CreateLinkRequest
            {
                Path = response.Response.PathDisplay
            };
            var reply = await DropboxApiUri
                .AppendSegments("2", "sharing", "create_shared_link_with_settings")
                .PostAsync<HttpResponse<CreateLinkReply, Error>>(createLinkRequest, cancellationToken).ConfigureAwait(false);
            if (reply.HasError)
            {
                throw new ApplicationException(reply.ErrorResponse.Summary);
            }
            return reply.Response.Url;
        }
    }
}