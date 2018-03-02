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
using GreenshotPlugin.Core;
using Dapplo.Log;
using GreenshotPlugin.Addons;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;

#endregion

namespace GreenshotDropboxPlugin
{
    [Destination("Dropbox")]
    public class DropboxDestination : AbstractDestination
	{
	    private static readonly LogSource Log = new LogSource();
        private readonly IDropboxPluginConfiguration _dropboxPluginConfiguration;

        [ImportingConstructor]
	    public DropboxDestination(IDropboxPluginConfiguration dropboxPluginConfiguration)
	    {
	        _dropboxPluginConfiguration = dropboxPluginConfiguration;
	    }

		public override Bitmap DisplayIcon
		{
			get
			{
				var resources = new ComponentResourceManager(typeof(DropboxPlugin));
				return (Bitmap)resources.GetObject("Dropbox");
			}
		}

		public override string Description => Language.GetString("dropbox", LangKey.upload_menu_item);

		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);
		    var uploaded = Upload(captureDetails, surface, out var uploadUrl);
			if (uploaded)
			{
				exportInformation.Uri = uploadUrl;
				exportInformation.ExportMade = true;
				if (_dropboxPluginConfiguration.AfterUploadLinkToClipBoard)
				{
					ClipboardHelper.SetClipboardData(uploadUrl);
				}
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}

	    /// <summary>
	    ///     This will be called when the menu item in the Editor is clicked
	    /// </summary>
	    private bool Upload(ICaptureDetails captureDetails, ISurface surfaceToUpload, out string uploadUrl)
	    {
	        uploadUrl = null;
	        var outputSettings = new SurfaceOutputSettings(_dropboxPluginConfiguration.UploadFormat, _dropboxPluginConfiguration.UploadJpegQuality, false);
	        try
	        {
	            string dropboxUrl = null;
	            new PleaseWaitForm().ShowAndWait("Dropbox", Language.GetString("dropbox", LangKey.communication_wait),
	                delegate
	                {
	                    var filename = Path.GetFileName(FilenameHelper.GetFilename(_dropboxPluginConfiguration.UploadFormat, captureDetails));
	                    dropboxUrl = DropboxUtils.UploadToDropbox(surfaceToUpload, outputSettings, filename);
	                }
	            );
	            if (dropboxUrl == null)
	            {
	                return false;
	            }
	            uploadUrl = dropboxUrl;
	            return true;
	        }
	        catch (Exception e)
	        {
	            Log.Error().WriteLine(e);
	            MessageBox.Show(Language.GetString("dropbox", LangKey.upload_failure) + " " + e.Message);
	            return false;
	        }
	    }
    }
}