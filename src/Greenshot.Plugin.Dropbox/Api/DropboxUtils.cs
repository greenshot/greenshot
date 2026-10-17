/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Core.OAuth;
using Dapplo.Ini;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Threading;
using Newtonsoft.Json;

namespace Greenshot.Plugin.Dropbox.Api;

/// <summary>
/// Description of DropboxUtils.
/// </summary>
public static class DropboxUtils
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(DropboxUtils));
    private static readonly IDropboxConfiguration DropboxConfig = IniConfigRegistry.GetSection<IDropboxConfiguration>();

    /// <summary>
    /// Upload the encoded capture
    /// </summary>
    /// <returns>true when uploaded, false when Dropbox didn't accept it, null when the user didn't authorize</returns>
    public static async Task<bool?> UploadToDropboxAsync(EncodedImage image, string filename, IUserInteraction userInteraction, IProgress<ProgressInfo> progress, CancellationToken cancellationToken)
    {
        // Stored encrypted; a token stored as plain text by an old version is returned unchanged by Decrypt.
        // A null result for a non-empty stored token means the DPAPI value belongs to another user profile/machine.
        string refreshToken = string.IsNullOrEmpty(DropboxConfig.RefreshToken) ? DropboxConfig.RefreshToken : DropboxConfig.RefreshToken.Decrypt();
        bool hasUnusableStoredToken = !string.IsNullOrEmpty(DropboxConfig.RefreshToken) && refreshToken == null;
        var oauth2Settings = new OAuth2Settings
        {
            AuthUrlPattern = "https://www.dropbox.com/oauth2/authorize?client_id={ClientId}&response_type=code&state={State}&redirect_uri={RedirectUrl}&token_access_type=offline",
            TokenUrl = "https://api.dropbox.com/oauth2/token",
            RedirectUrl = "https://getgreenshot.org/authorize/dropbox",
            CloudServiceName = "Dropbox",
            ClientId = DropBoxCredentials.CONSUMER_KEY,
            ClientSecret = DropBoxCredentials.CONSUMER_SECRET,
            AuthorizeMode = OAuth2AuthorizeMode.JsonReceiver,
            RefreshToken = refreshToken,
            AccessToken = hasUnusableStoredToken ? null : DropboxConfig.AccessToken,
            AccessTokenExpires = DropboxConfig.AccessTokenExpires
        };
        try
        {
            IDictionary<string, object> arguments = new Dictionary<string, object>
            {
                { "autorename", true },
                { "mute", true },
                { "path", "/" + filename.Replace(Path.DirectorySeparatorChar, '\\') }
            };

            var serializerSettings = new JsonSerializerSettings
            {
                StringEscapeHandling = StringEscapeHandling.EscapeNonAscii
            };

            var encodedArgs = JsonConvert.SerializeObject(arguments, serializerSettings);
            encodedArgs = encodedArgs.Replace("\x7F", "\\u007f");
            var request = await OAuth2Helper.CreateOAuth2RequestAsync(HttpMethod.Post, "https://content.dropboxapi.com/2/files/upload", oauth2Settings, userInteraction, cancellationToken).ConfigureAwait(false);
            if (request == null)
            {
                return null;
            }

            request.Headers.Add("Dropbox-API-Arg", encodedArgs);
            var content = NetworkHelper.CreateContent(image, progress);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            request.Content = content;
            var responseString = await NetworkHelper.SendAsync(request, cancellationToken).ConfigureAwait(false);
            Log.DebugFormat("Upload response: {0}", responseString);
            var response = JsonConvert.DeserializeObject<IDictionary<string, object>>(responseString);
            return response.ContainsKey("id");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Error("Upload error: ", ex);
            throw;
        }
        finally
        {
            // Copy the settings back to the config (on the UI thread), so they are stored.
            await UiDispatcher.Current.InvokeAsync(() =>
            {
                DropboxConfig.RefreshToken = string.IsNullOrEmpty(oauth2Settings.RefreshToken) ? oauth2Settings.RefreshToken : oauth2Settings.RefreshToken.Encrypt();
                DropboxConfig.AccessToken = oauth2Settings.AccessToken;
                DropboxConfig.AccessTokenExpires = oauth2Settings.AccessTokenExpires;
            }, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
