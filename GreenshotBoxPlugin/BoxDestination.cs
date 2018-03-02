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
using GreenshotPlugin.Addons;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;

#endregion

namespace GreenshotBoxPlugin
{
    [Destination("Box")]
    public class BoxDestination : AbstractDestination
	{
	    private static readonly LogSource Log = new LogSource();
        private readonly IBoxConfiguration _boxConfiguration;

	    [ImportingConstructor]
		public BoxDestination(IBoxConfiguration boxConfiguration)
	    {
	        _boxConfiguration = boxConfiguration;
	    }

	    public override string Description => Language.GetString("box", LangKey.upload_menu_item);

	    public override Bitmap DisplayIcon
		{
			get
			{
				var resources = new ComponentResourceManager(typeof(BoxPlugin));
				return (Bitmap) resources.GetObject("Box");
			}
		}

		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);
			var uploadUrl = Upload(captureDetails, surface);
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
	    private string Upload(ICaptureDetails captureDetails, ISurface surfaceToUpload)
	    {
	        var outputSettings = new SurfaceOutputSettings(_boxConfiguration.UploadFormat, _boxConfiguration.UploadJpegQuality, false);
	        try
	        {
	            string url = null;
	            var filename = Path.GetFileName(FilenameHelper.GetFilename(_boxConfiguration.UploadFormat, captureDetails));
	            var imageToUpload = new SurfaceContainer(surfaceToUpload, outputSettings, filename);

	            new PleaseWaitForm().ShowAndWait("Box", Language.GetString("box", LangKey.communication_wait),
	                delegate { url = BoxUtils.UploadToBox(imageToUpload, captureDetails.Title, filename); }
	            );

	            if (url != null && _boxConfiguration.AfterUploadLinkToClipBoard)
	            {
	                ClipboardHelper.SetClipboardData(url);
	            }

	            return url;
	        }
	        catch (Exception ex)
	        {
	            Log.Error().WriteLine(ex, "Error uploading.");
	            MessageBox.Show(Language.GetString("box", LangKey.upload_failure) + " " + ex.Message);
	            return null;
	        }
	    }

    }
}