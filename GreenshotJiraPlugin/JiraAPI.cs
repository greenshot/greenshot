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
using Dapplo.HttpExtensions;
using GreenshotPlugin.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GreenshotJiraPlugin
{
	/// <summary>
	/// Jira API, using Dapplo.HttpExtensions
	/// </summary>
	public class JiraApi : IDisposable {
		private static readonly INetworkConfiguration NetworkConfig = IniConfig.Current.Get<INetworkConfiguration>();
		private const string RestPath = "rest/api/2";
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
		/// <param name="baseUri">Base URL</param>
		public JiraApi(Uri baseUri) {
			JiraBaseUri = baseUri;
			_client = HttpClientFactory.CreateHttpClient(NetworkConfig);
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

		/// <summary>
		/// Get issue information
		/// See: https://docs.atlassian.com/jira/REST/latest/#d2e4539
		/// </summary>
		/// <param name="issue"></param>
		/// <param name="token"></param>
		/// <returns>dynamic</returns>
		public async Task<dynamic> Issue(string issue, CancellationToken token = default(CancellationToken)) {
			var issueUri = JiraBaseUri.AppendSegments(RestPath, "issue", issue);
			return await _client.GetAsJsonAsync(issueUri, true, token).ConfigureAwait(false);
		}

		/// <summary>
		/// Get server information
		/// See: https://docs.atlassian.com/jira/REST/latest/#d2e3828
		/// </summary>
		/// <returns>dynamic with ServerInfo</returns>
		public async Task<dynamic> ServerInfo(CancellationToken token = default(CancellationToken)) {
			var serverInfoUri = JiraBaseUri.AppendSegments(RestPath, "serverInfo");
			return await _client.GetAsJsonAsync(serverInfoUri, true, token).ConfigureAwait(false);
		}

		/// <summary>
		/// Get user information
		/// See: https://docs.atlassian.com/jira/REST/latest/#d2e5339
		/// </summary>
		/// <param name="username"></param>
		/// <param name="token"></param>
		/// <returns>dynamic with user information</returns>
		public async Task<dynamic> User(string username, CancellationToken token = default(CancellationToken)) {
			var userUri = JiraBaseUri.AppendSegments(RestPath, "user").ExtendQuery(new Dictionary<string, object> { { "username", username }});
			return await _client.GetAsJsonAsync(userUri, true, token).ConfigureAwait(false);
		}

		/// <summary>
		/// Get currrent user information
		/// See: https://docs.atlassian.com/jira/REST/latest/#d2e4253
		/// </summary>
		/// <returns>dynamic with user information</returns>
		public async Task<dynamic> Myself(CancellationToken token = default(CancellationToken)) {
			var myselfUri = JiraBaseUri.AppendSegments(RestPath, "myself");
			return await _client.GetAsJsonAsync(myselfUri, true, token).ConfigureAwait(false);
		}

		/// <summary>
		/// Get projects information
		/// See: https://docs.atlassian.com/jira/REST/latest/#d2e2779
		/// </summary>
		/// <returns>dynamic array</returns>
		public async Task<dynamic> Projects(CancellationToken token = default(CancellationToken)) {
			var projectUri = JiraBaseUri.AppendSegments(RestPath, "project");
			return await _client.GetAsJsonAsync(projectUri, true, token).ConfigureAwait(false);
		}

		/// <summary>
		/// Attach content to the specified issue
		/// See: https://docs.atlassian.com/jira/REST/latest/#d2e3035
		/// </summary>
		/// <param name="issueKey"></param>
		/// <param name="content">HttpContent, Make sure your HttpContent has a mime type...</param>
		/// <param name="token"></param>
		/// <returns>HttpResponseMessage</returns>
		public async Task<HttpResponseMessage> Attach(string issueKey, HttpContent content, CancellationToken token = default(CancellationToken)) {
			var attachUri = JiraBaseUri.AppendSegments(RestPath, "issue", issueKey, "attachments");
            using (var responseMessage = await _client.PostAsync(attachUri, content, token).ConfigureAwait(false)) {
				await responseMessage.HandleErrorAsync(token: token).ConfigureAwait(false);
				return responseMessage;
			}
		}

		/// <summary>
		/// Get filter favorites
		/// See: https://docs.atlassian.com/jira/REST/latest/#d2e1388
		/// </summary>
		/// <returns>dynamic (JsonArray)</returns>
		public async Task<dynamic> Filters(CancellationToken token = default(CancellationToken)) {
			var filterFavouriteUri = JiraBaseUri.AppendSegments(RestPath, "filter", "favourite");
			return await _client.GetAsJsonAsync(filterFavouriteUri, true, token).ConfigureAwait(false);
		}

		/// <summary>
		/// Search for issues, with a JQL (e.g. from a filter)
		/// See: https://docs.atlassian.com/jira/REST/latest/#d2e2713
		/// </summary>
		/// <returns>dynamic</returns>
		public async Task<dynamic> Search(string jql, CancellationToken token = default(CancellationToken)) {
			var searchUri = JiraBaseUri.AppendSegments(RestPath, "search", "favourite").ExtendQuery(new Dictionary<string, object> { { "jql", WebUtility.UrlEncode(jql) } });
			return await _client.GetAsJsonAsync(searchUri, true, token).ConfigureAwait(false);
		}

		/// <summary>
		/// Retrieve the 48x48 Avatar as a Stream for the supplied user
		/// </summary>
		/// <param name="user">dyamic object from User or Myself method</param>
		/// <param name="token"></param>
		/// <returns>Stream</returns>
		public async Task<Stream> Avatar(dynamic user, CancellationToken token = default(CancellationToken)) {
			var avatarUrl = new Uri(user.avatarUrls["48x48"]);
			return await avatarUrl.GetAsMemoryStreamAsync(true, token);
		}
	}
}
