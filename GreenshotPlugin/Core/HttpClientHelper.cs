using Greenshot.IniFile;
using log4net;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

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
		public static async Task<HttpResponseMessage> GetAsync(this Uri uri, bool throwError = true) {
			using (var client = uri.CreateHttpClient()) {
				try {
					return await client.GetAsync(uri);
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
		public static async Task<string> GetAsStringAsync(this Uri uri, bool throwError = true) {
			using (var client = uri.CreateHttpClient())
			using (var response = await client.GetAsync(uri)) {
				return await response.GetAsStringAsync(throwError).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Get the content as a MemoryStream
		/// </summary>
		/// <param name="response"></param>
		/// <param name="throwErrorOnNonSuccess"></param>
		/// <returns>MemoryStream</returns>
		public static async Task<MemoryStream> GetAsMemoryStreamAsync(this HttpResponseMessage response, bool throwErrorOnNonSuccess = true) {
			if (response.IsSuccessStatusCode) {
				using (var contentStream = await response.Content.ReadAsStreamAsync()) {
					var memoryStream = new MemoryStream();
					await contentStream.CopyToAsync(memoryStream);
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
		/// Set Basic Authentication for the current client
		/// </summary>
		/// <param name="user">username</param>
		/// <param name="password">password</param>
		public static HttpClient SetBasicAuthentication(this HttpClient client, string user, string password) {
			string credentials = Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes(string.Format("{0}:{1}", user, password)));
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
			return client;
		}

		/// <summary>
		/// Set Basic Authentication for the current client
		/// </summary>
		/// <param name="user">username</param>
		/// <param name="password">password</param>
		public static HttpClient SetBearer(this HttpClient client, string bearer) {
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
			return client;
		}

		/// <summary>
		/// Head method
		/// </summary>
		/// <param name="uri"></param>
		/// <returns></returns>
		public static async Task<HttpContentHeaders> Head(this Uri uri) {
			using (var client = uri.CreateHttpClient())
			using (var request = new HttpRequestMessage(HttpMethod.Head, uri)) {
				var response = await client.SendAsync(request);
				return response.Content.Headers;
			}
		}

		/// <summary>
		/// Get LastModified for a URI
		/// </summary>
		/// <param name="uri">Uri</param>
		/// <returns>DateTime</returns>
		public static async Task<DateTimeOffset> LastModifiedAsync(this Uri uri) {
			try {
				var headers = await uri.Head();
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
	}
}
