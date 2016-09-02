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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapplo.Jira.Entities;
using Greenshot.IniFile;
using GreenshotPlugin.Core;

namespace GreenshotJiraPlugin {
	/// <summary>
	/// Description of JiraUtils.
	/// </summary>
	public static class JiraUtils {
		private static readonly Regex JiraKeyRegex = new Regex(@"/browse/([A-Z0-9]+\-[0-9]+)");
		private static readonly JiraConfiguration Config = IniConfig.GetIniSection<JiraConfiguration>();
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(JiraUtils));

		public static async Task<IList<Issue>> GetCurrentJirasAsync() {
			// Make sure we suppress the login
			var jirakeys = new List<string>();
			foreach(string url in IEHelper.GetIEUrls()) {
				if (url == null) {
					continue;
				}
				var jiraKeyMatch = JiraKeyRegex.Matches(url);
				if (jiraKeyMatch.Count > 0) {
					string jiraKey = jiraKeyMatch[0].Groups[1].Value;
					jirakeys.Add(jiraKey);
				}
			}
			if (!string.IsNullOrEmpty(Config.LastUsedJira) && !jirakeys.Contains(Config.LastUsedJira)) {
				jirakeys.Add(Config.LastUsedJira);
			}
			if (jirakeys.Count > 0) {
				var jiraIssues = new List<Issue>();
				foreach(string jiraKey in jirakeys) {
					try
					{
						var issue = await JiraPlugin.Instance.JiraConnector.GetIssueAsync(jiraKey).ConfigureAwait(false);
						if (issue != null)
						{
							jiraIssues.Add(issue);
						}
					}
					catch (Exception ex)
					{
						Log.Error(ex);
						// Remove issue from the last used jira config, as it caused an issue (probably not there)
						if (Config.LastUsedJira == jiraKey)
						{
							Config.LastUsedJira = null;
						}
					}
				}
				if (jiraIssues.Count > 0) {
					return jiraIssues;
				}
			}
			return null;
		}
	}
}
