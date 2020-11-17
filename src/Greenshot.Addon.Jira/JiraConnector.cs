// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Dapplo.HttpExtensions;
using Dapplo.HttpExtensions.Extensions;
using Dapplo.Jira;
using Dapplo.Jira.Entities;
using Dapplo.Jira.SvgWinForms.Converters;
using Dapplo.Log;
using DynamicData;
using Greenshot.Addon.Jira.Configuration;
using Greenshot.Addons.Core;
using Greenshot.Addons.Core.Credentials;
using Greenshot.Addons.Extensions;
using Greenshot.Addons.Interfaces;
using Greenshot.Gfx;

namespace Greenshot.Addon.Jira
{
	/// <summary>
	///     This encapsulates the JiraClient to make it possible to change as less old Greenshot code as needed
	/// </summary>
	public class JiraConnector : IDisposable
	{
	    private static readonly LogSource Log = new LogSource();
	    private readonly IJiraConfiguration _jiraConfiguration;
	    private readonly JiraMonitor _jiraMonitor;
	    private readonly ICoreConfiguration _coreConfiguration;

	    // Used to remove the wsdl information from the old SOAP Uri
		public const string DefaultPostfix = "/rpc/soap/jirasoapservice-v2?wsdl";
		private readonly SourceCache<IssueTypeIcon, IssueType> _issueTypeBitmapCache = new SourceCache<IssueTypeIcon, IssueType>(icon => icon.Key);
		private readonly IJiraClient _jiraClient;
		private DateTimeOffset _loggedInTime = DateTimeOffset.MinValue;

		public JiraConnector(
		    IJiraConfiguration jiraConfiguration,
		    JiraMonitor jiraMonitor,
		    ICoreConfiguration coreConfiguration,
		    IHttpConfiguration httpConfiguration)
		{
		    jiraConfiguration.Url = jiraConfiguration.Url.Replace(DefaultPostfix, "");
		    _jiraConfiguration = jiraConfiguration;
		    _jiraMonitor = jiraMonitor;
		    _coreConfiguration = coreConfiguration;
		    _jiraClient = JiraClient.Create(new Uri(jiraConfiguration.Url), httpConfiguration);
		}

		public IBitmapWithNativeSupport FavIcon { get; private set; }

	    public IEnumerable<JiraDetails> RecentJiras => _jiraMonitor.RecentJiras;
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
				Task.Run(async () =>
                {
                    _issueTypeBitmapCache.Dispose();
                }).Wait();
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
		/// <returns>true if login was done successfully</returns>
		private async Task<bool> DoLoginAsync(string user, string password, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(password))
			{
				return false;
			}

			try
			{
                _jiraClient.SetBasicAuthentication(user, password);
				await _jiraMonitor.AddJiraInstanceAsync(_jiraClient, cancellationToken).ConfigureAwait(true);

				var favIconUri = _jiraClient.JiraBaseUri.AppendSegments("favicon.ico");
				try
				{
					FavIcon = BitmapWrapper.FromBitmap(await _jiraClient.Server.GetUriContentAsync<Bitmap>(favIconUri, cancellationToken).ConfigureAwait(true));
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
		public async Task LoginAsync(CancellationToken cancellationToken = default)
		{
			try
			{
				// Get the system name, so the user knows where to login to
				var credentialsDialog = new CredentialsDialog(_jiraConfiguration.Url)
				{
					Name = null
				};
				while (credentialsDialog.Show(null, credentialsDialog.Name) == DialogResult.OK)
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
		///     Get the favourite filters
		/// </summary>
		/// <returns>List with filters</returns>
		public async Task<IList<Filter>> GetFavoriteFiltersAsync(CancellationToken cancellationToken = default)
		{
			return await _jiraClient.Filter.GetFavoritesAsync(cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		///     Get the issue for a key
		/// </summary>
		/// <param name="issueKey">Jira issue key</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>Issue</returns>
		public async Task<Issue> GetIssueAsync(string issueKey, CancellationToken cancellationToken = default)
		{
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
        /// <param name="issueKey">string</param>
        /// <param name="surface">ISurface</param>
        /// <param name="filename">string</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Task</returns>
        public async Task AttachAsync(string issueKey, ISurface surface, string filename = null, CancellationToken cancellationToken = default)
		{
            using var memoryStream = new MemoryStream();
            surface.WriteToStream(memoryStream, _coreConfiguration, _jiraConfiguration);
            memoryStream.Seek(0, SeekOrigin.Begin);
            var contentType = surface.GenerateMimeType(_coreConfiguration, _jiraConfiguration);
            await _jiraClient.Attachment.AttachAsync(issueKey, memoryStream, filename ?? surface.GenerateFilename(_coreConfiguration, _jiraConfiguration), contentType, cancellationToken).ConfigureAwait(false);
        }

		/// <summary>
		///     Add a comment to the supplied issue
		/// </summary>
		/// <param name="issueKey">Jira issue key</param>
		/// <param name="body">text</param>
		/// <param name="visibility">the visibility role</param>
		/// <param name="cancellationToken">CancellationToken</param>
		public async Task AddCommentAsync(string issueKey, string body, CancellationToken cancellationToken = default)
		{
			await _jiraClient.Issue.AddCommentAsync(issueKey, body, cancellationToken: cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		///     Get the search results for the specified filter
		/// </summary>
		/// <param name="filter">Filter</param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task<IList<Issue>> SearchAsync(Filter filter, CancellationToken cancellationToken = default)
		{
			var searchResult =
				await _jiraClient.Issue.SearchAsync(filter.Jql,
                new Page { MaxResults = 20},
                new[] {"summary", "reporter", "assignee", "created", "issuetype"}, cancellationToken:cancellationToken).ConfigureAwait(false);
			return searchResult.Issues;
		}

		/// <summary>
		///     Get the bitmap representing the issue type of an issue, from cache.
		/// </summary>
		/// <param name="issue">Issue</param>
		/// <param name="cancellationToken">CancellationToken</param>
		/// <returns>Bitmap</returns>
		public async Task<BitmapSource> GetIssueTypeBitmapAsync(Issue issue, CancellationToken cancellationToken = default)
        {
            var result = _issueTypeBitmapCache.Lookup(issue.Fields.IssueType);
            if (result.HasValue)
            {
                return result.Value.Icon;
            }

            _issueTypeBitmapCache.ExpireAfter(icon => TimeSpan.FromMinutes(30), Scheduler.Default);
            var bitmap = await _jiraClient.Server.GetUriContentAsync<BitmapSource>(issue.Fields.IssueType.IconUri, cancellationToken).ConfigureAwait(false);

            var item = new IssueTypeIcon(issue.Fields.IssueType, bitmap);
            _issueTypeBitmapCache.AddOrUpdate(item);
			return bitmap;
		}
	}
}