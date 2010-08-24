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
        private ConfluenceConfiguration config;
        private ConfluenceSoapServiceService confluence;
        private Dictionary<string, string> userMap = new Dictionary<string, string>();

        public ConfluenceConnector(string configurationPath) {
        	this.config = IniConfig.GetIniSection<ConfluenceConfiguration>();
            confluence = new ConfluenceSoapServiceService();
            confluence.Url = config.Url;
        }

        ~ConfluenceConnector() {
            logout();
        }

        public void login() {
            logout();
            try {
	            if (config.HasPassword()) {
	            	this.credentials = confluence.login(config.User, config.Password);
            	} else if (config.HasTmpPassword()) {
	            	this.credentials = confluence.login(config.User, config.TmpPassword);
	            } else {
        			if (config.ShowConfigDialog()) {
        				if (config.HasPassword()) {
	            			this.credentials = confluence.login(config.User, config.Password);
	            		} else if (config.HasTmpPassword()) {
	            			this.credentials = confluence.login(config.User, config.TmpPassword);
        				}
	            	} else {
		            	throw new Exception("User pressed cancel!");
	            	}
	            }
				this.loggedInTime = DateTime.Now;
				this.loggedIn = true;
            } catch (Exception e) {
            	this.loggedIn = false;
            	this.credentials = null;
            	e.Data.Add("user",config.User);
            	e.Data.Add("url",config.Url);
            	if (e.Message.Contains(AUTH_FAILED_EXCEPTION_NAME)) {
            		// Login failed due to wrong user or password, password should be removed!
	            	config.Password = null;
	            	config.TmpPassword = null;
            		throw new Exception(e.Message.Replace(AUTH_FAILED_EXCEPTION_NAME+ ": ",""));
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
				if (loggedInTime.AddMinutes(config.Timeout-1).CompareTo(DateTime.Now) < 0) {
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
