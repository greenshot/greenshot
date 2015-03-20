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

using Flurl;
using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace GreenshotJiraPlugin {
	/// <summary>
	/// Jira API, using the FlurlClient
	/// </summary>
	public class JiraAPI : IDisposable {
		private const string restPath = "/rest/api/2";
		private readonly FlurlClient client;
		private readonly Url apiUrl;

		public string BaseUrl {
			get;
			private set;
		}

		/// <summary>
		/// Create the JIRA API for the specified URL, with username & password
		/// </summary>
		/// <param name="url"></param>
		/// <param name="username"></param>
		/// <param name="password"></param>
		public JiraAPI(string url, string username, string password) {
			BaseUrl = url;
			apiUrl = url.AppendPathSegment(restPath);
			client = new FlurlClient();
			client.WithBasicAuth(username, password).WithHeader("X-Atlassian-Token", "nocheck");
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
				if (client != null) {
					client.Dispose();
				}
			}
			// free native resources if there are any.
		}
		#endregion

		/// <summary>
		/// Get issue information
		/// </summary>
		/// <param name="issue"></param>
		/// <returns>dynamic</returns>
		public async Task<dynamic> Issue(string issue, CancellationToken token = default(CancellationToken)) {
			return await client.WithUrl(Url.Combine(apiUrl, "issue", issue)).GetJsonAsync();
		}

		/// <summary>
		/// Get user information
		/// </summary>
		/// <param name="username"></param>
		/// <returns>dynamic with user information</returns>
		public async Task<dynamic> User(string username, CancellationToken token = default(CancellationToken)) {
			return await client.WithUrl(Url.Combine(apiUrl, "user").SetQueryParam("username", username)).GetJsonAsync();
		}

		/// <summary>
		/// Get currrent user information
		/// </summary>
		/// <returns>dynamic with user information</returns>
		public async Task<dynamic> Myself(CancellationToken token = default(CancellationToken)) {
			return await client.WithUrl(Url.Combine(apiUrl, "myself")).GetJsonAsync();
		}

		/// <summary>
		/// Get projects information
		/// </summary>
		/// <returns>IList of dynamic</returns>
		public async Task<IList<dynamic>> Projects(CancellationToken token = default(CancellationToken)) {
			return await client.WithUrl(Url.Combine(apiUrl, "project")).GetJsonListAsync();
		}

		/// <summary>
		/// Upload content to
		/// </summary>
		/// <param name="content"></param>
		/// <returns></returns>
		public async Task<HttpResponseMessage> Attach(string issueKey, HttpContent content, CancellationToken token = default(CancellationToken)) {
			var url = Url.Combine(apiUrl, "issue", issueKey, "attachments");
			return await client.HttpClient.PostAsync(url, content, token);
		}
	}
}
