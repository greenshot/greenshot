/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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

using Dapplo.Config.Ini;
using GreenshotPlugin.Core;
using log4net;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace GreenshotPlugin.Extensions
{
	public static class UriExtensions
	{
		private static readonly ICoreConfiguration Config = IniConfig.Current.Get<ICoreConfiguration>();
		private static readonly ILog LOG = LogManager.GetLogger(typeof(UriExtensions));

		/// <summary>
		/// Create a IWebProxy Object which can be used to access the Internet
		/// This method will check the configuration if the proxy is allowed to be used.
		/// </summary>
		/// <param name="uri"></param>
		/// <returns>IWebProxy filled with all the proxy details or null if none is set/wanted</returns>
		public static IWebProxy CreateProxy(this Uri uri)
		{
			IWebProxy proxyToUse = null;
			proxyToUse = WebRequest.DefaultWebProxy;
			if (proxyToUse != null)
			{
				proxyToUse.Credentials = CredentialCache.DefaultCredentials;
			}
			return proxyToUse;
		}

		/// <summary>
		/// Create a NameValueCollection from the query part of the uri
		/// </summary>
		/// <param name="uri"></param>
		/// <returns>NameValueCollection</returns>
		public static NameValueCollection QueryToNameValues(this Uri uri)
		{
			if (!string.IsNullOrEmpty(uri.Query))
			{
				return HttpUtility.ParseQueryString(uri.Query);
			}
			return new NameValueCollection();
		}

		/// <summary>
		/// Create a IDictionary string,string from the query part of the uri
		/// </summary>
		/// <param name="uri"></param>
		/// <returns>IDictionary string,string</returns>
		public static IDictionary<string,string> QueryToDictionary(this Uri uri)
		{
			var returnValue = new Dictionary<string, string>();
			var nameValues = uri.QueryToNameValues();
			foreach(string key in nameValues.Keys)
			{
				if (returnValue.ContainsKey(key))
				{
					returnValue[key] = nameValues[key];
				} else
				{
					returnValue.Add(key, nameValues[key]);
				}
			}
			return returnValue;
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
		/// <returns>Uri</returns>
		public static Uri ExtendQuery<T>(this Uri uri, IDictionary<string, T> values)
		{
			var queryCollection = uri.QueryToNameValues();
			foreach (var kvp in values ?? new Dictionary<string, T>())
			{
				queryCollection[kvp.Key] = kvp.Value.ToString();
			}

			var uriBuilder = new UriBuilder(uri);
			if (queryCollection.Count > 0)
			{
				uriBuilder.Query = queryCollection.ToQueryString();
			}
			return uriBuilder.Uri;
		}

		/// <summary>
		/// Normalize the URI by replacing http...80 and https...443 without the port.
		/// </summary>
		/// <param name="uri">Uri to normalize</param>
		/// <returns>Uri</returns>
		public static Uri Normalize(this Uri uri)
		{
			string normalizedUrl = string.Format(CultureInfo.InvariantCulture, "{0}://{1}", uri.Scheme, uri.Host);
			if (!((uri.Scheme == "http" && uri.Port == 80) || (uri.Scheme == "https" && uri.Port == 443)))
			{
				normalizedUrl += ":" + uri.Port;
			}
			normalizedUrl += uri.AbsolutePath;
			return new Uri(normalizedUrl);
		}

		/// <summary>
		/// Get LastModified for a URI
		/// </summary>
		/// <param name="uri">Uri</param>
		/// <returns>DateTime</returns>
		public static async Task<DateTimeOffset> LastModifiedAsync(this Uri uri, CancellationToken token = default(CancellationToken))
		{
			try
			{
				var headers = await uri.HeadAsync(token).ConfigureAwait(false);
				if (headers.LastModified.HasValue)
				{
					return headers.LastModified.Value;
				}
			}
			catch
			{
				// Ignore
			}
			// Pretend it is old
			return DateTimeOffset.MinValue;
		}

		/// <summary>
		/// Head method
		/// </summary>
		/// <param name="uri"></param>
		/// <returns></returns>
		public static async Task<HttpContentHeaders> HeadAsync(this Uri uri, CancellationToken token = default(CancellationToken))
		{
			using (var client = uri.CreateHttpClient())
			using (var request = new HttpRequestMessage(HttpMethod.Head, uri))
			{
				var responseMessage = await client.SendAsync(request, token).ConfigureAwait(false);
				responseMessage.EnsureSuccessStatusCode();
				return responseMessage.Content.Headers;
			}
		}

		/// <summary>
		/// Post method
		/// </summary>
		/// <param name="uri"></param>
		/// <returns>HttpResponseMessage</returns>
		public static async Task<HttpResponseMessage> PostAsync(this Uri uri, CancellationToken token = default(CancellationToken))
		{
			using (var client = uri.CreateHttpClient())
			{
				return await client.PostAsync(uri, token);
			}
		}

		/// <summary>
		/// Simple extension to post Form-URLEncoded Content
		/// </summary>
		/// <param name="uri">Uri to post to</param>
		/// <param name="formContent">Dictionary with the values</param>
		/// <param name="token">Cancellationtoken</param>
		/// <returns>HttpResponseMessage</returns>
		public static async Task<HttpResponseMessage> PostFormUrlEncodedAsync(this Uri uri, IDictionary<string, string> formContent, CancellationToken token = default(CancellationToken))
		{
			var content = new FormUrlEncodedContent(formContent);
			using (var client = uri.CreateHttpClient())
			{
				return await client.PostAsync(uri, content);
			}
		}

		/// <summary>
		/// Download a uri response as string
		/// </summary>
		/// <param name="uri">An Uri to specify the download location</param>
		/// <returns>HttpResponseMessage</returns>
		public static async Task<HttpResponseMessage> GetAsync(this Uri uri, CancellationToken token = default(CancellationToken))
		{
			using (var client = uri.CreateHttpClient())
			{
				return await client.GetAsync(uri, token).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Create a HttpClient with default, configured, settings
		/// </summary>
		/// <param name="uri">Uri needed for the Proxy logic</param>
		/// <returns>HttpClient</returns>
		public static HttpClient CreateHttpClient(this Uri uri, bool useProxy = true, int connectionTimeout = 60)
		{
			var cookies = new CookieContainer();

			var handler = new HttpClientHandler
			{
				CookieContainer = cookies,
				UseCookies = true,
				UseDefaultCredentials = true,
				Credentials = CredentialCache.DefaultCredentials,
				AllowAutoRedirect = true,
				AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip,
				Proxy = useProxy ? uri.CreateProxy() : null,
				UseProxy = useProxy
			};

			var client = new HttpClient(handler);
			client.Timeout = TimeSpan.FromSeconds(connectionTimeout);
			return client;
		}

		/// <summary>
		/// Download a uri response as string
		/// </summary>
		/// <param name="uri">An Uri to specify the download location</param>
		/// <returns>string with the content</returns>
		public static async Task<string> GetAsStringAsync(this Uri uri, bool throwError = true, CancellationToken token = default(CancellationToken))
		{
			using (var client = uri.CreateHttpClient())
			using (var response = await client.GetAsync(uri, HttpCompletionOption.ResponseContentRead, token).ConfigureAwait(false))
			{
				return await response.GetAsStringAsync(token, throwError).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Simple append segment for a base Uri
		/// NOTE: Currently does NOT take the query parameters into account
		/// </summary>
		/// <param name="uri">Uri to extend</param>
		/// <param name="segments"></param>
		/// <returns>Uri</returns>
		public static Uri AppendSegments(this Uri uri, params object[] segments)
		{
			var uriBuilder = new UriBuilder(uri);

			var sb = new StringBuilder(uriBuilder.Path);
			foreach (var segment in segments)
			{
				if (sb.Length > 0)
				{
					sb.Append("/");

				}
				sb.Append(segment);
			}
			uriBuilder.Path = sb.ToString();
			return uriBuilder.Uri;
		}

		/// <summary>
		/// Download the uri to Bitmap
		/// </summary>
		/// <param name="url">Of an image</param>
		/// <returns>Bitmap</returns>
		public static async Task<Image> DownloadImageAsync(this Uri uri)
		{
			try
			{
				Exception initialException;
				string content;
				using (var response = await uri.GetAsync())
				using (var stream = await response.GetAsMemoryStreamAsync())
				{
					try
					{
						using (Image image = Image.FromStream(stream))
						{
							return ImageHelper.Clone(image, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
						}
					}
					catch (Exception ex)
					{
						// This might be okay, maybe it was just a search result
						initialException = ex;
					}
					stream.Seek(0, SeekOrigin.Begin);
					// If we arrive here, the image loading didn't work, try to see if the response has a http(s) URL to an image and just take this instead.
					using (var streamReader = new StreamReader(stream, Encoding.UTF8, true))
					{
						content = await streamReader.ReadLineAsync();
					}
				}
				Regex imageUrlRegex = new Regex(@"(http|https)://.*(\.png|\.gif|\.jpg|\.tiff|\.jpeg|\.bmp)");
				Match match = imageUrlRegex.Match(content);
				if (match.Success)
				{
					Uri contentUri = new Uri(match.Value);
					using (var response = await contentUri.GetAsync())
					using (var stream = await response.GetAsMemoryStreamAsync())
					{
						using (Image image = Image.FromStream(stream))
						{
							return ImageHelper.Clone(image, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
						}
					}
				}
				throw initialException;
			}
			catch (Exception e)
			{
				LOG.ErrorFormat("Problem downloading the image from: {0}", uri);
				LOG.Error(e);
			}
			return null;
		}

	}
}
