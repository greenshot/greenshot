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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Dapplo.HttpExtensions;
using Dapplo.HttpExtensions.Extensions;
using Dapplo.Ini;
using Dapplo.Log;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;

#endregion

namespace Greenshot.Addon.Flickr
{
	/// <summary>
	///     Description of FlickrUtils.
	/// </summary>
	public class FlickrUtils
	{
        private const string FlickrFarmUrl = "https://farm{0}.staticflickr.com/{1}/{2}_{3}.jpg";
        private static readonly LogSource Log = new LogSource();
        private static readonly IFlickrConfiguration Config = IniConfig.Current.Get<IFlickrConfiguration>();
        private static readonly Uri FlickrApiBaseUrl = new Uri("https://api.flickr.com/services");
        private static readonly Uri FlickrUploadUri = new Uri("https://up.flickr.com/services/upload");
        // REST
        private static readonly Uri FlickrRestUrl = FlickrApiBaseUrl.AppendSegments("rest");

        private static readonly Uri FlickrGetInfoUrl = FlickrRestUrl.ExtendQuery(new Dictionary<string, object>
        {
            {
                "method", "flickr.photos.getInfo"
            }
        });

        /// <summary>
        ///     Do the actual upload to Flickr
        ///     For more details on the available parameters, see: http://flickrnet.codeplex.com
        /// </summary>
        /// <param name="surfaceToUpload"></param>
        /// <param name="outputSettings"></param>
        /// <param name="title"></param>
        /// <param name="filename"></param>
        /// <param name="progress">IProgres is used to report the progress to</param>
        /// <param name="token"></param>
        /// <returns>url to image</returns>
        public static async Task<string> UploadToFlickrAsync(ISurface surfaceToUpload, SurfaceOutputSettings outputSettings, string title, string filename, CancellationToken token = default)
        {
            try
            {
                var signedParameters = new Dictionary<string, object>
                {
                    {"content_type", "2"}, // content = screenshot
					{"tags", "Greenshot"},
                    {"is_public", Config.IsPublic ? "1" : "0"},
                    {"is_friend", Config.IsFriend ? "1" : "0"},
                    {"is_family", Config.IsFamily ? "1" : "0"},
                    {"safety_level", $"{(int) Config.SafetyLevel}"},
                    {"hidden", Config.HiddenFromSearch ? "1" : "2"},
                    {"format", "json"}, // Doesn't work... :(
					{"nojsoncallback", "1"}
                };

                string photoId;
                using (var stream = new MemoryStream())
                {
                    ImageOutput.SaveToStream(surfaceToUpload, stream, outputSettings);
                    stream.Position = 0;
                    using (var streamContent = new StreamContent(stream))
                    {
                        streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/" + outputSettings.Format);
                        streamContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                        {
                            Name = "\"photo\"",
                            FileName = "\"" + filename + "\""
                        };
                        HttpBehaviour.Current.SetConfig(new HttpRequestMessageConfiguration
                        {
                            Properties = signedParameters
                        });
                        var response = await FlickrUploadUri.PostAsync<XDocument>(streamContent, token);
                        photoId = (from element in response.Root.Elements()
                                   where element.Name == "photoid"
                                   select element.Value).First();
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
                var photoInfo = await FlickrGetInfoUrl.PostAsync<dynamic>(signedParameters, token);
                if (Config.UsePageLink)
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