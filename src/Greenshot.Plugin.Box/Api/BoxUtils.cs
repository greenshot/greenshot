/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Core.OAuth;
using Greenshot.Base.Http;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Threading;
using Dapplo.Ini;

namespace Greenshot.Plugin.Box.Api;

/// <summary>
/// Description of BoxUtils.
/// </summary>
public static class BoxUtils
{
    private static readonly IBoxConfiguration Config = IniConfigRegistry.GetSection<IBoxConfiguration>();
    private const string UploadFileUri = "https://upload.box.com/api/2.0/files/content";
    private const string FilesUri = "https://www.box.com/api/2.0/files/{0}";

    /// <summary>
    /// The account in the settings: sign in, test, sign out
    /// </summary>
    public static OAuth2SignIn SignIn { get; } = new OAuth2SignIn("Box", CreateSettings, StoreSettings, GetAccountAsync);

    /// <summary>
    /// The OAuth 2 settings with the stored tokens
    /// </summary>
    internal static OAuth2Settings CreateSettings() => new OAuth2Settings
    {
        AuthorizeUrl = "https://app.box.com/api/oauth2/authorize",
        SignedInUrl = $"https://getgreenshot.org/signed-in/box/?version={Uri.EscapeDataString(EnvironmentInfo.GetGreenshotVersion(true))}",
        TokenUrl = "https://api.box.com/oauth2/token",
        CloudServiceName = "Box",
        ClientId = BoxCredentials.ClientId,
        ClientSecret = BoxCredentials.ClientSecret,
        // Box doesn't document PKCE
        UsePkce = false,
        // Stored encrypted
        RefreshToken = string.IsNullOrEmpty(Config.RefreshToken14) ? Config.RefreshToken14 : Config.RefreshToken14.Decrypt(),
        AccessToken = Config.AccessToken,
        AccessTokenExpires = Config.AccessTokenExpires
    };

    /// <summary>
    /// Store the tokens of the settings, on the UI thread
    /// </summary>
    internal static void StoreSettings(OAuth2Settings settings)
    {
        Config.RefreshToken14 = string.IsNullOrEmpty(settings.RefreshToken) ? settings.RefreshToken : settings.RefreshToken.Encrypt();
        Config.AccessToken = settings.AccessToken;
        Config.AccessTokenExpires = settings.AccessTokenExpires;
    }

    /// <summary>
    /// The login (e-mail address) of the signed in Box user
    /// </summary>
    private static async Task<string> GetAccountAsync(OAuth2Settings settings, IUserInteraction userInteraction, CancellationToken cancellationToken)
    {
        var request = await OAuth2Helper.CreateOAuth2RequestAsync(HttpMethod.Get, "https://api.box.com/2.0/users/me?fields=login", settings, userInteraction, cancellationToken).ConfigureAwait(false);
        if (request == null)
        {
            return null;
        }

        string response = (await HttpService.Current.SendAsync(request, cancellationToken).ConfigureAwait(false)).EnsureSuccess().Content;
        using var document = JsonDocument.Parse(response);
        return document.RootElement.TryGetProperty("login", out var login) ? login.GetString() : null;
    }

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
        var settings = CreateSettings();

        try
        {
            var request = await OAuth2Helper.CreateOAuth2RequestAsync(HttpMethod.Post, UploadFileUri, settings, userInteraction, cancellationToken).ConfigureAwait(false);
            if (request == null)
            {
                return null;
            }

            request.Content = HttpContents.Multipart("file", image, filename, new Dictionary<string, string>
            {
                { "parent_id", Config.FolderId }
            }, progress);
            var response = (await HttpService.Current.SendAsync(request, cancellationToken).ConfigureAwait(false)).EnsureSuccess().Content;

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
                string filesResponse = (await HttpService.Current.SendAsync(putRequest, cancellationToken).ConfigureAwait(false)).EnsureSuccess().Content;
                var file = JsonSerializer.Deserialize<FileEntry>(filesResponse);
                return file.SharedLink.Url;
            }

            return $"https://www.box.com/files/0/f/0/1/f_{upload.Entries[0].Id}";
        }
        finally
        {
            // Copy the settings back to the config (on the UI thread), so they are stored.
            await UiDispatcher.Current.InvokeAsync(() => StoreSettings(settings), CancellationToken.None).ConfigureAwait(false);
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