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
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core.FileFormatHandlers;
using Dapplo.Ini;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Drawing;
using Greenshot.Base.Interfaces.Plugin;
using log4net;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Description of NetworkHelper.
    /// </summary>
    public static class NetworkHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(NetworkHelper));
        private static readonly ICoreConfiguration Config = IniConfigRegistry.GetSection<ICoreConfiguration>();

        static NetworkHelper()
        {
            try
            {
                ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;
            }
            catch (Exception ex)
            {
                Log.Warn("An error has occurred while configuring certificate validation callback:", ex);
            }
        }

        /// <summary>
        /// Validates server SSL/TLS certificates according to standard rules and configured exceptions in the Core configuration.
        /// </summary>
        /// <param name="sender">The sender object (e.g. HttpWebRequest or ServicePoint)</param>
        /// <param name="certificate">The certificate to validate</param>
        /// <param name="chain">The certificate chain</param>
        /// <param name="sslPolicyErrors">Any SSL policy errors identified by the platform</param>
        /// <returns>True if the certificate is accepted; otherwise, false.</returns>
        public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // If standard platform validation succeeded without any policy errors, accept
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            if (certificate == null)
            {
                Log.Warn($"SSL/TLS certificate validation failed: certificate is null (Policy errors: {sslPolicyErrors}).");
                return false;
            }

            // 1. Check certificate thumbprint allowlist
            var certThumbprint = (certificate as X509Certificate2)?.Thumbprint ?? certificate.GetCertHashString();
            if (!string.IsNullOrEmpty(certThumbprint) && Config?.AllowedCertificateThumbprints != null)
            {
                string normalizedThumbprint = certThumbprint.Replace(":", "").Replace(" ", "");
                foreach (var allowedThumbprint in Config.AllowedCertificateThumbprints)
                {
                    if (string.IsNullOrWhiteSpace(allowedThumbprint))
                    {
                        continue;
                    }

                    string normalizedAllowed = allowedThumbprint.Replace(":", "").Replace(" ", "");
                    if (string.Equals(normalizedAllowed, normalizedThumbprint, StringComparison.OrdinalIgnoreCase))
                    {
                        Log.Info($"SSL/TLS certificate validation exception accepted by thumbprint '{certThumbprint}' for subject '{certificate.Subject}'.");
                        return true;
                    }
                }
            }

            // 2. Extract host from sender (HttpWebRequest, ServicePoint, Uri, string)
            string host = null;
            if (sender is HttpWebRequest webRequest && webRequest.RequestUri != null)
            {
                host = webRequest.RequestUri.Host;
            }
            else if (sender is ServicePoint servicePoint && servicePoint.Address != null)
            {
                host = servicePoint.Address.Host;
            }
            else if (sender is Uri uri)
            {
                host = uri.Host;
            }
            else if (sender is string hostStr)
            {
                if (Uri.TryCreate(hostStr, UriKind.Absolute, out var parsedUri))
                {
                    host = parsedUri.Host;
                }
                else
                {
                    host = hostStr;
                }
            }

            // 3. Check allowed untrusted hosts allowlist
            if (!string.IsNullOrEmpty(host) && Config?.AllowedUntrustedCertificateHosts != null)
            {
                foreach (var pattern in Config.AllowedUntrustedCertificateHosts)
                {
                    if (string.IsNullOrWhiteSpace(pattern))
                    {
                        continue;
                    }

                    if (IsHostMatch(host, pattern.Trim()))
                    {
                        Log.Warn($"SSL/TLS certificate validation exception accepted for host '{host}' (Policy errors: {sslPolicyErrors}, Subject: '{certificate.Subject}').");
                        return true;
                    }
                }
            }

            Log.Warn($"SSL/TLS certificate validation failed for host '{(host ?? "unknown")}' (Policy errors: {sslPolicyErrors}, Subject: '{certificate.Subject}', Thumbprint: '{certThumbprint}').");
            return false;
        }

        /// <summary>
        /// Checks if a host matches a pattern (supports exact match and wildcard like *.example.com or *example.com).
        /// </summary>
        /// <param name="host">The target hostname</param>
        /// <param name="pattern">The configured pattern</param>
        /// <returns>True if host matches pattern, otherwise false</returns>
        public static bool IsHostMatch(string host, string pattern)
        {
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(pattern))
            {
                return false;
            }

            // Strip port if specified in host or pattern (e.g. host:8443)
            int colonIndex = host.IndexOf(':');
            if (colonIndex >= 0)
            {
                host = host.Substring(0, colonIndex);
            }

            int patternColonIndex = pattern.IndexOf(':');
            if (patternColonIndex >= 0)
            {
                pattern = pattern.Substring(0, patternColonIndex);
            }

            if (string.Equals(host, pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (pattern.Contains("*") || pattern.Contains("?"))
            {
                string regexPattern = "^" + Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
                try
                {
                    return Regex.IsMatch(host, regexPattern, RegexOptions.IgnoreCase);
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Download the uri into a memory stream, without catching exceptions
        /// </summary>
        /// <param name="url">Of an image</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>MemoryStream which is already seek-ed to 0</returns>
        public static async Task<MemoryStream> GetAsMemoryStreamAsync(string url, CancellationToken cancellationToken)
        {
            using var response = await HttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var memoryStream = RecyclableMemoryStreamFactory.GetStream("NetworkHelper.GetAsMemoryStream");
            using (var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
            {
                await responseStream.CopyToAsync(memoryStream, 81920, cancellationToken).ConfigureAwait(false);
            }

            // Make sure it can be used directly
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }

        /// <summary>
        /// Download the url, if it's not an image but a page with an image url in its first line, download that.
        /// </summary>
        /// <typeparam name="T">what is loaded from the stream</typeparam>
        /// <param name="url">Of an image</param>
        /// <param name="load">loads the result from the stream and the extension, null when it's not possible</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>the result or default, errors are logged</returns>
        private static async Task<T> DownloadAsync<T>(string url, Func<Stream, string, T> load, CancellationToken cancellationToken) where T : class
        {
            var fileFormatHandlers = SimpleServiceProvider.Current.GetAllInstances<IFileFormatHandler>();
            var extensions = string.Join("|", fileFormatHandlers.ExtensionsFor(FileFormatHandlerActions.LoadFromStream));

            var imageUrlRegex = new Regex($@"(http|https)://.*(?<extension>{extensions})");
            var match = imageUrlRegex.Match(url);
            try
            {
                string content;
                using (var memoryStream = await GetAsMemoryStreamAsync(url, cancellationToken).ConfigureAwait(false))
                {
                    try
                    {
                        var result = load(memoryStream, match.Success ? match.Groups["extension"]?.Value : null);
                        if (result != null)
                        {
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Debug("The download is not an image, checking for an image url", ex);
                    }

                    // If we arrive here, the image loading didn't work, try to see if the response has a http(s) URL to an image and just take this instead.
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    using var streamReader = new StreamReader(memoryStream, Encoding.UTF8, true);
                    content = await streamReader.ReadLineAsync().ConfigureAwait(false);
                }

                if (string.IsNullOrEmpty(content))
                {
                    return null;
                }

                match = imageUrlRegex.Match(content);
                if (!match.Success)
                {
                    return null;
                }

                using var memoryStream2 = await GetAsMemoryStreamAsync(match.Value, cancellationToken).ConfigureAwait(false);
                return load(memoryStream2, match.Groups["extension"]?.Value);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception e)
            {
                Log.Error("Problem downloading the image from: " + url, e);
            }

            return null;
        }

        /// <summary>
        /// Download the uri to build an IDrawableContainer
        /// </summary>
        /// <param name="url">Of an image</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>IDrawableContainer, null when it's not possible</returns>
        public static Task<IDrawableContainer> DownloadImageAsDrawableContainerAsync(string url, CancellationToken cancellationToken)
        {
            var fileFormatHandlers = SimpleServiceProvider.Current.GetAllInstances<IFileFormatHandler>();
            return DownloadAsync(url, (stream, extension) => fileFormatHandlers.LoadDrawablesFromStream(stream, extension).FirstOrDefault(), cancellationToken);
        }

        /// <summary>
        /// Download the uri to create a Bitmap
        /// </summary>
        /// <param name="url">Of an image</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Bitmap, null when it's not possible</returns>
        public static Task<Bitmap> DownloadImageAsync(string url, CancellationToken cancellationToken)
        {
            var fileFormatHandlers = SimpleServiceProvider.Current.GetAllInstances<IFileFormatHandler>();
            return DownloadAsync(url, (stream, extension) => fileFormatHandlers.TryLoadFromStream(stream, extension, out var bitmap) ? bitmap : null, cancellationToken);
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

        private static readonly Lazy<HttpClient> SharedHttpClient = new Lazy<HttpClient>(CreateHttpClient);

        /// <summary>
        /// The one HttpClient of Greenshot, with the proxy, credentials, timeout and certificate validation of the configuration.
        /// </summary>
        public static HttpClient HttpClient => SharedHttpClient.Value;

        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                UseDefaultCredentials = true,
                UseProxy = Config?.UseProxy ?? true,
                ServerCertificateCustomValidationCallback = (message, certificate, chain, errors) => ValidateServerCertificate(message?.RequestUri, certificate, chain, errors)
            };
            if (handler.UseProxy)
            {
                var proxy = WebRequest.DefaultWebProxy;
                if (proxy != null)
                {
                    proxy.Credentials = CredentialCache.DefaultCredentials;
                    handler.Proxy = proxy;
                }
            }

            // The upload of a large capture needs the read/write timeout, the request itself the connect timeout
            int timeoutSeconds = Math.Max(Config?.WebRequestTimeout ?? 100, Config?.WebRequestReadWriteTimeout ?? 100);
            var httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(Math.Max(timeoutSeconds, 30))
            };
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"Greenshot/{EnvironmentInfo.GetGreenshotVersion(true)}");
            return httpClient;
        }

        /// <summary>
        /// Send the request and return the content of the response as string.
        /// </summary>
        /// <param name="request">HttpRequestMessage, disposed after sending</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <param name="alsoReturnContentOnError">true to return the content of an error response instead of throwing</param>
        /// <returns>content of the response</returns>
        /// <exception cref="UnauthorizedAccessException">HTTP 401</exception>
        /// <exception cref="HttpRequestException">other HTTP errors</exception>
        public static async Task<string> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken, bool alsoReturnContentOnError = false)
        {
            using (request)
            {
                using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
                string content = response.Content == null ? null : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                Log.InfoFormat("Response status of {0} {1}: {2}", request.Method, request.RequestUri, response.StatusCode);
                if (response.IsSuccessStatusCode)
                {
                    return content;
                }

                Log.ErrorFormat("HTTP error {0} with content: {1}", response.StatusCode, content);
                if (alsoReturnContentOnError)
                {
                    return content;
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    throw new UnauthorizedAccessException($"{(int) response.StatusCode} {response.ReasonPhrase}");
                }

                throw new HttpRequestException($"{(int) response.StatusCode} {response.ReasonPhrase}: {content}");
            }
        }

        /// <summary>
        /// Post the parameters "x-www-form-urlencoded"
        /// </summary>
        public static Task<string> PostFormUrlEncodedAsync(string url, IDictionary<string, object> parameters, CancellationToken cancellationToken, bool alsoReturnContentOnError = false)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(GenerateQueryParameters(parameters), Encoding.UTF8, "application/x-www-form-urlencoded")
            };
            return SendAsync(request, cancellationToken, alsoReturnContentOnError);
        }

        /// <summary>
        /// The encoded capture as HTTP content (without copying the bytes), reports the upload progress.
        /// </summary>
        /// <param name="image">EncodedImage</param>
        /// <param name="progress">IProgress for the percentage of the upload, optional</param>
        public static HttpContent CreateContent(EncodedImage image, IProgress<ProgressInfo> progress = null)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            var content = new UploadContent(image.Bytes, progress);
            content.Headers.ContentType = new MediaTypeHeaderValue(image.MimeType);
            return content;
        }

        /// <summary>
        /// Multipart form data with the encoded capture as file and the other parameters as strings.
        /// </summary>
        public static MultipartFormDataContent CreateMultipartContent(string fileParameterName, EncodedImage image, string filename, IDictionary<string, object> parameters = null,
            IProgress<ProgressInfo> progress = null)
        {
            var multipartContent = new MultipartFormDataContent($"----------{Guid.NewGuid():N}");
            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    multipartContent.Add(new StringContent(Convert.ToString(parameter.Value, CultureInfo.InvariantCulture) ?? string.Empty), $"\"{parameter.Key}\"");
                }
            }

            multipartContent.Add(CreateContent(image, progress), $"\"{fileParameterName}\"", $"\"{filename}\"");
            return multipartContent;
        }

        /// <summary>
        /// Writes the bytes in chunks and reports the percentage, the cancellation of the request stops it.
        /// </summary>
        private sealed class UploadContent : HttpContent
        {
            private const int ChunkSize = 64 * 1024;
            private readonly ReadOnlyMemory<byte> _bytes;
            private readonly IProgress<ProgressInfo> _progress;

            public UploadContent(ReadOnlyMemory<byte> bytes, IProgress<ProgressInfo> progress)
            {
                _bytes = bytes;
                _progress = progress;
            }

            protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
            {
                var segment = System.Runtime.InteropServices.MemoryMarshal.TryGetArray(_bytes, out var arraySegment)
                    ? arraySegment
                    : new ArraySegment<byte>(_bytes.ToArray());
                int written = 0;
                while (written < segment.Count)
                {
                    int chunk = Math.Min(ChunkSize, segment.Count - written);
                    await stream.WriteAsync(segment.Array, segment.Offset + written, chunk).ConfigureAwait(false);
                    written += chunk;
                    _progress?.Report(new ProgressInfo(null, 100.0 * written / segment.Count));
                }
            }

            protected override bool TryComputeLength(out long length)
            {
                length = _bytes.Length;
                return true;
            }
        }
    }
}
