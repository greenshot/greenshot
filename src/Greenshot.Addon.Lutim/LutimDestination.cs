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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autofac.Features.OwnedInstances;
using Dapplo.Addons;
using Dapplo.Log;
using Dapplo.Windows.Clipboard;
using Greenshot.Addon.Lutim.Configuration;
using Greenshot.Addon.Lutim.Entities;
using Greenshot.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Controls;
using Greenshot.Addons.Core;
using Greenshot.Addons.Extensions;
using Greenshot.Addons.Interfaces;
using Greenshot.Gfx;

namespace Greenshot.Addon.Lutim  {
    /// <summary>
    /// This is the destination for Lutim
    /// </summary>
    [Destination("Lutim")]
    public class LutimDestination : AbstractDestination
    {
        private static readonly LogSource Log = new LogSource();
        private readonly ILutimConfiguration _lutimConfiguration;
        private readonly ILutimLanguage _lutimLanguage;
        private readonly LutimApi _lutimApi;
        private readonly IResourceProvider _resourceProvider;
        private readonly ExportNotification _exportNotification;
        private readonly Func<CancellationTokenSource, Owned<PleaseWaitForm>> _pleaseWaitFormFactory;

        /// <summary>
        /// DI constructor
        /// </summary>
        /// <param name="lutimConfiguration">ILutimConfiguration</param>
        /// <param name="lutimLanguage">ILutimLanguage</param>
        /// <param name="lutimApi">LutimApi</param>
        /// <param name="resourceProvider">IResourceProvider</param>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="greenshotLanguage">IGreenshotLanguage</param>
        /// <param name="exportNotification">ExportNotification</param>
        /// <param name="pleaseWaitFormFactory">Func</param>
        public LutimDestination(ILutimConfiguration lutimConfiguration,
            ILutimLanguage lutimLanguage,
            LutimApi lutimApi,
            IResourceProvider resourceProvider,
            ICoreConfiguration coreConfiguration,
            IGreenshotLanguage greenshotLanguage,
            ExportNotification exportNotification,
            Func<CancellationTokenSource, Owned<PleaseWaitForm>> pleaseWaitFormFactory
        ) : base(coreConfiguration, greenshotLanguage)
        {
            _lutimConfiguration = lutimConfiguration;
            _lutimLanguage = lutimLanguage;
            _lutimApi = lutimApi;
            _resourceProvider = resourceProvider;
            _exportNotification = exportNotification;
            _pleaseWaitFormFactory = pleaseWaitFormFactory;
        }

        /// <inheritdoc />
        public override string Description => _lutimLanguage.UploadMenuItem;

        /// <inheritdoc />
        public override IBitmapWithNativeSupport DisplayIcon {
			get {
                // TODO: Optimize this by caching
			    using (var bitmapStream = _resourceProvider.ResourceAsStream(GetType().Assembly, "Lutim.png"))
			    {
			        return BitmapHelper.FromStream(bitmapStream);
			    }
            }
		}

        /// <inheritdoc />
        public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
        {
            var uploadUrl = await Upload(surface).ConfigureAwait(true);

            var exportInformation = new ExportInformation(Designation, Description)
		    {
		        ExportMade = uploadUrl != null,
		        Uri = uploadUrl
		    };
            _exportNotification.NotifyOfExport(this, exportInformation, surface);
            return exportInformation;
		}


        /// <summary>
        /// Upload the capture to lutim
        /// </summary>
        /// <param name="surfaceToUpload">ISurface</param>
        /// <returns>true if the upload succeeded</returns>
        private async Task<string> Upload(ISurface surfaceToUpload)
        {
            string uploadUrl;
            try
            {
                LutimInfo lutimInfo;

                var cancellationTokenSource = new CancellationTokenSource();
                using (var ownedPleaseWaitForm = _pleaseWaitFormFactory(cancellationTokenSource))
                {
                    ownedPleaseWaitForm.Value.SetDetails("Lutim", _lutimLanguage.CommunicationWait);
                    ownedPleaseWaitForm.Value.Show();
                    try
                    {
                        lutimInfo = await _lutimApi.UploadToLutim(surfaceToUpload).ConfigureAwait(true);
                        if (lutimInfo != null)
                        {
                            Log.Info().WriteLine("Storing lutim upload for hash {0} and delete hash {1}",
                                lutimInfo.Short, lutimInfo.Token);
                            // TODO: Write somewhere
                            // _lutimConfiguration.LutimUploadHistory.Add(lutimInfo.Short, lutimInfo.ToIniString());
                            _lutimConfiguration.RuntimeLutimHistory.Add(lutimInfo.Short, lutimInfo);
                            // TODO: Update
                            // UpdateHistoryMenuItem();
                        }
                    }
                    finally
                    {
                        ownedPleaseWaitForm.Value.Close();
                    }
                }


                if (lutimInfo != null)
                {
                    uploadUrl = lutimInfo.Short;
                    if (string.IsNullOrEmpty(uploadUrl) || !_lutimConfiguration.CopyLinkToClipboard)
                    {
                        return uploadUrl;
                    }
                    try
                    {
                        using (var clipboardAccessToken = ClipboardNative.Access())
                        {
                            clipboardAccessToken.ClearContents();
                            clipboardAccessToken.SetAsUrl(uploadUrl);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error().WriteLine(ex, "Can't write to clipboard: ");
                        return null;
                    }
                    return uploadUrl;
                }
            }
            catch (Exception e)
            {
                Log.Error().WriteLine(e, "Error uploading.");
                MessageBox.Show(_lutimLanguage.UploadFailure + " " + e.Message);
            }
            return null;
        }
    }
}
