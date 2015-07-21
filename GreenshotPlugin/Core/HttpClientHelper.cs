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
				// BUG-1655: Fix that Greenshot always uses the default proxy even if the "use default proxy" checkbox is unset
				Proxy = Config.UseProxy ? CreateProxy(uri) : null,
				UseProxy = Config.UseProxy,
			};

			var client = new HttpClient(handler);
			client.Timeout = TimeSpan.FromSeconds(Config.WebRequestTimeout);
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
		/// Download a uri response as string
		/// </summary>
		/// <param name="uri">An Uri to specify the download location</param>
		/// <returns>HttpResponseMessage</returns>
		public static async Task<HttpResponseMessage> GetAsync(this Uri uri, bool throwError = true, CancellationToken token = default(CancellationToken)) {
			using (var client = uri.CreateHttpClient()) {
				try {
					return await client.GetAsync(uri, token);
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
			using (var response = await client.GetAsync(uri, HttpCompletionOption.ResponseContentRead, token)) {
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
				using (var contentStream = await response.Content.ReadAsStreamAsync()) {
					var memoryStream = new MemoryStream();
					await contentStream.CopyToAsync(memoryStream, 4096, token);
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
						errorContent = await responseMessage.Content.ReadAsStringAsync();
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
		/// Head method
		/// </summary>
		/// <param name="uri"></param>
		/// <returns></returns>
		public static async Task<HttpContentHeaders> HeadAsync(this Uri uri, CancellationToken token = default(CancellationToken)) {
			using (var client = uri.CreateHttpClient())
			using (var request = new HttpRequestMessage(HttpMethod.Head, uri)) {
				var response = await client.SendAsync(request, token);
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
				var headers = await uri.HeadAsync(token);
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
		///     Adds query string value to an existing url, both absolute and relative URI's are supported.
		/// </summary>
		/// <example>
		/// <code>
		///     // returns "www.domain.com/test?param1=val1&amp;param2=val2&amp;param3=val3"
		///     new Uri("www.domain.com/test?param1=val1").ExtendQuery(new { param2 = "val2", param3 = "val3" }); 
		/// 
		///     // returns "/test?param1=val1&amp;param2=val2&amp;param3=val3"
		///     new Uri("/test?param1=val1").ExtendQuery(new { param2 = "val2", param3 = "val3" }); 
		/// </code>
		/// </example>
		/// <param name="uri"></param>
		/// <param name="values"></param>
		/// <returns></returns>
		public static Uri ExtendQuery(this Uri uri, object values) {
			return ExtendQuery(uri, values.GetType().GetProperties().ToDictionary
			(
				propInfo => propInfo.Name,
				propInfo => { var value = propInfo.GetValue(values); return value != null ? value.ToString() : null; }
			));
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
