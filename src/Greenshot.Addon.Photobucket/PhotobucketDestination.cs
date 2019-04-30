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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Addons;
using Dapplo.HttpExtensions;
using Dapplo.HttpExtensions.Extensions;
using Dapplo.HttpExtensions.OAuth;
using Dapplo.Log;
using Dapplo.Utils;
using Greenshot.Addon.Photobucket.Configuration;
using Greenshot.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Core;
using Greenshot.Addons.Extensions;
using Greenshot.Addons.Interfaces;
using Greenshot.Gfx;

namespace Greenshot.Addon.Photobucket
{
    /// <summary>
    ///     Description of PhotobucketDestination.
    /// </summary>
    [Destination("Photobucket")]
    public class PhotobucketDestination : AbstractDestination
	{
	    private static readonly Uri PhotobucketApiUri = new Uri("http://api.photobucket.com");
        private static readonly LogSource Log = new LogSource();
        private readonly string _albumPath;
		private readonly IPhotobucketConfiguration _photobucketConfiguration;
	    private readonly IPhotobucketLanguage _photobucketLanguage;
	    private readonly IHttpConfiguration _httpConfiguration;
	    private readonly IResourceProvider _resourceProvider;
	    private readonly ExportNotification _exportNotification;
	    private readonly OAuth1Settings _oAuthSettings;
	    private readonly OAuth1HttpBehaviour _oAuthHttpBehaviour;
	    private IList<string> _albumsCache;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="photobucketConfiguration">IPhotobucketConfiguration</param>
        /// <param name="photobucketLanguage">IPhotobucketLanguage</param>
        /// <param name="httpConfiguration">IHttpConfiguration</param>
        /// <param name="resourceProvider">IResourceProvider</param>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="greenshotLanguage">IGreenshotLanguage</param>
        /// <param name="exportNotification">ExportNotification</param>
        public PhotobucketDestination(
            IPhotobucketConfiguration photobucketConfiguration,
            IPhotobucketLanguage photobucketLanguage,
            IHttpConfiguration httpConfiguration,
            IResourceProvider resourceProvider,
	        ICoreConfiguration coreConfiguration,
	        IGreenshotLanguage greenshotLanguage,
            ExportNotification exportNotification
        ) : base(coreConfiguration, greenshotLanguage)
        {
	        _photobucketConfiguration = photobucketConfiguration;
	        _photobucketLanguage = photobucketLanguage;
	        _httpConfiguration = httpConfiguration;
	        _resourceProvider = resourceProvider;
            _exportNotification = exportNotification;

            _oAuthSettings = new OAuth1Settings
            {
                Token = photobucketConfiguration,
                ClientId = photobucketConfiguration.ClientId,
                ClientSecret = photobucketConfiguration.ClientSecret,
                CloudServiceName = "Photobucket",
                EmbeddedBrowserWidth = 1010,
                EmbeddedBrowserHeight = 400,
                AuthorizeMode = AuthorizeModes.EmbeddedBrowser,
                TokenUrl = PhotobucketApiUri.AppendSegments("login", "request"),
                TokenMethod = HttpMethod.Post,
                AccessTokenUrl = PhotobucketApiUri.AppendSegments("login", "access"),
                AccessTokenMethod = HttpMethod.Post,
                AuthorizationUri = PhotobucketApiUri.AppendSegments("apilogin", "login")
                    .ExtendQuery(new Dictionary<string, string>
                    {
                        {OAuth1Parameters.Token.EnumValueOf(), "{RequestToken}"},
                        {OAuth1Parameters.Callback.EnumValueOf(), "{RedirectUrl}"}
                    }),
                RedirectUrl = "http://getgreenshot.org",
                CheckVerifier = false
            };
            var oAuthHttpBehaviour = OAuth1HttpBehaviourFactory.Create(_oAuthSettings);
            oAuthHttpBehaviour.HttpSettings = httpConfiguration;
            // Store the leftover values
            oAuthHttpBehaviour.OnAccessTokenValues = values =>
            {
                if (values != null && values.ContainsKey("subdomain"))
                {
                    photobucketConfiguration.SubDomain = values["subdomain"];
                }
                if (values != null && values.ContainsKey("username"))
                {
                    photobucketConfiguration.Username = values["username"];
                }
            };

            oAuthHttpBehaviour.BeforeSend = httpRequestMessage =>
            {
                if (photobucketConfiguration.SubDomain == null)
                {
                    return;
                }

                var uriBuilder = new UriBuilder(httpRequestMessage.RequestUri)
                {
                    Host = photobucketConfiguration.SubDomain
                };
                httpRequestMessage.RequestUri = uriBuilder.Uri;
            };
            // Reset the OAuth token if there is no subdomain, without this we need to request it again.
            if (photobucketConfiguration.SubDomain == null || photobucketConfiguration.Username == null)
            {
                photobucketConfiguration.OAuthToken = null;
                photobucketConfiguration.OAuthTokenSecret = null;
            }
            _oAuthHttpBehaviour = oAuthHttpBehaviour;
        }

        /// <summary>
        /// DI constructor
        /// </summary>
        /// <param name="photobucketConfiguration">IPhotobucketConfiguration</param>
        /// <param name="photobucketLanguage">IPhotobucketLanguage</param>
        /// <param name="httpConfiguration">IHttpConfiguration</param>
        /// <param name="resourceProvider">IResourceProvider</param>
        /// <param name="albumPath">string</param>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="greenshotLanguage">IGreenshotLanguage</param>
        /// <param name="exportNotification">ExportNotification</param>
        protected PhotobucketDestination(
	        IPhotobucketConfiguration photobucketConfiguration,
	        IPhotobucketLanguage photobucketLanguage,
	        IHttpConfiguration httpConfiguration,
	        IResourceProvider resourceProvider,
	        string albumPath,
	        ICoreConfiguration coreConfiguration,
	        IGreenshotLanguage greenshotLanguage,
	        ExportNotification exportNotification) : this(photobucketConfiguration, photobucketLanguage, httpConfiguration, resourceProvider, coreConfiguration, greenshotLanguage, exportNotification)
		{
			_photobucketConfiguration = photobucketConfiguration;
			_albumPath = albumPath;
		}

        /// <inheritdoc />
        public override string Description
		{
			get
			{
				if (_albumPath != null)
				{
					return _albumPath;
				}
				return _photobucketLanguage.UploadMenuItem;
			}
		}

        /// <inheritdoc />
        public override IBitmapWithNativeSupport DisplayIcon
		{
			get
			{
                // TODO: Optimize this
			    using (var bitmapStream = _resourceProvider.ResourceAsStream(GetType().Assembly, "photobucket-logo.png"))
			    {
			        return BitmapHelper.FromStream(bitmapStream);
			    }
            }
		}

        /// <inheritdoc />
        public override bool IsDynamic => true;

        /// <inheritdoc />
        public override IEnumerable<IDestination> DynamicDestinations()
		{
			IList<string> albums = null;
			try
			{
				albums = Task.Run(RetrievePhotobucketAlbums).Result;
			    _albumsCache = albums;
			}
			catch (Exception ex)
			{
                Log.Error().WriteLine(ex, "Couldn't retrieve the photobucket albums.");
			}

			if (albums == null || albums.Count == 0)
			{
				yield break;
			}
			foreach (var album in albums)
			{
				yield return new PhotobucketDestination(
				    _photobucketConfiguration,
				    _photobucketLanguage,
				    _httpConfiguration,
				    _resourceProvider,
				    album,
				    CoreConfiguration,
				    GreenshotLanguage,
                    _exportNotification
				    );
			}
		}

		/// <summary>
		///     Export the capture to Photobucket
		/// </summary>
		/// <param name="manuallyInitiated"></param>
		/// <param name="surface"></param>
		/// <param name="captureDetails"></param>
		/// <returns></returns>
		public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);
            var uploaded = await UploadToPhotobucket(surface,_albumPath, captureDetails.Title);
			if (uploaded != null)
			{
				exportInformation.ExportMade = true;
				exportInformation.Uri = uploaded.Original;
			}
		    _exportNotification.NotifyOfExport(this, exportInformation, surface);
            return exportInformation;
		}


        /// <summary>
		///     Do the actual upload to Photobucket
		///     For more details on the available parameters, see: http://api.Photobucket.com/resources_anon
		/// </summary>
		/// <returns>PhotobucketResponse</returns>
		public async Task<PhotobucketInfo> UploadToPhotobucket(ISurface surfaceToUpload, string albumPath, string title, IProgress<int> progress = null, CancellationToken token = default)
        {
            string responseString;
            var filename = surfaceToUpload.GenerateFilename(CoreConfiguration, _photobucketConfiguration);
            var oAuthHttpBehaviour = _oAuthHttpBehaviour.ShallowClone();

            // Use UploadProgress
            if (progress != null)
            {
                oAuthHttpBehaviour.UploadProgress = percent => { UiContext.RunOn(() => progress.Report((int)(percent * 100)), token); };
            }
            _oAuthHttpBehaviour.MakeCurrent();
            if (_photobucketConfiguration.Username == null || _photobucketConfiguration.SubDomain == null)
            {
                await PhotobucketApiUri.AppendSegments("users").ExtendQuery("format", "json").GetAsAsync<object>(token);
            }
            if (_photobucketConfiguration.Album == null)
            {
                _photobucketConfiguration.Album = _photobucketConfiguration.Username;
            }
            var uploadUri = PhotobucketApiUri.AppendSegments("album", _photobucketConfiguration.Album, "upload");

            var signedParameters = new Dictionary<string, object> { { "type", "image" } };
            // add type
            // add title
            if (title != null)
            {
                signedParameters.Add("title", title);
            }
            // add filename
            if (filename != null)
            {
                signedParameters.Add("filename", filename);
            }
            // Add image
            using (var imageStream = new MemoryStream())
            {
                surfaceToUpload.WriteToStream(imageStream, CoreConfiguration, _photobucketConfiguration);
                imageStream.Position = 0;
                using (var streamContent = new StreamContent(imageStream))
                {
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue(surfaceToUpload.GenerateMimeType(CoreConfiguration, _photobucketConfiguration));
                    streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                    {
                        Name = "\"uploadfile\"",
                        FileName = "\"" + filename + "\""
                    };

                    HttpBehaviour.Current.SetConfig(new HttpRequestMessageConfiguration
                    {
                        Properties = signedParameters
                    });
                    try
                    {
                        responseString = await uploadUri.PostAsync<string>(streamContent, token);
                    }
                    catch (Exception ex)
                    {
                        Log.Error().WriteLine(ex, "Error uploading to Photobucket.");
                        throw;
                    }
                }
            }

            if (responseString == null)
            {
                return null;
            }
            Log.Info().WriteLine(responseString);
            var photobucketInfo = PhotobucketInfo.FromUploadResponse(responseString);
            Log.Debug().WriteLine("Upload to Photobucket was finished");
            return photobucketInfo;
        }


	    /// <summary>
	    ///     Get list of photobucket albums
	    /// </summary>
	    /// <returns>List of string</returns>
	    public async Task<IList<string>> RetrievePhotobucketAlbums()
	    {
	        if (_albumsCache != null)
	        {
	            return _albumsCache;
	        }
	        
	        IDictionary<string, object> signedParameters = new Dictionary<string, object>();
	        string responseString;

            try
            {
                var albumsUri = PhotobucketApiUri
                    .AppendSegments("album", _photobucketConfiguration.Username)
                    .ExtendQuery("format", "json");
                var oAuthHttpBehaviour = _oAuthHttpBehaviour.ShallowClone();
	            oAuthHttpBehaviour.SetConfig(new HttpRequestMessageConfiguration
	            {
	                Properties = signedParameters
	            });

                _oAuthHttpBehaviour.MakeCurrent();
	            responseString = await albumsUri.GetAsAsync<string>();
	        }
	        catch (Exception ex)
	        {
	            Log.Error().WriteLine(ex, "Error uploading to Photobucket.");
	            throw;
	        }
	        
	        if (responseString == null)
	        {
	            return null;
	        }
	        
	        return null;
	    }

    }
}