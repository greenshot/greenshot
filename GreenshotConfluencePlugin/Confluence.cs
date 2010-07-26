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

using GreenshotConfluencePlugin;
using Greenshot.Core;

/// <summary>
/// For details see the Confluence API site
/// See: http://confluence.atlassian.com/display/CONFDEV/Remote+API+Specification
/// </summary>
namespace Confluence {
	public class Page {
		public Page(RemotePage page) {
			id = page.id;
		}
		public long id {
			get;
			set;
		}
	}
	public class ConfluenceConnector {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ConfluenceConnector));
		private const string CONFLUENCE_URL_PROPERTY = "url";
		private const string CONFLUENCE_USER_PROPERTY = "user";
		private const string CONFLUENCE_PASSWORD_PROPERTY = "password";
        private const int DEFAULT_TIMEOUT = 29;
		public const string CONFIG_FILENAME = "confluence.properties";
		private const string DEFAULT_CONFLUENCE_URL = "http://confluence/rpc/soap-axis/confluenceservice-v1?wsdl";
		private string configurationPath = null;
        private string credentials = null;
        private DateTime loggedInTime = DateTime.Now;
        private bool loggedIn = false;
        private string tmpPassword = null;
        private Properties config;
        private ConfluenceSoapServiceService confluence;
        private Dictionary<string, string> userMap = new Dictionary<string, string>();

        public ConfluenceConnector(string configurationPath) {
        	this.configurationPath = configurationPath;
        	this.config = LoadConfig();
            confluence = new ConfluenceSoapServiceService();
            confluence.Url = config.GetProperty(CONFLUENCE_URL_PROPERTY);
        }

        ~ConfluenceConnector() {
            logout();
        }

        public bool HasPassword() {
        	return config.ContainsKey(CONFLUENCE_PASSWORD_PROPERTY);
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
			if (!config.ContainsKey(CONFLUENCE_URL_PROPERTY)) {
				config.AddProperty(CONFLUENCE_URL_PROPERTY, DEFAULT_CONFLUENCE_URL);
				changed = true;
			}
			if (!config.ContainsKey(CONFLUENCE_USER_PROPERTY)) {
				config.AddProperty(CONFLUENCE_USER_PROPERTY, Environment.UserName);
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
			comment.AppendLine("# The configuration file for the Confluence Plugin");
			comment.AppendLine("#");
			comment.AppendLine("# Example settings:");
			comment.AppendLine("# " + CONFLUENCE_URL_PROPERTY + "=" + DEFAULT_CONFLUENCE_URL);
			comment.AppendLine("# " + CONFLUENCE_USER_PROPERTY + "=Username");
			config.write(filename, comment.ToString());
		}

        public void login() {
            logout();
            try {
	            if (HasPassword()) {
	            	this.credentials = confluence.login(config.GetProperty(CONFLUENCE_USER_PROPERTY), config.GetProperty(CONFLUENCE_PASSWORD_PROPERTY));
	            } else if (tmpPassword != null) {
	            	this.credentials = confluence.login(config.GetProperty(CONFLUENCE_USER_PROPERTY), tmpPassword);
	            } else {
	            	LoginForm pwForm = new LoginForm();
	            	pwForm.User = config.GetProperty(CONFLUENCE_USER_PROPERTY);
	            	pwForm.Url = config.GetProperty(CONFLUENCE_URL_PROPERTY);
	            	DialogResult result = pwForm.ShowDialog();
	            	if (result == DialogResult.OK) {
		            	tmpPassword = pwForm.Password;
		            	if (!pwForm.User.Equals(config.GetProperty(CONFLUENCE_USER_PROPERTY)) ||!pwForm.Url.Equals(config.GetProperty(CONFLUENCE_URL_PROPERTY))) {
		            		config.ChangeProperty(CONFLUENCE_USER_PROPERTY, pwForm.User);
		            		config.ChangeProperty(CONFLUENCE_URL_PROPERTY, pwForm.Url);
		            		confluence.Url = config.GetProperty(CONFLUENCE_URL_PROPERTY);
		            		SaveConfig(config);
		            	}
	            		this.credentials = confluence.login(config.GetProperty(CONFLUENCE_USER_PROPERTY), tmpPassword);
	            	} else {
		            	throw new Exception("User pressed cancel!");
	            	}
	            }
				this.loggedInTime = DateTime.Now;
				this.loggedIn = true;
            } catch (Exception e) {
            	this.loggedIn = false;
            	this.credentials = null;
            	e.Data.Add("user",config.GetProperty(CONFLUENCE_USER_PROPERTY));
            	e.Data.Add("url",config.GetProperty(CONFLUENCE_URL_PROPERTY));
            	if (e.Message.Contains("com.atlassian.confluence.rpc.AuthenticationFailedException")) {
            		// Login failed due to wrong user or password, password should be removed!
	            	this.tmpPassword = null;
            		throw new Exception(e.Message.Replace("com.atlassian.confluence.rpc.AuthenticationFailedException: ",""));
            	}
            	throw e;
            }
        }

        public void logout() {
            if (credentials != null) {
                confluence.logout(credentials);
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
        
        public void addAttachment(long pageId, string mime, string comment, string filename, byte[] buffer) {
        	checkCredentials();
			RemoteAttachment attachment = new RemoteAttachment();
			// Comment is ignored, see: http://jira.atlassian.com/browse/CONF-9395
			attachment.comment = comment;
			attachment.fileName = filename;
			attachment.contentType = mime;
			confluence.addAttachment(credentials, pageId, attachment, buffer);
        }
        
        public Page getPage(string spaceKey, string pageTitle) {
        	checkCredentials();
        	RemotePage page = confluence.getPage(credentials, spaceKey, pageTitle);
        	return new Page(page);
        }
    }
}
