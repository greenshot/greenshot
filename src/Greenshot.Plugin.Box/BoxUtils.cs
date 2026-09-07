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
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Core.OAuth;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Base.Threading;
using Dapplo.Ini;

namespace Greenshot.Plugin.Box;

/// <summary>
/// Description of BoxUtils.
/// </summary>
public static class BoxUtils
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(BoxUtils));
    private static readonly IBoxConfiguration Config = IniConfigRegistry.GetSection<IBoxConfiguration>();
    private const string UploadFileUri = "https://upload.box.com/api/2.0/files/content";
    private const string FilesUri = "https://www.box.com/api/2.0/files/{0}";

    /// <summary>
    /// Do the actual upload to Box
    /// For more details on the available parameters, see: https://developers.box.net/w/page/12923951/ApiFunction_Upload%20and%20Download
    /// </summary>
    /// <param name="image">The encoded capture</param>
    /// <param name="filename">Filename of box upload</param>
    /// <param name="userInteraction">IUserInteraction for the authorization</param>
    /// <param name="progress">IProgress for the upload</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns>url to uploaded image, null when the user didn't authorize</returns>
    public static async Task<string> UploadToBoxAsync(EncodedImage image, string filename, IUserInteraction userInteraction, IProgress<ProgressInfo> progress, CancellationToken cancellationToken)
    {
        // Stored encrypted; a token stored as plain text by an old version is returned unchanged by Decrypt.
        // A null result for a non-empty stored token means the DPAPI value belongs to another user profile/machine.
        string refreshToken = string.IsNullOrEmpty(Config.RefreshToken) ? Config.RefreshToken : Config.RefreshToken.Decrypt();
        bool hasUnusableStoredToken = !string.IsNullOrEmpty(Config.RefreshToken) && refreshToken == null;
        // Fill the OAuth2Settings
        var settings = new OAuth2Settings
        {
            AuthUrlPattern = "https://app.box.com/api/oauth2/authorize?client_id={ClientId}&response_type=code&state={State}&redirect_uri={RedirectUrl}",
            TokenUrl = "https://api.box.com/oauth2/token",
            CloudServiceName = "Box",
            ClientId = BoxCredentials.ClientId,
            ClientSecret = BoxCredentials.ClientSecret,
            RedirectUrl = "https://getgreenshot.org/authorize/box",
            AuthorizeMode = OAuth2AuthorizeMode.JsonReceiver,
            RefreshToken = refreshToken,
            AccessToken = hasUnusableStoredToken ? null : Config.AccessToken,
            AccessTokenExpires = Config.AccessTokenExpires
        };

        try
        {
            var request = await OAuth2Helper.CreateOAuth2RequestAsync(HttpMethod.Post, UploadFileUri, settings, userInteraction, cancellationToken).ConfigureAwait(false);
            if (request == null)
            {
                return null;
            }

            request.Content = NetworkHelper.CreateMultipartContent("file", image, filename, new Dictionary<string, object>
            {
                { "parent_id", Config.FolderId }
            }, progress);
            var response = await NetworkHelper.SendAsync(request, cancellationToken).ConfigureAwait(false);

            Log.DebugFormat("Box response: {0}", response);

            var upload = JsonSerializer.Deserialize<Upload>(response);
            if (upload?.Entries == null || upload.Entries.Count == 0) return null;

            if (Config.UseSharedLink)
            {
                var putRequest = await OAuth2Helper.CreateOAuth2RequestAsync(HttpMethod.Put, string.Format(FilesUri, upload.Entries[0].Id), settings, userInteraction, cancellationToken).ConfigureAwait(false);
                if (putRequest == null)
                {
                    return null;
                }

                putRequest.Content = new StringContent("{\"shared_link\": {\"access\": \"open\"}}", Encoding.UTF8, "application/json");
                string filesResponse = await NetworkHelper.SendAsync(putRequest, cancellationToken).ConfigureAwait(false);
                var file = JsonSerializer.Deserialize<FileEntry>(filesResponse);
                return file.SharedLink.Url;
            }

            return $"https://www.box.com/files/0/f/0/1/f_{upload.Entries[0].Id}";
        }
        finally
        {
            // Copy the settings back to the config (on the UI thread), so they are stored.
            await UiDispatcher.Current.InvokeAsync(() =>
            {
                Config.RefreshToken = string.IsNullOrEmpty(settings.RefreshToken) ? settings.RefreshToken : settings.RefreshToken.Encrypt();
                Config.AccessToken = settings.AccessToken;
                Config.AccessTokenExpires = settings.AccessTokenExpires;
            }, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
/// <summary>
/// A simple helper class for the DataContractJsonSerializer
/// </summary>
internal static class JsonSerializer
{
    /// <summary>
    /// Helper method to parse JSON to object
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="jsonString"></param>
    /// <returns></returns>
    public static T Deserialize<T>(string jsonString)
    {
        var deserializer = new DataContractJsonSerializer(typeof(T));
        using var stream = new MemoryStream();
        byte[] content = Encoding.UTF8.GetBytes(jsonString);
        stream.Write(content, 0, content.Length);
        stream.Seek(0, SeekOrigin.Begin);
        return (T) deserializer.ReadObject(stream);
    }
}