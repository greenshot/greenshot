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
using Greenshot.Addon.Imgur.Configuration;
using Greenshot.Addon.Imgur.Entities;
using Greenshot.Addon.Imgur.ViewModels;
using Greenshot.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Controls;
using Greenshot.Addons.Core;
using Greenshot.Addons.Extensions;
using Greenshot.Addons.Interfaces;
using Greenshot.Gfx;

namespace Greenshot.Addon.Imgur
{
    /// <summary>
    ///     Description of ImgurDestination.
    /// </summary>
    [Destination("Imgur")]
    public class ImgurDestination : AbstractDestination
	{
	    private static readonly LogSource Log = new LogSource();
	    private readonly ExportNotification _exportNotification;
	    private readonly IImgurConfiguration _imgurConfiguration;
	    private readonly IImgurLanguage _imgurLanguage;
	    private readonly ImgurApi _imgurApi;
	    private readonly ImgurHistoryViewModel _imgurHistoryViewModel;
	    private readonly ImgurConfigViewModel _imgurConfigViewModel;
	    private readonly Func<CancellationTokenSource, Owned<PleaseWaitForm>> _pleaseWaitFormFactory;
	    private readonly IResourceProvider _resourceProvider;

		public ImgurDestination(
            ICoreConfiguration coreConfiguration,
            IGreenshotLanguage greenshotLanguage,
            ExportNotification exportNotification,
		    IImgurConfiguration imgurConfiguration,
	        IImgurLanguage imgurLanguage,
	        ImgurApi imgurApi,
	        ImgurHistoryViewModel imgurHistoryViewModel,
            ImgurConfigViewModel imgurConfigViewModel,
            Func<CancellationTokenSource, Owned<PleaseWaitForm>> pleaseWaitFormFactory,
            IResourceProvider resourceProvider) : base(coreConfiguration, greenshotLanguage)
        {
            _exportNotification = exportNotification;
            _imgurConfiguration = imgurConfiguration;
		    _imgurLanguage = imgurLanguage;
		    _imgurApi = imgurApi;
		    _imgurHistoryViewModel = imgurHistoryViewModel;
            _imgurConfigViewModel = imgurConfigViewModel;
            _pleaseWaitFormFactory = pleaseWaitFormFactory;
		    _resourceProvider = resourceProvider;
		}

		public override string Description => _imgurLanguage.UploadMenuItem;

		public override IBitmapWithNativeSupport DisplayIcon
		{
			get
			{
			    // TODO: Optimize this, by caching
			    using (var bitmapStream = _resourceProvider.ResourceAsStream(GetType().Assembly, "Imgur.png"))
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
		    _exportNotification.NotifyOfExport(this, exportInformation, surface, _imgurConfigViewModel);
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
            try
            {
                ImgurImage imgurImage;

                var cancellationTokenSource = new CancellationTokenSource();
                // TODO: Replace the form
                using (var ownedPleaseWaitForm = _pleaseWaitFormFactory(cancellationTokenSource))
                {
                    ownedPleaseWaitForm.Value.SetDetails("Imgur", _imgurLanguage.CommunicationWait);
                    ownedPleaseWaitForm.Value.Show();
                    try
                    {
                        imgurImage = await _imgurApi.UploadToImgurAsync(surfaceToUpload, captureDetails.Title, null, cancellationTokenSource.Token).ConfigureAwait(true);
                        if (imgurImage != null)
                        {
                            // Create thumbnail
                            using (var tmpImage = surfaceToUpload.GetBitmapForExport())
                            using (var thumbnail = tmpImage.CreateThumbnail(90, 90))
                            {
                                imgurImage.Image = thumbnail;
                            }
                            if (_imgurConfiguration.AnonymousAccess && _imgurConfiguration.TrackHistory)
                            {
                                Log.Debug().WriteLine("Storing imgur upload for hash {0} and delete hash {1}", imgurImage.Data.Id, imgurImage.Data.Deletehash);
                                _imgurConfiguration.ImgurUploadHistory.Add(imgurImage.Data.Id, imgurImage.Data.Deletehash);
                                _imgurConfiguration.RuntimeImgurHistory.Add(imgurImage.Data.Id, imgurImage);

                                // Update history
                                _imgurHistoryViewModel.ImgurHistory.Add(imgurImage);
                            }
                        }
                    }
                    finally
                    {
                        ownedPleaseWaitForm.Value.Close();
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
                        using (var clipboardAccessToken = ClipboardNative.Access())
                        {
                            clipboardAccessToken.ClearContents();
                            clipboardAccessToken.SetAsUrl(uploadUrl.AbsoluteUri);
                        }
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
    }
}