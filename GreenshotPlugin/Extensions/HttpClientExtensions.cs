/*
 * Greenshot - a free and open source screenshot tool
 * Copyright(C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.If not, see<http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GreenshotPlugin.Extensions
{
	/// <summary>
	/// Extensions for the HttpClient
	/// </summary>
	public static class HttpClientExtensions
	{
		/// <summary>
		/// Set Basic Authentication for the current client
		/// </summary>
		/// <param name="user">username</param>
		/// <param name="password">password</param>
		/// <returns>HttpClient for fluent usage</returns>
		public static HttpClient SetBasicAuthorization(this HttpClient client, string user, string password)
		{
			string credentials = Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes(string.Format("{0}:{1}", user, password)));
			return client.SetAuthorization("Basic", credentials);
		}

		/// <summary>
		/// Set Bearer "Authentication" for the current client
		/// </summary>
		/// <param name="user">username</param>
		/// <param name="password">password</param>
		/// <returns>HttpClient for fluent usage</returns>
		public static HttpClient SetBearer(this HttpClient client, string bearer)
		{
			return client.SetAuthorization("Bearer", bearer);
		}

		/// <summary>
		/// Set Authorization for the current client
		/// </summary>
		/// <param name="scheme">scheme</param>
		/// <param name="authorization">value</param>
		/// <returns>HttpClient for fluent usage</returns>
		public static HttpClient SetAuthorization(this HttpClient client, string scheme, string authorization)
		{
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme, authorization);
			return client;
		}

		/// <summary>
		/// Add default request header without validation
		/// </summary>
		/// <param name="scheme">name</param>
		/// <param name="authorization">value</param>
		public static HttpClient AddDefaultRequestHeader(this HttpClient client, string name, string value)
		{
			client.DefaultRequestHeaders.TryAddWithoutValidation(name, value);
			return client;
		}

		/// <summary>
		/// Post method
		/// </summary>
		/// <param name="client">HttpClient</param>
		/// <param name="uri"></param>
		/// <returns>HttpResponseMessage</returns>
		public static async Task<HttpResponseMessage> PostAsync(this HttpClient client, Uri uri, CancellationToken token = default(CancellationToken))
		{
			using (var request = new HttpRequestMessage(HttpMethod.Post, uri))
			{
				var responseMessage = await client.SendAsync(request, token).ConfigureAwait(false);
				responseMessage.EnsureSuccessStatusCode();
				return responseMessage;
			}
		}

		/// <summary>
		/// ParseQueryString without the requirement for System.Web
		/// </summary>
		/// <param name="queryString"></param>
		/// <returns>IDictionary string, string</returns>
		public static IDictionary<string, string> ParseQueryString(string queryString)
		{
			var parameters = new SortedDictionary<string, string>();
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
	}
}
