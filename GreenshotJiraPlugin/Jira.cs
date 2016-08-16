
/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Threading;
using System.Windows.Forms;
using Greenshot.IniFile;
using GreenshotJiraPlugin.Web_References.JiraSoap;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;

namespace GreenshotJiraPlugin {
	#region transport classes
	public class JiraFilter {
		public JiraFilter(string name, string id) {
			Name = name;
			Id = id;
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
			Key = key;
			Created = created;
			Reporter = reporter;
			Assignee = assignee;
			Project = project;
			Summary = summary;
			Description = description;
			Environment = environment;
			AttachmentNames = attachmentNames;
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
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(JiraConnector));
		private const string AuthFailedExceptionName = "com.atlassian.jira.rpc.exception.RemoteAuthenticationException";
		private static readonly JiraConfiguration Config = IniConfig.GetIniSection<JiraConfiguration>();
		public const string DefaultPostfix = "/rpc/soap/jirasoapservice-v2?wsdl";
		private string _credentials;
		private DateTime _loggedInTime = DateTime.Now;
		private bool _loggedIn;
		private JiraSoapServiceService _jira;
		private readonly int _timeout;
		private string _url;
		private readonly Cache<string, JiraIssue> _jiraCache = new Cache<string, JiraIssue>(60 * Config.Timeout);
		private readonly Cache<string, RemoteUser> _userCache = new Cache<string, RemoteUser>(60 * Config.Timeout);
		private readonly bool _suppressBackgroundForm;

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected void Dispose(bool disposing) {
			if (_jira != null) {
				Logout();
			}

			if (disposing) {
				if (_jira != null) {
					_jira.Dispose();
					_jira = null;
				}
			}
		}

		public JiraConnector() : this(false) {
		}

		public JiraConnector(bool suppressBackgroundForm) {
			_url = Config.Url;
			_timeout = Config.Timeout;
			_suppressBackgroundForm = suppressBackgroundForm;
			CreateService();
		}

		private void CreateService() {
			if (!_suppressBackgroundForm) {
				new PleaseWaitForm().ShowAndWait(JiraPlugin.Instance.JiraPluginAttributes.Name, Language.GetString("jira", LangKey.communication_wait),
					delegate {
						_jira = new JiraSoapServiceService();
					}
				);
			} else {
				_jira = new JiraSoapServiceService();
			}
			_jira.Url = _url;
			_jira.Proxy = NetworkHelper.CreateProxy(new Uri(_url));
			// Do not use:
			//jira.AllowAutoRedirect = true;
			_jira.UserAgent = "Greenshot";
		}

		~JiraConnector() {
			Dispose(false);
		}
		
		/// <summary>
		/// Internal login which catches the exceptions
		/// </summary>
		/// <returns>true if login was done sucessfully</returns>
		private bool DoLogin(string user, string password, bool suppressBackgroundForm) {

			// This is what needs to be done
			ThreadStart jiraLogin = delegate {
				Log.DebugFormat("Loggin in");
				try {
					_credentials = _jira.login(user, password);
				} catch (Exception) {
					if (!_url.EndsWith("wsdl")) {
						_url = _url + "/rpc/soap/jirasoapservice-v2?wsdl";
						// recreate the service with the new url
						CreateService();
						_credentials = _jira.login(user, password);
						// Worked, store the url in the configuration
						Config.Url = _url;
						IniConfig.Save();
					} else {
						throw;
					}
				}
				
				Log.DebugFormat("Logged in");
				_loggedInTime = DateTime.Now;
				_loggedIn = true;

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
				if (e.Message.Contains(AuthFailedExceptionName)) {
					return false;
				}
				// Not an authentication issue
				_loggedIn = false;
				_credentials = null;
				e.Data.Add("user", user);
				e.Data.Add("url", _url);
				throw;
			}
			return true;
		}
		
		public void Login() {
			Login(false);
		}
		public void Login(bool suppressBackgroundForm) {
			Logout();
			try {
				// Get the system name, so the user knows where to login to
				string systemName = _url.Replace(DefaultPostfix,"");
				CredentialsDialog dialog = new CredentialsDialog(systemName)
				{
					Name = null
				};
				while (dialog.Show(dialog.Name) == DialogResult.OK) {
					if (DoLogin(dialog.Name, dialog.Password, suppressBackgroundForm)) {
						if (dialog.SaveChecked) {
							dialog.Confirm(true);
						}
						return;
					}
					try {
						dialog.Confirm(false);
					} catch (ApplicationException e) {
						// exception handling ...
						Log.Error("Problem using the credentials dialog", e);
					}
					// For every windows version after XP show an incorrect password baloon
					dialog.IncorrectPassword = true;
					// Make sure the dialog is display, the password was false!
					dialog.AlwaysDisplay = true;
				}
			} catch (ApplicationException e) {
				// exception handling ...
				Log.Error("Problem using the credentials dialog", e);
			}
		}

		public void Logout() {
			if (_credentials != null) {
				_jira.logout(_credentials);
				_credentials = null;
				_loggedIn = false;
			}
		}

		private void CheckCredentials() {
			if (_loggedIn) {
				if (_loggedInTime.AddMinutes(_timeout-1).CompareTo(DateTime.Now) < 0) {
					Logout();
					Login();
				}
			} else {
				Login();
			}
		}

		public bool IsLoggedIn {
			get {
				return _loggedIn;
			}
		}

		public JiraFilter[] GetFilters() {
			List<JiraFilter> filters = new List<JiraFilter>();
			CheckCredentials();
			RemoteFilter[] remoteFilters = _jira.getSavedFilters(_credentials);
			foreach (RemoteFilter remoteFilter in remoteFilters) {
				filters.Add(new JiraFilter(remoteFilter.name, remoteFilter.id));
			}
			return filters.ToArray();
		}
		
		private JiraIssue CreateDummyErrorIssue(Exception e) {
			// Creating bogus jira to indicate a problem
			return new JiraIssue("error", DateTime.Now, "error", "error", "error", e.Message, "error", "error", null);
		}

		public JiraIssue GetIssue(string key) {
			JiraIssue jiraIssue = null;
			if (_jiraCache.Contains(key)) {
				jiraIssue = _jiraCache[key];
			}
			if (jiraIssue == null) {
				CheckCredentials();
				try {
					RemoteIssue issue = _jira.getIssue(_credentials, key);
					jiraIssue = new JiraIssue(issue.key, issue.created, GetUserFullName(issue.reporter), GetUserFullName(issue.assignee), issue.project, issue.summary, issue.description, issue.environment, issue.attachmentNames);
					_jiraCache.Add(key, jiraIssue);
				} catch (Exception e) {
					Log.Error("Problem retrieving Jira: " + key, e);
				}
			}
			return jiraIssue;
		}

		public JiraIssue[] GetIssuesForFilter(string filterId) {
			List<JiraIssue> issuesToReturn = new List<JiraIssue>();
			CheckCredentials();
			try {
				RemoteIssue[] issues = _jira.getIssuesFromFilter(_credentials, filterId);

				#region Username cache update
				List<string> users = new List<string>();
				foreach (RemoteIssue issue in issues) {
					if (issue.reporter != null && !HasUser(issue.reporter) && !users.Contains(issue.reporter)) {
						users.Add(issue.reporter);
					}
					if (issue.assignee != null && !HasUser(issue.assignee) && !users.Contains(issue.assignee)) {
						users.Add(issue.assignee);
					}
				}
				int taskCount = users.Count;
				if (taskCount > 0) {
					ManualResetEvent doneEvent = new ManualResetEvent(false);
					for (int i = 0; i < users.Count; i++) {
						ThreadPool.QueueUserWorkItem(delegate(object name) {
							Log.InfoFormat("Retrieving {0}", name);
							GetUserFullName((string)name);
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
						JiraIssue jiraIssue = new JiraIssue(issue.key, issue.created, GetUserFullName(issue.reporter), GetUserFullName(issue.assignee), issue.project, issue.summary, issue.description, "", issue.attachmentNames);
						issuesToReturn.Add(jiraIssue);
					} catch (Exception e) {
						Log.Error("Problem retrieving Jira: " + issue.key, e);
						JiraIssue jiraIssue = CreateDummyErrorIssue(e);
						jiraIssue.Key = issue.key;
						issuesToReturn.Add(jiraIssue);
					}
				}
			} catch (Exception e) {
				Log.Error("Problem retrieving Jiras for Filter: " + filterId, e);
				issuesToReturn.Add(CreateDummyErrorIssue(e));
			}
			return issuesToReturn.ToArray(); ;
		}

		public string GetUrl(string issueKey) {
			return _url.Replace(DefaultPostfix,"") + "/browse/" + issueKey;
		}

		public void AddAttachment(string issueKey, string filename, IBinaryContainer attachment) {
			CheckCredentials();
			try {
				_jira.addBase64EncodedAttachmentsToIssue(_credentials, issueKey, new[] { filename }, new[] { attachment.ToBase64String(Base64FormattingOptions.InsertLineBreaks) });
			} catch (Exception ex1) {
				Log.WarnFormat("Failed to upload by using method addBase64EncodedAttachmentsToIssue, error was {0}", ex1.Message);
				try {
					Log.Warn("Trying addAttachmentsToIssue instead");
					_jira.addAttachmentsToIssue(_credentials, issueKey, new[] { filename }, (sbyte[])(Array)attachment.ToByteArray());
				} catch (Exception ex2) {
					Log.WarnFormat("Failed to use alternative method, error was: {0}", ex2.Message);
					throw;
				}
			}
		}

		public void AddComment(string issueKey, string commentString) {
			RemoteComment comment = new RemoteComment
			{
				body = commentString
			};
			CheckCredentials();
			_jira.addComment(_credentials, issueKey, comment);
		}

		private bool HasUser(string user) {
			if (user != null) {
				return _userCache.Contains(user);
			}
			return false;
		}

		private string GetUserFullName(string user) {
			string fullname;
			if (user != null) {
				if (_userCache.Contains(user)) {
					fullname = _userCache[user].fullname;
				} else {
					CheckCredentials();
					RemoteUser remoteUser = _jira.getUser(_credentials, user);
					_userCache.Add(user, remoteUser);
					fullname = remoteUser.fullname;
				}
			} else {
				fullname = "Not assigned";
			}
			return fullname;
		}
	}
}