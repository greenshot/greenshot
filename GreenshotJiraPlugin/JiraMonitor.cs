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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GreenshotJiraPlugin {
	/// <summary>
	/// This class will monitor all _jira activity by registering for title changes
	/// It keeps a list of the last "accessed" jiras, and makes it easy to upload to one.
	/// </summary>
	public class JiraMonitor : IDisposable {
		private readonly Regex _jiraKeyPattern = new Regex(@"[A-Z0-9]+\-[0-9]+");
		private readonly TitleChangeMonitor _monitor;
		private readonly IList<JiraAPI> _jiraInstances = new List<JiraAPI>();
		private readonly IDictionary<string, JiraAPI> _projectJiraApiMap = new Dictionary<string, JiraAPI>();
		private readonly int _maxEntries;
		private IDictionary<string, JiraDetails> _recentJiras = new Dictionary<string, JiraDetails>();

		public JiraMonitor(int maxEntries = 40) {
			_maxEntries = maxEntries;
			_monitor = new TitleChangeMonitor();
			_monitor.TitleChangeEvent += monitor_TitleChangeEvent;
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
				_monitor.TitleChangeEvent -= monitor_TitleChangeEvent;
				_monitor.Dispose();
				foreach (var jiraInstance in _jiraInstances) {
					jiraInstance.Dispose();
				}
			}
			// free native resources if there are any.
		}
		#endregion

		/// <summary>
		/// Retrieve the API belonging to a JiraDetails
		/// </summary>
		/// <param name="jiraDetails"></param>
		/// <returns>JiraAPI</returns>
		public JiraAPI GetJiraApiForKey(JiraDetails jiraDetails) {
			return _projectJiraApiMap[jiraDetails.ProjectKey];
		}

		/// <summary>
		/// Get the "list" of recently seen Jiras
		/// </summary>
		public IEnumerable<JiraDetails> RecentJiras {
			get {
				return (from jiraDetails in _recentJiras.Values
						orderby jiraDetails.SeenAt descending
						select jiraDetails);
			}
		}

		/// <summary>
		/// Check if this monitor has active instances
		/// </summary>
		public bool HasJiraInstances {
			get {
				return _jiraInstances.Count > 0;
			}
		}


		/// <summary>
		/// Add an instance of a JIRA system
		/// </summary>
		/// <param name="url"></param>
		/// <param name="username"></param>
		/// <param name="password"></param>
		public async void AddJiraInstance(string url, string username, string password, CancellationToken token = default(CancellationToken)) {
			var jiraInstance = new JiraAPI(url, username, password);
			var serverInfo = await jiraInstance.ServerInfo();
			jiraInstance.ServerTitle = serverInfo.serverTitle;
			jiraInstance.JiraVersion = serverInfo.version;

			_jiraInstances.Add(jiraInstance);
			foreach (var project in await jiraInstance.Projects()) {
				if (!_projectJiraApiMap.ContainsKey(project.key)) {
					_projectJiraApiMap.Add(project.key, jiraInstance);
				}
			}
		}

		/// <summary>
		/// A helper method to retrieve the title for a Jira (so we can display this clean) in the background (async)
		/// </summary>
		/// <param name="jiraKey">key for the jira to retrieve the title (XYZ-1234)</param>
		/// <returns>title for the _jira key</returns>
		private async Task GetTitle(JiraDetails jiraDetails) {
			try {
				JiraAPI jiraApi;
				if (_projectJiraApiMap.TryGetValue(jiraDetails.ProjectKey, out jiraApi)) {
					var issue = await jiraApi.Issue(jiraDetails.JiraKey);
					jiraDetails.Title = issue.fields.summary;
				}
			} catch (Exception ex) {
				Console.WriteLine("Couldn't retrieve JIRA title: {0}", ex.Message);
			}
		}

		/// <summary>
		/// Try to make the title as clean as possible
		/// </summary>
		/// <param name="jiraApi"></param>
		/// <param name="windowsTitle"></param>
		/// <returns>a clean windows title</returns>
		private string CleanWindowTitle(JiraAPI jiraAPI, string jiraKey, string windowTitle) {
			var title = windowTitle.Replace(jiraAPI.ServerTitle, "");
			// Remove for emails:
			title = title.Replace("[JIRA]", "");
			title = Regex.Replace(title, string.Format(@"^[^a-zA-Z0-9]*{0}[^a-zA-Z0-9]*", jiraKey), "");


			title = Regex.Replace(title, "^[^a-zA-Z0-9]*(.*)[^a-zA-Z0-9]*$", "$1");
			return title;
		}

		/// <summary>
		/// Handle title changes, check for JIRA
		/// </summary>
		/// <param name="eventArgs"></param>
		private void monitor_TitleChangeEvent(TitleChangeEventArgs eventArgs) {
			string windowTitle = eventArgs.Title;
			if (string.IsNullOrEmpty(windowTitle)) {
				return;
			}
			var jiraKeyMatch = _jiraKeyPattern.Match(windowTitle);
			if (jiraKeyMatch.Success) {
				// Found a possible JIRA title
				var jiraKey = jiraKeyMatch.Value;
				var jiraKeyParts = jiraKey.Split('-');
				var projectKey = jiraKeyParts[0];
				var jiraId = jiraKeyParts[1];

				JiraAPI jiraAPI;
				// Check if we have a JIRA instance with a project for this key
				if (_projectJiraApiMap.TryGetValue(projectKey, out jiraAPI)) {
					// We have found a project for this _jira key, so it must be a valid & known JIRA
					JiraDetails currentJiraDetails;
					if (_recentJiras.TryGetValue(jiraKey, out currentJiraDetails)) {
						// update 
						currentJiraDetails.SeenAt = DateTimeOffset.Now;
						// Nothing else to do
						return;
					}
					// We detected an unknown JIRA, so add it to our list
					currentJiraDetails = new JiraDetails() {
						Id = jiraId,
						ProjectKey = projectKey,
						Title = CleanWindowTitle(jiraAPI, jiraKey, windowTitle) // Try to make it as clean as possible, although we retrieve the issue title later
					};
					_recentJiras.Add(currentJiraDetails.JiraKey, currentJiraDetails);

					// Make sure we don't collect _jira's until the memory is full
					if (_recentJiras.Count > _maxEntries) {
						// Add it to the list of recent Jiras
						IList<JiraDetails> clonedList = new List<JiraDetails>(_recentJiras.Values);
						_recentJiras = (from jiraDetails in clonedList
										orderby jiraDetails.SeenAt descending
										select jiraDetails).Take(10).ToDictionary(jd => jd.JiraKey, jd => jd);

					}
					// Now we can get the title from JIRA itself
					var updateTitleTask = GetTitle(currentJiraDetails);
				}
			}
		}
	}
}
