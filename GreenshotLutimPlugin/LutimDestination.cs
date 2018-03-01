/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Dapplo.Log;
using Greenshot.Gfx;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;

namespace GreenshotLutimPlugin  {
    /// <summary>
    /// Description of LutimDestination.
    /// </summary>
    [Export(typeof(IDestination))]
    public class LutimDestination : AbstractDestination {
        private static readonly LogSource Log = new LogSource();
        private readonly ILutimConfiguration _lutimConfiguration;

        [ImportingConstructor]
        public LutimDestination(ILutimConfiguration lutimConfiguration) {
			_lutimConfiguration = lutimConfiguration;
		}
		
		public override string Designation => "Lutim";

		public override string Description => Language.GetString("lutim", LangKey.upload_menu_item);

		public override Bitmap DisplayIcon {
			get {
				var resources = new ComponentResourceManager(typeof(LutimPlugin));
				return (Bitmap)resources.GetObject("Lutim");
			}
		}

		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
		    var exportInformation = new ExportInformation(Designation, Description)
		    {
		        ExportMade = Upload(captureDetails, surface, out var uploadUrl),
		        Uri = uploadUrl
		    };
		    ProcessExport(exportInformation, surface);
			return exportInformation;
		}


        /// <summary>
        /// Upload the capture to lutim
        /// </summary>
        /// <param name="captureDetails">ICaptureDetails</param>
        /// <param name="surfaceToUpload">ISurface</param>
        /// <param name="uploadUrl">out string for the url</param>
        /// <returns>true if the upload succeeded</returns>
        private bool Upload(ICaptureDetails captureDetails, ISurface surfaceToUpload, out string uploadUrl)
        {
            SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(_lutimConfiguration.UploadFormat, _lutimConfiguration.UploadJpegQuality, _lutimConfiguration.UploadReduceColors);
            try
            {
                string filename = Path.GetFileName(FilenameHelper.GetFilenameFromPattern(_lutimConfiguration.FilenamePattern, _lutimConfiguration.UploadFormat, captureDetails));
                LutimInfo lutimInfo = null;

                // Run upload in the background
                new PleaseWaitForm().ShowAndWait("Lutim plug-in", Language.GetString("lutim", LangKey.communication_wait),
                    delegate
                    {
                        lutimInfo = LutimUtils.UploadToLutim(surfaceToUpload, outputSettings, filename);
                        if (lutimInfo != null)
                        {
                            Log.Info().WriteLine("Storing lutim upload for hash {0} and delete hash {1}", lutimInfo.Short, lutimInfo.Token);
                            _lutimConfiguration.LutimUploadHistory.Add(lutimInfo.Short, lutimInfo.ToIniString());
                            _lutimConfiguration.RuntimeLutimHistory.Add(lutimInfo.Short, lutimInfo);
                            // TODO: Update
                            // UpdateHistoryMenuItem();
                        }
                    }
                );

                if (lutimInfo != null)
                {
                    // TODO: Optimize a second call for export
                    using (var tmpBitmap = surfaceToUpload.GetBitmapForExport())
                    {
                        lutimInfo.Thumb = tmpBitmap.CreateThumbnail(90, 90);
                    }
                    uploadUrl = lutimInfo.Uri.AbsoluteUri;
                    if (string.IsNullOrEmpty(uploadUrl) || !_lutimConfiguration.CopyLinkToClipboard)
                    {
                        return true;
                    }
                    try
                    {
                        ClipboardHelper.SetClipboardData(uploadUrl);
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
                MessageBox.Show(Language.GetString("lutim", LangKey.upload_failure) + " " + e.Message);
            }
            uploadUrl = null;
            return false;
        }
    }
}
