/*
 * A GooglePhotos Plugin for Greenshot
 * Copyright (C) 2011  Francis Noel
 * 
 * For more information see: https://getgreenshot.org/
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Xml;
using Greenshot.Base.Core;
using Greenshot.Base.Core.OAuth;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;

namespace Greenshot.Plugin.GooglePhotos
{
    /// <summary>
    /// Description of GooglePhotosUtils.
    /// </summary>
    public static class GooglePhotosUtils
    {
        private const string GooglePhotosScope = "https://picasaweb.google.com/data/";
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(GooglePhotosUtils));
        private static readonly GooglePhotosConfiguration Config = IniConfig.GetIniSection<GooglePhotosConfiguration>();

        private const string AuthUrl = "https://accounts.google.com/o/oauth2/auth?response_type=code&client_id={ClientId}&redirect_uri={RedirectUrl}&state={State}&scope=" +
                                       GooglePhotosScope;

        private const string TokenUrl = "https://www.googleapis.com/oauth2/v3/token";
        private const string UploadUrl = "https://picasaweb.google.com/data/feed/api/user/{0}/albumid/{1}";

        /// <summary>
        /// Do the actual upload to GooglePhotos
        /// </summary>
        /// <param name="surfaceToUpload">Image to upload</param>
        /// <param name="outputSettings">SurfaceOutputSettings</param>
        /// <param name="title">string</param>
        /// <param name="filename">string</param>
        /// <returns>GooglePhotosResponse</returns>
        public static string UploadToGooglePhotos(ISurface surfaceToUpload, SurfaceOutputSettings outputSettings, string title, string filename)
        {
            // Fill the OAuth2Settings
            var settings = new OAuth2Settings
            {
                AuthUrlPattern = AuthUrl,
                TokenUrl = TokenUrl,
                CloudServiceName = "GooglePhotos",
                ClientId = GooglePhotosCredentials.ClientId,
                ClientSecret = GooglePhotosCredentials.ClientSecret,
                AuthorizeMode = OAuth2AuthorizeMode.JsonReceiver,
                RefreshToken = Config.RefreshToken,
                AccessToken = Config.AccessToken,
                AccessTokenExpires = Config.AccessTokenExpires
            };

            // Copy the settings from the config, which is kept in memory and on the disk

            try
            {
                var webRequest = OAuth2Helper.CreateOAuth2WebRequest(HTTPMethod.POST, string.Format(UploadUrl, Config.UploadUser, Config.UploadAlbum), settings);
                if (Config.AddFilename)
                {
                    webRequest.Headers.Add("Slug", NetworkHelper.EscapeDataString(filename));
                }

                SurfaceContainer container = new SurfaceContainer(surfaceToUpload, outputSettings, filename);
                container.Upload(webRequest);

                string response = NetworkHelper.GetResponseAsString(webRequest);

                return ParseResponse(response);
            }
            finally
            {
                // Copy the settings back to the config, so they are stored.
                Config.RefreshToken = settings.RefreshToken;
                Config.AccessToken = settings.AccessToken;
                Config.AccessTokenExpires = settings.AccessTokenExpires;
                Config.IsDirty = true;
                IniConfig.Save();
            }
        }

        /// <summary>
        /// Parse the upload URL from the response
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
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(response);
                XmlNodeList nodes = doc.GetElementsByTagName("link", "*");
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
                            if (rel != null && rel.EndsWith("canonical"))
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
                Log.ErrorFormat("Could not parse GooglePhotos response due to error {0}, response was: {1}", e.Message, response);
            }

            return null;
        }
    }
}