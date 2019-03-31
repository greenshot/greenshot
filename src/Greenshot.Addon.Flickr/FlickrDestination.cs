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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using Autofac.Features.OwnedInstances;
using Dapplo.Addons;
using Dapplo.HttpExtensions;
using Dapplo.HttpExtensions.Extensions;
using Dapplo.HttpExtensions.OAuth;
using Dapplo.Log;
using Dapplo.Windows.Clipboard;
using Greenshot.Addon.Flickr.Configuration;
using Greenshot.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Controls;
using Greenshot.Addons.Core;
using Greenshot.Addons.Extensions;
using Greenshot.Addons.Interfaces;
using Greenshot.Gfx;

namespace Greenshot.Addon.Flickr
{
    /// <summary>
    /// This defines the flickr destination
    /// </summary>
    [Destination("Flickr")]
    public sealed class FlickrDestination : AbstractDestination
	{
	    private static readonly LogSource Log = new LogSource();
	    private static readonly Uri FlickrOAuthUri = new Uri("https://api.flickr.com/services/oauth");
        private readonly IFlickrConfiguration _flickrConfiguration;
	    private readonly IFlickrLanguage _flickrLanguage;
	    private readonly IResourceProvider _resourceProvider;
	    private readonly ExportNotification _exportNotification;
	    private readonly Func<CancellationTokenSource, Owned<PleaseWaitForm>> _pleaseWaitFormFactory;
	    private readonly OAuth1Settings _oAuthSettings;
	    private readonly OAuth1HttpBehaviour _oAuthHttpBehaviour;
	    private const string FlickrFarmUrl = "https://farm{0}.staticflickr.com/{1}/{2}_{3}.jpg";
	    private static readonly Uri FlickrApiBaseUrl = new Uri("https://api.flickr.com/services");
	    private static readonly Uri FlickrUploadUri = new Uri("https://up.flickr.com/services/upload");
	    private static readonly Uri FlickrRestUrl = FlickrApiBaseUrl.AppendSegments("rest");

	    private static readonly Uri FlickrGetInfoUrl = FlickrRestUrl.ExtendQuery(new Dictionary<string, object>
	    {
	        {
	            "method", "flickr.photos.getInfo"
	        }
	    });

        public FlickrDestination(
            IFlickrConfiguration flickrConfiguration,
            IFlickrLanguage flickrLanguage,
            IHttpConfiguration httpConfiguration,
            IResourceProvider resourceProvider,
            ICoreConfiguration coreConfiguration,
	        IGreenshotLanguage greenshotLanguage,
            ExportNotification exportNotification,
            Func<CancellationTokenSource, Owned<PleaseWaitForm>> pleaseWaitFormFactory
        ) : base(coreConfiguration, greenshotLanguage)
        {
	        _flickrConfiguration = flickrConfiguration;
	        _flickrLanguage = flickrLanguage;
	        _resourceProvider = resourceProvider;
            _exportNotification = exportNotification;
            _pleaseWaitFormFactory = pleaseWaitFormFactory;

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
                // TODO: Remove this, use Greenshot redirect like with Imgur, doesn't work anyway
	            // Create a localhost redirect uri, prefer port 47336, but use the first free found
	            RedirectUrl = "http://localhost:47336",//new[] { 47336 }.CreateLocalHostUri().AbsoluteUri,
	            CheckVerifier = true
	        };

	        _oAuthHttpBehaviour = OAuth1HttpBehaviourFactory.Create(_oAuthSettings);
	        _oAuthHttpBehaviour.ValidateResponseContentType = false;
            // Use the default network settings
	        _oAuthHttpBehaviour.HttpSettings = httpConfiguration;
	    }

		public override string Description => _flickrLanguage.UploadMenuItem;

		public override IBitmapWithNativeSupport DisplayIcon
		{
			get
			{
                // TODO: Optimize this by caching
			    using (var bitmapStream = _resourceProvider.ResourceAsStream(GetType().Assembly, "flickr.png"))
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
	        _exportNotification.NotifyOfExport(this, exportInformation, surface);
            return exportInformation;
		}


	    public async Task<string> Upload(ICaptureDetails captureDetails, ISurface surface)
	    {
	        string uploadUrl = null;

	        try
	        {

	            var cancellationTokenSource = new CancellationTokenSource();
	            using (var ownedPleaseWaitForm = _pleaseWaitFormFactory(cancellationTokenSource))
	            {
	                ownedPleaseWaitForm.Value.SetDetails("Flickr", _flickrLanguage.CommunicationWait);
                    ownedPleaseWaitForm.Value.Show();
	                try
	                {
	                    uploadUrl = await UploadToFlickrAsync(surface, captureDetails.Title, cancellationTokenSource.Token);
                    }
	                finally
	                {
	                    ownedPleaseWaitForm.Value.Close();
	                }
	            }
               

	            if (uploadUrl == null)
	            {
	                return null;
	            }
	            if (_flickrConfiguration.AfterUploadLinkToClipBoard)
	            {
	                using (var clipboardAccessToken = ClipboardNative.Access())
	                {
	                    clipboardAccessToken.ClearContents();
                        clipboardAccessToken.SetAsUrl(uploadUrl);
	                }
                }
	            
	        }
	        catch (Exception e)
	        {
	            Log.Error().WriteLine(e, "Error uploading.");
	            MessageBox.Show(_flickrLanguage.UploadFailure + @" " + e.Message);
	        }
	        return uploadUrl;
	    }

        /// <summary>
        ///     Do the actual upload to Flickr
        ///     For more details on the available parameters, see: http://flickrnet.codeplex.com
        /// </summary>
        /// <param name="surfaceToUpload"></param>
        /// <param name="title"></param>
        /// <param name="token"></param>
        /// <returns>url to image</returns>
        public async Task<string> UploadToFlickrAsync(ISurface surfaceToUpload, string title, CancellationToken token = default)
        {
            var filename = surfaceToUpload.GenerateFilename(CoreConfiguration, _flickrConfiguration);
            try
            {
                var signedParameters = new Dictionary<string, object>
                {
                    {"content_type", "2"}, // content = screenshot
					{"tags", "Greenshot"},
                    {"is_public", _flickrConfiguration.IsPublic ? "1" : "0"},
                    {"is_friend", _flickrConfiguration.IsFriend ? "1" : "0"},
                    {"is_family", _flickrConfiguration.IsFamily ? "1" : "0"},
                    {"safety_level", $"{(int) _flickrConfiguration.SafetyLevel}"},
                    {"hidden", _flickrConfiguration.HiddenFromSearch ? "1" : "2"},
                    {"format", "json"}, // Doesn't work... :(
					{"nojsoncallback", "1"}
                };

                string photoId;
                using (var stream = new MemoryStream())
                {
                    surfaceToUpload.WriteToStream(stream, CoreConfiguration, _flickrConfiguration);
                    stream.Position = 0;
                    using (var streamContent = new StreamContent(stream))
                    {
                        streamContent.Headers.ContentType = new MediaTypeHeaderValue(surfaceToUpload.GenerateMimeType(CoreConfiguration, _flickrConfiguration));
                        streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                        {
                            Name = "\"photo\"",
                            FileName = "\"" + filename + "\""
                        };
                        HttpBehaviour.Current.SetConfig(new HttpRequestMessageConfiguration
                        {
                            Properties = signedParameters
                        });
                        var response = await FlickrUploadUri.PostAsync<XDocument>(streamContent, token).ConfigureAwait(false);
                        photoId = (from element in response?.Root?.Elements() ?? Enumerable.Empty<XElement>()
                                   where element.Name == "photoid"
                                   select element.Value).FirstOrDefault();
                    }
                }

                // Get Photo Info
                signedParameters = new Dictionary<string, object>
                {
                    {"photo_id", photoId},
                    {"format", "json"},
                    {"nojsoncallback", "1"}
                };
                HttpBehaviour.Current.SetConfig(new HttpRequestMessageConfiguration
                {
                    Properties = signedParameters
                });
                var photoInfo = await FlickrGetInfoUrl.PostAsync<dynamic>(signedParameters, token).ConfigureAwait(false);
                if (_flickrConfiguration.UsePageLink)
                {
                    return photoInfo.photo.urls.url[0]._content;
                }
                return string.Format(FlickrFarmUrl, photoInfo.photo.farm, photoInfo.photo.server, photoId, photoInfo.photo.secret);
            }
            catch (Exception ex)
            {
                Log.Error().WriteLine(ex, "Upload error: ");
                throw;
            }
        }
    }
}