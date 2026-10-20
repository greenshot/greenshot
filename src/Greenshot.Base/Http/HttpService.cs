/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base.Core;
using log4net;
using Newtonsoft.Json;

namespace Greenshot.Base.Http
{
    /// <summary>
    /// All HTTP of Greenshot goes through here: one HttpClient with the proxy, the certificate rules and the user agent.
    /// Nothing that is sent or received (headers, query, content) is logged, only method, host and status, because they can hold tokens.
    /// </summary>
    public sealed class HttpService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(HttpService));

        /// <summary>
        /// HttpClient has one timeout for the whole request: a large upload over a slow connection needs time, the user can cancel,
        /// unattended flows end at the latest after this.
        /// </summary>
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(10);

        private static readonly Lazy<HttpService> SharedService = new Lazy<HttpService>(() => new HttpService(CreateHandler(true), DefaultTimeout));
        private static readonly Lazy<HttpService> SharedServiceWithoutRedirects = new Lazy<HttpService>(() => new HttpService(CreateHandler(false), DefaultTimeout));

        private readonly HttpClient _httpClient;

        /// <summary>
        /// The HttpService of Greenshot, configured from the core configuration.
        /// </summary>
        public static HttpService Current => SharedService.Value;

        /// <summary>
        /// Like <see cref="Current"/>, but a redirect is not followed and comes back as the 3xx result.
        /// For requests with headers or content of the caller (keys, tokens): HttpClientHandler would send them to whatever host
        /// the redirect names, on .NET Framework even from https to http.
        /// </summary>
        public static HttpService WithoutRedirects => SharedServiceWithoutRedirects.Value;

        /// <summary>
        /// An HttpService over the handler, for tests or a special handler.
        /// </summary>
        public HttpService(HttpMessageHandler handler, TimeSpan timeout)
        {
            _httpClient = new HttpClient(handler ?? throw new ArgumentNullException(nameof(handler)))
            {
                Timeout = timeout
            };
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"Greenshot/{EnvironmentInfo.GetGreenshotVersion(true)}");
        }

        internal static HttpClientHandler CreateHandler(bool followRedirects)
        {
            var config = IniConfigRegistry.GetSection<ICoreConfiguration>();
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = followRedirects,
                // Windows credentials only go to the proxy (below), never to a server which asks for them
                UseDefaultCredentials = false,
                UseProxy = config?.UseProxy ?? true,
                ServerCertificateCustomValidationCallback = (message, certificate, chain, errors) => CertificateValidator.Validate(message?.RequestUri, certificate, chain, errors)
            };
            if (handler.UseProxy && WebRequest.DefaultWebProxy is { } proxy)
            {
                proxy.Credentials = CredentialCache.DefaultCredentials;
                handler.Proxy = proxy;
            }

            return handler;
        }

        /// <summary>
        /// Send the request and read the whole response. HTTP errors don't throw, check <see cref="HttpResult.IsSuccessStatusCode"/> or call <see cref="HttpResult.EnsureSuccess"/>.
        /// </summary>
        /// <param name="request">HttpRequestMessage, disposed after sending</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <exception cref="TimeoutException">the request took longer than the timeout</exception>
        /// <exception cref="HttpRequestException">no connection</exception>
        public async Task<HttpResult> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            using (request)
            {
                using var response = await SendCoreAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
                string content = response.Content == null ? null : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return new HttpResult(response.StatusCode, response.ReasonPhrase, response.RequestMessage?.RequestUri ?? request.RequestUri, HttpResult.CollectHeaders(response), content);
            }
        }

        /// <summary>
        /// Post the fields as application/x-www-form-urlencoded.
        /// </summary>
        public Task<HttpResult> PostFormAsync(string url, IEnumerable<KeyValuePair<string, string>> fields, CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new FormUrlEncodedContent(fields ?? Array.Empty<KeyValuePair<string, string>>())
            };
            return SendAsync(request, cancellationToken);
        }

        /// <summary>
        /// Get the JSON at the uri as T.
        /// </summary>
        /// <exception cref="HttpRequestException">for an HTTP error</exception>
        public async Task<T> GetJsonAsync<T>(Uri uri, CancellationToken cancellationToken)
        {
            var result = await SendAsync(new HttpRequestMessage(HttpMethod.Get, uri), cancellationToken).ConfigureAwait(false);
            result.EnsureSuccess();
            return string.IsNullOrEmpty(result.Content) ? default : JsonConvert.DeserializeObject<T>(result.Content);
        }

        /// <summary>
        /// Get the content at the url as bytes.
        /// </summary>
        /// <exception cref="HttpRequestException">for an HTTP error</exception>
        public Task<byte[]> GetBytesAsync(string url, CancellationToken cancellationToken) => GetBytesAsync(new HttpRequestMessage(HttpMethod.Get, url), cancellationToken);

        /// <summary>
        /// Send the request (with headers of the caller) and read the content as bytes.
        /// </summary>
        /// <param name="request">HttpRequestMessage, disposed after sending</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <exception cref="HttpRequestException">for an HTTP error</exception>
        public async Task<byte[]> GetBytesAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            using var disposeRequest = request;
            using var response = await SendCoreAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"{(int)response.StatusCode} {response.ReasonPhrase}");
            }

            return response.Content == null ? Array.Empty<byte>() : await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// The status of a GET without reading the content, null when there is no connection or it timed out.
        /// </summary>
        public async Task<HttpStatusCode?> GetStatusAsync(string url, CancellationToken cancellationToken)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await SendCoreAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                return response.StatusCode;
            }
            catch (Exception ex) when (ex is HttpRequestException or TimeoutException)
            {
                return null;
            }
        }

        private async Task<HttpResponseMessage> SendCoreAsync(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken)
        {
            if (Log.IsDebugEnabled)
            {
                Log.DebugFormat("{0} {1}: sending {2}", request.Method, Describe(request.RequestUri), DescribeContent(request.Content));
            }

            var stopwatch = Stopwatch.StartNew();
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, completionOption, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                // HttpClient reports its timeout as a cancellation
                throw new TimeoutException($"{request.Method} {Describe(request.RequestUri)} timed out after {_httpClient.Timeout}", ex);
            }

            Log.InfoFormat("{0} {1}: {2}", request.Method, Describe(request.RequestUri), (int)response.StatusCode);
            if (Log.IsDebugEnabled)
            {
                Log.DebugFormat("{0} {1}: answer {2} after {3} ms", request.Method, Describe(request.RequestUri), DescribeContent(response.Content), stopwatch.ElapsedMilliseconds);
            }

            return response;
        }

        /// <summary>
        /// The shape of a body for the debug log, never a value: the type and length, for a form the names of the fields and the
        /// name, type and length of a file. Shows whether the file went out without logging keys or contents.
        /// </summary>
        internal static string DescribeContent(HttpContent content)
        {
            if (content == null)
            {
                return "no body";
            }

            string type = content.Headers.ContentType?.MediaType ?? "no type";
            if (content is not MultipartContent multipart)
            {
                return $"{type}, {DescribeLength(content.Headers)}";
            }

            var parts = multipart.Select(part =>
            {
                var disposition = part.Headers.ContentDisposition;
                string name = disposition?.Name?.Trim('"') ?? "(no name)";
                string fileName = disposition?.FileNameStar ?? disposition?.FileName?.Trim('"');
                return fileName == null
                    ? name
                    : $"{name} (file \"{fileName}\", {part.Headers.ContentType?.MediaType ?? "no type"}, {DescribeLength(part.Headers)})";
            });
            return $"{type} with {string.Join(", ", parts)}";
        }

        private static string DescribeLength(HttpContentHeaders headers) => headers.ContentLength is long length ? $"{length} bytes" : "unknown length";

        /// <summary>
        /// Scheme, host and port: the path and the query can hold tokens (a webhook url, an api key), they are not logged.
        /// </summary>
        internal static string Describe(Uri uri) => uri == null ? "(no uri)" : uri.IsAbsoluteUri ? uri.GetLeftPart(UriPartial.Authority) : "(relative uri)";
    }
}
