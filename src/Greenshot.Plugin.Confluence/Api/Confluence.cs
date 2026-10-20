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
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Http;
using Dapplo.Ini;

namespace Greenshot.Plugin.Confluence.Api;

/// <summary>
/// The login and the calls of the Confluence plugin over the <see cref="ConfluenceClient"/>, everything is async (rule R12).
/// </summary>
public class ConfluenceConnector : IDisposable
{
    private static readonly IConfluenceConfiguration Config = IniConfigRegistry.GetSection<IConfluenceConfiguration>();
    private DateTime _signedInTime = DateTime.Now;
    private volatile ConfluenceClient _confluence;
    private readonly Cache<string, Entities.Page> _pageCache = new Cache<string, Entities.Page>(60 * Config.Timeout);
    private IList<Entities.Space> _spaces;
    private DateTime _spacesLoaded;

    public ConfluenceConnector()
    {
        SignIn = new ServerSignIn("Confluence", () => Config.Url, ConnectAsync, ServerSignInOptions.Atlassian);
        SignIn.Changed += OnSignInChanged;
    }

    /// <summary>
    /// The sign-in to Confluence, also used by the settings
    /// </summary>
    public ServerSignIn SignIn { get; }

    public void Dispose()
    {
        SignIn.Changed -= OnSignInChanged;
        Logout();
    }

    // Signed out in the settings: nothing loaded for the account stays
    private void OnSignInChanged(object sender, EventArgs e)
    {
        if (!SignIn.IsSignedIn)
        {
            Logout();
        }
    }

    /// <summary>
    /// Connect with the credentials and check them
    /// </summary>
    /// <returns>the name of the user</returns>
    private async Task<string> ConnectAsync(string url, ServerCredentials credentials, CancellationToken cancellationToken)
    {
        var confluence = new ConfluenceClient(new Uri(url), credentials);
        string account = await confluence.CheckCredentialsAsync(cancellationToken).ConfigureAwait(false);
        if (_confluence?.BaseUri != confluence.BaseUri)
        {
            // Another server
            _spaces = null;
        }

        _confluence = confluence;
        _signedInTime = DateTime.Now;
        return account;
    }

    /// <summary>
    /// Forget the connection, the credentials stay
    /// </summary>
    public void Logout()
    {
        _confluence = null;
        _spaces = null;
    }

    /// <summary>
    /// Make sure the connector is signed in: with the stored credentials, otherwise the sign-in window.
    /// After the session timeout of the configuration the credentials are checked again.
    /// </summary>
    /// <returns>true when signed in, false when the user canceled the sign-in</returns>
    public async Task<bool> EnsureLoggedInAsync(CancellationToken cancellationToken = default)
    {
        if (_confluence == null || _signedInTime.AddMinutes(Config.Timeout - 1).CompareTo(DateTime.Now) < 0)
        {
            SignIn.Recheck();
        }

        return await SignIn.EnsureSignedInAsync(cancellationToken).ConfigureAwait(false) && _confluence != null;
    }

    private async Task CheckCredentialsAsync(CancellationToken cancellationToken)
    {
        if (!await EnsureLoggedInAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new UnauthorizedAccessException($"Not logged in to {Config.Url}");
        }
    }

    public bool IsLoggedIn => _confluence != null && SignIn.IsSignedIn;

    public async Task AddAttachmentAsync(long pageId, EncodedImage image, string filename, string comment, CancellationToken cancellationToken = default)
    {
        await CheckCredentialsAsync(cancellationToken).ConfigureAwait(false);
        await _confluence.AttachAsync(pageId, image, filename, comment, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Entities.Page> GetPageAsync(long pageId, CancellationToken cancellationToken = default)
    {
        // The same id can be another page on another server
        string cacheKey = Config.Url + "|" + pageId;
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
