/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2010  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Text;
using System.Windows.Forms;

using GreenshotJiraPlugin;
using GreenshotPlugin.Core;

namespace Jira {
	#region transport classes
    public class JiraFilter {
        public JiraFilter(string name, string id) {
            this.name = name;
            this.id = id;
        }
        public string name {
            get;
            set;
        }
        public string id;
    }

    public class JiraIssue {
        public JiraIssue(string key, DateTime? created, string reporter, string assignee, string project, string summary, string description, string environment, string [] attachmentNames) {
            this.key = key;
            this.created = created;
            this.reporter = reporter;
            this.assignee = assignee;
            this.project = project;
            this.summary = summary;
            this.description = description;
            this.environment = environment;
            this.attachmentNames = attachmentNames;
        }
        public string key {
            get;
            private set;
        }
        public DateTime? created {
            get;
            private set;
        }
        public string reporter {
            get;
            private set;
        }
        public string assignee {
            get;
            private set;
        }
        public string project {
            get;
            private set;
        }
        public string summary {
            get;
            private set;
        }
        public string description {
            get;
            private set;
        }
        public string environment {
            get;
            private set;
        }
        public string[] attachmentNames {
            get;
            private set;
        }
    }
	#endregion

	public class JiraConnector {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(JiraConnector));
		private const string JIRA_URL_PROPERTY = "url";
		private const string JIRA_USER_PROPERTY = "user";
		private const string JIRA_PASSWORD_PROPERTY = "password";
        private const int DEFAULT_TIMEOUT = 29;
		public const string CONFIG_FILENAME = "jira.properties";
		private const string DEFAULT_JIRA_URL = "http://jira/rpc/soap/jirasoapservice-v2?wsdl";
		private const string AUTH_FAILED_EXCEPTION_NAME = "com.atlassian.jira.rpc.exception.RemoteAuthenticationException";
		private string configurationPath = null;
        private string credentials = null;
        private DateTime loggedInTime = DateTime.Now;
        private bool loggedIn = false;
        private string tmpPassword = null;
        private Properties config;
        private JiraSoapServiceService jira;
        private Dictionary<string, string> userMap = new Dictionary<string, string>();

        public JiraConnector(string configurationPath) {
        	this.configurationPath = configurationPath;
        	this.config = LoadConfig();
            jira = new JiraSoapServiceService();
            jira.Url = config.GetProperty(JIRA_URL_PROPERTY);
        }

        ~JiraConnector() {
            logout();
        }

        public bool HasPassword() {
        	return config.ContainsKey(JIRA_PASSWORD_PROPERTY);
        }
        
        public void SetTmpPassword(string password) {
        	tmpPassword = password;
        }

		private Properties LoadConfig() {
			Properties config = null;
			string filename = Path.Combine(configurationPath, CONFIG_FILENAME);
			if (File.Exists(filename)) {
				LOG.Debug("Loading configuration from: " + filename);
				config = Properties.read(filename);
			}
			bool changed = false;
			if (config == null) {
				config = new Properties();
				changed = true;
			}
			if (!config.ContainsKey(JIRA_URL_PROPERTY)) {
				config.AddProperty(JIRA_URL_PROPERTY, DEFAULT_JIRA_URL);
				changed = true;
			}
			if (!config.ContainsKey(JIRA_USER_PROPERTY)) {
				config.AddProperty(JIRA_USER_PROPERTY, Environment.UserName);
				changed = true;
			}
			if (changed) {
				SaveConfig(config);
			}
			return config;
		}

		private void SaveConfig(Properties config) {
			string filename = Path.Combine(configurationPath, CONFIG_FILENAME);
			LOG.Debug("Saving configuration to: " + filename);
			StringBuilder comment = new StringBuilder();
			comment.AppendLine("# The configuration file for the JIRA Plugin");
			comment.AppendLine("#");
			comment.AppendLine("# Example settings:");
			comment.AppendLine("# " + JIRA_URL_PROPERTY + "=" + DEFAULT_JIRA_URL);
			comment.AppendLine("# " + JIRA_USER_PROPERTY + "=Username");
			config.write(filename, comment.ToString());
		}
        
        public void login() {
        	logout();
            try {
	            if (HasPassword()) {
	            	this.credentials = jira.login(config.GetProperty(JIRA_USER_PROPERTY), config.GetProperty(JIRA_PASSWORD_PROPERTY));
	            } else if (tmpPassword != null) {
	            	this.credentials = jira.login(config.GetProperty(JIRA_USER_PROPERTY), tmpPassword);
	            } else {
	            	LoginForm pwForm = new LoginForm();
	            	pwForm.User = config.GetProperty(JIRA_USER_PROPERTY);
	            	pwForm.Url = config.GetProperty(JIRA_URL_PROPERTY);
	            	DialogResult result = pwForm.ShowDialog();
	            	if (result == DialogResult.OK) {
		            	tmpPassword = pwForm.Password;
		            	if (!pwForm.User.Equals(config.GetProperty(JIRA_USER_PROPERTY)) ||!pwForm.Url.Equals(config.GetProperty(JIRA_URL_PROPERTY))) {
		            		config.ChangeProperty(JIRA_USER_PROPERTY, pwForm.User);
		            		config.ChangeProperty(JIRA_URL_PROPERTY, pwForm.Url);
		            		jira.Url = config.GetProperty(JIRA_URL_PROPERTY);
		            		SaveConfig(config);
		            	}
	            		this.credentials = jira.login(config.GetProperty(JIRA_USER_PROPERTY), tmpPassword);
	            	} else {
		            	throw new Exception("User pressed cancel!");
	            	}

	            }
				this.loggedInTime = DateTime.Now;
				this.loggedIn = true;
            } catch (Exception e) {
            	this.loggedIn = false;
            	this.credentials = null;
            	e.Data.Add("user",config.GetProperty(JIRA_USER_PROPERTY));
            	e.Data.Add("url",config.GetProperty(JIRA_URL_PROPERTY));
            	if (e.Message.Contains(AUTH_FAILED_EXCEPTION_NAME)) {
            		// Login failed due to wrong user or password, password should be removed!
	            	this.tmpPassword = null;
            		throw new Exception(e.Message.Replace(AUTH_FAILED_EXCEPTION_NAME+ ": ",""));
            	}
            	throw e;
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
				if (loggedInTime.AddMinutes(DEFAULT_TIMEOUT).CompareTo(DateTime.Now) < 0) {
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

        public JiraIssue[] getIssuesForFilter(string filterId) {
            List<JiraIssue> issuesToReturn = new List<JiraIssue>();
            checkCredentials();
            RemoteIssue[] issues = jira.getIssuesFromFilter(credentials, filterId);
            foreach (RemoteIssue issue in issues) {
                JiraIssue jiraIssue = new JiraIssue(issue.key, issue.created, getUserFullName(issue.reporter), getUserFullName(issue.assignee), issue.project, issue.summary, issue.description, issue.environment, issue.attachmentNames);
                issuesToReturn.Add(jiraIssue);
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
