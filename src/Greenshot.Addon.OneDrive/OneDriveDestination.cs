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
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autofac.Features.OwnedInstances;
using Dapplo.Addons;
using Dapplo.HttpExtensions;
using Dapplo.HttpExtensions.JsonNet;
using Dapplo.HttpExtensions.OAuth;
using Dapplo.Log;
using Dapplo.Utils;
using Dapplo.Windows.Clipboard;
using Greenshot.Addon.OneDrive.Entities;
using Greenshot.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Controls;
using Greenshot.Addons.Core;
using Greenshot.Addons.Extensions;
using Greenshot.Addons.Interfaces;
using Greenshot.Gfx;

#endregion

namespace Greenshot.Addon.OneDrive
{
    /// <summary>
    ///     Description of OneDriveDestination.
    /// </summary>
    [Destination("OneDrive")]
    public class OneDriveDestination : AbstractDestination
    {
        private static readonly LogSource Log = new LogSource();
        private readonly IOneDriveConfiguration _oneDriveConfiguration;
        private readonly IOneDriveLanguage _oneDriveLanguage;
        private readonly IResourceProvider _resourceProvider;
        private readonly Func<CancellationTokenSource, Owned<PleaseWaitForm>> _pleaseWaitFormFactory;
        private readonly OAuth2Settings _oauth2Settings;
        private static readonly Uri GraphUri = new Uri("https://graph.microsoft.com");
        private static readonly Uri OneDriveUri = GraphUri.AppendSegments("v1.0", "me", "drive");
        private static readonly Uri OAuth2Uri = new Uri("https://login.microsoftonline.com").AppendSegments("common", "oauth2", "v2.0");

        private readonly HttpBehaviour _oneDriveHttpBehaviour;

        public OneDriveDestination(
            IOneDriveConfiguration oneDriveConfiguration,
            IOneDriveLanguage oneDriveLanguage,
            INetworkConfiguration networkConfiguration,
            IResourceProvider resourceProvider,
            Func<CancellationTokenSource, Owned<PleaseWaitForm>> pleaseWaitFormFactory,
            ICoreConfiguration coreConfiguration,
            IGreenshotLanguage greenshotLanguage
        ) : base(coreConfiguration, greenshotLanguage)
        {
            _oneDriveConfiguration = oneDriveConfiguration;
            _oneDriveLanguage = oneDriveLanguage;
            _resourceProvider = resourceProvider;
            _pleaseWaitFormFactory = pleaseWaitFormFactory;
            // Configure the OAuth2 settings for OneDrive communication
            _oauth2Settings = new OAuth2Settings
            {
                AuthorizationUri = OAuth2Uri.AppendSegments("authorize")
                    .ExtendQuery(new Dictionary<string, string>
                    {
                        {"response_type", "code"},
                        {"client_id", "{ClientId}"},
                        {"redirect_uri", "{RedirectUrl}"},
                        {"state", "{State}"},
                        {"scope", "files.readwrite offline_access"}
                    }),
                TokenUrl = OAuth2Uri.AppendSegments("token"),
                CloudServiceName = "OneDrive",
                ClientId = _oneDriveConfiguration.ClientId,
                ClientSecret = "",
                RedirectUrl = "https://login.microsoftonline.com/common/oauth2/nativeclient",
                AuthorizeMode = AuthorizeModes.EmbeddedBrowser,
                Token = oneDriveConfiguration
            };
            _oneDriveHttpBehaviour = new HttpBehaviour
            {
                HttpSettings = networkConfiguration,
                JsonSerializer = new JsonNetJsonSerializer()
            };
        }

        public override string Description => _oneDriveLanguage.UploadMenuItem;

        public override Bitmap DisplayIcon
        {
            get
            {
                // TODO: Optimize this by caching
                using (var bitmapStream = _resourceProvider.ResourceAsStream(GetType().Assembly, "onedrive.png"))
                {
                    return BitmapHelper.FromStream(bitmapStream);
                }
            }
        }

        public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface,
            ICaptureDetails captureDetails)
        {
            var uploadUrl = await Upload(captureDetails, surface);

            var exportInformation = new ExportInformation(Designation, Description)
            {
                ExportMade = uploadUrl != null,
                Uri = uploadUrl?.AbsoluteUri
            };
            ProcessExport(exportInformation, surface);
            return exportInformation;
        }

        /// <summary>
        /// Upload the capture to OneDrive
        /// </summary>
        /// <param name="captureDetails">ICaptureDetails</param>
        /// <param name="surfaceToUpload">ISurface</param>
        /// <returns>Uri</returns>
        private async Task<Uri> Upload(ICaptureDetails captureDetails, ISurface surfaceToUpload)
        {
            try
            {
                Uri response;

                var cancellationTokenSource = new CancellationTokenSource();
                using (var ownedPleaseWaitForm = _pleaseWaitFormFactory(cancellationTokenSource))
                {
                    ownedPleaseWaitForm.Value.SetDetails("OneDrive", _oneDriveLanguage.CommunicationWait);
                    ownedPleaseWaitForm.Value.Show();
                    try
                    {
                        var oneDriveResponse = await UploadToOneDriveAsync(_oauth2Settings, surfaceToUpload, null, cancellationTokenSource.Token);
                        Log.Info().WriteLine($"id: {oneDriveResponse.Id}");

                        if (_oneDriveConfiguration.LinkType == OneDriveLinkType.@private)
                        {
                            response = new Uri(oneDriveResponse.WebUrl);
                        }
                        else
                        {
                            var sharableLink = await CreateSharableLinkAync(_oauth2Settings, oneDriveResponse.Id, _oneDriveConfiguration.LinkType);
                            response = new Uri(sharableLink.Link.WebUrl);
                        }
                    }
                    finally
                    {
                        ownedPleaseWaitForm.Value.Close();
                    }
                }

                if (_oneDriveConfiguration.AfterUploadLinkToClipBoard)
                {
                    using (var clipboardAccessToken = ClipboardNative.Access())
                    {
                        clipboardAccessToken.ClearContents();
                        clipboardAccessToken.SetAsUrl(response.AbsoluteUri);
                    }
                }

                return response;
            }
            catch (Exception e)
            {
                Log.Error().WriteLine(e, "Error uploading.");
                MessageBox.Show(_oneDriveLanguage.UploadFailure + " " + e.Message);
            }

            return null;
        }


        /// <summary>
        ///     Do the actual upload to OneDrive
        /// </summary>
        /// <param name="oAuth2Settings">OAuth2Settings</param>
        /// <param name="surfaceToUpload">ISurface to upload</param>
        /// <param name="progress">IProgress</param>
        /// <param name="token">CancellationToken</param>
        /// <returns>OneDriveUploadResponse with details</returns>
        private async Task<OneDriveUploadResponse> UploadToOneDriveAsync(OAuth2Settings oAuth2Settings,
            ISurface surfaceToUpload, IProgress<int> progress = null,
            CancellationToken token = default)
        {
            var filename = surfaceToUpload.GenerateFilename(CoreConfiguration, _oneDriveConfiguration);
            var uploadUri = OneDriveUri.AppendSegments("root:", "Screenshots", filename + ":", "content");
            var localBehaviour = _oneDriveHttpBehaviour.ShallowClone();
            if (progress != null)
            {
                localBehaviour.UploadProgress = percent => { UiContext.RunOn(() => progress.Report((int)(percent * 100)), token); };
            }
            var oauthHttpBehaviour = OAuth2HttpBehaviourFactory.Create(oAuth2Settings, localBehaviour);
            using (var imageStream = new MemoryStream())
            {
                surfaceToUpload.WriteToStream(imageStream, CoreConfiguration, _oneDriveConfiguration);
                imageStream.Position = 0;
                using (var content = new StreamContent(imageStream))
                {
                    content.Headers.Add("Content-Type", surfaceToUpload.GenerateMimeType(CoreConfiguration, _oneDriveConfiguration));
                    oauthHttpBehaviour.MakeCurrent();
                    return await uploadUri.PutAsync<OneDriveUploadResponse>(content, token);
                }
            }
        }

        private async Task<OneDriveGetLinkResponse> CreateSharableLinkAync(OAuth2Settings oAuth2Settings,
            string imageId, OneDriveLinkType oneDriveLinkType)
        {
            var sharableLink = OneDriveUri.AppendSegments("items", imageId, "createLink");
            var localBehaviour = _oneDriveHttpBehaviour.ShallowClone();
            var oauthHttpBehaviour = OAuth2HttpBehaviourFactory.Create(oAuth2Settings, localBehaviour);
            oauthHttpBehaviour.MakeCurrent();
            var body = new OneDriveGetLinkRequest();
            body.Scope = oneDriveLinkType == OneDriveLinkType.@public ? "anonymous" : "organization";
            body.Type = "view";
            return await sharableLink.PostAsync<OneDriveGetLinkResponse>(body);
        }
    }
}