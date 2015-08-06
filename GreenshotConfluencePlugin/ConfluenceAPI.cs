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

namespace GreenshotConfluencePlugin {
	/// <summary>
	/// Confluence API, using the FlurlClient
	/// </summary>
	public class ConfluenceAPI : IDisposable {
		private const string restPath = "rest/api/";
		private readonly HttpClient _client;
		public string ConfluenceVersion {
			get;
			set;
		}

		public string ServerTitle {
			get;
			set;
		}

		public Uri ConfluenceBaseUri
		{
			get;
			set;
		}

		/// <summary>
		/// Create the ConfluenceAPI object, here the HttpClient is configured
		/// </summary>
		/// <param name="baseurl">Base URL</param>
		public ConfluenceAPI(Uri baseUri, bool useProxy = true, bool useDefaultCredentials = true, bool useCookieContainer = true) {
			ConfluenceBaseUri = baseUri;
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
			return string.Format("{0}{1}/{2}", ConfluenceBaseUri.AbsoluteUri, restPath, path1);
		}

		private string Format(string path1, string path2) {
			return string.Format("{0}{1}/{2}/{3}", ConfluenceBaseUri.AbsoluteUri, restPath, path1, path2);
		}

		private string Format(string path1, string path2, string path3) {
			return string.Format("{0}{1}/{2}/{3}/{4}", ConfluenceBaseUri.AbsoluteUri, restPath, path1, path2, path3);
		}


		/// <summary>
		/// Get the spaces
		/// </summary>
		/// <param name="key"></param>
		/// <returns>Spaces</returns>
		public async Task<dynamic> GetSpacesAsync(CancellationToken token = default(CancellationToken)) {
			var responseMessage = await _client.GetAsync(Format("space?expand=space"), token).ConfigureAwait(false);
			await responseMessage.HandleErrorAsync(token).ConfigureAwait(false);
			return DynamicJson.Parse(await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false));
		}

		/// <summary>
		/// Get the content by id
		/// </summary>
		/// <param name="id"></param>
		/// <returns>Content</returns>
		public async Task<dynamic> GetContentAsync(string id, CancellationToken token = default(CancellationToken)) {
			var responseMessage = await _client.GetAsync(Format("content", id), token).ConfigureAwait(false);
			await responseMessage.HandleErrorAsync(token).ConfigureAwait(false);
			return DynamicJson.Parse(await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false));
		}

		/// <summary>
		/// Get the children of the content
		/// </summary>
		/// <param name="id"></param>
		/// <returns>list of content</returns>
		public async Task<dynamic> GetChildrenAsync(string contentId, CancellationToken token = default(CancellationToken)) {
			var responseMessage = await _client.GetAsync(Format("content", contentId, "child?expand=page"), token).ConfigureAwait(false);
			await responseMessage.HandleErrorAsync(token).ConfigureAwait(false);
			return DynamicJson.Parse(await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false));
		}

		/// <summary>
		/// Search content
		/// </summary>
		/// <param name="cql">Confluence Query Language</param>
		/// <returns>list of content</returns>
		public async Task<dynamic> SearchAsync(string cql, CancellationToken token = default(CancellationToken)) {
			var responseMessage = await _client.GetAsync(Format(string.Format("content/search?cql={0}", Uri.EscapeUriString(cql))), token).ConfigureAwait(false);
			await responseMessage.HandleErrorAsync(token).ConfigureAwait(false);
			return DynamicJson.Parse(await responseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false));
		}

		/// <summary>
		/// Attacht the stream as filename to the confluence content with the supplied id
		/// </summary>
		/// <param name="id"></param>
		/// <param name="filename"></param>
		/// <param name="content">HttpContent like StreamContent or ByteArrayContent</param>
		public async Task<HttpResponseMessage> AttachToContentAsync(string id, HttpContent content, CancellationToken token = default(CancellationToken)) {
			var responseMessage = await _client.PostAsync(Format("content", id, "attachments"), content, token).ConfigureAwait(false);
			await responseMessage.HandleErrorAsync(token).ConfigureAwait(false);
			return responseMessage;
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

	}
}
