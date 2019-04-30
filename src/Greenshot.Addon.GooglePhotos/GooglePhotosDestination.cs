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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Autofac.Features.OwnedInstances;
using Dapplo.Addons;
using Dapplo.HttpExtensions;
using Dapplo.HttpExtensions.OAuth;
using Dapplo.Log;
using Dapplo.Utils;
using Dapplo.Windows.Clipboard;
using Greenshot.Addon.GooglePhotos.Configuration;
using Greenshot.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Controls;
using Greenshot.Addons.Core;
using Greenshot.Addons.Extensions;
using Greenshot.Addons.Interfaces;
using Greenshot.Gfx;

namespace Greenshot.Addon.GooglePhotos
{
    [Destination("GooglePhotos")]
    public sealed class GooglePhotosDestination : AbstractDestination
	{
	    private static readonly LogSource Log = new LogSource();
        private readonly IGooglePhotosConfiguration _googlePhotosConfiguration;
	    private readonly IGooglePhotosLanguage _googlePhotosLanguage;
	    private readonly IHttpConfiguration _httpConfiguration;
	    private readonly IResourceProvider _resourceProvider;
	    private readonly ExportNotification _exportNotification;
	    private readonly Func<CancellationTokenSource, Owned<PleaseWaitForm>> _pleaseWaitFormFactory;
	    private readonly OAuth2Settings _oAuth2Settings;

        public GooglePhotosDestination(
	        IGooglePhotosConfiguration googlePhotosConfiguration,
	        IGooglePhotosLanguage googlePhotosLanguage,
	        IHttpConfiguration httpConfiguration,
	        IResourceProvider resourceProvider,
            ICoreConfiguration coreConfiguration,
            IGreenshotLanguage greenshotLanguage,
	        ExportNotification exportNotification,
	        Func<CancellationTokenSource, Owned<PleaseWaitForm>> pleaseWaitFormFactory
            ) : base(coreConfiguration, greenshotLanguage)
        {
	        _googlePhotosConfiguration = googlePhotosConfiguration;
	        _googlePhotosLanguage = googlePhotosLanguage;
	        _httpConfiguration = httpConfiguration;
	        _resourceProvider = resourceProvider;
            _exportNotification = exportNotification;
            _pleaseWaitFormFactory = pleaseWaitFormFactory;

            _oAuth2Settings = new OAuth2Settings
	        {
	            AuthorizationUri = new Uri("https://accounts.google.com").AppendSegments("o", "oauth2", "auth").
	                ExtendQuery(new Dictionary<string, string>
	                {
	                    {"response_type", "code"},
	                    {"client_id", "{ClientId}"},
	                    {"redirect_uri", "{RedirectUrl}"},
	                    {"state", "{State}"},
	                    {"scope", "https://picasaweb.google.com/data/"}
	                }),
	            TokenUrl = new Uri("https://www.googleapis.com/oauth2/v3/token"),
	            CloudServiceName = "GooglePhotos",
	            ClientId = googlePhotosConfiguration.ClientId,
	            ClientSecret = googlePhotosConfiguration.ClientSecret,
	            RedirectUrl = "http://getgreenshot.org",
	            AuthorizeMode = AuthorizeModes.LocalhostServer,
	            Token = googlePhotosConfiguration
            };
        }

        /// <inheritdoc />
        public override string Description => _googlePhotosLanguage.UploadMenuItem;

        /// <inheritdoc />
        public override IBitmapWithNativeSupport DisplayIcon
		{
			get
			{
                // TODO: Optimize this by caching
			    using (var bitmapStream = _resourceProvider.ResourceAsStream(GetType().Assembly, "GooglePhotos.png"))
			    {
			        return BitmapHelper.FromStream(bitmapStream);
			    }
			}
		}

        /// <inheritdoc />
        public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);
		    var uploadUrl = await Upload(surface).ConfigureAwait(true);
			if (uploadUrl != null)
			{
				exportInformation.ExportMade = true;
				exportInformation.Uri = uploadUrl;
			}
		    _exportNotification.NotifyOfExport(this, exportInformation, surface);
            return exportInformation;
		}

	    private async Task<string> Upload(ISurface surfaceToUpload)
	    {
	        try
            {
                string url;
                using (var ownedPleaseWaitForm = _pleaseWaitFormFactory(default))
	            {
	                ownedPleaseWaitForm.Value.SetDetails("GooglePhotos", _googlePhotosLanguage.CommunicationWait);
                    ownedPleaseWaitForm.Value.Show();
	                try
	                {
	                    url = await UploadToGooglePhotos(surfaceToUpload).ConfigureAwait(true);
	                }
	                finally
	                {
	                    ownedPleaseWaitForm.Value.Close();
	                }
	            }

	            if (url != null && _googlePhotosConfiguration.AfterUploadLinkToClipBoard)
	            {
	                using (var clipboardAccessToken = ClipboardNative.Access())
	                {
	                    clipboardAccessToken.ClearContents();
                        clipboardAccessToken.SetAsUrl(url);
	                }
                }
	            return url;
	        }
	        catch (Exception e)
	        {
	            Log.Error().WriteLine(e, "Error uploading.");
	            MessageBox.Show(_googlePhotosLanguage.UploadFailure + @" " + e.Message);
	        }
	        return null;
	    }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="progress"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task<string> UploadToGooglePhotos(ISurface surface, IProgress<int> progress = null, CancellationToken token = default)
        {
            string filename = surface.GenerateFilename(CoreConfiguration, _googlePhotosConfiguration);
            
            var oAuthHttpBehaviour = HttpBehaviour.Current.ShallowClone();
            oAuthHttpBehaviour.HttpSettings = _httpConfiguration;
            // Use UploadProgress
            if (progress != null)
            {
                oAuthHttpBehaviour.UploadProgress = percent => { UiContext.RunOn(() => progress.Report((int)(percent * 100)), token); };
            }
            oAuthHttpBehaviour.OnHttpMessageHandlerCreated = httpMessageHandler => new OAuth2HttpMessageHandler(_oAuth2Settings, oAuthHttpBehaviour, httpMessageHandler);
            if (_googlePhotosConfiguration.AddFilename)
            {
                oAuthHttpBehaviour.OnHttpClientCreated = httpClient => httpClient.AddDefaultRequestHeader("Slug", Uri.EscapeDataString(filename));
            }

            string response;
            var uploadUri = new Uri("https://picasaweb.google.com/data/feed/api/user").AppendSegments(_googlePhotosConfiguration.UploadUser, "albumid", _googlePhotosConfiguration.UploadAlbum);
            using (var imageStream = new MemoryStream())
            {
                surface.WriteToStream(imageStream, CoreConfiguration, _googlePhotosConfiguration);
                imageStream.Position = 0;
                using (var content = new StreamContent(imageStream))
                {
                    content.Headers.Add("Content-Type", surface.GenerateMimeType(CoreConfiguration, _googlePhotosConfiguration));

                    oAuthHttpBehaviour.MakeCurrent();
                    response = await uploadUri.PostAsync<string>(content, token).ConfigureAwait(true);
                }
            }

            return ParseResponse(response);
        }
	    /// <summary>
	    ///     Parse the upload URL from the response
	    /// </summary>
	    /// <param name="response"></param>
	    /// <returns></returns>
	    public static string ParseResponse(string response)
	    {
	        if (response == null)
	        {
	            return null;
	        }
	        try
	        {
	            var doc = new XmlDocument();
	            doc.LoadXml(response);
	            var nodes = doc.GetElementsByTagName("link", "*");
	            if (nodes.Count > 0)
	            {
	                string url = null;
	                foreach (XmlNode node in nodes)
	                {
	                    if (node.Attributes != null)
	                    {
	                        url = node.Attributes["href"].Value;
	                        string rel = node.Attributes["rel"].Value;
	                        // Pictures with rel="http://schemas.google.com/photos/2007#canonical" are the direct link
	                        if ((rel != null) && rel.EndsWith("canonical"))
	                        {
	                            break;
	                        }
	                    }
	                }
	                return url;
	            }
	        }
	        catch (Exception e)
	        {
	            Log.Error().WriteLine("Could not parse Picasa response due to error {0}, response was: {1}", e.Message, response);
	        }
	        return null;
	    }
    }
}