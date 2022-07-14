/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Greenshot.Base.Core;
using Greenshot.Base.IniFile;
using Greenshot.Plugin.Confluence.Entities;
using GreenshotConfluencePlugin.confluence;

namespace Greenshot.Plugin.Confluence
{
    /// <summary>
    /// For details see the Confluence API site
    /// See: https://confluence.atlassian.com/display/CONFDEV/Remote+API+Specification
    /// </summary>
    public class ConfluenceConnector : IDisposable
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ConfluenceConnector));
        private const string AuthFailedExceptionName = "com.atlassian.confluence.rpc.AuthenticationFailedException";
        private const string V2Failed = "AXIS";
        private static readonly ConfluenceConfiguration Config = IniConfig.GetIniSection<ConfluenceConfiguration>();
        private string _credentials;
        private DateTime _loggedInTime = DateTime.Now;
        private bool _loggedIn;
        private ConfluenceSoapServiceService _confluence;
        private readonly int _timeout;
        private string _url;
        private readonly Cache<string, RemotePage> _pageCache = new(60 * Config.Timeout);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_confluence != null)
            {
                Logout();
            }

            if (disposing)
            {
                if (_confluence != null)
                {
                    _confluence.Dispose();
                    _confluence = null;
                }
            }
        }

        public ConfluenceConnector(string url, int timeout)
        {
            _timeout = timeout;
            Init(url);
        }

        private void Init(string url)
        {
            _url = url;
            _confluence = new ConfluenceSoapServiceService
            {
                Url = url,
                Proxy = NetworkHelper.CreateProxy(new Uri(url))
            };
        }

        ~ConfluenceConnector()
        {
            Dispose(false);
        }

        /// <summary>
        /// Internal login which catches the exceptions
        /// </summary>
        /// <returns>true if login was done sucessfully</returns>
        private bool DoLogin(string user, string password)
        {
            try
            {
                _credentials = _confluence.login(user, password);
                _loggedInTime = DateTime.Now;
                _loggedIn = true;
            }
            catch (Exception e)
            {
                // Check if confluence-v2 caused an error, use v1 instead
                if (e.Message.Contains(V2Failed) && _url.Contains("v2"))
                {
                    Init(_url.Replace("v2", "v1"));
                    return DoLogin(user, password);
                }

                // check if auth failed
                if (e.Message.Contains(AuthFailedExceptionName))
                {
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

        public void Login()
        {
            Logout();
            try
            {
                // Get the system name, so the user knows where to login to
                string systemName = _url.Replace(ConfluenceConfiguration.DEFAULT_POSTFIX1, "");
                systemName = systemName.Replace(ConfluenceConfiguration.DEFAULT_POSTFIX2, "");
                var dialog = new CredentialsDialog(systemName)
                {
                    Name = null
                };
                while (dialog.Show(dialog.Name) == DialogResult.OK)
                {
                    if (DoLogin(dialog.Name, dialog.Password))
                    {
                        if (dialog.SaveChecked)
                        {
                            dialog.Confirm(true);
                        }

                        return;
                    }
                    else
                    {
                        try
                        {
                            dialog.Confirm(false);
                        }
                        catch (ApplicationException e)
                        {
                            // exception handling ...
                            Log.Error("Problem using the credentials dialog", e);
                        }

                        // For every windows version after XP show an incorrect password baloon
                        dialog.IncorrectPassword = true;
                        // Make sure the dialog is display, the password was false!
                        dialog.AlwaysDisplay = true;
                    }
                }
            }
            catch (ApplicationException e)
            {
                // exception handling ...
                Log.Error("Problem using the credentials dialog", e);
            }
        }

        public void Logout()
        {
            if (_credentials != null)
            {
                _confluence.logout(_credentials);
                _credentials = null;
                _loggedIn = false;
            }
        }

        private void CheckCredentials()
        {
            if (_loggedIn)
            {
                if (_loggedInTime.AddMinutes(_timeout - 1).CompareTo(DateTime.Now) < 0)
                {
                    Logout();
                    Login();
                }
            }
            else
            {
                Login();
            }
        }

        public bool IsLoggedIn => _loggedIn;

        public void AddAttachment(long pageId, string mime, string comment, string filename, IBinaryContainer image)
        {
            CheckCredentials();
            // Comment is ignored, see: https://jira.atlassian.com/browse/CONF-9395
            var attachment = new RemoteAttachment
            {
                comment = comment,
                fileName = filename,
                contentType = mime
            };
            _confluence.addAttachment(_credentials, pageId, attachment, image.ToByteArray());
        }

        public Page GetPage(string spaceKey, string pageTitle)
        {
            RemotePage page = null;
            string cacheKey = spaceKey + pageTitle;
            if (_pageCache.Contains(cacheKey))
            {
                page = _pageCache[cacheKey];
            }

            if (page == null)
            {
                CheckCredentials();
                page = _confluence.getPage(_credentials, spaceKey, pageTitle);
                _pageCache.Add(cacheKey, page);
            }

            return new Page(page);
        }

        public Page GetPage(long pageId)
        {
            RemotePage page = null;
            string cacheKey = pageId.ToString();

            if (_pageCache.Contains(cacheKey))
            {
                page = _pageCache[cacheKey];
            }

            if (page == null)
            {
                CheckCredentials();
                page = _confluence.getPage(_credentials, pageId);
                _pageCache.Add(cacheKey, page);
            }

            return new Page(page);
        }

        public Page GetSpaceHomepage(Space spaceSummary)
        {
            CheckCredentials();
            RemoteSpace spaceDetail = _confluence.getSpace(_credentials, spaceSummary.Key);
            RemotePage page = _confluence.getPage(_credentials, spaceDetail.homePage);
            return new Page(page);
        }

        public IEnumerable<Space> GetSpaceSummaries()
        {
            CheckCredentials();
            RemoteSpaceSummary[] spaces = _confluence.getSpaces(_credentials);
            foreach (RemoteSpaceSummary space in spaces)
            {
                yield return new Space(space);
            }
        }

        public IEnumerable<Page> GetPageChildren(Page parentPage)
        {
            CheckCredentials();
            RemotePageSummary[] pages = _confluence.getChildren(_credentials, parentPage.Id);
            foreach (RemotePageSummary page in pages)
            {
                yield return new Page(page);
            }
        }

        public IEnumerable<Page> GetPageSummaries(Space space)
        {
            CheckCredentials();
            RemotePageSummary[] pages = _confluence.getPages(_credentials, space.Key);
            foreach (RemotePageSummary page in pages)
            {
                yield return new Page(page);
            }
        }

        public IEnumerable<Page> SearchPages(string query, string space)
        {
            CheckCredentials();
            foreach (var searchResult in _confluence.search(_credentials, query, 20))
            {
                Log.DebugFormat("Got result of type {0}", searchResult.type);
                if ("page".Equals(searchResult.type))
                {
                    yield return new Page(searchResult, space);
                }
            }
        }
    }
}