/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Windows.Forms;
using Greenshot.IniFile;
using GreenshotConfluencePlugin;
using GreenshotConfluencePlugin.confluence;
using GreenshotPlugin.Core;

/// <summary>
/// For details see the Confluence API site
/// See: http://confluence.atlassian.com/display/CONFDEV/Remote+API+Specification
/// </summary>
namespace Confluence {
	#region transport classes
	public class Page {
		public Page(RemotePage page) {
			id = page.id;
			Title = page.title;
			SpaceKey = page.space;
			Url = page.url;
			Content = page.content;
		}
		public Page(RemoteSearchResult searchResult, string space) {
			id = searchResult.id;
			Title = searchResult.title;
			SpaceKey = space;
			Url = searchResult.url;
			Content = searchResult.excerpt;
		}
		
		public Page(RemotePageSummary pageSummary) {
			id = pageSummary.id;
			Title = pageSummary.title;
			SpaceKey = pageSummary.space;
			Url =pageSummary.url;
		}
		public long id {
			get;
			set;
		}
		public string Title {
			get;
			set;
		}
		public string Url {
			get;
			set;
		}
		public string Content {
			get;
			set;
		}
		public string SpaceKey {
			get;
			set;
		}
	}
	public class Space {
		public Space(RemoteSpaceSummary space) {
			Key = space.key;
			Name = space.name;
		}
		public string Key {
			get;
			set;
		}
		public string Name {
			get;
			set;
		}
	}
	#endregion

	public class ConfluenceConnector : IDisposable {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ConfluenceConnector));
		private const string AUTH_FAILED_EXCEPTION_NAME = "com.atlassian.confluence.rpc.AuthenticationFailedException";
        private const string V2_FAILED = "AXIS";
        private static ConfluenceConfiguration config = IniConfig.GetIniSection<ConfluenceConfiguration>();
		private string credentials = null;
		private DateTime loggedInTime = DateTime.Now;
		private bool loggedIn = false;
		private ConfluenceSoapServiceService confluence;
		private int timeout;
		private string url;
		private Cache<string, RemotePage> pageCache = new Cache<string, RemotePage>(60 * config.Timeout);

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) {
			if (confluence != null) {
				logout();
			}
			if (disposing) {
				if (confluence != null) {
					confluence.Dispose();
					confluence = null;
				}
			}
		}

		public ConfluenceConnector(string url, int timeout) {
			this.timeout = timeout;
            init(url);
		}

        private void init(string url) {
            this.url = url;
            confluence = new ConfluenceSoapServiceService();
            confluence.Url = url;
            confluence.Proxy = NetworkHelper.CreateProxy(new Uri(url));
        }

		~ConfluenceConnector() {
			Dispose(false);
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
                // Check if confluence-v2 caused an error, use v1 instead
                if (e.Message.Contains(V2_FAILED) && url.Contains("v2")) {
                    init(url.Replace("v2", "v1"));
                    return doLogin(user, password);
                }
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
			logout();
			try {
				// Get the system name, so the user knows where to login to
				string systemName = url.Replace(ConfluenceConfiguration.DEFAULT_POSTFIX1,"");
                systemName = url.Replace(ConfluenceConfiguration.DEFAULT_POSTFIX2, "");
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

		public bool isLoggedIn {
			get {
				return loggedIn;
			}
		}
		
		public void addAttachment(long pageId, string mime, string comment, string filename, IBinaryContainer image) {
			checkCredentials();
			RemoteAttachment attachment = new RemoteAttachment();
			// Comment is ignored, see: http://jira.atlassian.com/browse/CONF-9395
			attachment.comment = comment;
			attachment.fileName = filename;
			attachment.contentType = mime;
			confluence.addAttachment(credentials, pageId, attachment, image.ToByteArray());
		}
		
		public Page getPage(string spaceKey, string pageTitle) {
			RemotePage page = null;
			string cacheKey = spaceKey + pageTitle;
			if (pageCache.Contains(cacheKey)) {
				page = pageCache[cacheKey];
			}
			if (page == null) {
				checkCredentials();
				page = confluence.getPage(credentials, spaceKey, pageTitle);
				pageCache.Add(cacheKey, page);
			}
			return new Page(page);
		}

		public Page getPage(long pageId) {
			RemotePage page = null;
			string cacheKey = "" + pageId;
			
			if (pageCache.Contains(cacheKey)) {
				page = pageCache[cacheKey];
			}
			if (page == null) {
				checkCredentials();
				page = confluence.getPage(credentials, pageId);
				pageCache.Add(cacheKey, page);
			}
			return new Page(page);
		}

		public Page getSpaceHomepage(Space spaceSummary) {
			checkCredentials();
			RemoteSpace spaceDetail = confluence.getSpace(credentials, spaceSummary.Key);
			RemotePage page = confluence.getPage(credentials, spaceDetail.homePage);
			return new Page(page);
		}

		public List<Space> getSpaceSummaries() {
			checkCredentials();
			RemoteSpaceSummary [] spaces = confluence.getSpaces(credentials);
			List<Space> returnSpaces = new List<Space>();
			foreach(RemoteSpaceSummary space in spaces) {
				returnSpaces.Add(new Space(space));
			}
			returnSpaces.Sort((x, y) => string.Compare(x.Name, y.Name));
			return returnSpaces;
		}
		
		public List<Page> getPageChildren(Page parentPage) {
			checkCredentials();
			List<Page> returnPages = new List<Page>();
			RemotePageSummary[] pages = confluence.getChildren(credentials, parentPage.id);
			foreach(RemotePageSummary page in pages) {
				returnPages.Add(new Page(page));
			}
			returnPages.Sort((x, y) => string.Compare(x.Title, y.Title));
			return returnPages;
		}

		public List<Page> getPageSummaries(Space space) {
			checkCredentials();
			List<Page> returnPages = new List<Page>();
			RemotePageSummary[] pages = confluence.getPages(credentials, space.Key);
			foreach(RemotePageSummary page in pages) {
				returnPages.Add(new Page(page));
			}
			returnPages.Sort((x, y) => string.Compare(x.Title, y.Title));
			return returnPages;
		}
		
		public List<Page> searchPages(string query, string space) {
			checkCredentials();
			List<Page> results = new List<Page>();
			foreach(RemoteSearchResult searchResult in confluence.search(credentials, query, 20)) {
				LOG.DebugFormat("Got result of type {0}", searchResult.type);
				if ("page".Equals(searchResult.type)) {
					results.Add(new Page(searchResult, space));
				}
			}
			return results;
		}
	}
}
