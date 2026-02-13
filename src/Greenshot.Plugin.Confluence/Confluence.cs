/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Confluence;
using Dapplo.Confluence.Entities;
using Dapplo.Confluence.Query;
using Greenshot.Base.Core;
using Greenshot.Base.IniFile;

namespace Greenshot.Plugin.Confluence;

/// <summary>
/// Confluence connector using REST API via Dapplo.Confluence
/// See: https://docs.atlassian.com/ConfluenceServer/rest/
/// </summary>
public class ConfluenceConnector : IDisposable
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ConfluenceConnector));
    private static readonly ConfluenceConfiguration Config = IniConfig.GetIniSection<ConfluenceConfiguration>();
    private DateTime _loggedInTime = DateTime.Now;
    private bool _loggedIn;
    private IConfluenceClient _confluence;
    private readonly int _timeout;
    private string _url;
    private readonly Cache<string, Content> _pageCache = new Cache<string, Content>(60 * Config.Timeout);

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
            _confluence = null;
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
        var baseUri = new Uri(url);
        // Set up proxy if needed
        _confluence = ConfluenceClient.Create(baseUri);
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
            _confluence.SetBasicAuthentication(user, password);
            
            // Test the credentials by getting current user info
            var testTask = Task.Run(async () => await _confluence.User.GetCurrentUserAsync());
            testTask.Wait();
            
            _loggedInTime = DateTime.Now;
            _loggedIn = true;
        }
        catch (Exception e)
        {
            // Check if auth failed
            if (e.InnerException != null && (e.InnerException.Message.Contains("401") || e.InnerException.Message.Contains("Unauthorized")))
            {
                return false;
            }

            // Not an authentication issue
            _loggedIn = false;
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
            string systemName = _url;
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
        if (_loggedIn)
        {
            _confluence = null;
            Init(_url);
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
        
        var attachTask = Task.Run(async () =>
        {
            using var stream = new System.IO.MemoryStream(image.ToByteArray());
            await _confluence.Attachment.AttachAsync(pageId, stream, filename, comment, mime);
        });
        
        attachTask.Wait();
    }

    public Entities.Page GetPage(string spaceKey, string pageTitle)
    {
        Content page = null;
        string cacheKey = spaceKey + pageTitle;
        if (_pageCache.Contains(cacheKey))
        {
            page = _pageCache[cacheKey];
        }

        if (page == null)
        {
            CheckCredentials();
            
            var pageTask = Task.Run(async () =>
            {
                var query = Where.And(Where.Type.IsPage, Where.Title.Is(pageTitle), Where.Space.Is(spaceKey));
                var searchResult = await _confluence.Content.SearchAsync(query, pagingInformation: new PagingInformation
                {
                    Limit = 1
                });
                return searchResult.Results.FirstOrDefault();
            });
            
            page = pageTask.Result;
            
            if (page != null)
            {
                // Get full page details with body
                var detailTask = Task.Run(async () => 
                    await _confluence.Content.GetAsync(page, ConfluenceClientConfig.ExpandGetContentWithStorage));
                page = detailTask.Result;
                _pageCache.Add(cacheKey, page);
            }
        }

        return page != null ? new Entities.Page(page) : null;
    }

    public Entities.Page GetPage(long pageId)
    {
        Content page = null;
        string cacheKey = pageId.ToString();

        if (_pageCache.Contains(cacheKey))
        {
            page = _pageCache[cacheKey];
        }

        if (page == null)
        {
            CheckCredentials();
            
            var pageTask = Task.Run(async () =>
                await _confluence.Content.GetAsync(pageId, ConfluenceClientConfig.ExpandGetContentWithStorage));
                
            page = pageTask.Result;
            _pageCache.Add(cacheKey, page);
        }

        return new Entities.Page(page);
    }

    public Entities.Page GetSpaceHomepage(Entities.Space spaceSummary)
    {
        CheckCredentials();
        
        var homePageTask = Task.Run(async () =>
        {
            var space = await _confluence.Space.GetAsync(spaceSummary.Key);
            if (space.HomepageId != 0)
            {
                return await _confluence.Content.GetAsync(space.HomepageId, ConfluenceClientConfig.ExpandGetContentWithStorage);
            }
            return null;
        });
        
        var page = homePageTask.Result;
        return page != null ? new Entities.Page(page) : null;
    }

    public IEnumerable<Entities.Space> GetSpaceSummaries()
    {
        CheckCredentials();
        
        var spacesTask = Task.Run(async () =>
        {
            var allSpaces = new List<Dapplo.Confluence.Entities.Space>();
            var spacesResult = await _confluence.Space.GetAllAsync();
            
            foreach (var space in spacesResult)
            {
                allSpaces.Add(space);
            }
            
            return allSpaces;
        });
        
        var spaces = spacesTask.Result;
        
        foreach (var space in spaces)
        {
            yield return new Entities.Space(space);
        }
    }

    public IEnumerable<Entities.Page> GetPageChildren(Entities.Page parentPage)
    {
        CheckCredentials();
        
        var childrenTask = Task.Run(async () =>
        {
            var children = new List<Content>();
            var childrenResult = await _confluence.Content.GetChildrenAsync(parentPage.Id);
            
            if (childrenResult?.Results != null)
            {
                children.AddRange(childrenResult.Results);
            }
            
            return children;
        });
        
        var pages = childrenTask.Result;
        
        foreach (var page in pages)
        {
            yield return new Entities.Page(page);
        }
    }

    public IEnumerable<Entities.Page> GetPageSummaries(Entities.Space space)
    {
        CheckCredentials();
        
        var pagesTask = Task.Run(async () =>
        {
            var allPages = new List<Content>();
            var query = Where.And(Where.Type.IsPage, Where.Space.Is(space.Key));
            var searchResult = await _confluence.Content.SearchAsync(query);
            
            foreach (var page in searchResult.Results)
            {
                allPages.Add(page);
            }
            
            return allPages;
        });
        
        var pages = pagesTask.Result;
        
        foreach (var page in pages)
        {
            yield return new Entities.Page(page);
        }
    }

    public IEnumerable<Entities.Page> SearchPages(string query, string space)
    {
        CheckCredentials();
        
        var searchTask = Task.Run(async () =>
        {
            var allPages = new List<Content>();
            IFinalClause whereClause;
            
            if (!string.IsNullOrEmpty(space))
            {
                whereClause = Where.And(Where.Type.IsPage, Where.Text.Contains(query), Where.Space.Is(space));
            }
            else
            {
                whereClause = Where.And(Where.Type.IsPage, Where.Text.Contains(query));
            }
            
            var searchResult = await _confluence.Content.SearchAsync(whereClause, pagingInformation: new PagingInformation { Limit = 20 });
            
            foreach (var page in searchResult.Results)
            {
                Log.DebugFormat("Got result of type {0}", page.Type);
                if (page.Type == ContentTypes.Page)
                {
                    allPages.Add(page);
                }
            }
            
            return allPages;
        });
        
        var pages = searchTask.Result;
        
        foreach (var page in pages)
        {
            yield return new Entities.Page(page);
        }
    }
}