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

namespace Greenshot.Addon.Flickr
{
    [Destination("Flickr")]
    public class FlickrDestination : AbstractDestination
	{
	    private static readonly LogSource Log = new LogSource();
        private readonly IFlickrConfiguration _flickrConfiguration;
	    private readonly IFlickrLanguage _flickrLanguage;

	    [ImportingConstructor]
        public FlickrDestination(IFlickrConfiguration flickrConfiguration, IFlickrLanguage flickrLanguage)
	    {
	        _flickrConfiguration = flickrConfiguration;
	        _flickrLanguage = flickrLanguage;
	    }

		public override string Description => _flickrLanguage.UploadMenuItem;

		public override Bitmap DisplayIcon
		{
			get
			{
			    // TODO: Optimize this
			    var embeddedResource = GetType().Assembly.FindEmbeddedResources(@".*flickr\.png").FirstOrDefault();
			    using (var bitmapStream = GetType().Assembly.GetEmbeddedResourceAsStream(embeddedResource))
			    {
			        return BitmapHelper.FromStream(bitmapStream);
			    }
            }
		}

		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);
		    var uploaded = Upload(captureDetails, surface, out var uploadUrl);
			if (uploaded)
			{
				exportInformation.ExportMade = true;
				exportInformation.Uri = uploadUrl;
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}


	    public bool Upload(ICaptureDetails captureDetails, ISurface surface, out string uploadUrl)
	    {
	        var outputSettings = new SurfaceOutputSettings(_flickrConfiguration.UploadFormat, _flickrConfiguration.UploadJpegQuality, false);
	        uploadUrl = null;
	        try
	        {
	            string flickrUrl = null;
	            new PleaseWaitForm().ShowAndWait("Flickr", _flickrLanguage.CommunicationWait,
	                delegate
	                {
	                    var filename = Path.GetFileName(FilenameHelper.GetFilename(_flickrConfiguration.UploadFormat, captureDetails));
	                    flickrUrl = FlickrUtils.UploadToFlickr(surface, outputSettings, captureDetails.Title, filename);
	                }
	            );

	            if (flickrUrl == null)
	            {
	                return false;
	            }
	            uploadUrl = flickrUrl;

	            if (_flickrConfiguration.AfterUploadLinkToClipBoard)
	            {
	                ClipboardHelper.SetClipboardData(flickrUrl);
	            }
	            return true;
	        }
	        catch (Exception e)
	        {
	            Log.Error().WriteLine(e, "Error uploading.");
	            MessageBox.Show(_flickrLanguage.UploadFailure + " " + e.Message);
	        }
	        return false;
	    }
    }
}