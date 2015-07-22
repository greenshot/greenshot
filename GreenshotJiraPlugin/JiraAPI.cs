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

		public Uri JiraBaseUri
		{
			get;
			set;
		}

		/// <summary>
		/// Create the JiraAPI object, here the HttpClient is configured
		/// </summary>
		/// <param name="baseurl">Base URL</param>
		public JiraAPI(Uri baseUri, bool useProxy = true, bool useDefaultCredentials = true, bool useCookieContainer = true) {
			JiraBaseUri = baseUri;
			_client = baseUri.CreateHttpClient();
			_client.AddDefaultRequestHeader("X-Atlassian-Token", "nocheck");
		}

		/// <summary>
		/// Set Basic Authentication for the current client
		/// </summary>
		/// <param name="user">username</param>
		/// <param name="password">password</param>
		public void SetBasicAuthentication(string user, string password) {
			_client.SetBasicAuthorization(user, password);
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
			var responseMessage = await _client.GetAsync(Format("issue", issue), token).ConfigureAwait(false);
			await responseMessage.HandleErrorAsync(token).ConfigureAwait(false);
			return DynamicJson.Parse(await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false));
		}

		/// <summary>
		/// Get server information
		/// See: https://docs.atlassian.com/jira/REST/latest/#d2e3828
		/// </summary>
		/// <returns>dynamic with ServerInfo</returns>
		public async Task<dynamic> ServerInfo(CancellationToken token = default(CancellationToken)) {
			var responseMessage = await _client.GetAsync(Format("serverInfo"), token).ConfigureAwait(false);
			await responseMessage.HandleErrorAsync(token).ConfigureAwait(false);
			return DynamicJson.Parse(await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false));
		}

		/// <summary>
		/// Get user information
		/// See: https://docs.atlassian.com/jira/REST/latest/#d2e5339
		/// </summary>
		/// <param name="username"></param>
		/// <returns>dynamic with user information</returns>
		public async Task<dynamic> User(string username, CancellationToken token = default(CancellationToken)) {
			var responseMessage = await _client.GetAsync(Format("user") + string.Format("?{0}={1}", "username", username), token).ConfigureAwait(false);
			await responseMessage.HandleErrorAsync(token).ConfigureAwait(false);
			return DynamicJson.Parse(await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false));
		}

		/// <summary>
		/// Get currrent user information
		/// See: https://docs.atlassian.com/jira/REST/latest/#d2e4253
		/// </summary>
		/// <returns>dynamic with user information</returns>
		public async Task<dynamic> Myself(CancellationToken token = default(CancellationToken)) {
			var responseMessage = await _client.GetAsync(Format("myself"), token).ConfigureAwait(false);
			await responseMessage.HandleErrorAsync(token).ConfigureAwait(false);
			return DynamicJson.Parse(await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false));
		}

		/// <summary>
		/// Get projects information
		/// See: https://docs.atlassian.com/jira/REST/latest/#d2e2779
		/// </summary>
		/// <returns>dynamic array</returns>
		public async Task<dynamic> Projects(CancellationToken token = default(CancellationToken)) {
			var responseMessage = await _client.GetAsync(Format("project"), token).ConfigureAwait(false);
			await responseMessage.HandleErrorAsync(token).ConfigureAwait(false);
			return DynamicJson.Parse(await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false));
		}

		/// <summary>
		/// Attach content to the specified issue
		/// See: https://docs.atlassian.com/jira/REST/latest/#d2e3035
		/// </summary>
		/// <param name="content">HttpContent, Make sure your HttpContent has a mime type...</param>
		/// <returns></returns>
		public async Task<HttpResponseMessage> Attach(string issueKey, HttpContent content, CancellationToken token = default(CancellationToken)) {
			var responseMessage = await _client.PostAsync(Format("issue", issueKey, "attachments"), content, token).ConfigureAwait(false);
			await responseMessage.HandleErrorAsync(token).ConfigureAwait(false);
			return responseMessage;
		}

		/// <summary>
		/// Get filter favorites
		/// See: https://docs.atlassian.com/jira/REST/latest/#d2e1388
		/// </summary>
		/// <returns>IList of dynamic</returns>
		public async Task<dynamic> Filters(CancellationToken token = default(CancellationToken)) {
			var responseMessage = await _client.GetAsync(Format("filter", "favourite"), token).ConfigureAwait(false);
			await responseMessage.HandleErrorAsync(token).ConfigureAwait(false);
			return DynamicJson.Parse(await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false));
		}

		/// <summary>
		/// Search for issues, with a JQL (e.g. from a filter)
		/// See: https://docs.atlassian.com/jira/REST/latest/#d2e2713
		/// </summary>
		/// <returns>dynamic</returns>
		public async Task<dynamic> Search(string jql, CancellationToken token = default(CancellationToken)) {
			var responseMessage = await _client.GetAsync(Format("search", "favourite") + string.Format("&jql=", WebUtility.UrlEncode(jql)), token).ConfigureAwait(false);
			await responseMessage.HandleErrorAsync(token).ConfigureAwait(false);
			return DynamicJson.Parse(await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false));
		}

		/// <summary>
		/// Retrieve the 48x48 Avatar as a Stream for the supplied user
		/// </summary>
		/// <param name="user">dyamic object from User or Myself method</param>
		/// <param name="token"></param>
		/// <returns>Stream</returns>
		public async Task<Stream> Avatar(dynamic user, CancellationToken token = default(CancellationToken)) {
			var avatarUrl = (string)GetProperty(user.avatarUrls, "48x48");
			var responseMessage = await _client.GetAsync(avatarUrl, token).ConfigureAwait(false);
			await responseMessage.HandleErrorAsync(token).ConfigureAwait(false);
			return await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
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
