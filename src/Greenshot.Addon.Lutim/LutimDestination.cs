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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Addons.Bootstrapper.Resolving;
using Dapplo.Log;
using Greenshot.Addon.Lutim.Entities;
using Greenshot.Addons.Addons;
using Greenshot.Addons.Controls;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;
using Greenshot.Gfx;

namespace Greenshot.Addon.Lutim  {
    /// <summary>
    /// Description of LutimDestination.
    /// </summary>
    [Destination("Lutim")]
    public class LutimDestination : AbstractDestination
    {
        private static readonly LogSource Log = new LogSource();
        private readonly ILutimConfiguration _lutimConfiguration;
        private readonly ILutimLanguage _lutimLanguage;

        [ImportingConstructor]
        public LutimDestination(ILutimConfiguration lutimConfiguration, ILutimLanguage lutimLanguage)
        {
            _lutimConfiguration = lutimConfiguration;
            _lutimLanguage = lutimLanguage;
        }

		public override string Description => _lutimLanguage.UploadMenuItem;

		public override Bitmap DisplayIcon {
			get {
			    // TODO: Optimize this
			    var embeddedResource = GetType().Assembly.FindEmbeddedResources(@".*Lutim\.png").FirstOrDefault();
			    using (var bitmapStream = GetType().Assembly.GetEmbeddedResourceAsStream(embeddedResource))
			    {
			        return BitmapHelper.FromStream(bitmapStream);
			    }
            }
		}

        public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
        {
            var uploadUrl = await Upload(captureDetails, surface);

            var exportInformation = new ExportInformation(Designation, Description)
		    {
		        ExportMade = uploadUrl != null,
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
        private async Task<string> Upload(ICaptureDetails captureDetails, ISurface surfaceToUpload)
        {
            string uploadUrl;
            SurfaceOutputSettings outputSettings = new SurfaceOutputSettings(_lutimConfiguration.UploadFormat, _lutimConfiguration.UploadJpegQuality, _lutimConfiguration.UploadReduceColors);
            try
            {
                string filename = Path.GetFileName(FilenameHelper.GetFilenameFromPattern(_lutimConfiguration.FilenamePattern, _lutimConfiguration.UploadFormat, captureDetails));
                LutimInfo lutimInfo;

                var cancellationTokenSource = new CancellationTokenSource();
                using (var pleaseWaitForm = new PleaseWaitForm("Lutim", _lutimLanguage.CommunicationWait, cancellationTokenSource))
                {
                    pleaseWaitForm.Show();
                    try
                    {
                        lutimInfo = await LutimUtils.UploadToLutim(surfaceToUpload, outputSettings, filename);
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
                        pleaseWaitForm.Close();
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
                        ClipboardHelper.SetClipboardData(uploadUrl);
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
