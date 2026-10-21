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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Http;
using Greenshot.Base.Interfaces;
using Dapplo.Ini;
using Greenshot.Base.Threading;
using Greenshot.Plugin.Atlassian.Jira.Monitoring;

namespace Greenshot.Plugin.Atlassian.Jira.Api;

/// <summary>
/// The login, the title monitor and the calls of the Jira plugin, over the <see cref="JiraClient"/>
/// </summary>
public sealed class JiraConnector : IDisposable
{
    private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(JiraConnector));
    private static readonly IJiraConfiguration JiraConfig = IniConfigRegistry.GetSection<IJiraConfiguration>();

    private static readonly ICoreConfiguration CoreConfig = IniConfigRegistry.GetSection<ICoreConfiguration>();

    // Used to remove the wsdl information from the old SOAP Uri
    public const string DefaultPostfix = "/rpc/soap/jirasoapservice-v2?wsdl";
    private JiraClient _jiraClient;

    // The issue type icons in the icon size, replaced when the icon size changes
    private IssueTypeIcons _issueTypeIcons = new IssueTypeIcons();

    private void OnCoreConfigPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(CoreConfig.IconSize))
        {
            ClearIssueTypeBitmaps();
        }
    }

    /// <summary>
    /// Dispose, logout the users
    /// </summary>
    public void Dispose()
    {
        CoreConfig.PropertyChanged -= OnCoreConfigPropertyChanged;
        SignIn.Changed -= OnSignInChanged;
        Logout();
        ClearIssueTypeBitmaps();
        FavIcon?.Dispose();
    }

    /// <summary>
    /// Constructor
    /// </summary>
    public JiraConnector()
    {
        JiraConfig.Url = JiraConfig.Url.Replace(DefaultPostfix, string.Empty);
        CoreConfig.PropertyChanged += OnCoreConfigPropertyChanged;
        SignIn = new ServerSignIn("Jira", () => JiraConfig.Url, ConnectAsync, AtlassianSignIn.Options);
        SignIn.Changed += OnSignInChanged;
    }

    /// <summary>
    /// The sign-in to Jira, also used by the settings
    /// </summary>
    public ServerSignIn SignIn { get; }

    /// <summary>
    /// Access the jira monitor
    /// </summary>
    public JiraMonitor Monitor { get; private set; }

    public Bitmap FavIcon { get; private set; }

    /// <summary>
    /// True when signed in and connected to the Jira of the configuration
    /// </summary>
    public bool IsLoggedIn => _jiraClient != null && SignIn.IsSignedIn;

    // Signed out in the settings: stop the monitor
    private void OnSignInChanged(object sender, EventArgs e)
    {
        if (!SignIn.IsSignedIn)
        {
            Logout();
        }
    }

    /// <summary>
    /// Connect with the credentials: check them, start the title monitor and get the icon of the server
    /// </summary>
    /// <returns>the name of the user</returns>
    private async Task<string> ConnectAsync(string url, ServerCredentials credentials, CancellationToken cancellationToken)
    {
        var jiraClient = new JiraClient(new Uri(url), credentials);
        var myself = await jiraClient.GetMyselfAsync(cancellationToken).ConfigureAwait(false);

        // The title monitor only accepts keys of the projects
        var monitor = new JiraMonitor();
        try
        {
            await monitor.AddJiraInstanceAsync(jiraClient, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // The monitor hooks the title changes of all windows, don't leave one running for a failed sign-in
            monitor.Dispose();
            throw;
        }

        var oldMonitor = Monitor;
        Monitor = monitor;
        _jiraClient = jiraClient;
        oldMonitor?.Dispose();

        await LoadFavIconAsync(jiraClient, cancellationToken).ConfigureAwait(false);
        return myself?.DisplayName;
    }

    /// <summary>
    /// The favicon of the server (or the icon link of its start page) for the destination, the Jira logo without one
    /// </summary>
    private async Task LoadFavIconAsync(JiraClient jiraClient, CancellationToken cancellationToken)
    {
        try
        {
            var favIcon = await SiteIcon.FetchAsync(HttpService.Current, jiraClient.BaseUri, false, cancellationToken).ConfigureAwait(false);
            if (favIcon == null)
            {
                return;
            }

            using var stream = new MemoryStream(favIcon);
            using var image = Image.FromStream(stream);
            var oldFavIcon = FavIcon;
            FavIcon = new Bitmap(image);
            if (oldFavIcon != null)
            {
                // JiraDestination clones the icon under this lock
                lock (oldFavIcon)
                {
                    oldFavIcon.Dispose();
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Warn($"Couldn't load the icon of {jiraClient.BaseUri}", ex);
        }
    }

    /// <summary>
    /// Sign in: with the stored credentials, otherwise the sign-in window
    /// </summary>
    public async Task LoginAsync(CancellationToken cancellationToken = default)
    {
        if (_jiraClient == null && SignIn.IsSignedIn)
        {
            // Logged out (plugin stopped), connect again with the same credentials
            SignIn.Recheck();
        }

        await SignIn.EnsureSignedInAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Stop the monitor and the connection, the credentials stay
    /// </summary>
    public void Logout()
    {
        _jiraClient = null;
        var monitor = Monitor;
        Monitor = null;
        monitor?.Dispose();
    }

    /// <summary>
    /// check the login credentials, or makes a login
    /// The login marshals its dialog to the UI thread, so this can be called from any thread.
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">the user canceled the sign-in</exception>
    private async Task CheckCredentialsAsync(CancellationToken cancellationToken = default)
    {
        if (!IsLoggedIn)
        {
            await LoginAsync(cancellationToken).ConfigureAwait(false);
        }

        if (!IsLoggedIn)
        {
            throw new UnauthorizedAccessException($"Not signed in to {JiraConfig.Url}");
        }
    }

    /// <summary>
    /// Get the favorite filters
    /// </summary>
    /// <returns>List with filters</returns>
    public async Task<IList<JiraFilter>> GetFavoriteFiltersAsync(CancellationToken cancellationToken = default)
    {
        await CheckCredentialsAsync(cancellationToken);
        return await _jiraClient.GetFavoriteFiltersAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Get the issue for a key
    /// </summary>
    /// <param name="issueKey">Jira issue key</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns>JiraIssue, null when it couldn't be read</returns>
    public async Task<JiraIssue> GetIssueAsync(string issueKey, CancellationToken cancellationToken = default)
    {
        await CheckCredentialsAsync(cancellationToken);
        try
        {
            return await _jiraClient.GetIssueAsync(issueKey, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Log.Debug($"Couldn't read Jira issue {issueKey}", ex);
            return null;
        }
    }

    /// <summary>
    /// Attach the encoded capture to the jira
    /// </summary>
    /// <param name="issueKey"></param>
    /// <param name="content">EncodedImage</param>
    /// <param name="filename">Filename of the attachment</param>
    /// <param name="cancellationToken"></param>
    public async Task AttachAsync(string issueKey, EncodedImage content, string filename, CancellationToken cancellationToken = default)
    {
        await CheckCredentialsAsync(cancellationToken).ConfigureAwait(false);
        await _jiraClient.AttachAsync(issueKey, content, filename, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Add a comment to the supplied issue
    /// </summary>
    /// <param name="issueKey">Jira issue key</param>
    /// <param name="body">text</param>
    /// <param name="cancellationToken">CancellationToken</param>
    public async Task AddCommentAsync(string issueKey, string body, CancellationToken cancellationToken = default)
    {
        await CheckCredentialsAsync(cancellationToken);
        await _jiraClient.AddCommentAsync(issueKey, body, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Get the search results for the specified filter
    /// </summary>
    /// <param name="filter">JiraFilter</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IList<JiraIssue>> SearchAsync(JiraFilter filter, CancellationToken cancellationToken = default)
    {
        await CheckCredentialsAsync(cancellationToken);
        return await _jiraClient.SearchAsync(filter.Jql, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Get the bitmap representing the issue type of an issue, from cache.
    /// </summary>
    /// <param name="issue">JiraIssue</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns>Bitmap, owned by the cache: lock it while using it</returns>
    public Task<Bitmap> GetIssueTypeBitmapAsync(JiraIssue issue, CancellationToken cancellationToken = default)
    {
        return GetIssueTypeBitmapAsync(issue?.Fields?.IssueType, cancellationToken);
    }

    /// <summary>
    /// Get the bitmap representing the issue type, from cache, in the icon size.
    /// </summary>
    /// <param name="issueType">JiraIssueType</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns>Bitmap, owned by the cache: lock it while using it</returns>
    public async Task<Bitmap> GetIssueTypeBitmapAsync(JiraIssueType issueType, CancellationToken cancellationToken = default)
    {
        var client = _jiraClient;
        if (issueType?.IconUrl == null || client == null)
        {
            return null;
        }

        var icons = _issueTypeIcons;
        var task = icons.Bitmaps.GetOrAdd(issueType.IconUrl, iconUrl => LoadIconAsync(client, iconUrl, icons));
        try
        {
            return await task.ConfigureAwait(false);
        }
        catch
        {
            // Try again the next time
            icons.Bitmaps.TryRemove(issueType.IconUrl, out _);
            throw;
        }
    }

    // Not cancelled by the caller: the task is shared by everyone asking for the icon
    private static async Task<Bitmap> LoadIconAsync(JiraClient client, Uri iconUrl, IssueTypeIcons icons)
    {
        var bitmap = await LoadIconAsync(client, iconUrl).ConfigureAwait(false);
        if (bitmap != null)
        {
            icons.Loaded.Enqueue(bitmap);
        }

        return bitmap;
    }

    private static async Task<Bitmap> LoadIconAsync(JiraClient client, Uri iconUrl)
    {
        var bytes = await client.GetIconBytesAsync(iconUrl).ConfigureAwait(false);
        if (bytes == null || bytes.Length == 0)
        {
            return null;
        }

        // Cloud serves SVG icons, Server PNG or GIF
        using var image = ImageIO.FromStream(new MemoryStream(bytes), GetImageExtension(bytes));
        if (image == null)
        {
            return null;
        }

        var iconSize = CoreConfig.IconSize;
        if (image.Width == iconSize.Width && image.Height == iconSize.Height)
        {
            return ImageHelper.Clone(image) as Bitmap;
        }

        return ImageHelper.ResizeImage(image, true, iconSize.Width, iconSize.Height, null) as Bitmap;
    }

    /// <summary>
    /// The extension for the file format handlers, from the first bytes
    /// </summary>
    internal static string GetImageExtension(byte[] bytes)
    {
        if (bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == (byte)'P' && bytes[2] == (byte)'N' && bytes[3] == (byte)'G')
        {
            return ".png";
        }

        if (bytes.Length >= 3 && bytes[0] == (byte)'G' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F')
        {
            return ".gif";
        }

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8)
        {
            return ".jpg";
        }

        // Text: an SVG, maybe with a BOM, an XML declaration or white space in front
        return bytes.Take(64).Any(b => b == (byte)'<') ? ".svg" : null;
    }

    private void ClearIssueTypeBitmaps()
    {
        var old = Interlocked.Exchange(ref _issueTypeIcons, new IssueTypeIcons());
        while (old.Loaded.TryDequeue(out var bitmap))
        {
            // JiraDestination clones the icon under this lock
            lock (bitmap)
            {
                bitmap.Dispose();
            }
        }
    }

    private sealed class IssueTypeIcons
    {
        public ConcurrentDictionary<Uri, Task<Bitmap>> Bitmaps { get; } = new ConcurrentDictionary<Uri, Task<Bitmap>>();

        // The loaded bitmaps, to dispose them
        public ConcurrentQueue<Bitmap> Loaded { get; } = new ConcurrentQueue<Bitmap>();
    }

    /// <summary>
    /// Get the base uri
    /// </summary>
    public Uri JiraBaseUri => _jiraClient?.BaseUri;

    /// <summary>
    /// The page of an issue in the browser
    /// </summary>
    public Uri GetBrowseUri(string issueKey) => _jiraClient?.GetBrowseUri(issueKey);
}
