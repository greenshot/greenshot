/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using Dapplo.Jira;
using Dapplo.Log;
using Greenshot.Base.Hooking;

namespace Greenshot.Plugin.Jira
{
    /// <summary>
    /// This class will monitor all _jira activity by registering for title changes
    /// It keeps a list of the last "accessed" jiras, and makes it easy to upload to one.
    /// Make sure this is instanciated on the UI thread!
    /// </summary>
    public class JiraMonitor : IDisposable
    {
        private static readonly LogSource Log = new LogSource();
        private readonly Regex _jiraKeyPattern = new Regex(@"[A-Z][A-Z0-9]+\-[0-9]+");
        private readonly WindowsTitleMonitor _monitor;
        private readonly IList<IJiraClient> _jiraInstances = new List<IJiraClient>();
        private readonly IDictionary<string, IJiraClient> _projectJiraClientMap = new Dictionary<string, IJiraClient>();

        private readonly int _maxEntries;

        // TODO: Add issues from issueHistory (JQL -> Where.IssueKey.InIssueHistory())
        private IDictionary<string, JiraDetails> _recentJiras = new Dictionary<string, JiraDetails>();

        /// <summary>
        /// Register to this event to get events when new jira issues are detected
        /// </summary>
        public event EventHandler<JiraEventArgs> JiraEvent;

        public JiraMonitor(int maxEntries = 40)
        {
            _maxEntries = maxEntries;
            _monitor = new WindowsTitleMonitor();
            _monitor.TitleChangeEvent += MonitorTitleChangeEvent;
        }

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
        protected void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            // free managed resources
            _monitor.TitleChangeEvent -= MonitorTitleChangeEvent;
            _monitor.Dispose();
            // free native resources if there are any.
        }

        /// <summary>
        /// Retrieve the API belonging to a JiraDetails
        /// </summary>
        /// <param name="jiraDetails"></param>
        /// <returns>IJiraClient</returns>
        public IJiraClient GetJiraClientForKey(JiraDetails jiraDetails)
        {
            return _projectJiraClientMap[jiraDetails.ProjectKey];
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
        /// <param name="jiraInstance">IJiraClient</param>
        /// <param name="token">CancellationToken</param>
        public async Task AddJiraInstanceAsync(IJiraClient jiraInstance, CancellationToken token = default)
        {
            _jiraInstances.Add(jiraInstance);
            var projects = await jiraInstance.Project.GetAllAsync(cancellationToken: token).ConfigureAwait(false);
            if (projects != null)
            {
                foreach (var project in projects)
                {
                    if (!_projectJiraClientMap.ContainsKey(project.Key))
                    {
                        _projectJiraClientMap.Add(project.Key, jiraInstance);
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
                if (_projectJiraClientMap.TryGetValue(jiraDetails.ProjectKey, out var jiraClient))
                {
                    var issue = await jiraClient.Issue.GetAsync(jiraDetails.JiraKey).ConfigureAwait(false);
                    jiraDetails.JiraIssue = issue;
                }

                // Send event
                JiraEvent?.Invoke(this, new JiraEventArgs
                {
                    Details = jiraDetails,
                    EventType = JiraEventTypes.DetectedNewJiraIssue
                });
            }
            catch (Exception ex)
            {
                Log.Warn().WriteLine("Couldn't retrieve JIRA title: {0}", ex.Message);
            }
        }

        /// <summary>
        /// Handle title changes, check for JIRA
        /// </summary>
        /// <param name="eventArgs"></param>
        private void MonitorTitleChangeEvent(TitleChangeEventArgs eventArgs)
        {
            string windowTitle = eventArgs.Title;
            if (string.IsNullOrEmpty(windowTitle))
            {
                return;
            }

            var jiraKeyMatch = _jiraKeyPattern.Match(windowTitle);
            if (!jiraKeyMatch.Success)
            {
                return;
            }

            // Found a possible JIRA title
            var jiraKey = jiraKeyMatch.Value;
            var jiraKeyParts = jiraKey.Split('-');
            var projectKey = jiraKeyParts[0];
            var jiraId = jiraKeyParts[1];

            // Check if we have a JIRA instance with a project for this key
            if (_projectJiraClientMap.TryGetValue(projectKey, out var jiraClient))
            {
                // We have found a project for this _jira key, so it must be a valid & known JIRA
                if (_recentJiras.TryGetValue(jiraKey, out var currentJiraDetails))
                {
                    // update 
                    currentJiraDetails.SeenAt = DateTimeOffset.Now;

                    // Notify the order change
                    JiraEvent?.Invoke(this, new JiraEventArgs
                    {
                        Details = currentJiraDetails,
                        EventType = JiraEventTypes.OrderChanged
                    });
                    // Nothing else to do

                    return;
                }

                // We detected an unknown JIRA, so add it to our list
                currentJiraDetails = new JiraDetails
                {
                    Id = jiraId,
                    ProjectKey = projectKey
                };
                _recentJiras.Add(currentJiraDetails.JiraKey, currentJiraDetails);

                // Make sure we don't collect _jira's until the memory is full
                if (_recentJiras.Count > _maxEntries)
                {
                    // Add it to the list of recent Jiras
                    _recentJiras = (from jiraDetails in _recentJiras.Values.ToList()
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