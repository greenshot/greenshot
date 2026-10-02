/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Threading;
using Dapplo.Ini;


namespace Greenshot.Plugin.Imgur;

/// <summary>
/// A collection of Imgur helper methods, the network calls are async and the configuration is changed on the UI thread.
/// </summary>
public static class ImgurUtils
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ImgurUtils));
    private const string SmallUrlPattern = "https://i.imgur.com/{0}s.jpg";
    private static readonly IImgurConfiguration Config = IniConfigRegistry.GetSection<IImgurConfiguration>();

    /// <summary>
    /// Check if we need to load the history
    /// </summary>
    /// <returns></returns>
    public static bool IsHistoryLoadingNeeded()
    {
        int uploadCount = Config?.ImgurUploadHistory?.Count ?? 0;
        int runtimeCount = Config?.RuntimeImgurHistory?.Count ?? 0;
        Log.InfoFormat("Checking if imgur cache loading needed, configuration has {0} imgur hashes, loaded are {1} hashes.", uploadCount, runtimeCount);
        return runtimeCount != uploadCount;
    }

    /// <summary>
    /// Load the complete history of the imgur uploads, with the corresponding information
    /// </summary>
    /// <returns>true (for IUserInteraction.RunWithProgressAsync)</returns>
    public static async Task<bool> LoadHistoryAsync(CancellationToken cancellationToken)
    {
        if (!IsHistoryLoadingNeeded() || Config?.ImgurUploadHistory == null)
        {
            return true;
        }

        var ui = UiDispatcher.Current;
        var history = await ui.InvokeAsync(() =>
        {
            Config.RuntimeImgurHistory ??= new Dictionary<string, ImgurInfo>();
            return Config.ImgurUploadHistory.Where(entry => !Config.RuntimeImgurHistory.ContainsKey(entry.Key)).ToList();
        }, cancellationToken).ConfigureAwait(false);

        // Load the ImgUr history
        foreach (var entry in history)
        {
            string hash = entry.Key;
            try
            {
                ImgurInfo imgurInfo = await RetrieveImgurInfoAsync(hash, entry.Value, cancellationToken).ConfigureAwait(false);
                if (imgurInfo != null)
                {
                    await RetrieveImgurThumbnailAsync(imgurInfo, cancellationToken).ConfigureAwait(false);
                    await ui.InvokeAsync(() => Config.RuntimeImgurHistory[hash] = imgurInfo, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    Log.InfoFormat("Deleting unknown ImgUr {0} from config, delete hash was {1}.", hash, entry.Value);
                    await RemoveFromHistoryAsync(hash).ConfigureAwait(false);
                }
            }
            catch (ImgurStatusException statusException) when (statusException.StatusCode == HttpStatusCode.Forbidden)
            {
                Log.Error("Imgur loading forbidden", statusException);
                break;
            }
            catch (ImgurStatusException statusException) when (statusException.StatusCode == HttpStatusCode.Redirect)
            {
                // Image no longer available
                Log.InfoFormat("ImgUr image for hash {0} is no longer available, removing it from the history", hash);
                await RemoveFromHistoryAsync(hash).ConfigureAwait(false);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                Log.Error("Problem loading ImgUr history for hash " + hash, e);
            }
        }

        return true;
    }

    private static Task RemoveFromHistoryAsync(string hash)
    {
        return UiDispatcher.Current.InvokeAsync(() =>
        {
            Config.ImgurUploadHistory.Remove(hash);
            Config.RuntimeImgurHistory?.Remove(hash);
        }, CancellationToken.None);
    }

    /// <summary>
    /// A request with the Client-ID, so Imgur knows from where the upload comes.
    /// </summary>
    internal static HttpRequestMessage CreateRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.TryAddWithoutValidation("Authorization", "Client-ID " + ImgurCredentials.CONSUMER_KEY);
        request.Headers.ExpectContinue = false;
        return request;
    }

    /// <summary>
    /// Send the request, the status code is part of the exception
    /// </summary>
    private static async Task<string> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using (request)
        {
            using var response = await NetworkHelper.HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new ImgurStatusException(response.StatusCode, content);
            }

            return content;
        }
    }

    /// <summary>
    /// Retrieve the thumbnail of an imgur image
    /// </summary>
    /// <param name="imgurInfo"></param>
    /// <param name="cancellationToken">CancellationToken</param>
    public static async Task RetrieveImgurThumbnailAsync(ImgurInfo imgurInfo, CancellationToken cancellationToken)
    {
        if (imgurInfo.SmallSquare == null)
        {
            Log.Warn("Imgur URL was null, not retrieving thumbnail.");
            return;
        }

        Log.InfoFormat("Retrieving Imgur image for {0} with url {1}", imgurInfo.Hash, imgurInfo.SmallSquare);
        // Not with the client id, getting the thumbnail is anonymous
        using var response = await NetworkHelper.HttpClient.GetAsync(string.Format(SmallUrlPattern, imgurInfo.Hash), cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        using var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        // TODO: Replace with some other code, like the file format handler
        imgurInfo.Image = ImageIO.FromStream(responseStream);
    }

    /// <summary>
    /// Retrieve information on an imgur image
    /// </summary>
    /// <param name="hash"></param>
    /// <param name="deleteHash"></param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns>ImgurInfo, null when Imgur doesn't know it</returns>
    public static async Task<ImgurInfo> RetrieveImgurInfoAsync(string hash, string deleteHash, CancellationToken cancellationToken)
    {
        string url = Config.ImgurApi3Url + "/image/" + hash + ".xml";
        Log.InfoFormat("Retrieving Imgur info for {0} with url {1}", hash, url);
        string responseString;
        try
        {
            responseString = await SendAsync(CreateRequest(HttpMethod.Get, url), cancellationToken).ConfigureAwait(false);
        }
        catch (ImgurStatusException statusException) when (statusException.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        ImgurInfo imgurInfo = null;
        if (responseString != null)
        {
            Log.Debug(responseString);
            imgurInfo = ImgurInfo.ParseResponse(responseString);
            imgurInfo.DeleteHash = deleteHash;
        }

        return imgurInfo;
    }

    /// <summary>
    /// Delete an imgur image, this is done by specifying the delete hash
    /// </summary>
    /// <param name="imgurInfo"></param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns>true (for IUserInteraction.RunWithProgressAsync)</returns>
    public static async Task<bool> DeleteImgurImageAsync(ImgurInfo imgurInfo, CancellationToken cancellationToken)
    {
        Log.InfoFormat("Deleting Imgur image for {0}", imgurInfo.DeleteHash);

        try
        {
            string url = Config.ImgurApi3Url + "/image/" + imgurInfo.DeleteHash + ".xml";
            string responseString = await SendAsync(CreateRequest(HttpMethod.Delete, url), cancellationToken).ConfigureAwait(false);
            Log.InfoFormat("Delete result: {0}", responseString);
        }
        catch (ImgurStatusException statusException) when (statusException.StatusCode == HttpStatusCode.BadRequest)
        {
            // Allow "Bad request" this means we already deleted it
        }

        // Make sure we remove it from the history, if no error occurred
        await RemoveFromHistoryAsync(imgurInfo.Hash).ConfigureAwait(false);
        imgurInfo.Image = null;
        return true;
    }
}

/// <summary>
/// An Imgur call answered with an error status
/// </summary>
public sealed class ImgurStatusException : Exception
{
    public ImgurStatusException(HttpStatusCode statusCode, string content) : base($"Imgur answered {(int) statusCode} {statusCode}: {content}")
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}
