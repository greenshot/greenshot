
/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.HttpExtensions;
using Dapplo.Jira;
using Dapplo.Jira.Entities;
using Greenshot.IniFile;
using GreenshotPlugin.Core;

namespace GreenshotJiraPlugin {
	/// <summary>
	/// This encapsulates the JiraApi to make it possible to change as less old Greenshot code as needed
	/// </summary>
	public class JiraConnector : IDisposable {
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(JiraConnector));
		private static readonly JiraConfiguration JiraConfig = IniConfig.GetIniSection<JiraConfiguration>();
		private static readonly CoreConfiguration CoreConfig = IniConfig.GetIniSection<CoreConfiguration>();
		// Used to remove the wsdl information from the old SOAP Uri
		public const string DefaultPostfix = "/rpc/soap/jirasoapservice-v2?wsdl";
		private DateTimeOffset _loggedInTime = DateTimeOffset.MinValue;
		private bool _loggedIn;
		private readonly int _timeout;
		private readonly object _lock = new object();
		private string _url;
		private JiraApi _jiraApi;
		private IssueTypeBitmapCache _issueTypeBitmapCache;
		private static readonly SvgBitmapHttpContentConverter SvgBitmapHttpContentConverterInstance = new SvgBitmapHttpContentConverter();

		static JiraConnector()
		{
			if (HttpExtensionsGlobals.HttpContentConverters.All(x => x.GetType() != typeof(SvgBitmapHttpContentConverter)))
			{
				HttpExtensionsGlobals.HttpContentConverters.Add(SvgBitmapHttpContentConverterInstance);
			}
			SvgBitmapHttpContentConverterInstance.Width = CoreConfig.IconSize.Width;
			SvgBitmapHttpContentConverterInstance.Height = CoreConfig.IconSize.Height;
			CoreConfig.PropertyChanged += (sender, args) =>
			{
				if (args.PropertyName == nameof(CoreConfig.IconSize))
				{
					SvgBitmapHttpContentConverterInstance.Width = CoreConfig.IconSize.Width;
					SvgBitmapHttpContentConverterInstance.Height = CoreConfig.IconSize.Height;
				}
			};

		}

		public void Dispose() {
			if (_jiraApi != null)
			{
				Task.Run(async () => await Logout()).Wait();
			}
		}

		public JiraConnector()
		{
			_url = JiraConfig.Url.Replace(DefaultPostfix, "");
			_timeout = JiraConfig.Timeout;
		}

		/// <summary>
		/// Internal login which catches the exceptions
		/// </summary>
		/// <returns>true if login was done sucessfully</returns>
		private async Task<bool> DoLogin(string user, string password)
		{
			lock (_lock)
			{
				if (_url.EndsWith("wsdl"))
				{
					_url = _url.Replace(DefaultPostfix, "");
				}
				if (_jiraApi == null)
				{
					// recreate the service with the new url
					_jiraApi = new JiraApi(new Uri(_url));
					_issueTypeBitmapCache = new IssueTypeBitmapCache(_jiraApi);
				}
			}

			LoginInfo loginInfo;
			try
			{
				loginInfo = await _jiraApi.StartSessionAsync(user, password);
				// Worked, store the url in the configuration
				JiraConfig.Url = _url;
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

		/// <summary>
		/// End the session, if there was one
		/// </summary>
		public async Task Logout() {
			if (_jiraApi != null && _loggedIn)
			{
				await _jiraApi.EndSessionAsync();
				_loggedIn = false;
			}
		}

		/// <summary>
		/// check the login credentials, to prevent timeouts of the session, or makes a login
		/// Do not use ConfigureAwait to call this, as it will move await from the UI thread.
		/// </summary>
		/// <returns></returns>
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

		/// <summary>
		/// Get the favourite filters 
		/// </summary>
		/// <returns>List with filters</returns>
		public async Task<IList<Filter>> GetFavoriteFiltersAsync()
		{
			await CheckCredentials();
			return await _jiraApi.GetFavoriteFiltersAsync().ConfigureAwait(false);
		}

		/// <summary>
		/// Get the issue for a key
		/// </summary>
		/// <param name="issueKey">Jira issue key</param>
		/// <returns>Issue</returns>
		public async Task<Issue> GetIssueAsync(string issueKey)
		{
			await CheckCredentials();
			try
			{
				return await _jiraApi.GetIssueAsync(issueKey).ConfigureAwait(false);
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Attach the content to the jira
		/// </summary>
		/// <param name="issueKey"></param>
		/// <param name="content">IBinaryContainer</param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task AttachAsync(string issueKey, IBinaryContainer content, CancellationToken cancellationToken = default(CancellationToken))
		{
			await CheckCredentials();
			using (var memoryStream = new MemoryStream())
			{
				content.WriteToStream(memoryStream);
				memoryStream.Seek(0, SeekOrigin.Begin);
				await _jiraApi.AttachAsync(issueKey, memoryStream, content.Filename, content.ContentType, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Add a comment to the supplied issue
		/// </summary>
		/// <param name="issueKey">Jira issue key</param>
		/// <param name="body">text</param>
		/// <param name="visibility">the visibility role</param>
		/// <param name="cancellationToken">CancellationToken</param>
		public async Task AddCommentAsync(string issueKey, string body, string visibility = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			await CheckCredentials();
			await _jiraApi.AddCommentAsync(issueKey, body, visibility, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Get the search results for the specified filter
		/// </summary>
		/// <param name="filter">Filter</param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task<IList<Issue>> SearchAsync(Filter filter, CancellationToken cancellationToken = default(CancellationToken))
		{
			await CheckCredentials();
			var searchResult = await _jiraApi.SearchAsync(filter.Jql, 20, new[] { "summary", "reporter", "assignee", "created", "issuetype" }, cancellationToken).ConfigureAwait(false);
			return searchResult.Issues;
		}

		/// <summary>
		/// Get the bitmap representing the issue type of an issue, from cache.
		/// </summary>
		/// <param name="issue">Issue</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>Bitmap</returns>
		public async Task<Bitmap> GetIssueTypeBitmapAsync(Issue issue, CancellationToken cancellationToken = default(CancellationToken))
		{
			return await _issueTypeBitmapCache.GetOrCreateAsync(issue.Fields.IssueType, cancellationToken).ConfigureAwait(false);
		}

		public Uri JiraBaseUri => _jiraApi.JiraBaseUri;

		public bool IsLoggedIn => _loggedIn;
	}
}