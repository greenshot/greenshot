/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Confluence;
using Dapplo.Confluence.Entities;
using Dapplo.Confluence.Query;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Threading;
using Dapplo.Ini;

namespace Greenshot.Plugin.Confluence;

/// <summary>
/// Confluence connector using REST API via Dapplo.Confluence, everything is async (rule R12).
/// See: https://docs.atlassian.com/ConfluenceServer/rest/
/// </summary>
public class ConfluenceConnector : IDisposable
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ConfluenceConnector));
    private static readonly IConfluenceConfiguration Config = IniConfigRegistry.GetSection<IConfluenceConfiguration>();
    private readonly SemaphoreSlim _loginLock = new SemaphoreSlim(1, 1);
    private DateTime _loggedInTime = DateTime.Now;
    private volatile bool _loggedIn;
    private IConfluenceClient _confluence;
    private readonly int _timeout;
    private string _url;
    private readonly Cache<string, Content> _pageCache = new Cache<string, Content>(60 * Config.Timeout);
    private IList<Entities.Space> _spaces;
    private DateTime _spacesLoaded;

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
    /// Internal login which catches the authentication failure
    /// </summary>
    /// <returns>true if login was done successfully</returns>
    private async Task<bool> DoLoginAsync(string user, string password, CancellationToken cancellationToken)
    {
        try
        {
            _confluence.SetBasicAuthentication(user, password);

            // Test the credentials by getting current user info
            await _confluence.User.GetCurrentUserAsync(cancellationToken).ConfigureAwait(false);

            _loggedInTime = DateTime.Now;
            _loggedIn = true;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            // Check if auth failed
            if (e.InnerException != null && (e.InnerException.Message.Contains("401") || e.InnerException.Message.Contains("Unauthorized")))
            {
                return false;
            }

            // Not an authentication issue
            _loggedIn = false;
            e.Data["user"] = user;
            e.Data["url"] = _url;
            throw;
        }

        return true;
    }

    /// <summary>
    /// Ask the user for the credentials (on the UI thread) until the login worked or the user canceled.
    /// </summary>
    public async Task LoginAsync(CancellationToken cancellationToken = default)
    {
        Logout();
        var ui = UiDispatcher.Current;
        try
        {
            // Get the system name, so the user knows where to login to
            string systemName = _url;
            var dialog = new CredentialsDialog(systemName)
            {
                Name = null
            };
            while (await ui.InvokeAsync(() => dialog.Show(dialog.Name), cancellationToken).ConfigureAwait(false) == DialogResult.OK)
            {
                if (await DoLoginAsync(dialog.Name, dialog.Password, cancellationToken).ConfigureAwait(false))
                {
                    if (dialog.SaveChecked)
                    {
                        await ui.InvokeAsync(() => dialog.Confirm(true), cancellationToken).ConfigureAwait(false);
                    }

                    return;
                }

                try
                {
                    await ui.InvokeAsync(() => dialog.Confirm(false), cancellationToken).ConfigureAwait(false);
                }
                catch (ApplicationException e)
                {
                    // exception handling ...
                    Log.Error("Problem using the credentials dialog", e);
                }

                // For every windows version after XP show an incorrect password balloon
                dialog.IncorrectPassword = true;
                // Make sure the dialog is display, the password was false!
                dialog.AlwaysDisplay = true;
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

    /// <summary>
    /// Make sure the connector is logged in (asks the user when needed), one login at a time.
    /// </summary>
    /// <returns>true when logged in, false when the user canceled the login</returns>
    public async Task<bool> EnsureLoggedInAsync(CancellationToken cancellationToken = default)
    {
        await _loginLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_loggedIn && _loggedInTime.AddMinutes(_timeout - 1).CompareTo(DateTime.Now) < 0)
            {
                Logout();
            }

            if (!_loggedIn)
            {
                await LoginAsync(cancellationToken).ConfigureAwait(false);
            }

            return _loggedIn;
        }
        finally
        {
            _loginLock.Release();
        }
    }

    private async Task CheckCredentialsAsync(CancellationToken cancellationToken)
    {
        if (!await EnsureLoggedInAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new UnauthorizedAccessException($"Not logged in to {_url}");
        }
    }

    public bool IsLoggedIn => _loggedIn;

    public async Task AddAttachmentAsync(long pageId, EncodedImage image, string filename, string comment, CancellationToken cancellationToken = default)
    {
        await CheckCredentialsAsync(cancellationToken).ConfigureAwait(false);
        using var stream = image.OpenRead();
        await _confluence.Attachment.AttachAsync(pageId, stream, filename, comment, image.MimeType, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Entities.Page> GetPageAsync(string spaceKey, string pageTitle, CancellationToken cancellationToken = default)
    {
        Content page = null;
        string cacheKey = spaceKey + pageTitle;
        if (_pageCache.Contains(cacheKey))
        {
            page = _pageCache[cacheKey];
        }

        if (page == null)
        {
            await CheckCredentialsAsync(cancellationToken).ConfigureAwait(false);
            var query = Where.And(Where.Type.IsPage, Where.Title.Is(pageTitle), Where.Space.Is(spaceKey));
            var searchResult = await _confluence.Content.SearchAsync(query, pagingInformation: new PagingInformation
            {
                Limit = 1
            }, cancellationToken: cancellationToken).ConfigureAwait(false);
            page = searchResult.Results.FirstOrDefault();

            if (page != null)
            {
                // Get full page details with body
                page = await _confluence.Content.GetAsync(page, ConfluenceClientConfig.ExpandGetContentWithStorage, cancellationToken).ConfigureAwait(false);
                _pageCache.Add(cacheKey, page);
            }
        }

        return page != null ? new Entities.Page(page) : null;
    }

    public async Task<Entities.Page> GetPageAsync(long pageId, CancellationToken cancellationToken = default)
    {
        Content page = null;
        string cacheKey = pageId.ToString();

        if (_pageCache.Contains(cacheKey))
        {
            page = _pageCache[cacheKey];
        }

        if (page == null)
        {
            await CheckCredentialsAsync(cancellationToken).ConfigureAwait(false);
            page = await _confluence.Content.GetAsync(pageId, ConfluenceClientConfig.ExpandGetContentWithStorage, cancellationToken).ConfigureAwait(false);
            _pageCache.Add(cacheKey, page);
        }

        return new Entities.Page(page);
    }

    public async Task<Entities.Page> GetSpaceHomepageAsync(Entities.Space spaceSummary, CancellationToken cancellationToken = default)
    {
        await CheckCredentialsAsync(cancellationToken).ConfigureAwait(false);
        var space = await _confluence.Space.GetAsync(spaceSummary.Key, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (space.HomepageId == 0)
        {
            return null;
        }

        var page = await _confluence.Content.GetAsync(space.HomepageId, ConfluenceClientConfig.ExpandGetContentWithStorage, cancellationToken).ConfigureAwait(false);
        return page != null ? new Entities.Page(page) : null;
    }

    /// <summary>
    /// The spaces, sorted by name and cached for an hour
    /// </summary>
    public async Task<IList<Entities.Space>> GetSpaceSummariesAsync(CancellationToken cancellationToken = default)
    {
        var spaces = _spaces;
        if (spaces != null && DateTime.Now.AddMinutes(-60).CompareTo(_spacesLoaded) < 0)
        {
            return spaces;
        }

        await CheckCredentialsAsync(cancellationToken).ConfigureAwait(false);
        var spacesResult = await _confluence.Space.GetAllAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        spaces = spacesResult.Select(space => new Entities.Space(space)).OrderBy(space => space.Name).ToList();
        _spaces = spaces;
        _spacesLoaded = DateTime.Now;
        return spaces;
    }

    public async Task<IList<Entities.Page>> GetPageChildrenAsync(Entities.Page parentPage, CancellationToken cancellationToken = default)
    {
        await CheckCredentialsAsync(cancellationToken).ConfigureAwait(false);
        var childrenResult = await _confluence.Content.GetChildrenAsync(parentPage.Id, cancellationToken: cancellationToken).ConfigureAwait(false);
        return childrenResult?.Results?.Select(page => new Entities.Page(page)).ToList() ?? new List<Entities.Page>();
    }

    public async Task<IList<Entities.Page>> GetPageSummariesAsync(Entities.Space space, CancellationToken cancellationToken = default)
    {
        await CheckCredentialsAsync(cancellationToken).ConfigureAwait(false);
        var query = Where.And(Where.Type.IsPage, Where.Space.Is(space.Key));
        var searchResult = await _confluence.Content.SearchAsync(query, cancellationToken: cancellationToken).ConfigureAwait(false);
        return searchResult.Results.Select(page => new Entities.Page(page)).ToList();
    }

    public async Task<IList<Entities.Page>> SearchPagesAsync(string query, string space, CancellationToken cancellationToken = default)
    {
        await CheckCredentialsAsync(cancellationToken).ConfigureAwait(false);
        IFinalClause whereClause;

        if (!string.IsNullOrEmpty(space))
        {
            whereClause = Where.And(Where.Type.IsPage, Where.Text.Contains(query), Where.Space.Is(space));
        }
        else
        {
            whereClause = Where.And(Where.Type.IsPage, Where.Text.Contains(query));
        }

        var searchResult = await _confluence.Content.SearchAsync(whereClause, pagingInformation: new PagingInformation { Limit = 20 }, cancellationToken: cancellationToken).ConfigureAwait(false);

        var pages = new List<Entities.Page>();
        foreach (var page in searchResult.Results)
        {
            Log.DebugFormat("Got result of type {0}", page.Type);
            if (page.Type == ContentTypes.Page)
            {
                pages.Add(new Entities.Page(page));
            }
        }

        return pages;
    }
}
