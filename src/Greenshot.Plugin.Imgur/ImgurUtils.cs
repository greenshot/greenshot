/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.IO;
using System.Linq;
using System.Net;
using Greenshot.Base.Core;
using Greenshot.Base.IniFile;


namespace Greenshot.Plugin.Imgur;

/// <summary>
/// A collection of Imgur helper methods
/// </summary>
public static class ImgurUtils
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ImgurUtils));
    private const string SmallUrlPattern = "https://i.imgur.com/{0}s.jpg";
    private static readonly ImgurConfiguration Config = IniConfig.GetIniSection<ImgurConfiguration>();

    /// <summary>
    /// Check if we need to load the history
    /// </summary>
    /// <returns></returns>
    public static bool IsHistoryLoadingNeeded()
    {
        Log.InfoFormat("Checking if imgur cache loading needed, configuration has {0} imgur hashes, loaded are {1} hashes.", Config.ImgurUploadHistory.Count, Config.runtimeImgurHistory.Count);
        return Config.runtimeImgurHistory.Count != Config.ImgurUploadHistory.Count;
    }

    /// <summary>
    /// Load the complete history of the imgur uploads, with the corresponding information
    /// </summary>
    public static void LoadHistory()
    {
        if (!IsHistoryLoadingNeeded())
        {
            return;
        }

        bool saveNeeded = false;

        // Load the ImUr history
        foreach (string hash in Config.ImgurUploadHistory.Keys.ToList())
        {
            if (Config.runtimeImgurHistory.ContainsKey(hash))
            {
                // Already loaded
                continue;
            }

            try
            {
                var deleteHash = Config.ImgurUploadHistory[hash];
                ImgurInfo imgurInfo = RetrieveImgurInfo(hash, deleteHash);
                if (imgurInfo != null)
                {
                    RetrieveImgurThumbnail(imgurInfo);
                    Config.runtimeImgurHistory[hash] = imgurInfo;
                }
                else
                {
                    Log.InfoFormat("Deleting unknown ImgUr {0} from config, delete hash was {1}.", hash, deleteHash);
                    Config.ImgurUploadHistory.Remove(hash);
                    Config.runtimeImgurHistory.Remove(hash);
                    saveNeeded = true;
                }
            }
            catch (WebException wE)
            {
                bool redirected = false;
                if (wE.Status == WebExceptionStatus.ProtocolError)
                {
                    HttpWebResponse response = (HttpWebResponse) wE.Response;

                    if (response.StatusCode == HttpStatusCode.Forbidden)
                    {
                        Log.Error("Imgur loading forbidden", wE);
                        break;
                    }

                    // Image no longer available?
                    if (response.StatusCode == HttpStatusCode.Redirect)
                    {
                        Log.InfoFormat("ImgUr image for hash {0} is no longer available, removing it from the history", hash);
                        Config.ImgurUploadHistory.Remove(hash);
                        Config.runtimeImgurHistory.Remove(hash);
                        redirected = true;
                    }
                }

                if (!redirected)
                {
                    Log.Error("Problem loading ImgUr history for hash " + hash, wE);
                }
            }
            catch (Exception e)
            {
                Log.Error("Problem loading ImgUr history for hash " + hash, e);
            }
        }

        if (saveNeeded)
        {
            // Save needed changes
            IniConfig.Save();
        }
    }

    /// <summary>
    /// Use this to make sure Imgur knows from where the upload comes.
    /// </summary>
    /// <param name="webRequest"></param>
    private static void SetClientId(HttpWebRequest webRequest)
    {
        webRequest.Headers.Add("Authorization", "Client-ID " + ImgurCredentials.CONSUMER_KEY);
    }

    /// <summary>
    /// Retrieve the thumbnail of an imgur image
    /// </summary>
    /// <param name="imgurInfo"></param>
    public static void RetrieveImgurThumbnail(ImgurInfo imgurInfo)
    {
        if (imgurInfo.SmallSquare == null)
        {
            Log.Warn("Imgur URL was null, not retrieving thumbnail.");
            return;
        }

        Log.InfoFormat("Retrieving Imgur image for {0} with url {1}", imgurInfo.Hash, imgurInfo.SmallSquare);
        HttpWebRequest webRequest = NetworkHelper.CreateWebRequest(string.Format(SmallUrlPattern, imgurInfo.Hash), HTTPMethod.GET);
        webRequest.ServicePoint.Expect100Continue = false;
        // Not for getting the thumbnail, in anonymous mode
        //SetClientId(webRequest);
        using WebResponse response = webRequest.GetResponse();
        Stream responseStream = response.GetResponseStream();
        if (responseStream != null)
        {
            // TODO: Replace with some other code, like the file format handler
            imgurInfo.Image = ImageIO.FromStream(responseStream);
        }
    }

    /// <summary>
    /// Retrieve information on an imgur image
    /// </summary>
    /// <param name="hash"></param>
    /// <param name="deleteHash"></param>
    /// <returns>ImgurInfo</returns>
    public static ImgurInfo RetrieveImgurInfo(string hash, string deleteHash)
    {
        string url = Config.ImgurApi3Url + "/image/" + hash + ".xml";
        Log.InfoFormat("Retrieving Imgur info for {0} with url {1}", hash, url);
        HttpWebRequest webRequest = NetworkHelper.CreateWebRequest(url, HTTPMethod.GET);
        webRequest.ServicePoint.Expect100Continue = false;
        SetClientId(webRequest);
        string responseString = null;
        try
        {
            using WebResponse response = webRequest.GetResponse();
            var responseStream = response.GetResponseStream();
            if (responseStream != null)
            {
                using StreamReader reader = new StreamReader(responseStream, true);
                responseString = reader.ReadToEnd();
            }
        }
        catch (WebException wE)
        {
            if (wE.Status == WebExceptionStatus.ProtocolError)
            {
                if (((HttpWebResponse) wE.Response).StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }
            }

            throw;
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
    public static void DeleteImgurImage(ImgurInfo imgurInfo)
    {
        Log.InfoFormat("Deleting Imgur image for {0}", imgurInfo.DeleteHash);

        try
        {
            string url = Config.ImgurApi3Url + "/image/" + imgurInfo.DeleteHash + ".xml";
            HttpWebRequest webRequest = NetworkHelper.CreateWebRequest(url, HTTPMethod.DELETE);
            webRequest.ServicePoint.Expect100Continue = false;
            SetClientId(webRequest);
            string responseString = null;
            using (WebResponse response = webRequest.GetResponse())
            {
                var responseStream = response.GetResponseStream();
                if (responseStream != null)
                {
                    using StreamReader reader = new StreamReader(responseStream, true);
                    responseString = reader.ReadToEnd();
                }
            }

            Log.InfoFormat("Delete result: {0}", responseString);
        }
        catch (WebException wE)
        {
            // Allow "Bad request" this means we already deleted it
            if (wE.Status == WebExceptionStatus.ProtocolError)
            {
                if (((HttpWebResponse) wE.Response).StatusCode != HttpStatusCode.BadRequest)
                {
                    throw;
                }
            }
        }

        // Make sure we remove it from the history, if no error occurred
        Config.runtimeImgurHistory.Remove(imgurInfo.Hash);
        Config.ImgurUploadHistory.Remove(imgurInfo.Hash);
        imgurInfo.Image = null;
    }
}