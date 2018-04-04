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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Addons.Bootstrapper.Resolving;
using Dapplo.HttpExtensions;
using Dapplo.HttpExtensions.OAuth;
using Dapplo.Log;
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
        private static readonly Uri MicrosoftOAuth2Uri = new Uri("https://login.microsoftonline.com/common/oauth2/v2.0");

        [ImportingConstructor]
        public OneDriveDestination(IOneDriveConfiguration oneDriveConfiguration, IOneDriveLanguage oneDriveLanguage)
        {
            _oneDriveConfiguration = oneDriveConfiguration;
            _oneDriveLanguage = oneDriveLanguage;
            // Configure the OAuth2 settings for OneDrive communication
            _oauth2Settings = new OAuth2Settings
            {
                AuthorizationUri = MicrosoftOAuth2Uri.AppendSegments("authorize")
                    .ExtendQuery(new Dictionary<string, string>
                    {
                        {"response_type", "code"},
                        {"client_id", "{ClientId}"},
                        {"redirect_uri", "{RedirectUrl}"},
                        {"state", "{State}"},
                        {"scope", "files.readwrite offline_access"}
                    }),
                TokenUrl = MicrosoftOAuth2Uri.AppendSegments("token"),
                CloudServiceName = "OneDrive",
                ClientId = _oneDriveConfiguration.ClientId,
                ClientSecret = _oneDriveConfiguration.ClientSecret,
                RedirectUrl = "http://getgreenshot.org",
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
            var outputSettings = new SurfaceOutputSettings(_oneDriveConfiguration.UploadFormat, _oneDriveConfiguration.UploadJpegQuality,
                _oneDriveConfiguration.UploadReduceColors);
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
                        var oneDriveResponse = await OneDriveUtils.UploadToOneDriveAsync(_oauth2Settings, surfaceToUpload,
                            outputSettings, captureDetails.Title, filename, null, cancellationTokenSource.Token);
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
    }
}