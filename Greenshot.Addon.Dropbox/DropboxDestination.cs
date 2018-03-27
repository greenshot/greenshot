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

namespace Greenshot.Addon.Dropbox
{
    [Destination("Dropbox")]
    public class DropboxDestination : AbstractDestination
	{
	    private static readonly LogSource Log = new LogSource();
        private readonly IDropboxConfiguration _dropboxPluginConfiguration;
	    private readonly IDropboxLanguage _dropboxLanguage;

	    [ImportingConstructor]
	    public DropboxDestination(IDropboxConfiguration dropboxPluginConfiguration, IDropboxLanguage dropboxLanguage)
	    {
	        _dropboxPluginConfiguration = dropboxPluginConfiguration;
	        _dropboxLanguage = dropboxLanguage;
	    }

		public override Bitmap DisplayIcon
		{
			get
			{
			    // TODO: Optimize this
			    var embeddedResource = GetType().Assembly.FindEmbeddedResources(@".*Dropbox\.gif").FirstOrDefault();
			    using (var bitmapStream = GetType().Assembly.GetEmbeddedResourceAsStream(embeddedResource))
			    {
			        return BitmapHelper.FromStream(bitmapStream);
			    }
            }
		}

		public override string Description => _dropboxLanguage.UploadMenuItem;

	    protected override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
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
	            new PleaseWaitForm().ShowAndWait("Dropbox", _dropboxLanguage.CommunicationWait,
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
	            MessageBox.Show(_dropboxLanguage.UploadFailure + " " + e.Message);
	            return false;
	        }
	    }
    }
}