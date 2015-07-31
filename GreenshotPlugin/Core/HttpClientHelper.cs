/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Greenshot.IniFile;
using log4net;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Linq;
using System.Globalization;
using System.Drawing;
using System.Text.RegularExpressions;

namespace GreenshotPlugin.Core {

	/// <summary>
	/// Supply the HttpClient "helper" methods as extension methods, if possible.
	/// </summary>
	public static class HttpClientHelper {
		private static readonly CoreConfiguration Config = IniConfig.GetIniSection<CoreConfiguration>();
		private static readonly ILog LOG = LogManager.GetLogger(typeof(HttpClientHelper));

		/// <summary>
		/// Create a IWebProxy Object which can be used to access the Internet
		/// This method will check the configuration if the proxy is allowed to be used.
		/// </summary>
		/// <param name="uri"></param>
		/// <returns>IWebProxy filled with all the proxy details or null if none is set/wanted</returns>
		private static IWebProxy CreateProxy(Uri uri) {
			IWebProxy proxyToUse = null;
			if (Config.UseProxy) {
				proxyToUse = WebRequest.DefaultWebProxy;
				if (proxyToUse != null) {
					proxyToUse.Credentials = CredentialCache.DefaultCredentials;
					if (LOG.IsDebugEnabled) {
						// check the proxy for the Uri
						if (!proxyToUse.IsBypassed(uri)) {
							Uri proxyUri = proxyToUse.GetProxy(uri);
							if (proxyUri != null) {
								LOG.Debug("Using proxy: " + proxyUri + " for " + uri);
							} else {
								LOG.Debug("No proxy found!");
							}
						} else {
							LOG.Debug("Proxy bypass for: " + uri);
						}
					}
				} else {
					LOG.Debug("No proxy found!");
				}
			}
			return proxyToUse;
		}

		/// <summary>
		/// Create a HttpClient with default, configured, settings
		/// </summary>
		/// <param name="uri">Uri needed for the Proxy logic</param>
		/// <returns>HttpClient</returns>
		public static HttpClient CreateHttpClient(this Uri uri) {
			var cookies = new CookieContainer();

			var handler = new HttpClientHandler {
				CookieContainer = cookies,
				UseCookies = true,
				UseDefaultCredentials = true,
				Credentials = CredentialCache.DefaultCredentials,
				AllowAutoRedirect = true,
				AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
				// BUG-1655: Fix that Greenshot always uses the default proxy even if the "use default proxy" checkbox is unset
				Proxy = Config.UseProxy ? CreateProxy(uri) : null,
				UseProxy = Config.UseProxy,
			};

			var client = new HttpClient(handler);
			client.Timeout = TimeSpan.FromSeconds(Config.HttpConnectionTimeout);
			return client;
		}

		
		/// <summary>
		/// Set Basic Authentication for the current client
		/// </summary>
		/// <param name="user">username</param>
		/// <param name="password">password</param>
		public static HttpClient SetBasicAuthorization(this HttpClient client, string user, string password) {
			string credentials = Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes(string.Format("{0}:{1}", user, password)));
			return client.SetAuthorization("Basic", credentials);
		}

		/// <summary>
		/// Set Bearer "Authentication" for the current client
		/// </summary>
		/// <param name="user">username</param>
		/// <param name="password">password</param>
		public static HttpClient SetBearer(this HttpClient client, string bearer) {
			return client.SetAuthorization("Bearer", bearer);
		}

		/// <summary>
		/// Set Authorization for the current client
		/// </summary>
		/// <param name="scheme">scheme</param>
		/// <param name="authorization">value</param>
		public static HttpClient SetAuthorization(this HttpClient client, string scheme, string authorization) {
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme, authorization);
			return client;
		}

		/// <summary>
		/// Add default request header without validation
		/// </summary>
		/// <param name="scheme">name</param>
		/// <param name="authorization">value</param>
		public static HttpClient AddDefaultRequestHeader(this HttpClient client, string name, string value) {
			client.DefaultRequestHeaders.TryAddWithoutValidation("X-Atlassian-Token", "nocheck");
			return client;
		}

		/// <summary>
		/// ReadAsStringAsync
		/// </summary>
		/// <param name="response"></param>
		/// <param name="throwErrorOnNonSuccess"></param>
		/// <returns></returns>
		public static async Task<string> GetAsStringAsync(this HttpResponseMessage response, bool throwErrorOnNonSuccess = true) {
			if (response.IsSuccessStatusCode) {
				return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
			}
			if (throwErrorOnNonSuccess) {
				response.EnsureSuccessStatusCode();
			}
			return null;
		}

		/// <summary>
		/// GetAsJsonAsync
		/// </summary>
		/// <param name="response"></param>
		/// <param name="throwErrorOnNonSuccess"></param>
		/// <returns>dynamic (DyanmicJson)</returns>
		public static async Task<dynamic> GetAsJsonAsync(this HttpResponseMessage response, bool throwErrorOnNonSuccess = true) {
			if (response.IsSuccessStatusCode) {
				var jsonString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
				return DynamicJson.Parse(jsonString);
			}
			if (throwErrorOnNonSuccess) {
				response.EnsureSuccessStatusCode();
			}
			return null;
		}

		/// <summary>
		/// Download a uri response as string
		/// </summary>
		/// <param name="uri">An Uri to specify the download location</param>
		/// <returns>HttpResponseMessage</returns>
		public static async Task<HttpResponseMessage> GetAsync(this Uri uri, CancellationToken token = default(CancellationToken)) {
			using (var client = uri.CreateHttpClient()) {
				try {
					return await client.GetAsync(uri, token).ConfigureAwait(false);
				} catch (Exception ex) {
					LOG.Warn(ex);
					throw;
				}
			}
		}

		/// <summary>
		/// Download a uri response as string
		/// </summary>
		/// <param name="uri">An Uri to specify the download location</param>
		/// <returns>string with the content</returns>
		public static async Task<string> GetAsStringAsync(this Uri uri, bool throwError = true, CancellationToken token = default(CancellationToken)) {
			using (var client = uri.CreateHttpClient())
			using (var response = await client.GetAsync(uri, HttpCompletionOption.ResponseContentRead, token).ConfigureAwait(false)) {
				return await response.GetAsStringAsync(throwError).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Get the content as a MemoryStream
		/// </summary>
		/// <param name="response"></param>
		/// <param name="throwErrorOnNonSuccess"></param>
		/// <returns>MemoryStream</returns>
		public static async Task<MemoryStream> GetAsMemoryStreamAsync(this HttpResponseMessage response, bool throwErrorOnNonSuccess = true, CancellationToken token = default(CancellationToken)) {
			if (response.IsSuccessStatusCode) {
				using (var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false)) {
					var memoryStream = new MemoryStream();
					await contentStream.CopyToAsync(memoryStream, 4096, token).ConfigureAwait(false);
					return memoryStream;
				}
			}
			try {
				response.EnsureSuccessStatusCode();
			} catch (Exception ex) {
				LOG.WarnFormat("{0} -> {1}", response.RequestMessage.RequestUri, ex.Message);
				if (throwErrorOnNonSuccess) {
					throw;
				}
			}
			return null;
		}


		/// <summary>
		/// Simple extension to post Form-URLEncoded Content
		/// </summary>
		/// <param name="uri">Uri to post to</param>
		/// <param name="formContent">Dictionary with the values</param>
		/// <param name="token">Cancellationtoken</param>
		/// <returns>HttpResponseMessage</returns>
		public static async Task<HttpResponseMessage> PostFormUrlEncodedAsync(this Uri uri, IDictionary<string, string> formContent, CancellationToken token = default(CancellationToken)) {
			var content = new FormUrlEncodedContent(formContent);
			using (var client = uri.CreateHttpClient()) {
				return await client.PostAsync(uri, content);
			}
		}

		/// <summary>
		/// Simplified error handling, this makes sure the uri & response are logged
		/// </summary>
		/// <param name="responseMessage"></param>
		public static async Task HandleErrorAsync(this HttpResponseMessage responseMessage, CancellationToken token = default(CancellationToken)) {
			Exception throwException = null;
			string errorContent = null;
			Uri requestUri = null;
			try {
				if (!responseMessage.IsSuccessStatusCode) {
					requestUri = responseMessage.RequestMessage.RequestUri;
					try {
						// try reading the content, so this is not lost
						errorContent = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
						LOG.WarnFormat("Error loading {0}: {1}", requestUri, errorContent);
					} catch {
						// Ignore
					}
					responseMessage.EnsureSuccessStatusCode();
				}
			} catch (Exception ex) {
				throwException = ex;
				throwException.Data.Add("uri", requestUri);
			}
			if (throwException != null) {
				if (errorContent != null) {
					throwException.Data.Add("response", errorContent);
				}
				throw throwException;
			}
			return;
		}

		/// <summary>
		/// Download the uri to Bitmap
		/// </summary>
		/// <param name="url">Of an image</param>
		/// <returns>Bitmap</returns>
		public static async Task<Image> DownloadImageAsync(this Uri uri) {
			try {
				Exception initialException;
				string content;
				using (var response = await uri.GetAsync())
				using (var stream = await response.GetAsMemoryStreamAsync()) {
					try {
						using (Image image = Image.FromStream(stream)) {
							return ImageHelper.Clone(image, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
						}
					} catch (Exception ex) {
						// This might be okay, maybe it was just a search result
						initialException = ex;
					}
					stream.Seek(0, SeekOrigin.Begin);
					// If we arrive here, the image loading didn't work, try to see if the response has a http(s) URL to an image and just take this instead.
					using (var streamReader = new StreamReader(stream, Encoding.UTF8, true)) {
						content = await streamReader.ReadLineAsync();
					}
				}
				Regex imageUrlRegex = new Regex(@"(http|https)://.*(\.png|\.gif|\.jpg|\.tiff|\.jpeg|\.bmp)");
				Match match = imageUrlRegex.Match(content);
				if (match.Success) {
					Uri contentUri = new Uri(match.Value);
					using (var response = await contentUri.GetAsync())
					using (var stream = await response.GetAsMemoryStreamAsync()) {
						using (Image image = Image.FromStream(stream)) {
							return ImageHelper.Clone(image, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
						}
					}
				}
				throw initialException;
			} catch (Exception e) {
				LOG.ErrorFormat("Problem downloading the image from: {0}", uri);
				LOG.Error(e);
			}
			return null;
		}

		/// <summary>
		/// Head method
		/// </summary>
		/// <param name="uri"></param>
		/// <returns></returns>
		public static async Task<HttpContentHeaders> HeadAsync(this Uri uri, CancellationToken token = default(CancellationToken)) {
			using (var client = uri.CreateHttpClient())
			using (var request = new HttpRequestMessage(HttpMethod.Head, uri)) {
				var response = await client.SendAsync(request, token).ConfigureAwait(false);
				return response.Content.Headers;
			}
		}

		/// <summary>
		/// Get LastModified for a URI
		/// </summary>
		/// <param name="uri">Uri</param>
		/// <returns>DateTime</returns>
		public static async Task<DateTimeOffset> LastModifiedAsync(this Uri uri, CancellationToken token = default(CancellationToken)) {
			try {
				var headers = await uri.HeadAsync(token).ConfigureAwait(false);
				if (headers.LastModified.HasValue) {
					return headers.LastModified.Value;
				}
			} catch (Exception wE) {
				LOG.WarnFormat("Problem requesting HTTP - HEAD on uri {0}", uri);
				LOG.Warn(wE.Message);
			}
			// Pretend it is old
			return DateTimeOffset.MinValue;
		}

		/// <summary>
		///     Adds query string value to an existing url, both absolute and relative URI's are supported.
		/// </summary>
		/// <example>
		/// <code>
		///     // returns "www.domain.com/test?param1=val1&amp;param2=val2&amp;param3=val3"
		///     new Uri("www.domain.com/test?param1=val1").ExtendQuery(new Dictionary&lt;string, string&gt; { { "param2", "val2" }, { "param3", "val3" } }); 
		/// 
		///     // returns "/test?param1=val1&amp;param2=val2&amp;param3=val3"
		///     new Uri("/test?param1=val1").ExtendQuery(new Dictionary&lt;string, string&gt; { { "param2", "val2" }, { "param3", "val3" } }); 
		/// </code>
		/// </example>
		/// <param name="uri"></param>
		/// <param name="values"></param>
		/// <returns></returns>
		public static Uri ExtendQuery(this Uri uri, IDictionary<string, string> values) {
			var baseUrl = uri.ToString();
			var queryString = string.Empty;
			if (baseUrl.Contains("?")) {
				var urlSplit = baseUrl.Split('?');
				baseUrl = urlSplit[0];
				queryString = urlSplit.Length > 1 ? urlSplit[1] : string.Empty;
			}

			NameValueCollection queryCollection = HttpUtility.ParseQueryString(queryString);
			foreach (var kvp in values ?? new Dictionary<string, string>()) {
				queryCollection[kvp.Key] = kvp.Value;
			}
			var uriKind = uri.IsAbsoluteUri ? UriKind.Absolute : UriKind.Relative;
			return queryCollection.Count == 0
			  ? new Uri(baseUrl, uriKind)
			  : new Uri(string.Format("{0}?{1}", baseUrl, queryCollection), uriKind);
		}

		/// <summary>
		/// Normalize the URI by replacing http...80 and https...443 without the port.
		/// </summary>
		/// <param name="uri">Uri to normalize</param>
		/// <returns>Uri</returns>
		public static Uri Normalize(this Uri uri) {
			string normalizedUrl = string.Format(CultureInfo.InvariantCulture, "{0}://{1}", uri.Scheme, uri.Host);
			if (!((uri.Scheme == "http" && uri.Port == 80) || (uri.Scheme == "https" && uri.Port == 443))) {
				normalizedUrl += ":" + uri.Port;
			}
			normalizedUrl += uri.AbsolutePath;
			return new Uri(normalizedUrl);
		}
	}
}
