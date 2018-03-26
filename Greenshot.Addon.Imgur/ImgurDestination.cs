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
using System.Windows.Forms;
using Dapplo.Addons.Bootstrapper.Resolving;
using Dapplo.HttpExtensions;
using Dapplo.HttpExtensions.OAuth;
using Dapplo.Log;
using Dapplo.Windows.Extensions;
using Greenshot.Addon.Imgur.Entities;
using Greenshot.Addons.Addons;
using Greenshot.Addons.Controls;
using Greenshot.Addons.Core;
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
	    private readonly Dapplo.HttpExtensions.OAuth.OAuth2Settings _oauth2Settings;

	    [ImportingConstructor]
		public ImgurDestination(IImgurConfiguration imgurConfiguration, IImgurLanguage imgurLanguage)
		{
			_imgurConfiguration = imgurConfiguration;
		    _imgurLanguage = imgurLanguage;
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

		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
		    var exportInformation = new ExportInformation(Designation, Description)
		    {
		        ExportMade = Upload(captureDetails, surface, out var uploadUrl),
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
        /// <param name="uploadUrl">out string for the url</param>
        /// <returns>true if the upload succeeded</returns>
        private bool Upload(ICaptureDetails captureDetails, ISurface surfaceToUpload, out Uri uploadUrl)
        {
            SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(_imgurConfiguration.UploadFormat, _imgurConfiguration.UploadJpegQuality, _imgurConfiguration.UploadReduceColors);
            try
            {
                string filename = Path.GetFileName(FilenameHelper.GetFilenameFromPattern(_imgurConfiguration.FilenamePattern, _imgurConfiguration.UploadFormat, captureDetails));
                ImgurImage imgurImage = null;

                // Run upload in the background
                new PleaseWaitForm().ShowAndWait("Imgur plug-in", _imgurLanguage.CommunicationWait,
                    delegate
                    {
                        imgurImage = ImgurUtils.UploadToImgurAsync(_oauth2Settings, surfaceToUpload, outputSettings, captureDetails.Title, filename).Result;
                        if (imgurImage == null)
                        {
                            return;
                        }
                        // Create thumbnail
                        using (var tmpImage = surfaceToUpload.GetBitmapForExport())
                        using (var thumbnail = tmpImage.CreateThumbnail(90, 90))
                        {
                            imgurImage.Image = thumbnail.ToBitmapSource();
                        }
                        if (!_imgurConfiguration.AnonymousAccess || !_imgurConfiguration.TrackHistory)
                        {
                            return;
                        }

                        Log.Info().WriteLine("Storing imgur upload for hash {0} and delete hash {1}", imgurImage.Data.Id, imgurImage.Data.Deletehash);
                        _imgurConfiguration.ImgurUploadHistory.Add(imgurImage.Data.Id, imgurImage.Data.Deletehash);
                        _imgurConfiguration.RuntimeImgurHistory.Add(imgurImage.Data.Id, imgurImage);
                        // TODO: Update History - ViewModel!!!
                        // UpdateHistoryMenuItem();
                    }
                );

                if (imgurImage != null)
                {
                    uploadUrl = _imgurConfiguration.UsePageLink ? imgurImage.Data.LinkPage: imgurImage.Data.Link;
                    if (uploadUrl == null || !_imgurConfiguration.CopyLinkToClipboard)
                    {
                        return true;
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
                    return true;
                }
            }
            catch (Exception e)
            {
                Log.Error().WriteLine(e, "Error uploading.");
                MessageBox.Show(_imgurLanguage.UploadFailure + " " + e.Message);
            }
            uploadUrl = null;
            return false;
        }
    }
}