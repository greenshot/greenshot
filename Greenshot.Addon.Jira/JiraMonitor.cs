/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub: https://github.com/greenshot
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
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Config.Ini;
using Dapplo.Jira;
using Dapplo.Jira.Entities;
using Dapplo.Windows;
using Greenshot.Addon.Configuration;
using Dapplo.Log.Facade;

namespace Greenshot.Addon.Jira
{

	/// <summary>
	/// This class will monitor all _jira activity by registering for title changes
	/// It keeps a list of the last "accessed" jiras, and makes it easy to upload to one.
	/// Make sure this is instanciated on the UI thread!
	/// </summary>
	public class JiraMonitor : IDisposable
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly INetworkConfiguration NetworkConfig = IniConfig.Current.Get<INetworkConfiguration>();
		private readonly Regex _jiraKeyPattern = new Regex(@"[A-Z][A-Z0-9]+\-[0-9]+");
		private readonly WindowsTitleMonitor _monitor;
		private readonly IList<JiraApi> _jiraInstances = new List<JiraApi>();
		private readonly IDictionary<string, JiraApi> _projectJiraApiMap = new Dictionary<string, JiraApi>();
		private readonly IDictionary<Uri, ServerInfo> _jiraServerInfos = new Dictionary<Uri, ServerInfo>();
		private readonly int _maxEntries;
		private IDictionary<string, JiraDetails> _recentJiras = new Dictionary<string, JiraDetails>();

		/// <summary>
		/// Register to this event to get events when new jira issues are detected
		/// </summary>
		public event EventHandler<JiraEventArgs> JiraEvent;

		public JiraMonitor(int maxEntries = 40)
		{
			_maxEntries = maxEntries;
			_monitor = new WindowsTitleMonitor();
			_monitor.TitleChangeEvent += monitor_TitleChangeEvent;
		}

		#region Dispose

		/// <summary>
		/// Dispose
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose all managed resources
		/// </summary>
		/// <param name="disposing">when true is passed all managed resources are disposed.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				// free managed resources
				_monitor.TitleChangeEvent -= monitor_TitleChangeEvent;
				_monitor.Dispose();
			}
			// free native resources if there are any.
		}

		#endregion

		/// <summary>
		/// Retrieve the API belonging to a JiraDetails
		/// </summary>
		/// <param name="jiraDetails"></param>
		/// <returns>JiraAPI</returns>
		public JiraApi GetJiraApiForKey(JiraDetails jiraDetails)
		{
			return _projectJiraApiMap[jiraDetails.ProjectKey];
		}

		/// <summary>
		/// Get the "list" of recently seen Jiras
		/// </summary>
		public IEnumerable<JiraDetails> RecentJiras =>
			(from jiraDetails in _recentJiras.Values
			orderby jiraDetails.SeenAt descending
			select jiraDetails);

		/// <summary>
		/// Check if this monitor has active instances
		/// </summary>
		public bool HasJiraInstances => _jiraInstances.Count > 0;

		/// <summary>
		/// Add an instance of a JIRA system
		/// </summary>
		/// <param name="uri"></param>
		/// <param name="username"></param>
		/// <param name="password"></param>
		/// <param name="token"></param>
		public async Task AddJiraInstanceAsync(Uri uri, string username, string password, CancellationToken token = default(CancellationToken))
		{
			var jiraInstance = new JiraApi(uri, NetworkConfig);
			jiraInstance.SetBasicAuthentication(username, password);

			if (!_jiraServerInfos.ContainsKey(uri))
			{
				var serverInfo = await jiraInstance.GetServerInfoAsync(token);
				_jiraServerInfos.Add(jiraInstance.JiraBaseUri, serverInfo);
			}

			_jiraInstances.Add(jiraInstance);
			var projects = await jiraInstance.GetProjectsAsync(token);
			if (projects != null)
			{
				foreach (var project in projects)
				{
					if (!_projectJiraApiMap.ContainsKey(project.Key))
					{
						_projectJiraApiMap.Add(project.Key, jiraInstance);
					}
				}
			}
		}

		/// <summary>
		/// This method will update details, like the title, and send an event to registed listeners of the JiraEvent
		/// </summary>
		/// <param name="jiraDetails">Contains the jira key to retrieve the title (XYZ-1234)</param>
		/// <returns>Task</returns>
		private async Task DetectedNewJiraIssueAsync(JiraDetails jiraDetails)
		{
			try
			{
				JiraApi jiraApi;
				if (_projectJiraApiMap.TryGetValue(jiraDetails.ProjectKey, out jiraApi))
				{
					var issue = await jiraApi.GetIssueAsync(jiraDetails.JiraKey).ConfigureAwait(false);
					jiraDetails.Title = issue.Fields.Summary;
				}
				// Send event
				JiraEvent?.Invoke(this, new JiraEventArgs { Details = jiraDetails, EventType = JiraEventTypes.DetectedNewJiraIssue });
			}
			catch (Exception ex)
			{
				Log.Warn().WriteLine("Couldn't retrieve JIRA title: {0}", ex.Message);
			}
		}

		/// <summary>
		/// Try to make the title as clean as possible, this is a quick indicator of the jira title
		/// Later the title will be retrieved from the Jira system itself
		/// </summary>
		/// <param name="jiraApi"></param>
		/// <param name="windowTitle"></param>
		/// <param name="jiraKey"></param>
		/// <returns>a clean windows title</returns>
		private string CleanWindowTitle(JiraApi jiraApi, string jiraKey, string windowTitle)
		{
			var serverInfo = _jiraServerInfos[jiraApi.JiraBaseUri];
			var title = windowTitle.Replace(serverInfo.ServerTitle, "");
			// Remove for emails:
			title = title.Replace("[JIRA]", "");
			title = Regex.Replace(title, $@"^[^a-zA-Z0-9]*{jiraKey}[^a-zA-Z0-9]*", "");
			title = Regex.Replace(title, "^[^a-zA-Z0-9]*(.*)[^a-zA-Z0-9]*$", "$1");
			return title;
		}

		/// <summary>
		/// Handle title changes, check for JIRA
		/// </summary>
		/// <param name="eventArgs"></param>
		private void monitor_TitleChangeEvent(TitleChangeEventArgs eventArgs)
		{
			string windowTitle = eventArgs.Title;
			if (string.IsNullOrEmpty(windowTitle))
			{
				return;
			}
			var jiraKeyMatch = _jiraKeyPattern.Match(windowTitle);
			if (jiraKeyMatch.Success)
			{
				// Found a possible JIRA title
				var jiraKey = jiraKeyMatch.Value;
				var jiraKeyParts = jiraKey.Split('-');
				var projectKey = jiraKeyParts[0];
				var jiraId = jiraKeyParts[1];

				JiraApi jiraApi;
				// Check if we have a JIRA instance with a project for this key
				if (_projectJiraApiMap.TryGetValue(projectKey, out jiraApi))
				{
					var serverInfo = _jiraServerInfos[jiraApi.JiraBaseUri];
					Log.Info().WriteLine("Matched {0} to {1}, loading details and placing it in the recent JIRAs list.", jiraKey, serverInfo.ServerTitle);
					// We have found a project for this _jira key, so it must be a valid & known JIRA
					JiraDetails currentJiraDetails;
					if (_recentJiras.TryGetValue(jiraKey, out currentJiraDetails))
					{
						// update 
						currentJiraDetails.SeenAt = DateTimeOffset.Now;

						// Notify the order change
						JiraEvent?.Invoke(this, new JiraEventArgs { Details = currentJiraDetails, EventType = JiraEventTypes.OrderChanged });
						// Nothing else to do

						return;
					}
					// We detected an unknown JIRA, so add it to our list
					currentJiraDetails = new JiraDetails()
					{
						Id = jiraId, ProjectKey = projectKey, Title = CleanWindowTitle(jiraApi, jiraKey, windowTitle) // Try to make it as clean as possible, although we retrieve the issue title later
					};
					_recentJiras.Add(currentJiraDetails.JiraKey, currentJiraDetails);

					// Make sure we don't collect _jira's until the memory is full
					if (_recentJiras.Count > _maxEntries)
					{
						// Add it to the list of recent Jiras
						IList<JiraDetails> clonedList = new List<JiraDetails>(_recentJiras.Values);
						_recentJiras = (from jiraDetails in clonedList
							orderby jiraDetails.SeenAt descending
							select jiraDetails).Take(_maxEntries).ToDictionary(jd => jd.JiraKey, jd => jd);
					}
					// Now we can get the title from JIRA itself
					// ReSharper disable once UnusedVariable
					var updateTitleTask = DetectedNewJiraIssueAsync(currentJiraDetails);
				}
				else
				{
					Log.Info().WriteLine("Couldn't match possible JIRA key {0} to projects in a configured JIRA instance, ignoring", projectKey);
				}
			}
		}
	}
}