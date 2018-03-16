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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Dapplo.Log;
using Greenshot.Gfx;
using GreenshotPlugin.Addons;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;

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

        [ImportingConstructor]
		public ImgurDestination(IImgurConfiguration imgurConfiguration)
		{
			_imgurConfiguration = imgurConfiguration;
		}

		public override string Description => Language.GetString("imgur", LangKey.upload_menu_item);

		public override Bitmap DisplayIcon
		{
			get
			{
				var resources = new ComponentResourceManager(typeof(ImgurPlugin));
				return (Bitmap) resources.GetObject("Imgur");
			}
		}

		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
		    var exportInformation = new ExportInformation(Designation, Description)
		    {
		        ExportMade = Upload(captureDetails, surface, out var uploadUrl),
		        Uri = uploadUrl
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
        private bool Upload(ICaptureDetails captureDetails, ISurface surfaceToUpload, out string uploadUrl)
        {
            SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(_imgurConfiguration.UploadFormat, _imgurConfiguration.UploadJpegQuality, _imgurConfiguration.UploadReduceColors);
            try
            {
                string filename = Path.GetFileName(FilenameHelper.GetFilenameFromPattern(_imgurConfiguration.FilenamePattern, _imgurConfiguration.UploadFormat, captureDetails));
                ImgurInfo imgurInfo = null;

                // Run upload in the background
                new PleaseWaitForm().ShowAndWait("Imgur plug-in", Language.GetString("imgur", LangKey.communication_wait),
                    delegate
                    {
                        imgurInfo = ImgurUtils.UploadToImgur(surfaceToUpload, outputSettings, captureDetails.Title, filename);
                        if (imgurInfo != null && _imgurConfiguration.AnonymousAccess)
                        {
                            Log.Info().WriteLine("Storing imgur upload for hash {0} and delete hash {1}", imgurInfo.Hash, imgurInfo.DeleteHash);
                            _imgurConfiguration.ImgurUploadHistory.Add(imgurInfo.Hash, imgurInfo.DeleteHash);
                            _imgurConfiguration.RuntimeImgurHistory.Add(imgurInfo.Hash, imgurInfo);
                            // TODO: Update History!!!
                            // UpdateHistoryMenuItem();
                        }
                    }
                );

                if (imgurInfo != null)
                {
                    // TODO: Optimize a second call for export
                    using (var tmpImage = surfaceToUpload.GetBitmapForExport())
                    {
                        imgurInfo.Image = tmpImage.CreateThumbnail(90, 90);
                    }

                    if (_imgurConfiguration.UsePageLink)
                    {
                        uploadUrl = imgurInfo.Page;
                    }
                    else
                    {
                        uploadUrl = imgurInfo.Original;
                    }
                    if (!string.IsNullOrEmpty(uploadUrl) && _imgurConfiguration.CopyLinkToClipboard)
                    {
                        try
                        {
                            ClipboardHelper.SetClipboardData(uploadUrl);

                        }
                        catch (Exception ex)
                        {
                            Log.Error().WriteLine(ex, "Can't write to clipboard: ");
                            uploadUrl = null;
                        }
                    }
                    return true;
                }
            }
            catch (Exception e)
            {
                Log.Error().WriteLine(e, "Error uploading.");
                MessageBox.Show(Language.GetString("imgur", LangKey.upload_failure) + " " + e.Message);
            }
            uploadUrl = null;
            return false;
        }
    }
}