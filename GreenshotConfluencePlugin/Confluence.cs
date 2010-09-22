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

using Greenshot.Core;
using Greenshot.Helpers;
using GreenshotConfluencePlugin;

/// <summary>
/// For details see the Confluence API site
/// See: http://confluence.atlassian.com/display/CONFDEV/Remote+API+Specification
/// </summary>
namespace Confluence {
	#region transport classes
	public class Page {
		public Page(RemotePage page) {
			id = page.id;
		}
		public long id {
			get;
			set;
		}
	}
	#endregion

	public class ConfluenceConnector {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ConfluenceConnector));
		private const string AUTH_FAILED_EXCEPTION_NAME = "com.atlassian.confluence.rpc.AuthenticationFailedException";

        private string credentials = null;
        private DateTime loggedInTime = DateTime.Now;
        private bool loggedIn = false;
        private ConfluenceSoapServiceService confluence;
        private int timeout;
        private string url;
        private Dictionary<string, string> userMap = new Dictionary<string, string>();

        public ConfluenceConnector(string url, int timeout) {
        	this.url = url;
        	this.timeout = timeout;
            confluence = new ConfluenceSoapServiceService();
            confluence.Url = url;
        }

        ~ConfluenceConnector() {
            logout();
        }
        /// <summary>
        /// Internal login which catches the exceptions
        /// </summary>
        /// <returns>true if login was done sucessfully</returns>
        private bool doLogin(string user, string password) {
        	try {
	        	this.credentials = confluence.login(user, password);
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
            	throw e;
            }
        	return true;
        }

        public void login() {
            logout();
            try {
        		// Get the system name, so the user knows where to login to
        		string systemName = url.Replace(ConfluenceConfiguration.DEFAULT_POSTFIX,"");
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
                confluence.logout(credentials);
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
