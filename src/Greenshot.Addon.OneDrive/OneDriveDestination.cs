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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Addons.Bootstrapper.Resolving;
using Dapplo.HttpExtensions;
using Dapplo.HttpExtensions.OAuth;
using Dapplo.Log;
using Dapplo.Utils;
using Greenshot.Addon.OneDrive.Entities;
using Greenshot.Addons.Addons;
using Greenshot.Addons.Controls;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;
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
        private readonly OAuth2Settings _oauth2Settings;
        private static readonly Uri GraphUri = new Uri("https://graph.microsoft.com");
        private static readonly Uri OneDriveUri = GraphUri.AppendSegments("v1.0", "me", "drive");
        private static readonly Uri OAuth2Uri = new Uri("https://login.microsoftonline.com/common/oauth2/v2.0");
        private static readonly HttpBehaviour OneDriveHttpBehaviour = new HttpBehaviour();


        [ImportingConstructor]
        public OneDriveDestination(IOneDriveConfiguration oneDriveConfiguration, IOneDriveLanguage oneDriveLanguage)
        {
            _oneDriveConfiguration = oneDriveConfiguration;
            _oneDriveLanguage = oneDriveLanguage;
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
                ClientSecret = _oneDriveConfiguration.ClientSecret,
                RedirectUrl = "https://getgreenshot.org/onedrive",
                AuthorizeMode = AuthorizeModes.EmbeddedBrowser,
                Token = oneDriveConfiguration
            };
        }

        public override string Description => _oneDriveLanguage.UploadMenuItem;

        public override Bitmap DisplayIcon
        {
            get
            {
                // TODO: Optimize this
                var embeddedResource = GetType().Assembly.FindEmbeddedResources(@".*onedrive\.png").FirstOrDefault();
                using (var bitmapStream = GetType().Assembly.GetEmbeddedResourceAsStream(embeddedResource))
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
            var outputSettings = new SurfaceOutputSettings(_oneDriveConfiguration.UploadFormat, _oneDriveConfiguration.UploadJpegQuality, _oneDriveConfiguration.UploadReduceColors);
            try
            {
                string filename = Path.GetFileName(FilenameHelper.GetFilenameFromPattern(_oneDriveConfiguration.FilenamePattern,
                    _oneDriveConfiguration.UploadFormat, captureDetails));
                Uri response;

                var cancellationTokenSource = new CancellationTokenSource();
                using (var pleaseWaitForm = new PleaseWaitForm("OneDrive plug-in", _oneDriveLanguage.CommunicationWait,
                    cancellationTokenSource))
                {
                    pleaseWaitForm.Show();
                    try
                    {
                        var oneDriveResponse = await UploadToOneDriveAsync(_oauth2Settings, surfaceToUpload,
                            outputSettings, filename, null, cancellationTokenSource.Token);
                        response = new Uri(oneDriveResponse.WebUrl);
                    }
                    finally
                    {
                        pleaseWaitForm.Close();
                    }
                }

                if (_oneDriveConfiguration.AfterUploadLinkToClipBoard)
                {
                    ClipboardHelper.SetClipboardData(response.ToString());
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
        /// <param name="outputSettings">OutputSettings for the image file format</param>
        /// <param name="filename">Filename</param>
        /// <param name="progress">IProgress</param>
        /// <param name="token">CancellationToken</param>
        /// <returns>OneDriveUploadResponse with details</returns>
        private async Task<OneDriveUploadResponse> UploadToOneDriveAsync(OAuth2Settings oAuth2Settings, ISurface surfaceToUpload,
            SurfaceOutputSettings outputSettings, string filename, IProgress<int> progress = null,
            CancellationToken token = default)
        {
            var uploadUri = OneDriveUri.AppendSegments("root:", "Screenshots", filename, "content");
            var localBehaviour = OneDriveHttpBehaviour.ShallowClone();
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
                    return await uploadUri.PostAsync<OneDriveUploadResponse>(content, token);
                }
            }
        }
    }
}