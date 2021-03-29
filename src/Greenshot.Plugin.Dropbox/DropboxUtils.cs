/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
using System.Collections.Generic;
using System.IO;
using Greenshot.Base.Core;
using Greenshot.Base.Core.OAuth;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Newtonsoft.Json;

namespace Greenshot.Plugin.Dropbox
{
    /// <summary>
    /// Description of DropboxUtils.
    /// </summary>
    public class DropboxUtils
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(DropboxUtils));
        private static readonly DropboxPluginConfiguration DropboxConfig = IniConfig.GetIniSection<DropboxPluginConfiguration>();

        private DropboxUtils()
        {
        }

        public static bool UploadToDropbox(ISurface surfaceToUpload, SurfaceOutputSettings outputSettings, ICaptureDetails captureDetails)
        {
            var oauth2Settings = new OAuth2Settings
            {
                AuthUrlPattern = "https://api.dropbox.com/oauth2/authorize?response_type=token&client_id={ClientId}&state={State}&redirect_uri={RedirectUrl}",
                TokenUrl = "https://api.dropbox.com/oauth2/token",
                RedirectUrl = "https://getgreenshot.org/authorize/dropbox",
                CloudServiceName = "Dropbox",
                ClientId = DropBoxCredentials.CONSUMER_KEY,
                ClientSecret = DropBoxCredentials.CONSUMER_SECRET,
                AuthorizeMode = OAuth2AuthorizeMode.JsonReceiver,
                RefreshToken = DropboxConfig.RefreshToken,
                AccessToken = DropboxConfig.AccessToken,
                AccessTokenExpires = DropboxConfig.AccessTokenExpires
            };
            try
            {
                string filename = Path.GetFileName(FilenameHelper.GetFilename(DropboxConfig.UploadFormat, captureDetails));
                SurfaceContainer image = new SurfaceContainer(surfaceToUpload, outputSettings, filename);

                IDictionary<string, object> arguments = new Dictionary<string, object>
                {
                    {
                        "autorename", true
                    },
                    {
                        "mute", true
                    },
                    {
                        "path", "/" + filename.Replace(Path.DirectorySeparatorChar, '\\')
                    }
                };
                IDictionary<string, object> headers = new Dictionary<string, object>
                {
                    {
                        "Dropbox-API-Arg", JsonConvert.SerializeObject(arguments)
                    }
                };
                var webRequest = OAuth2Helper.CreateOAuth2WebRequest(HTTPMethod.POST, "https://content.dropboxapi.com/2/files/upload", oauth2Settings);

                NetworkHelper.Post(webRequest, headers, image);
                var responseString = NetworkHelper.GetResponseAsString(webRequest);
                Log.DebugFormat("Upload response: {0}", responseString);
                var response = JsonConvert.DeserializeObject<IDictionary<string, string>>(responseString);
                return response.ContainsKey("id");
            }
            catch (Exception ex)
            {
                Log.Error("Upload error: ", ex);
                throw;
            }
            finally
            {
                DropboxConfig.RefreshToken = oauth2Settings.RefreshToken;
                DropboxConfig.AccessToken = oauth2Settings.AccessToken;
                DropboxConfig.AccessTokenExpires = oauth2Settings.AccessTokenExpires;
                DropboxConfig.IsDirty = true;
                IniConfig.Save();
            }
        }
    }
}