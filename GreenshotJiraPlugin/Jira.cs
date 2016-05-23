
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

using Greenshot.IniFile;
using GreenshotJiraPlugin;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;

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

	public class JiraConnector : IDisposable {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(JiraConnector));
		private const string AUTH_FAILED_EXCEPTION_NAME = "com.atlassian.jira.rpc.exception.RemoteAuthenticationException";
		private static readonly JiraConfiguration config = IniConfig.GetIniSection<JiraConfiguration>();
		public const string DEFAULT_POSTFIX = "/rpc/soap/jirasoapservice-v2?wsdl";
		private string credentials;
		private DateTime loggedInTime = DateTime.Now;
		private bool loggedIn;
		private JiraSoapServiceService jira;
		private readonly int timeout;
		private string url;
		private readonly Cache<string, JiraIssue> jiraCache = new Cache<string, JiraIssue>(60 * config.Timeout);
		private readonly Cache<string, RemoteUser> userCache = new Cache<string, RemoteUser>(60 * config.Timeout);
		private readonly bool suppressBackgroundForm = false;

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) {
			if (jira != null) {
				logout();
			}

			if (disposing) {
				if (jira != null) {
					jira.Dispose();
					jira = null;
				}
			}
		}

		public JiraConnector() : this(false) {
		}

		public JiraConnector(bool suppressBackgroundForm) {
			this.url = config.Url;
			this.timeout = config.Timeout;
			this.suppressBackgroundForm = suppressBackgroundForm;
			createService();
		}

		private void createService() {
			if (!suppressBackgroundForm) {
				new PleaseWaitForm().ShowAndWait(JiraPlugin.Instance.JiraPluginAttributes.Name, Language.GetString("jira", LangKey.communication_wait),
					delegate() {
						jira = new JiraSoapServiceService();
					}
				);
			} else {
				jira = new JiraSoapServiceService();
			}
			jira.Url = url;
			jira.Proxy = NetworkHelper.CreateProxy(new Uri(url));
			// Do not use:
			//jira.AllowAutoRedirect = true;
			jira.UserAgent = "Greenshot";
		}

		~JiraConnector() {
			Dispose(false);
		}
		
		/// <summary>
		/// Internal login which catches the exceptions
		/// </summary>
		/// <returns>true if login was done sucessfully</returns>
		private bool doLogin(string user, string password, bool suppressBackgroundForm) {

			// This is what needs to be done
			ThreadStart jiraLogin = delegate {
				LOG.DebugFormat("Loggin in");
				try {
					this.credentials = jira.login(user, password);
				} catch (Exception) {
					if (!url.EndsWith("wsdl")) {
						url = url + "/rpc/soap/jirasoapservice-v2?wsdl";
						// recreate the service with the new url
						createService();
						this.credentials = jira.login(user, password);
						// Worked, store the url in the configuration
						config.Url = url;
						IniConfig.Save();
					} else {
						throw;
					}
				}
				
				LOG.DebugFormat("Logged in");
				this.loggedInTime = DateTime.Now;
				this.loggedIn = true;

			};
			// Here we do it
			try {
				if (!suppressBackgroundForm) {
					new PleaseWaitForm().ShowAndWait(JiraPlugin.Instance.JiraPluginAttributes.Name, Language.GetString("jira", LangKey.communication_wait), jiraLogin);
				} else {
					jiraLogin.Invoke();
				}
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
			}
			return true;
		}
		
		public void login() {
			login(false);
		}
		public void login(bool suppressBackgroundForm) {
			logout();
			try {
				// Get the system name, so the user knows where to login to
				string systemName = url.Replace(DEFAULT_POSTFIX,"");
				CredentialsDialog dialog = new CredentialsDialog(systemName);
				dialog.Name = null;
				while (dialog.Show(dialog.Name) == DialogResult.OK) {
					if (doLogin(dialog.Name, dialog.Password, suppressBackgroundForm)) {
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

		public bool isLoggedIn {
			get {
				return loggedIn;
			}
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
			if (jiraCache.Contains(key)) {
				jiraIssue = jiraCache[key];
			}
			if (jiraIssue == null) {
				checkCredentials();
				try {
					RemoteIssue issue = jira.getIssue(credentials, key);
					jiraIssue = new JiraIssue(issue.key, issue.created, getUserFullName(issue.reporter), getUserFullName(issue.assignee), issue.project, issue.summary, issue.description, issue.environment, issue.attachmentNames);
					jiraCache.Add(key, jiraIssue);
				} catch (Exception e) {
					LOG.Error("Problem retrieving Jira: " + key, e);
				}
			}
			return jiraIssue;
		}

		public JiraIssue[] getIssuesForFilter(string filterId) {
			List<JiraIssue> issuesToReturn = new List<JiraIssue>();
			checkCredentials();
			try {
				RemoteIssue[] issues = jira.getIssuesFromFilter(credentials, filterId);

				#region Username cache update
				List<string> users = new List<string>();
				foreach (RemoteIssue issue in issues) {
					if (issue.reporter != null && !hasUser(issue.reporter) && !users.Contains(issue.reporter)) {
						users.Add(issue.reporter);
					}
					if (issue.assignee != null && !hasUser(issue.assignee) && !users.Contains(issue.assignee)) {
						users.Add(issue.assignee);
					}
				}
				int taskCount = users.Count;
				if (taskCount > 0) {
					ManualResetEvent doneEvent = new ManualResetEvent(false);
					for (int i = 0; i < users.Count; i++) {
						ThreadPool.QueueUserWorkItem(delegate(object name) {
							LOG.InfoFormat("Retrieving {0}", name);
							getUserFullName((string)name);
							if (Interlocked.Decrement(ref taskCount) == 0) {
								doneEvent.Set();
							}
						}, users[i]);
					}
					doneEvent.WaitOne();
				}
				#endregion

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

		public string getURL(string issueKey) {
			return url.Replace(DEFAULT_POSTFIX,"") + "/browse/" + issueKey;
		}

		public void addAttachment(string issueKey, string filename, IBinaryContainer attachment) {
			checkCredentials();
			try {
				jira.addBase64EncodedAttachmentsToIssue(credentials, issueKey, new string[] { filename }, new string[] { attachment.ToBase64String(Base64FormattingOptions.InsertLineBreaks) });
			} catch (Exception ex1) {
				LOG.WarnFormat("Failed to upload by using method addBase64EncodedAttachmentsToIssue, error was {0}", ex1.Message);
				try {
					LOG.Warn("Trying addAttachmentsToIssue instead");
					jira.addAttachmentsToIssue(credentials, issueKey, new string[] { filename }, (sbyte[])(Array)attachment.ToByteArray());
				} catch (Exception ex2) {
					LOG.WarnFormat("Failed to use alternative method, error was: {0}", ex2.Message);
					throw;
				}
			}
		}

		public void addComment(string issueKey, string commentString) {
			RemoteComment comment = new RemoteComment();
			comment.body = commentString;
			checkCredentials();
			jira.addComment(credentials, issueKey, comment);
		}

		private bool hasUser(string user) {
			if (user != null) {
				return userCache.Contains(user);
			}
			return false;
		}

		private string getUserFullName(string user) {
			string fullname = null;
			if (user != null) {
				if (userCache.Contains(user)) {
					fullname = userCache[user].fullname;
				} else {
					checkCredentials();
					RemoteUser remoteUser = jira.getUser(credentials, user);
					userCache.Add(user, remoteUser);
					fullname = remoteUser.fullname;
				}
			} else {
				fullname = "Not assigned";
			}
			return fullname;
		}
	}
}