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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Addons.Bootstrapper.Resolving;
using Dapplo.HttpExtensions;
using Dapplo.HttpExtensions.Extensions;
using Dapplo.HttpExtensions.Listener;
using Dapplo.HttpExtensions.OAuth;
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
	    private static readonly Uri FlickrOAuthUri = new Uri("https://api.flickr.com/services/oauth");
        private readonly IFlickrConfiguration _flickrConfiguration;
	    private readonly IFlickrLanguage _flickrLanguage;
	    private readonly OAuth1Settings _oAuthSettings;
	    private readonly OAuth1HttpBehaviour _oAuthHttpBehaviour;

	    [ImportingConstructor]
        public FlickrDestination(IFlickrConfiguration flickrConfiguration, IFlickrLanguage flickrLanguage)
	    {
	        _flickrConfiguration = flickrConfiguration;
	        _flickrLanguage = flickrLanguage;

            _oAuthSettings = new OAuth1Settings
	        {
	            Token = flickrConfiguration,
	            ClientId = flickrConfiguration.ClientId,
	            ClientSecret = flickrConfiguration.ClientSecret,
	            CloudServiceName = "Flickr",
	            AuthorizeMode = AuthorizeModes.LocalhostServer,
	            TokenUrl = FlickrOAuthUri.AppendSegments("request_token"),
	            TokenMethod = HttpMethod.Post,
	            AccessTokenUrl = FlickrOAuthUri.AppendSegments("access_token"),
	            AccessTokenMethod = HttpMethod.Post,
	            AuthorizationUri = FlickrOAuthUri.AppendSegments("authorize")
	                .ExtendQuery(new Dictionary<string, string>
	                {
	                    {OAuth1Parameters.Token.EnumValueOf(), "{RequestToken}"},
	                    {OAuth1Parameters.Callback.EnumValueOf(), "{RedirectUrl}"}
	                }),
	            // Create a localhost redirect uri, prefer port 47336, but use the first free found
	            RedirectUrl = new[] { 47336, 0 }.CreateLocalHostUri().AbsoluteUri,
	            CheckVerifier = true
	        };

	        _oAuthHttpBehaviour = OAuth1HttpBehaviourFactory.Create(_oAuthSettings);
	        _oAuthHttpBehaviour.ValidateResponseContentType = false;
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

	    public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
	    {
	        var flickrUri = await Upload(captureDetails, surface);

	        var exportInformation = new ExportInformation(Designation, Description)
	        {
	            ExportMade = flickrUri != null,
	            Uri = flickrUri
	        };
	        ProcessExport(exportInformation, surface);
			return exportInformation;
		}


	    public async Task<string> Upload(ICaptureDetails captureDetails, ISurface surface)
	    {
	        string uploadUrl = null;

            var outputSettings = new SurfaceOutputSettings(_flickrConfiguration.UploadFormat, _flickrConfiguration.UploadJpegQuality, false);
	        try
	        {

	            var cancellationTokenSource = new CancellationTokenSource();
	            using (var pleaseWaitForm = new PleaseWaitForm("Flickr", _flickrLanguage.CommunicationWait, cancellationTokenSource))
	            {
	                pleaseWaitForm.Show();
	                try
	                {
	                    var filename = Path.GetFileName(FilenameHelper.GetFilename(_flickrConfiguration.UploadFormat, captureDetails));
	                    uploadUrl = await FlickrUtils.UploadToFlickrAsync(surface, outputSettings, captureDetails.Title, filename);
                    }
	                finally
	                {
	                    pleaseWaitForm.Close();
	                }
	            }
               

	            if (uploadUrl == null)
	            {
	                return null;
	            }
	            if (_flickrConfiguration.AfterUploadLinkToClipBoard)
	            {
	                ClipboardHelper.SetClipboardData(uploadUrl);
	            }
	            
	        }
	        catch (Exception e)
	        {
	            Log.Error().WriteLine(e, "Error uploading.");
	            MessageBox.Show(_flickrLanguage.UploadFailure + " " + e.Message);
	        }
	        return uploadUrl;
	    }
    }
}