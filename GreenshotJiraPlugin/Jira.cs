/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2011  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;

using GreenshotJiraPlugin;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;

namespace Jira {
	#region transport classes
	public class JiraFilter {
		public JiraFilter(string name, string id) {
			this.Name = name;
			this.Id = id;
		}
		public string Name {
			get;
			set;
		}
		public string Id {
			get;
			set;
		}
	}

	public class JiraIssue {
		public JiraIssue(string key, DateTime? created, string reporter, string assignee, string project, string summary, string description, string environment, string [] attachmentNames) {
			this.Key = key;
			this.Created = created;
			this.Reporter = reporter;
			this.Assignee = assignee;
			this.Project = project;
			this.Summary = summary;
			this.Description = description;
			this.Environment = environment;
			this.AttachmentNames = attachmentNames;
		}
		public string Key {
			get;
			set;
		}
		public DateTime? Created {
			get;
			private set;
		}
		public string Reporter {
			get;
			private set;
		}
		public string Assignee {
			get;
			private set;
		}
		public string Project {
			get;
			private set;
		}
		public string Summary {
			get;
			private set;
		}
		public string Description {
			get;
			private set;
		}
		public string Environment {
			get;
			private set;
		}
		public string[] AttachmentNames {
			get;
			private set;
		}
	}
	#endregion

	public class JiraConnector {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(JiraConnector));
		private const string AUTH_FAILED_EXCEPTION_NAME = "com.atlassian.jira.rpc.exception.RemoteAuthenticationException";
		public const string DEFAULT_POSTFIX = "/rpc/soap/jirasoapservice-v2?wsdl";
		private ILanguage lang = Language.GetInstance();
		private string credentials;
		private DateTime loggedInTime = DateTime.Now;
		private bool loggedIn;
		private JiraSoapServiceService jira;
		private int timeout;
		private string url;
		private Dictionary<string, string> userMap = new Dictionary<string, string>();
		private JiraConfiguration config = IniConfig.GetIniSection<JiraConfiguration>();

		public JiraConnector() {
			this.url = config.Url;
			this.timeout = config.Timeout;
			BackgroundForm backgroundForm = BackgroundForm.ShowAndWait(JiraPlugin.JiraPluginAttributes.Name, lang.GetString(LangKey.communication_wait));
			try {
				jira = new JiraSoapServiceService();
			} finally {
				backgroundForm.CloseDialog();
			}
			jira.Url = url;
		   	jira.Proxy = NetworkHelper.CreateProxy(new Uri(url));
		}

		~JiraConnector() {
			logout();
		}
		
		/// <summary>
		/// Internal login which catches the exceptions
		/// </summary>
		/// <returns>true if login was done sucessfully</returns>
		private bool doLogin(string user, string password) {
			BackgroundForm backgroundForm = BackgroundForm.ShowAndWait(JiraPlugin.JiraPluginAttributes.Name, lang.GetString(LangKey.communication_wait));
			try {
				LOG.DebugFormat("Loggin in");
				this.credentials = jira.login(user, password);
				LOG.DebugFormat("Logged in");
				this.loggedInTime = DateTime.Now;
				this.loggedIn = true;
			} catch (Exception e) {
				// check if auth failed
				if (e.Message.Contains(AUTH_FAILED_EXCEPTION_NAME)) {
					return false;
				}
				// Not an authentication issue
				this.loggedIn = false;
				this.credentials = null;
				e.Data.Add("user", user);
				e.Data.Add("url", url);
				throw;
			} finally {
				backgroundForm.CloseDialog();
			}
			return true;
		}
		
		public void login() {
			logout();
			try {
				// Get the system name, so the user knows where to login to
				string systemName = url.Replace(DEFAULT_POSTFIX,"");
		   		CredentialsDialog dialog = new CredentialsDialog(systemName);
				dialog.Name = null;
				while (dialog.Show(dialog.Name) == DialogResult.OK) {
					if (doLogin(dialog.Name, dialog.Password)) {
						if (dialog.SaveChecked) {
							dialog.Confirm(true);
						}
						return;
					} else {
						try {
							dialog.Confirm(false);
						} catch (ApplicationException e) {
							// exception handling ...
							LOG.Error("Problem using the credentials dialog", e);
						}
						// For every windows version after XP show an incorrect password baloon
						dialog.IncorrectPassword = true;
						// Make sure the dialog is display, the password was false!
						dialog.AlwaysDisplay = true;
					}
				}
			} catch (ApplicationException e) {
				// exception handling ...
				LOG.Error("Problem using the credentials dialog", e);
			}
		}

		public void logout() {
			if (credentials != null) {
				jira.logout(credentials);
				credentials = null;
				loggedIn = false;
			}
		}

		private void checkCredentials() {
			if (loggedIn) {
				if (loggedInTime.AddMinutes(timeout-1).CompareTo(DateTime.Now) < 0) {
					logout();
					login();
				}
			} else {
				login();
			}
		}

		public bool isLoggedIn() {
			return loggedIn;
		}

		public JiraFilter[] getFilters() {
			List<JiraFilter> filters = new List<JiraFilter>();
			checkCredentials();
			RemoteFilter[] remoteFilters = jira.getSavedFilters(credentials);
			foreach (RemoteFilter remoteFilter in remoteFilters) {
				filters.Add(new JiraFilter(remoteFilter.name, remoteFilter.id));
			}
			return filters.ToArray();
		}
		
		private JiraIssue createDummyErrorIssue(Exception e) {
			// Creating bogus jira to indicate a problem
			return new JiraIssue("error", DateTime.Now, "error", "error", "error", e.Message, "error", "error", null);
		}

		public JiraIssue getIssue(string key) {
			JiraIssue jiraIssue = null; 
			checkCredentials();
			try {
				RemoteIssue issue = jira.getIssue(credentials, key);
				jiraIssue = new JiraIssue(issue.key, issue.created, getUserFullName(issue.reporter), getUserFullName(issue.assignee), issue.project, issue.summary, issue.description, issue.environment, issue.attachmentNames);
			} catch (Exception e) {
				LOG.Error("Problem retrieving Jira: " + key, e);
			}
			return jiraIssue;
		}

		public JiraIssue[] getIssuesForFilter(string filterId) {
			List<JiraIssue> issuesToReturn = new List<JiraIssue>();
			checkCredentials();
			try {
				RemoteIssue[] issues = jira.getIssuesFromFilter(credentials, filterId);
				foreach (RemoteIssue issue in issues) {
					try {
						JiraIssue jiraIssue = new JiraIssue(issue.key, issue.created, getUserFullName(issue.reporter), getUserFullName(issue.assignee), issue.project, issue.summary, issue.description, "", issue.attachmentNames);
						issuesToReturn.Add(jiraIssue);
					} catch (Exception e) {
						LOG.Error("Problem retrieving Jira: " + issue.key, e);
						JiraIssue jiraIssue = createDummyErrorIssue(e);
						jiraIssue.Key = issue.key;
						issuesToReturn.Add(jiraIssue);
					}
				}
			} catch (Exception e) {
				LOG.Error("Problem retrieving Jiras for Filter: " + filterId, e);
				issuesToReturn.Add(createDummyErrorIssue(e));
			}
			return issuesToReturn.ToArray(); ;
		}

		public void addAttachment(string issueKey, string filename, byte [] buffer) {
			checkCredentials();
			string base64String = Convert.ToBase64String(buffer, Base64FormattingOptions.InsertLineBreaks);
			jira.addBase64EncodedAttachmentsToIssue(credentials, issueKey, new string[] { filename }, new string[] { base64String });
		}

		public void addAttachment(string issueKey, string filename, string attachmentText) {
			Encoding WINDOWS1252 = Encoding.GetEncoding(1252);
			byte[] attachment = WINDOWS1252.GetBytes(attachmentText.ToCharArray());
			addAttachment(issueKey, filename, attachment);
		}

		public void addAttachment(string issueKey, string filePath) {
			FileInfo fileInfo = new FileInfo(filePath);
			byte[] buffer = new byte[fileInfo.Length];
			using (FileStream stream = new FileStream(filePath, FileMode.Open)) {
				stream.Read(buffer, 0, (int)fileInfo.Length);
			}
			addAttachment(issueKey, Path.GetFileName(filePath), buffer);
		}

		public void addComment(string issueKey, string commentString) {
			RemoteComment comment = new RemoteComment();
			comment.body = commentString;
			checkCredentials();
			jira.addComment(credentials, issueKey, comment);
		}

		private string getUserFullName(string user) {
			string fullname = null;
			if (user != null) {
				if (userMap.ContainsKey(user)) {
					fullname = userMap[user];
				} else {
					checkCredentials();
					RemoteUser remoteUser = jira.getUser(credentials, user);
					fullname = remoteUser.fullname;
					userMap.Add(user, fullname);
				}
			} else {
				fullname = "Not assigned";
			}
			return fullname;
		}
	}
}