/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Threading;
using Dapplo.Ini;

namespace Greenshot.Plugin.Confluence.Api;

/// <summary>
/// The login and the calls of the Confluence plugin over the <see cref="ConfluenceClient"/>, everything is async (rule R12).
/// </summary>
public class ConfluenceConnector : IDisposable
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(ConfluenceConnector));
    private static readonly IConfluenceConfiguration Config = IniConfigRegistry.GetSection<IConfluenceConfiguration>();
    private readonly SemaphoreSlim _loginLock = new SemaphoreSlim(1, 1);
    private DateTime _loggedInTime = DateTime.Now;
    private volatile bool _loggedIn;
    private ConfluenceClient _confluence;
    private readonly int _timeout;
    private readonly string _url;
    private readonly Cache<string, Entities.Page> _pageCache = new Cache<string, Entities.Page>(60 * Config.Timeout);
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
        _url = url;
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
        var confluence = new ConfluenceClient(new Uri(_url), user, password);
        try
        {
            // Test the credentials by getting current user info
            await confluence.CheckCredentialsAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            // Not an authentication issue
            _loggedIn = false;
            e.Data["user"] = user;
            e.Data["url"] = _url;
            throw;
        }

        _confluence = confluence;
        _loggedInTime = DateTime.Now;
        _loggedIn = true;
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
            _loggedIn = false;
            _confluence = null;
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
        await _confluence.AttachAsync(pageId, image, filename, comment, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Entities.Page> GetPageAsync(string spaceKey, string pageTitle, CancellationToken cancellationToken = default)
    {
        string cacheKey = spaceKey + pageTitle;
        var page = _pageCache.Contains(cacheKey) ? _pageCache[cacheKey] : null;
        if (page == null)
        {
            page = await SearchFirstPageAsync($"type=page and space={ConfluenceClient.Quote(spaceKey)} and title={ConfluenceClient.Quote(pageTitle)}", cancellationToken).ConfigureAwait(false);
            if (page != null)
            {
                _pageCache.Add(cacheKey, page);
            }
        }

        return page;
    }

    public async Task<Entities.Page> GetPageAsync(long pageId, CancellationToken cancellationToken = default)
    {
        string cacheKey = pageId.ToString();
        var page = _pageCache.Contains(cacheKey) ? _pageCache[cacheKey] : null;
        if (page == null)
        {
            page = await SearchFirstPageAsync($"id={pageId}", cancellationToken).ConfigureAwait(false);
            if (page != null)
            {
                _pageCache.Add(cacheKey, page);
            }
        }

        return page;
    }

    public async Task<Entities.Page> GetSpaceHomepageAsync(Entities.Space space, CancellationToken cancellationToken = default)
    {
        if (space.Homepage != null || space.HomepageId == null)
        {
            return space.Homepage;
        }

        return await GetPageAsync(space.HomepageId.Value, cancellationToken).ConfigureAwait(false);
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
        var confluenceSpaces = await _confluence.GetSpacesAsync(cancellationToken).ConfigureAwait(false);
        spaces = confluenceSpaces
            .Select(space => new Entities.Space(space, space.Homepage == null ? null : ToPage(space.Homepage)))
            .OrderBy(space => space.Name)
            .ToList();
        _spaces = spaces;
        _spacesLoaded = DateTime.Now;
        return spaces;
    }

    public async Task<IList<Entities.Page>> GetPageChildrenAsync(Entities.Page parentPage, CancellationToken cancellationToken = default)
    {
        return await SearchPagesByCqlAsync($"type=page and parent={parentPage.Id}", 200, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IList<Entities.Page>> SearchPagesAsync(string query, string space, CancellationToken cancellationToken = default)
    {
        string cql = $"type=page and text~{ConfluenceClient.Quote(query)}";
        if (!string.IsNullOrEmpty(space))
        {
            cql += $" and space={ConfluenceClient.Quote(space)}";
        }

        return await SearchPagesByCqlAsync(cql, 20, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Entities.Page> SearchFirstPageAsync(string cql, CancellationToken cancellationToken)
    {
        var pages = await SearchPagesByCqlAsync(cql, 1, cancellationToken).ConfigureAwait(false);
        return pages.FirstOrDefault();
    }

    private async Task<IList<Entities.Page>> SearchPagesByCqlAsync(string cql, int limit, CancellationToken cancellationToken)
    {
        await CheckCredentialsAsync(cancellationToken).ConfigureAwait(false);
        var results = await _confluence.SearchAsync(cql, limit, cancellationToken).ConfigureAwait(false);
        return results
            .Where(content => content.Type == null || content.Type == "page")
            .Select(ToPage)
            .ToList();
    }

    private Entities.Page ToPage(ConfluenceContent content) => new Entities.Page(content, _confluence.GetWebUrl(content));
}
