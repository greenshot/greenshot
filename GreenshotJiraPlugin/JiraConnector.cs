#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.HttpExtensions;
using Dapplo.HttpExtensions.Extensions;
using Dapplo.Jira;
using Dapplo.Jira.Converters;
using Dapplo.Jira.Entities;
using GreenshotPlugin.Core;
using GreenshotPlugin.Core.Credentials;
using GreenshotPlugin.IniFile;
using Dapplo.Log;

#endregion

namespace GreenshotJiraPlugin
{
	/// <summary>
	///     This encapsulates the JiraClient to make it possible to change as less old Greenshot code as needed
	/// </summary>
	public class JiraConnector : IDisposable
	{
		// Used to remove the wsdl information from the old SOAP Uri
		public const string DefaultPostfix = "/rpc/soap/jirasoapservice-v2?wsdl";
		private static readonly LogSource Log = new LogSource();
		private static readonly JiraConfiguration JiraConfig = IniConfig.GetIniSection<JiraConfiguration>();
		private readonly int _timeout;
		private IssueTypeBitmapCache _issueTypeBitmapCache;
		private readonly IJiraClient _jiraClient;
		private DateTimeOffset _loggedInTime = DateTimeOffset.MinValue;

		/// <summary>
		///     Constructor
		/// </summary>
		public JiraConnector()
		{
			JiraConfig.Url = JiraConfig.Url.Replace(DefaultPostfix, "");
			_timeout = JiraConfig.Timeout;
			_jiraClient = JiraClient.Create(new Uri(JiraConfig.Url));
		}

		/// <summary>
		///     Access the jira monitor
		/// </summary>
		public JiraMonitor Monitor { get; private set; }

		public Bitmap FavIcon { get; private set; }

		/// <summary>
		///     Get the base uri
		/// </summary>
		public Uri JiraBaseUri => _jiraClient.JiraBaseUri;

		/// <summary>
		///     Is the user "logged in?
		/// </summary>
		public bool IsLoggedIn { get; private set; }

		/// <summary>
		///     Dispose, logout the users
		/// </summary>
		public void Dispose()
		{
			if (_jiraClient != null)
			{
				Task.Run(async () => await LogoutAsync()).Wait();
			}
			FavIcon?.Dispose();
		}

		public void UpdateSvgSize(int size)
		{
			_jiraClient.Behaviour.SetConfig(new SvgConfiguration { Width = size, Height = size });
		}

		/// <summary>
		///     Internal login which catches the exceptions
		/// </summary>
		/// <returns>true if login was done sucessfully</returns>
		private async Task<bool> DoLoginAsync(string user, string password, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
			{
				return false;
			}

			_issueTypeBitmapCache = new IssueTypeBitmapCache(_jiraClient);
			try
			{
				await _jiraClient.Session.StartAsync(user, password, cancellationToken);
				Monitor = new JiraMonitor();
				await Monitor.AddJiraInstanceAsync(_jiraClient, cancellationToken);

				var favIconUri = _jiraClient.JiraBaseUri.AppendSegments("favicon.ico");
				try
				{
					FavIcon = await _jiraClient.Server.GetUriContentAsync<Bitmap>(favIconUri, cancellationToken);
				}
				catch (Exception ex)
				{
					Log.Warn().WriteLine("Couldn't load favicon from {0}", favIconUri);
					Log.Warn().WriteLine(ex, "Exception details: ");
				}
			}
			catch (Exception)
			{
				return false;
			}
			return true;
		}

		/// <summary>
		///     Use the credentials dialog, this will show if there are not correct credentials.
		///     If there are credentials, call the real login.
		/// </summary>
		/// <returns>Task</returns>
		public async Task LoginAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			await LogoutAsync(cancellationToken);
			try
			{
				// Get the system name, so the user knows where to login to
				var credentialsDialog = new CredentialsDialog(JiraConfig.Url)
				{
					Name = null
				};
				while (credentialsDialog.Show(credentialsDialog.Name) == DialogResult.OK)
				{
					if (await DoLoginAsync(credentialsDialog.Name, credentialsDialog.Password, cancellationToken))
					{
						if (credentialsDialog.SaveChecked)
						{
							credentialsDialog.Confirm(true);
						}
						IsLoggedIn = true;
						_loggedInTime = DateTime.Now;
						return;
					}
					// Login failed, confirm this
					try
					{
						credentialsDialog.Confirm(false);
					}
					catch (ApplicationException e)
					{
						// exception handling ...
						Log.Error().WriteLine(e, "Problem using the credentials dialog");
					}
					// For every windows version after XP show an incorrect password baloon
					credentialsDialog.IncorrectPassword = true;
					// Make sure the dialog is display, the password was false!
					credentialsDialog.AlwaysDisplay = true;
				}
			}
			catch (ApplicationException e)
			{
				// exception handling ...
				Log.Error().WriteLine(e, "Problem using the credentials dialog");
			}
		}

		/// <summary>
		///     End the session, if there was one
		/// </summary>
		public async Task LogoutAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			if (_jiraClient != null && IsLoggedIn)
			{
				Monitor.Dispose();
				await _jiraClient.Session.EndAsync(cancellationToken);
				IsLoggedIn = false;
			}
		}

		/// <summary>
		///     check the login credentials, to prevent timeouts of the session, or makes a login
		///     Do not use ConfigureAwait to call this, as it will move await from the UI thread.
		/// </summary>
		/// <returns></returns>
		private async Task CheckCredentialsAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			if (IsLoggedIn)
			{
				if (_loggedInTime.AddMinutes(_timeout - 1).CompareTo(DateTime.Now) < 0)
				{
					await LogoutAsync(cancellationToken);
					await LoginAsync(cancellationToken);
				}
			}
			else
			{
				await LoginAsync(cancellationToken);
			}
		}

		/// <summary>
		///     Get the favourite filters
		/// </summary>
		/// <returns>List with filters</returns>
		public async Task<IList<Filter>> GetFavoriteFiltersAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			await CheckCredentialsAsync(cancellationToken);
			return await _jiraClient.Filter.GetFavoritesAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		///     Get the issue for a key
		/// </summary>
		/// <param name="issueKey">Jira issue key</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>Issue</returns>
		public async Task<Issue> GetIssueAsync(string issueKey, CancellationToken cancellationToken = default(CancellationToken))
		{
			await CheckCredentialsAsync(cancellationToken);
			try
			{
				return await _jiraClient.Issue.GetAsync(issueKey, cancellationToken).ConfigureAwait(false);
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		///     Attach the content to the jira
		/// </summary>
		/// <param name="issueKey"></param>
		/// <param name="content">IBinaryContainer</param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task AttachAsync(string issueKey, IBinaryContainer content, CancellationToken cancellationToken = default(CancellationToken))
		{
			await CheckCredentialsAsync(cancellationToken);
			using (var memoryStream = new MemoryStream())
			{
				content.WriteToStream(memoryStream);
				memoryStream.Seek(0, SeekOrigin.Begin);
				await _jiraClient.Attachment.AttachAsync(issueKey, memoryStream, content.Filename, content.ContentType, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <summary>
		///     Add a comment to the supplied issue
		/// </summary>
		/// <param name="issueKey">Jira issue key</param>
		/// <param name="body">text</param>
		/// <param name="visibility">the visibility role</param>
		/// <param name="cancellationToken">CancellationToken</param>
		public async Task AddCommentAsync(string issueKey, string body, string visibility = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			await CheckCredentialsAsync(cancellationToken);
			await _jiraClient.Issue.AddCommentAsync(issueKey, body, visibility, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		///     Get the search results for the specified filter
		/// </summary>
		/// <param name="filter">Filter</param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task<IList<Issue>> SearchAsync(Filter filter, CancellationToken cancellationToken = default(CancellationToken))
		{
			await CheckCredentialsAsync(cancellationToken);
			var searchResult =
				await _jiraClient.Issue.SearchAsync(filter.Jql,
                new Page { MaxResults = 20},
                new[] {"summary", "reporter", "assignee", "created", "issuetype"}, cancellationToken).ConfigureAwait(false);
			return searchResult.Issues;
		}

		/// <summary>
		///     Get the bitmap representing the issue type of an issue, from cache.
		/// </summary>
		/// <param name="issue">Issue</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>Bitmap</returns>
		public async Task<Bitmap> GetIssueTypeBitmapAsync(Issue issue, CancellationToken cancellationToken = default(CancellationToken))
		{
			return await _issueTypeBitmapCache.GetOrCreateAsync(issue.Fields.IssueType, cancellationToken).ConfigureAwait(false);
		}
	}
}