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
using System.Xml;
using Dapplo.Addons.Bootstrapper.Resolving;
using Dapplo.HttpExtensions;
using Dapplo.HttpExtensions.OAuth;
using Dapplo.Log;
using Dapplo.Utils;
using Greenshot.Addons.Addons;
using Greenshot.Addons.Controls;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;
using Greenshot.Gfx;

#endregion

namespace Greenshot.Addon.GooglePhotos
{
    [Destination("GooglePhotos")]
    public class GooglePhotosDestination : AbstractDestination
	{
	    private static readonly LogSource Log = new LogSource();
        private readonly IGooglePhotosConfiguration _googlePhotosConfiguration;
	    private readonly IGooglePhotosLanguage _googlePhotosLanguage;
	    private readonly OAuth2Settings _oAuth2Settings;

	    [ImportingConstructor]
        public GooglePhotosDestination(IGooglePhotosConfiguration googlePhotosConfiguration, IGooglePhotosLanguage googlePhotosLanguage)
	    {
	        _googlePhotosConfiguration = googlePhotosConfiguration;
	        _googlePhotosLanguage = googlePhotosLanguage;

	        _oAuth2Settings = new OAuth2Settings
	        {
	            AuthorizationUri = new Uri("https://accounts.google.com").AppendSegments("o", "oauth2", "auth").
	                ExtendQuery(new Dictionary<string, string>
	                {
	                    {"response_type", "code"},
	                    {"client_id", "{ClientId}"},
	                    {"redirect_uri", "{RedirectUrl}"},
	                    {"state", "{State}"},
	                    {"scope", "https://picasaweb.google.com/data/"}
	                }),
	            TokenUrl = new Uri("https://www.googleapis.com/oauth2/v3/token"),
	            CloudServiceName = "GooglePhotos",
	            ClientId = googlePhotosConfiguration.ClientId,
	            ClientSecret = googlePhotosConfiguration.ClientSecret,
	            RedirectUrl = "http://getgreenshot.org",
	            AuthorizeMode = AuthorizeModes.LocalhostServer,
	            Token = googlePhotosConfiguration
            };
        }

		public override string Description => _googlePhotosLanguage.UploadMenuItem;

		public override Bitmap DisplayIcon
		{
			get
			{
			    // TODO: Optimize this
			    var embeddedResource = GetType().Assembly.FindEmbeddedResources(@".*GooglePhotos\.png").FirstOrDefault();
			    using (var bitmapStream = GetType().Assembly.GetEmbeddedResourceAsStream(embeddedResource))
			    {
			        return BitmapHelper.FromStream(bitmapStream);
			    }
			}
		}

	    public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);
		    var uploadUrl = await Upload(captureDetails, surface);
			if (uploadUrl != null)
			{
				exportInformation.ExportMade = true;
				exportInformation.Uri = uploadUrl;
			}
			ProcessExport(exportInformation, surface);
			return exportInformation;
		}

	    public async Task<string> Upload(ICaptureDetails captureDetails, ISurface surfaceToUpload)
	    {
	        try
            {
                string url;
                using (var pleaseWaitForm = new PleaseWaitForm("GooglePhotos", _googlePhotosLanguage.CommunicationWait))
	            {
	                pleaseWaitForm.Show();
	                try
	                {
	                    url = await UploadToPicasa(surfaceToUpload);
	                }
	                finally
	                {
	                    pleaseWaitForm.Close();
	                }
	            }

	            if (url != null && _googlePhotosConfiguration.AfterUploadLinkToClipBoard)
	            {
	                ClipboardHelper.SetClipboardData(url);
	            }
	            return url;
	        }
	        catch (Exception e)
	        {
	            Log.Error().WriteLine(e, "Error uploading.");
	            MessageBox.Show(_googlePhotosLanguage.UploadFailure + " " + e.Message);
	        }
	        return null;
	    }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="progress"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<string> UploadToPicasa(ISurface surface, IProgress<int> progress = null, CancellationToken token = default)
        {
            string filename = Path.GetFileName(FilenameHelper.GetFilename(_googlePhotosConfiguration.UploadFormat, surface.CaptureDetails));
            var outputSettings = new SurfaceOutputSettings(_googlePhotosConfiguration.UploadFormat, _googlePhotosConfiguration.UploadJpegQuality);
            // Fill the OAuth2Settings
            
            var oAuthHttpBehaviour = HttpBehaviour.Current.ShallowClone();

            // Use UploadProgress
            if (progress != null)
            {
                oAuthHttpBehaviour.UploadProgress = percent => { UiContext.RunOn(() => progress.Report((int)(percent * 100))); };
            }
            oAuthHttpBehaviour.OnHttpMessageHandlerCreated = httpMessageHandler => new OAuth2HttpMessageHandler(_oAuth2Settings, oAuthHttpBehaviour, httpMessageHandler);
            if (_googlePhotosConfiguration.AddFilename)
            {
                oAuthHttpBehaviour.OnHttpClientCreated = httpClient => httpClient.AddDefaultRequestHeader("Slug", Uri.EscapeDataString(filename));
            }

            string response;
            var uploadUri = new Uri("https://picasaweb.google.com/data/feed/api/user").AppendSegments(_googlePhotosConfiguration.UploadUser, "albumid", _googlePhotosConfiguration.UploadAlbum);
            using (var imageStream = new MemoryStream())
            {
                ImageOutput.SaveToStream(surface, imageStream, outputSettings);
                imageStream.Position = 0;
                using (var content = new StreamContent(imageStream))
                {
                    content.Headers.Add("Content-Type", "image/" + outputSettings.Format);

                    oAuthHttpBehaviour.MakeCurrent();
                    response = await uploadUri.PostAsync<string>(content, token);
                }
            }

            return ParseResponse(response);
        }
	    /// <summary>
	    ///     Parse the upload URL from the response
	    /// </summary>
	    /// <param name="response"></param>
	    /// <returns></returns>
	    public static string ParseResponse(string response)
	    {
	        if (response == null)
	        {
	            return null;
	        }
	        try
	        {
	            var doc = new XmlDocument();
	            doc.LoadXml(response);
	            var nodes = doc.GetElementsByTagName("link", "*");
	            if (nodes.Count > 0)
	            {
	                string url = null;
	                foreach (XmlNode node in nodes)
	                {
	                    if (node.Attributes != null)
	                    {
	                        url = node.Attributes["href"].Value;
	                        string rel = node.Attributes["rel"].Value;
	                        // Pictures with rel="http://schemas.google.com/photos/2007#canonical" are the direct link
	                        if ((rel != null) && rel.EndsWith("canonical"))
	                        {
	                            break;
	                        }
	                    }
	                }
	                return url;
	            }
	        }
	        catch (Exception e)
	        {
	            Log.Error().WriteLine("Could not parse Picasa response due to error {0}, response was: {1}", e.Message, response);
	        }
	        return null;
	    }
    }
}