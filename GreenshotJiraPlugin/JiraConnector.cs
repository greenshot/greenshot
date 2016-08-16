
/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Jira;
using Dapplo.Jira.Entities;
using Greenshot.IniFile;
using GreenshotPlugin.Core;

namespace GreenshotJiraPlugin {
	public class JiraConnector : IDisposable {
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(JiraConnector));
		private static readonly JiraConfiguration Config = IniConfig.GetIniSection<JiraConfiguration>();
		public const string DefaultPostfix = "/rpc/soap/jirasoapservice-v2?wsdl";
		private DateTime _loggedInTime = DateTime.Now;
		private bool _loggedIn;
		private readonly int _timeout;
		private string _url;
		private JiraApi _jiraApi;

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected void Dispose(bool disposing) {
			if (_jiraApi != null)
			{
				Task.Run(async () => await Logout()).Wait();
			}
		}

		public JiraConnector() {
			_url = Config.Url.Replace(DefaultPostfix, "");
			_timeout = Config.Timeout;
			_jiraApi = new JiraApi(new Uri(_url));
		}

		~JiraConnector() {
			Dispose(false);
		}

		/// <summary>
		/// Internal login which catches the exceptions
		/// </summary>
		/// <returns>true if login was done sucessfully</returns>
		private async Task<bool> DoLogin(string user, string password)
		{
			if (_url.EndsWith("wsdl"))
			{
				_url = _url.Replace(DefaultPostfix, "");
				// recreate the service with the new url
				_jiraApi = new JiraApi(new Uri(_url));
			}

			LoginInfo loginInfo;
			try
			{
				loginInfo = await _jiraApi.StartSessionAsync(user, password);
				// Worked, store the url in the configuration
				Config.Url = _url;
				IniConfig.Save();
			}
			catch (Exception)
			{
				return false;
			}
			return loginInfo != null;
		}
		
		public async Task Login() {
			await Logout();
			try {
				// Get the system name, so the user knows where to login to
				string systemName = _url.Replace(DefaultPostfix,"");
				var credentialsDialog = new CredentialsDialog(systemName)
				{
					Name = null
				};
				while (credentialsDialog.Show(credentialsDialog.Name) == DialogResult.OK) {
					if (await DoLogin(credentialsDialog.Name, credentialsDialog.Password)) {
						if (credentialsDialog.SaveChecked) {
							credentialsDialog.Confirm(true);
						}
						_loggedIn = true;
						_loggedInTime = DateTime.Now;
						return;
					}
					try {
						credentialsDialog.Confirm(false);
					} catch (ApplicationException e) {
						// exception handling ...
						Log.Error("Problem using the credentials dialog", e);
					}
					// For every windows version after XP show an incorrect password baloon
					credentialsDialog.IncorrectPassword = true;
					// Make sure the dialog is display, the password was false!
					credentialsDialog.AlwaysDisplay = true;
				}
			} catch (ApplicationException e) {
				// exception handling ...
				Log.Error("Problem using the credentials dialog", e);
			}

		}

		public async Task Logout() {
			if (_jiraApi != null)
			{
				await _jiraApi.EndSessionAsync();
				_loggedIn = false;
			}
		}

		private async Task CheckCredentials() {
			if (_loggedIn) {
				if (_loggedInTime.AddMinutes(_timeout-1).CompareTo(DateTime.Now) < 0) {
					await Logout();
					await Login();
				}
			} else {
				await Login();
			}
		}

		public async Task<IList<Filter>> GetFavoriteFiltersAsync()
		{
			await CheckCredentials();
			return await _jiraApi.GetFavoriteFiltersAsync().ConfigureAwait(false);
		}

		public async Task<Issue> GetIssueAsync(string issueKey)
		{
			await CheckCredentials();
			return await _jiraApi.GetIssueAsync(issueKey).ConfigureAwait(false);
		}
		public async Task<Attachment> AttachAsync<TContent>(string issueKey, TContent content, string filename, string contentType = null, CancellationToken cancellationToken = default(CancellationToken)) where TContent : class
		{
			await CheckCredentials().ConfigureAwait(false);
			return await _jiraApi.AttachAsync(issueKey, content, filename, contentType, cancellationToken).ConfigureAwait(false);
		}

		public async Task AddCommentAsync(string issueKey, string body, string visibility = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			await CheckCredentials();
			await _jiraApi.AddCommentAsync(issueKey, body, visibility, cancellationToken).ConfigureAwait(false);
		}
		public async Task<SearchResult> SearchAsync(string jql, int maxResults = 20, IList<string> fields = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			await CheckCredentials();
			return await _jiraApi.SearchAsync(jql, maxResults, fields, cancellationToken).ConfigureAwait(false);
		}

		public Uri JiraBaseUri => _jiraApi.JiraBaseUri;

		public bool IsLoggedIn => _loggedIn;
	}
}