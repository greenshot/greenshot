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

using GreenshotPlugin.Core;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GreenshotJiraPlugin {
	/// <summary>
	/// Jira API, using the FlurlClient
	/// </summary>
	public class JiraAPI : IDisposable {
		private const string restPath = "/rest/api/2";
		private readonly HttpClient _client;
		public string JiraVersion {
			get;
			set;
		}

		public string ServerTitle {
			get;
			set;
		}

		/// <summary>
		/// Create the JiraAPI object, here the HttpClient is configured
		/// </summary>
		/// <param name="baseurl">Base URL</param>
		public JiraAPI(string baseurl, bool useProxy = true, bool useDefaultCredentials = true, bool useCookieContainer = true) {
			// Initialize the HTTP-Client
			IWebProxy proxy;
			try {
				proxy = WebRequest.GetSystemWebProxy();
			} catch {
				proxy = WebRequest.DefaultWebProxy;
			}
			if (useProxy && proxy != null) {
				proxy.Credentials = CredentialCache.DefaultNetworkCredentials;
			} else {
				proxy = null;
			}

			var handler = new HttpClientHandler {
				Proxy = proxy,
				UseProxy = useProxy,
				CookieContainer = useCookieContainer ? new CookieContainer() : null,
				UseDefaultCredentials = useDefaultCredentials,
				AllowAutoRedirect = false
			};

			_client = new HttpClient(handler) {
				BaseAddress = new Uri(baseurl)
			};
			_client.DefaultRequestHeaders.TryAddWithoutValidation("X-Atlassian-Token", "nocheck");
		}

		/// <summary>
		/// Set Basic Authentication for the current client
		/// </summary>
		/// <param name="user">username</param>
		/// <param name="password">password</param>
		public void SetBasicAuthentication(string user, string password) {
			string credentials = Convert.ToBase64String(UTF8Encoding.UTF8.GetBytes(string.Format("{0}:{1}", user, password)));
			_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
		}

		/// <summary>
		/// Add a default request header
		/// </summary>
		public void AddDefaultRequestHeader(string key, string value) {
			_client.DefaultRequestHeaders.TryAddWithoutValidation(key, value);
		}

		/// <summary>
		/// Error handling
		/// </summary>
		/// <param name="responseMessage"></param>
		protected async virtual Task HandleErrorAsync(HttpMethod method, HttpResponseMessage responseMessage, CancellationToken token = default(CancellationToken)) {
			// For all other cases we let the default exception be generated
			Exception throwException = null;
			string errorContent = null;
			try {
				if (!responseMessage.IsSuccessStatusCode) {
					try {
						// try reading the content, so this is not lost
						errorContent = await responseMessage.Content.ReadAsStringAsync();
					} catch {
						// Ignore
					}
					responseMessage.EnsureSuccessStatusCode();
				}
			} catch (Exception ex) {
				throwException = ex;
				throwException.Data.Add("uri", responseMessage.RequestMessage.RequestUri);
			}
			if (throwException != null) {
				if (errorContent != null) {
					throwException.Data.Add("response", errorContent);
				}
				throw throwException;
			}
			return;
		}


		#region Dispose
		/// <summary>
		/// Dispose
		/// </summary>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose all managed resources
		/// </summary>
		/// <param name="disposing">when true is passed all managed resources are disposed.</param>
		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				// free managed resources
				if (_client != null) {
					_client.Dispose();
				}
			}
			// free native resources if there are any.
		}
		#endregion

		private string Format(string path1) {
			return string.Format("{0}/{1}", restPath, path1);
		}

		private string Format(string path1, string path2) {
			return string.Format("{0}/{1}/{2}", restPath, path1, path2);
		}
		private string Format(string path1, string path2, string path3) {
			return string.Format("{0}/{1}/{2}/{3}", restPath, path1, path2, path3);
		}

		/// <summary>
		/// Get issue information
		/// See: https://docs.atlassian.com/jira/REST/latest/#d2e4539
		/// </summary>
		/// <param name="issue"></param>
		/// <returns>dynamic</returns>
		public async Task<dynamic> Issue(string issue, CancellationToken token = default(CancellationToken)) {
			var responseMessage = await _client.GetAsync(Format("issue", issue), token);
			await HandleErrorAsync(HttpMethod.Get, responseMessage, token);
			return DynamicJson.Parse(await responseMessage.Content.ReadAsStreamAsync());
		}

		/// <summary>
		/// Get server information
		/// See: https://docs.atlassian.com/jira/REST/latest/#d2e3828
		/// </summary>
		/// <returns>dynamic with ServerInfo</returns>
		public async Task<dynamic> ServerInfo(CancellationToken token = default(CancellationToken)) {
			var responseMessage = await _client.GetAsync(Format("serverInfo"), token);
			await HandleErrorAsync(HttpMethod.Get, responseMessage, token);
			return DynamicJson.Parse(await responseMessage.Content.ReadAsStreamAsync());
		}

		/// <summary>
		/// Get user information
		/// See: https://docs.atlassian.com/jira/REST/latest/#d2e5339
		/// </summary>
		/// <param name="username"></param>
		/// <returns>dynamic with user information</returns>
		public async Task<dynamic> User(string username, CancellationToken token = default(CancellationToken)) {
			var responseMessage = await _client.GetAsync(Format("user") + string.Format("?{0}={1}", "username", username), token);
			await HandleErrorAsync(HttpMethod.Get, responseMessage, token);
			return DynamicJson.Parse(await responseMessage.Content.ReadAsStreamAsync());
		}

		/// <summary>
		/// Get currrent user information
		/// See: https://docs.atlassian.com/jira/REST/latest/#d2e4253
		/// </summary>
		/// <returns>dynamic with user information</returns>
		public async Task<dynamic> Myself(CancellationToken token = default(CancellationToken)) {
			var responseMessage = await _client.GetAsync(Format("myself"), token);
			await HandleErrorAsync(HttpMethod.Get, responseMessage, token);
			return DynamicJson.Parse(await responseMessage.Content.ReadAsStreamAsync());
		}

		/// <summary>
		/// Get projects information
		/// See: https://docs.atlassian.com/jira/REST/latest/#d2e2779
		/// </summary>
		/// <returns>dynamic array</returns>
		public async Task<dynamic> Projects(CancellationToken token = default(CancellationToken)) {
			var responseMessage = await _client.GetAsync(Format("project"), token);
			await HandleErrorAsync(HttpMethod.Get, responseMessage, token);
			return DynamicJson.Parse(await responseMessage.Content.ReadAsStreamAsync());
		}

		/// <summary>
		/// Attach content to the specified issue
		/// See: https://docs.atlassian.com/jira/REST/latest/#d2e3035
		/// </summary>
		/// <param name="content">HttpContent, Make sure your HttpContent has a mime type...</param>
		/// <returns></returns>
		public async Task<HttpResponseMessage> Attach(string issueKey, HttpContent content, CancellationToken token = default(CancellationToken)) {
			var responseMessage = await _client.PostAsync(Format("issue", issueKey, "attachments"), content, token);
			await HandleErrorAsync(HttpMethod.Get, responseMessage, token);
			return responseMessage;
		}

		/// <summary>
		/// Get filter favorites
		/// See: https://docs.atlassian.com/jira/REST/latest/#d2e1388
		/// </summary>
		/// <returns>IList of dynamic</returns>
		public async Task<dynamic> Filters(CancellationToken token = default(CancellationToken)) {
			var responseMessage = await _client.GetAsync(Format("filter", "favourite"), token);
			await HandleErrorAsync(HttpMethod.Get, responseMessage, token);
			return DynamicJson.Parse(await responseMessage.Content.ReadAsStreamAsync());
		}

		/// <summary>
		/// Search for issues, with a JQL (e.g. from a filter)
		/// See: https://docs.atlassian.com/jira/REST/latest/#d2e2713
		/// </summary>
		/// <returns>dynamic</returns>
		public async Task<dynamic> Search(string jql, CancellationToken token = default(CancellationToken)) {
			var responseMessage = await _client.GetAsync(Format("search", "favourite") + string.Format("&jql=", WebUtility.UrlEncode(jql)), token);
			await HandleErrorAsync(HttpMethod.Get, responseMessage, token);
			return DynamicJson.Parse(await responseMessage.Content.ReadAsStreamAsync());
		}

		/// <summary>
		/// Retrieve the 48x48 Avatar as a Stream for the supplied user
		/// </summary>
		/// <param name="user">dyamic object from User or Myself method</param>
		/// <param name="token"></param>
		/// <returns>Stream</returns>
		public async Task<Stream> Avatar(dynamic user, CancellationToken token = default(CancellationToken)) {
			var avatarUrl = (string)GetProperty(user.avatarUrls, "48x48");
			var responseMessage = await _client.GetAsync(avatarUrl, token);
			await HandleErrorAsync(HttpMethod.Get, responseMessage, token);
			return await responseMessage.Content.ReadAsStreamAsync();
		}

		/// <summary>
		/// Private method to get a property from a dynamic, this is used if the property name is not usable in normal compiler code.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		private static object GetProperty(object target, string name) {
			var site = System.Runtime.CompilerServices.CallSite<Func<System.Runtime.CompilerServices.CallSite, object, object>>.Create(Microsoft.CSharp.RuntimeBinder.Binder.GetMember(0, name, target.GetType(), new[] { Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo.Create(0, null) }));
			return site.Target(site, target);
		}
	}
}
