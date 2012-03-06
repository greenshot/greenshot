/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Text.RegularExpressions;

using Greenshot.Plugin;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using Greenshot.IniFile;
using Jira;

namespace GreenshotJiraPlugin {
	/// <summary>
	/// Description of JiraUtils.
	/// </summary>
	public class JiraUtils {
		private static readonly Regex JIRA_KEY_REGEX = new Regex(@"/browse/([A-Z0-9]+\-[0-9]+)");
		private static JiraConfiguration config = IniConfig.GetIniSection<JiraConfiguration>();

		public static List<JiraIssue> GetCurrentJiras() {
			// Make sure we suppress the login
			List<string> jirakeys = new List<string>();
			foreach(string url in IEHelper.GetIEUrls()) {
				if (url == null) {
					continue;
				}
				MatchCollection jiraKeyMatch = JIRA_KEY_REGEX.Matches(url);
				if (jiraKeyMatch != null && jiraKeyMatch.Count > 0) {
					jirakeys.Add(jiraKeyMatch[0].Groups[1].Value);
				}
			}
			if (!string.IsNullOrEmpty(config.LastUsedJira) && !jirakeys.Contains(config.LastUsedJira)) {
				jirakeys.Add(config.LastUsedJira);
			}
			if (jirakeys.Count > 0) {
				List<JiraIssue> jiraIssues = new List<JiraIssue>();
				foreach(string jiraKey in jirakeys) {
					try {
						JiraIssue issue = JiraPlugin.Instance.JiraConnector.getIssue(jiraKey);
						jiraIssues.Add(issue);
					} catch {}
				}
				if (jiraIssues.Count > 0) {
					return jiraIssues;
				}
			}
			return null;
		}
	}
}
