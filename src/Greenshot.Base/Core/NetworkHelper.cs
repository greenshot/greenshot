/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Greenshot.Base.Core.FileFormatHandlers;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Interfaces.Plugin;
using log4net;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// HTTP Method to make sure we have the correct method
    /// </summary>
    public enum HTTPMethod
    {
        GET,
        POST,
        PUT,
        DELETE
    };

    /// <summary>
    /// Description of NetworkHelper.
    /// </summary>
    public static class NetworkHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(NetworkHelper));
        private static readonly CoreConfiguration Config = IniConfig.GetIniSection<CoreConfiguration>();

        static NetworkHelper()
        {
            try
            {
                // Disable certificate checking
                ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
            }
            catch (Exception ex)
            {
                Log.Warn("An error has occurred while allowing self-signed certificates:", ex);
            }
        }

        /// <summary>
        /// Download the uri into a memory stream, without catching exceptions
        /// </summary>
        /// <param name="url">Of an image</param>
        /// <returns>MemoryStream which is already seek-ed to 0</returns>
        public static MemoryStream GetAsMemoryStream(string url)
        {
            var request = CreateWebRequest(url);
            using var response = (HttpWebResponse) request.GetResponse();
            var memoryStream = new MemoryStream();
            using (var responseStream = response.GetResponseStream())
            {
                responseStream?.CopyTo(memoryStream);
                // Make sure it can be used directly
                memoryStream.Seek(0, SeekOrigin.Begin);
            }

            return memoryStream;
        }

        /// <summary>
        /// Download the uri to build an IDrawableContainer
        /// </summary>
        /// <param name="url">Of an image</param>
        /// <returns>IDrawableContainer</returns>
        public static IDrawableContainer DownloadImageAsDrawableContainer(string url)
        {
            var fileFormatHandlers = SimpleServiceProvider.Current.GetAllInstances<IFileFormatHandler>();
            var extensions = string.Join("|", fileFormatHandlers.ExtensionsFor(FileFormatHandlerActions.LoadFromStream));

            var imageUrlRegex = new Regex($@"(http|https)://.*(?<extension>{extensions})");
            var match = imageUrlRegex.Match(url);
            try
            {
                using var memoryStream = GetAsMemoryStream(url);
                try
                {
                    var extension = match.Success ? match.Groups["extension"]?.Value : null;
                    var drawableContainer = fileFormatHandlers.LoadDrawablesFromStream(memoryStream, extension).FirstOrDefault();
                    if (drawableContainer != null)
                    {
                        return drawableContainer;
                    }
                }
                catch (Exception)
                {
                    // If we arrive here, the image loading didn't work, try to see if the response has a http(s) URL to an image and just take this instead.
                    string content;
                    using (var streamReader = new StreamReader(memoryStream, Encoding.UTF8, true))
                    {
                        content = streamReader.ReadLine();
                    }

                    if (string.IsNullOrEmpty(content))
                    {
                        throw;
                    }

                    match = imageUrlRegex.Match(content);
                    if (!match.Success)
                    {
                        throw;
                    }

                    using var memoryStream2 = GetAsMemoryStream(match.Value);

                    var extension = match.Success ? match.Groups["extension"]?.Value : null;
                    var drawableContainer = fileFormatHandlers.LoadDrawablesFromStream(memoryStream2, extension).FirstOrDefault();
                    if (drawableContainer != null)
                    {
                        return drawableContainer;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Problem downloading the image from: " + url, e);
            }

            return null;
        }

        /// <summary>
        /// Download the uri to create a Bitmap
        /// </summary>
        /// <param name="url">Of an image</param>
        /// <returns>Bitmap</returns>
        public static Bitmap DownloadImage(string url)
        {
            var fileFormatHandlers = SimpleServiceProvider.Current.GetAllInstances<IFileFormatHandler>();

            var extensions = string.Join("|", fileFormatHandlers.ExtensionsFor(FileFormatHandlerActions.LoadFromStream));

            var imageUrlRegex = new Regex($@"(http|https)://.*(?<extension>{extensions})");
            var match = imageUrlRegex.Match(url);
            try
            {
                using var memoryStream = GetAsMemoryStream(url);
                try
                {
                    if (fileFormatHandlers.TryLoadFromStream(memoryStream, match.Success ? match.Groups["extension"]?.Value : null, out var bitmap))
                    {
                        return bitmap;
                    }
                }
                catch (Exception)
                {
                    // If we arrive here, the image loading didn't work, try to see if the response has a http(s) URL to an image and just take this instead.
                    string content;
                    using (var streamReader = new StreamReader(memoryStream, Encoding.UTF8, true))
                    {
                        content = streamReader.ReadLine();
                    }

                    if (string.IsNullOrEmpty(content))
                    {
                        throw;
                    }

                    match = imageUrlRegex.Match(content);
                    if (!match.Success)
                    {
                        throw;
                    }

                    using var memoryStream2 = GetAsMemoryStream(match.Value);
                    if (fileFormatHandlers.TryLoadFromStream(memoryStream2, match.Success ? match.Groups["extension"]?.Value : null, out var bitmap))
                    {
                        return bitmap;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error("Problem downloading the image from: " + url, e);
            }

            return null;
        }

        /// <summary>
        /// Helper method to create a web request with a lot of default settings
        /// </summary>
        /// <param name="uri">string with uri to connect to</param>
        /// <returns>WebRequest</returns>
        public static HttpWebRequest CreateWebRequest(string uri)
        {
            return CreateWebRequest(new Uri(uri));
        }

        /// <summary>
        /// Helper method to create a web request with a lot of default settings
        /// </summary>
        /// <param name="uri">string with uri to connect to</param>
        /// /// <param name="method">Method to use</param>
        /// <returns>WebRequest</returns>
        public static HttpWebRequest CreateWebRequest(string uri, HTTPMethod method)
        {
            return CreateWebRequest(new Uri(uri), method);
        }

        /// <summary>
        /// Helper method to create a web request with a lot of default settings
        /// </summary>
        /// <param name="uri">Uri with uri to connect to</param>
        /// <param name="method">Method to use</param>
        /// <returns>WebRequest</returns>
        public static HttpWebRequest CreateWebRequest(Uri uri, HTTPMethod method)
        {
            var webRequest = CreateWebRequest(uri);
            webRequest.Method = method.ToString();
            return webRequest;
        }

        /// <summary>
        /// Helper method to create a web request, eventually with proxy
        /// </summary>
        /// <param name="uri">Uri with uri to connect to</param>
        /// <returns>WebRequest</returns>
        public static HttpWebRequest CreateWebRequest(Uri uri)
        {
            var webRequest = (HttpWebRequest) WebRequest.Create(uri);
            webRequest.Proxy = Config.UseProxy ? CreateProxy(uri) : null;
            // Make sure the default credentials are available
            webRequest.Credentials = CredentialCache.DefaultCredentials;

            // Allow redirect, this is usually needed so that we don't get a problem when a service moves
            webRequest.AllowAutoRedirect = true;
            // Set default timeouts
            webRequest.Timeout = Config.WebRequestTimeout * 1000;
            webRequest.ReadWriteTimeout = Config.WebRequestReadWriteTimeout * 1000;
            return webRequest;
        }

        /// <summary>
        /// Create a IWebProxy Object which can be used to access the Internet
        /// This method will check the configuration if the proxy is allowed to be used.
        /// Usages can be found in the DownloadFavIcon or Jira and Confluence plugins
        /// </summary>
        /// <param name="uri"></param>
        /// <returns>IWebProxy filled with all the proxy details or null if none is set/wanted</returns>
        public static IWebProxy CreateProxy(Uri uri)
        {
            IWebProxy proxyToUse = null;
            if (!Config.UseProxy)
            {
                return proxyToUse;
            }

            proxyToUse = WebRequest.DefaultWebProxy;
            if (proxyToUse != null)
            {
                proxyToUse.Credentials = CredentialCache.DefaultCredentials;
                if (!Log.IsDebugEnabled)
                {
                    return proxyToUse;
                }

                // check the proxy for the Uri
                if (!proxyToUse.IsBypassed(uri))
                {
                    var proxyUri = proxyToUse.GetProxy(uri);
                    if (proxyUri != null)
                    {
                        Log.Debug("Using proxy: " + proxyUri + " for " + uri);
                    }
                    else
                    {
                        Log.Debug("No proxy found!");
                    }
                }
                else
                {
                    Log.Debug("Proxy bypass for: " + uri);
                }
            }
            else
            {
                Log.Debug("No proxy found!");
            }

            return proxyToUse;
        }

        /// <summary>
        /// UrlEncodes a string without the requirement for System.Web
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        // [Obsolete("Use System.Uri.EscapeDataString instead")]
        public static string UrlEncode(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                // System.Uri provides reliable parsing, but doesn't encode spaces.
                return Uri.EscapeDataString(text).Replace("%20", "+");
            }

            return null;
        }

        /// <summary>
        /// A wrapper around the EscapeDataString, as the limit is 32766 characters
        /// See: https://msdn.microsoft.com/en-us/library/system.uri.escapedatastring%28v=vs.110%29.aspx
        /// </summary>
        /// <param name="text"></param>
        /// <returns>escaped data string</returns>
        public static string EscapeDataString(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }
            var result = new StringBuilder();
            int currentLocation = 0;
            while (currentLocation < text.Length)
            {
                string process = text.Substring(currentLocation, Math.Min(16384, text.Length - currentLocation));
                result.Append(Uri.EscapeDataString(process));
                currentLocation += 16384;
            }

            return result.ToString();

        }

        /// <summary>
        /// UrlDecodes a string without requiring System.Web
        /// </summary>
        /// <param name="text">String to decode.</param>
        /// <returns>decoded string</returns>
        public static string UrlDecode(string text)
        {
            // pre-process for + sign space formatting since System.Uri doesn't handle it
            // plus literals are encoded as %2b normally so this should be safe
            text = text.Replace("+", " ");
            return Uri.UnescapeDataString(text);
        }

        /// <summary>
        /// ParseQueryString without the requirement for System.Web
        /// </summary>
        /// <param name="queryString"></param>
        /// <returns>IDictionary string, string</returns>
        public static IDictionary<string, string> ParseQueryString(string queryString)
        {
            IDictionary<string, string> parameters = new SortedDictionary<string, string>();
            // remove anything other than query string from uri
            if (queryString.Contains("?"))
            {
                queryString = queryString.Substring(queryString.IndexOf('?') + 1);
            }

            foreach (string vp in Regex.Split(queryString, "&"))
            {
                if (string.IsNullOrEmpty(vp))
                {
                    continue;
                }

                string[] singlePair = Regex.Split(vp, "=");
                if (parameters.ContainsKey(singlePair[0]))
                {
                    parameters.Remove(singlePair[0]);
                }

                parameters.Add(singlePair[0], singlePair.Length == 2 ? singlePair[1] : string.Empty);
            }

            return parameters;
        }

        /// <summary>
        /// Generate the query parameters
        /// </summary>
        /// <param name="queryParameters">the list of query parameters</param>
        /// <returns>a string with the query parameters</returns>
        public static string GenerateQueryParameters(IDictionary<string, object> queryParameters)
        {
            if (queryParameters == null || queryParameters.Count == 0)
            {
                return string.Empty;
            }

            queryParameters = new SortedDictionary<string, object>(queryParameters);

            var sb = new StringBuilder();
            foreach (string key in queryParameters.Keys)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0}={1}&", key, UrlEncode($"{queryParameters[key]}"));
            }

            sb.Remove(sb.Length - 1, 1);

            return sb.ToString();
        }

        /// <summary>
        /// Write Multipart Form Data directly to the HttpWebRequest
        /// </summary>
        /// <param name="webRequest">HttpWebRequest to write the multipart form data to</param>
        /// <param name="postParameters">Parameters to include in the multipart form data</param>
        public static void WriteMultipartFormData(HttpWebRequest webRequest, IDictionary<string, object> postParameters)
        {
            string boundary = $"----------{Guid.NewGuid():N}";
            webRequest.ContentType = "multipart/form-data; boundary=" + boundary;
            using Stream formDataStream = webRequest.GetRequestStream();
            WriteMultipartFormData(formDataStream, boundary, postParameters);
        }

        /// <summary>
        /// Write Multipart Form Data to a Stream, content-type should be set before this!
        /// </summary>
        /// <param name="formDataStream">Stream to write the multipart form data to</param>
        /// <param name="boundary">String boundary for the multipart/form-data</param>
        /// <param name="postParameters">Parameters to include in the multipart form data</param>
        public static void WriteMultipartFormData(Stream formDataStream, string boundary, IDictionary<string, object> postParameters)
        {
            bool needsClrf = false;
            foreach (var param in postParameters)
            {
                // Add a CRLF to allow multiple parameters to be added.
                // Skip it on the first parameter, add it to subsequent parameters.
                if (needsClrf)
                {
                    formDataStream.Write(Encoding.UTF8.GetBytes("\r\n"), 0, Encoding.UTF8.GetByteCount("\r\n"));
                }

                needsClrf = true;

                if (param.Value is IBinaryContainer binaryContainer)
                {
                    binaryContainer.WriteFormDataToStream(boundary, param.Key, formDataStream);
                }
                else
                {
                    string postData = $"--{boundary}\r\nContent-Disposition: form-data; name=\"{param.Key}\"\r\n\r\n{param.Value}";
                    formDataStream.Write(Encoding.UTF8.GetBytes(postData), 0, Encoding.UTF8.GetByteCount(postData));
                }
            }

            // Add the end of the request.  Start with a newline
            string footer = "\r\n--" + boundary + "--\r\n";
            formDataStream.Write(Encoding.UTF8.GetBytes(footer), 0, Encoding.UTF8.GetByteCount(footer));
        }

        /// <summary>
        /// Post content HttpWebRequest
        /// </summary>
        /// <param name="webRequest">HttpWebRequest to write the multipart form data to</param>
        /// <param name="headers">IDictionary with the headers</param>
        /// <param name="binaryContainer">IBinaryContainer</param>
        public static void Post(HttpWebRequest webRequest, IDictionary<string, object> headers, IBinaryContainer binaryContainer = null)
        {
            foreach (var header in headers)
            {
                switch (header.Key)
                {
                    case "Content-Type":
                        webRequest.ContentType = header.Value as string;
                        break;
                    case "Accept":
                        webRequest.Accept = header.Value as string;
                        break;
                    default:
                        webRequest.Headers.Add(header.Key, Convert.ToString(header.Value));
                        break;
                }
            }

            if (!headers.ContainsKey("Content-Type"))
            {
                webRequest.ContentType = "application/octet-stream";
            }

            if (binaryContainer != null)
            {
                using var requestStream = webRequest.GetRequestStream();
                binaryContainer.WriteToStream(requestStream);
            }
        }

        /// <summary>
        /// Post content HttpWebRequest
        /// </summary>
        /// <param name="webRequest">HttpWebRequest to write the multipart form data to</param>
        /// <param name="headers">IDictionary with the headers</param>
        /// <param name="jsonString">string</param>
        public static void Post(HttpWebRequest webRequest, IDictionary<string, object> headers, string jsonString)
        {
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    switch (header.Key)
                    {
                        case "Content-Type":
                            webRequest.ContentType = header.Value as string;
                            break;
                        case "Accept":
                            webRequest.Accept = header.Value as string;
                            break;
                        default:
                            webRequest.Headers.Add(header.Key, Convert.ToString(header.Value));
                            break;
                    }
                }

                if (!headers.ContainsKey("Content-Type"))
                {
                    webRequest.ContentType = "application/json";
                }
            }
            else
            {
                webRequest.ContentType = "application/json";
            }

            if (jsonString != null)
            {
                using var requestStream = webRequest.GetRequestStream();
                using var streamWriter = new StreamWriter(requestStream);
                streamWriter.Write(jsonString);
            }
        }

        /// <summary>
        /// Post the parameters "x-www-form-urlencoded"
        /// </summary>
        /// <param name="webRequest"></param>
        /// <param name="parameters"></param>
        public static void UploadFormUrlEncoded(HttpWebRequest webRequest, IDictionary<string, object> parameters)
        {
            webRequest.ContentType = "application/x-www-form-urlencoded";
            string urlEncoded = GenerateQueryParameters(parameters);

            byte[] data = Encoding.UTF8.GetBytes(urlEncoded);
            using var requestStream = webRequest.GetRequestStream();
            requestStream.Write(data, 0, data.Length);
        }

        /// <summary>
        /// Log the headers of the WebResponse, if IsDebugEnabled
        /// </summary>
        /// <param name="response">WebResponse</param>
        private static void DebugHeaders(WebResponse response)
        {
            if (!Log.IsDebugEnabled)
            {
                return;
            }

            Log.DebugFormat("Debug information on the response from {0} :", response.ResponseUri);
            foreach (string key in response.Headers.AllKeys)
            {
                Log.DebugFormat("Reponse-header: {0}={1}", key, response.Headers[key]);
            }
        }

        /// <summary>
        /// Process the web response.
        /// </summary>
        /// <param name="webRequest">The request object.</param>
        /// <returns>The response data.</returns>
        /// TODO: This method should handle the StatusCode better!
        public static string GetResponseAsString(HttpWebRequest webRequest)
        {
            return GetResponseAsString(webRequest, false);
        }

        /// <summary>
        /// Read the response as string
        /// </summary>
        /// <param name="response"></param>
        /// <returns>string or null</returns>
        private static string GetResponseAsString(HttpWebResponse response)
        {
            string responseData = null;
            if (response == null)
            {
                return null;
            }

            using (response)
            {
                Stream responseStream = response.GetResponseStream();
                if (responseStream != null)
                {
                    using StreamReader reader = new StreamReader(responseStream, true);
                    responseData = reader.ReadToEnd();
                }
            }

            return responseData;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="webRequest"></param>
        /// <param name="alsoReturnContentOnError"></param>
        /// <returns></returns>
        public static string GetResponseAsString(HttpWebRequest webRequest, bool alsoReturnContentOnError)
        {
            string responseData = null;
            HttpWebResponse response = null;
            bool isHttpError = false;
            try
            {
                response = (HttpWebResponse) webRequest.GetResponse();
                Log.InfoFormat("Response status: {0}", response.StatusCode);
                isHttpError = (int) response.StatusCode >= 300;
                if (isHttpError)
                {
                    Log.ErrorFormat("HTTP error {0}", response.StatusCode);
                }

                DebugHeaders(response);
                responseData = GetResponseAsString(response);
                if (isHttpError)
                {
                    Log.ErrorFormat("HTTP response {0}", responseData);
                }
            }
            catch (WebException e)
            {
                response = (HttpWebResponse) e.Response;
                HttpStatusCode statusCode = HttpStatusCode.Unused;
                if (response != null)
                {
                    statusCode = response.StatusCode;
                    Log.ErrorFormat("HTTP error {0}", statusCode);
                    string errorContent = GetResponseAsString(response);
                    if (alsoReturnContentOnError)
                    {
                        return errorContent;
                    }

                    Log.ErrorFormat("Content: {0}", errorContent);
                }

                Log.Error("WebException: ", e);
                if (statusCode == HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException(e.Message);
                }

                throw;
            }
            finally
            {
                if (response != null)
                {
                    if (isHttpError)
                    {
                        Log.ErrorFormat("HTTP error {0} with content: {1}", response.StatusCode, responseData);
                    }

                    response.Close();
                }
            }

            return responseData;
        }
    }

    /// <summary>
    /// This interface can be used to pass binary information around, like byte[] or Image
    /// </summary>
    public interface IBinaryContainer
    {
        void WriteFormDataToStream(string boundary, string name, Stream formDataStream);
        void WriteToStream(Stream formDataStream);
        string ToBase64String(Base64FormattingOptions formattingOptions);
        byte[] ToByteArray();
        void Upload(HttpWebRequest webRequest);

        string ContentType { get; }
        string Filename { get; set; }
    }

    /// A container to supply surfaces to a Multi-part form data upload
    /// </summary>
    public class SurfaceContainer : IBinaryContainer
    {
        private readonly ISurface _surface;
        private readonly SurfaceOutputSettings _outputSettings;

        public SurfaceContainer(ISurface surface, SurfaceOutputSettings outputSettings, string filename)
        {
            _surface = surface;
            _outputSettings = outputSettings;
            Filename = filename;
        }

        /// <summary>
        /// Create a Base64String from the Surface by saving it to a memory stream and converting it.
        /// Should be avoided if possible, as this uses a lot of memory.
        /// </summary>
        /// <returns>string</returns>
        public string ToBase64String(Base64FormattingOptions formattingOptions)
        {
            using MemoryStream stream = new MemoryStream();
            ImageIO.SaveToStream(_surface, stream, _outputSettings);
            return Convert.ToBase64String(stream.GetBuffer(), 0, (int) stream.Length, formattingOptions);
        }

        /// <summary>
        /// Create a byte[] from the image by saving it to a memory stream.
        /// Should be avoided if possible, as this uses a lot of memory.
        /// </summary>
        /// <returns>byte[]</returns>
        public byte[] ToByteArray()
        {
            using MemoryStream stream = new MemoryStream();
            ImageIO.SaveToStream(_surface, stream, _outputSettings);
            return stream.ToArray();
        }

        /// <summary>
        /// Write Multipart Form Data directly to the HttpWebRequest response stream
        /// </summary>
        /// <param name="boundary">Multipart separator</param>
        /// <param name="name">Name of the thing</param>
        /// <param name="formDataStream">Stream to write to</param>
        public void WriteFormDataToStream(string boundary, string name, Stream formDataStream)
        {
            // Add just the first part of this param, since we will write the file data directly to the Stream
            string header = $"--{boundary}\r\nContent-Disposition: form-data; name=\"{name}\"; filename=\"{Filename ?? name}\";\r\nContent-Type: {ContentType}\r\n\r\n";

            formDataStream.Write(Encoding.UTF8.GetBytes(header), 0, Encoding.UTF8.GetByteCount(header));
            ImageIO.SaveToStream(_surface, formDataStream, _outputSettings);
        }

        /// <summary>
        /// A plain "write data to stream"
        /// </summary>
        /// <param name="dataStream"></param>
        public void WriteToStream(Stream dataStream)
        {
            // Write the file data directly to the Stream, rather than serializing it to a string.
            ImageIO.SaveToStream(_surface, dataStream, _outputSettings);
        }

        /// <summary>
        /// Upload the Surface as image to the webrequest
        /// </summary>
        /// <param name="webRequest"></param>
        public void Upload(HttpWebRequest webRequest)
        {
            webRequest.ContentType = ContentType;
            using var requestStream = webRequest.GetRequestStream();
            WriteToStream(requestStream);
        }

        public string ContentType => "image/" + _outputSettings.Format;
        public string Filename { get; set; }
    }
}