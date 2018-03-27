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
using System.ComponentModel.Composition;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Addons.Bootstrapper.Resolving;
using Dapplo.Log;
using Greenshot.Addons.Addons;
using Greenshot.Addons.Controls;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;
using Greenshot.Gfx;

#endregion

namespace Greenshot.Addon.Box
{
    [Destination("Box")]
    public class BoxDestination : AbstractDestination
	{
	    private static readonly LogSource Log = new LogSource();
        private readonly IBoxConfiguration _boxConfiguration;
	    private readonly IBoxLanguage _boxLanguage;

	    [ImportingConstructor]
		public BoxDestination(IBoxConfiguration boxConfiguration, IBoxLanguage boxLanguage)
	    {
	        _boxConfiguration = boxConfiguration;
	        _boxLanguage = boxLanguage;
	    }

	    public override string Description => _boxLanguage.UploadMenuItem;

	    public override Bitmap DisplayIcon
		{
			get
			{
                // TODO: Optimize this
			    var embeddedResource = GetType().Assembly.FindEmbeddedResources(@".*box\.png").FirstOrDefault();
			    using (var bitmapStream = GetType().Assembly.GetEmbeddedResourceAsStream(embeddedResource))
			    {
			        return BitmapHelper.FromStream(bitmapStream);
			    }
			}
		}

		public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);
			var uploadUrl = await UploadAsync(captureDetails, surface);
			if (uploadUrl != null)
			{
				exportInformation.ExportMade = true;
				exportInformation.Uri = uploadUrl;
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}

	    /// <summary>
	    ///     This will be called when the menu item in the Editor is clicked
	    /// </summary>
	    private async Task<string> UploadAsync(ICaptureDetails captureDetails, ISurface surfaceToUpload)
	    {
	        var outputSettings = new SurfaceOutputSettings(_boxConfiguration.UploadFormat, _boxConfiguration.UploadJpegQuality, false);
	        try
	        {
	            var filename = Path.GetFileName(FilenameHelper.GetFilename(_boxConfiguration.UploadFormat, captureDetails));
	            var imageToUpload = new SurfaceContainer(surfaceToUpload, outputSettings, filename);

	            string url;
	            using (var pleaseWaitForm = new PleaseWaitForm("Imgur plug-in", _boxLanguage.CommunicationWait))
	            {
	                pleaseWaitForm.Show();
	                try
	                {
	                    url = await Task.Run(() => BoxUtils.UploadToBox(imageToUpload, captureDetails.Title, filename));
	                }
	                finally
	                {
	                    pleaseWaitForm.Close();
	                }
	            }

	            if (url != null && _boxConfiguration.AfterUploadLinkToClipBoard)
	            {
	                ClipboardHelper.SetClipboardData(url);
	            }

	            return url;
	        }
	        catch (Exception ex)
	        {
	            Log.Error().WriteLine(ex, "Error uploading.");
	            MessageBox.Show(_boxLanguage.UploadFailure + " " + ex.Message);
	            return null;
	        }
	    }
    }
}