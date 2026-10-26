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

using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Greenshot.Linux.Core;
using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Core.Configuration;
using Greenshot.Linux.Core.Destinations;
using Greenshot.Linux.Core.Http;
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.Core.Naming;
using Greenshot.Linux.Core.Output;
using Greenshot.Linux.Core.Plugins;
using Capture = Greenshot.Linux.Core.Capturing.Capture;

namespace Greenshot.Linux.Plugins.Atlassian;

/// <summary>
/// The keys of [Jira] and [Confluence] which both have
/// </summary>
public abstract class AtlassianSettings(CoreSettings settings, string section, string placeholderUrl)
{
    public string Section => section;

    protected CoreSettings Core => settings;

    /// <summary>
    /// The server; the default of Windows (https://jira, https://confluence) counts as not set
    /// </summary>
    public string Url
    {
        get
        {
            var url = settings.GetValue(section, nameof(Url))?.Trim() ?? string.Empty;
            return url.Equals(placeholderUrl, StringComparison.OrdinalIgnoreCase) ? string.Empty : url;
        }
        set => settings.SetValue(section, nameof(Url), value.Trim());
    }

    public bool IsConfigured => Uri.TryCreate(Url, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http";

    public string UploadFormat
    {
        get => ImageFormats.Normalize(settings.GetValue(section, nameof(UploadFormat)) ?? "png");
        set => settings.SetValue(section, nameof(UploadFormat), value);
    }

    public int UploadJpegQuality =>
        int.TryParse(settings.GetValue(section, nameof(UploadJpegQuality)), NumberStyles.Integer, CultureInfo.InvariantCulture, out var q) ? Math.Clamp(q, 1, 100) : 80;

    public EncodedImage Encode(Capture capture) => new(ImageFormats.Encode(capture.Image, UploadFormat, UploadJpegQuality), UploadFormat);

    /// <summary>
    /// The file name of the pattern of the output settings, with the extension of the upload format
    /// </summary>
    public string Filename(Capture capture) =>
        Path.GetFileName(FilenamePattern.ForSettings(settings).GetFilename(settings.OutputFileFilenamePattern, UploadFormat, capture.Details));

    protected bool GetBool(string key, bool defaultValue) => bool.TryParse(settings.GetValue(section, key), out var value) ? value : defaultValue;

    protected void SetBool(string key, bool value) => settings.SetValue(section, key, value ? "True" : "False");
}

public sealed class JiraSettings(CoreSettings settings) : AtlassianSettings(settings, "Jira", "https://jira")
{
    // The url of the old SOAP service, Windows removes it the same way
    private const string SoapPostfix = "/rpc/soap/jirasoapservice-v2?wsdl";

    public string ServerUrl => Url.Replace(SoapPostfix, string.Empty, StringComparison.OrdinalIgnoreCase);
}

public sealed class ConfluenceSettings(CoreSettings settings) : AtlassianSettings(settings, "Confluence", "https://confluence")
{
    /// <summary>
    /// Session timeout in minutes, the credentials are checked again after it
    /// </summary>
    public int Timeout
    {
        get => int.TryParse(Core.GetValue(Section, nameof(Timeout)), NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes)
            ? Math.Clamp(minutes, 1, 1440)
            : 30;
        set => Core.SetValue(Section, nameof(Timeout), value.ToString(CultureInfo.InvariantCulture));
    }

    public bool OpenPageAfterUpload
    {
        get => GetBool(nameof(OpenPageAfterUpload), true);
        set => SetBool(nameof(OpenPageAfterUpload), value);
    }

    public bool CopyWikiMarkupForImageToClipboard
    {
        get => GetBool(nameof(CopyWikiMarkupForImageToClipboard), true);
        set => SetBool(nameof(CopyWikiMarkupForImageToClipboard), value);
    }

    public string? SearchSpaceKey
    {
        get => Core.GetValue(Section, nameof(SearchSpaceKey));
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                Core.RemoveValue(Section, nameof(SearchSpaceKey));
            }
            else
            {
                Core.SetValue(Section, nameof(SearchSpaceKey), value);
            }
        }
    }

    public bool IncludePersonSpaces
    {
        get => GetBool(nameof(IncludePersonSpaces), false);
        set => SetBool(nameof(IncludePersonSpaces), value);
    }
}

#region Dialogs

/// <summary>
/// What the Jira upload dialog asks for: the connector to load the filters and issues, the issue to preselect
/// </summary>
public sealed record JiraUploadRequest(JiraConnector Connector, string Filename, JiraIssue? SuggestedIssue);

public sealed record JiraUploadChoice(JiraIssue Issue, string Filename, string? Comment);

/// <summary>
/// What the Confluence upload dialog shows: search and browse the pages of the spaces
/// </summary>
public sealed record ConfluenceUploadRequest(ConfluenceConnector Connector, ConfluenceSettings Settings, string Filename, IList<ConfluenceSpaceInfo> Spaces);

public sealed record ConfluenceUploadChoice(ConfluencePage Page, string Filename);

/// <summary>
/// The page already has an attachment with the name: a new version of it, or another name
/// </summary>
public sealed record AttachmentExistsRequest(string PageTitle, string Filename)
{
    public string OtherName => AttachmentNames.Next(Filename);
}

public sealed record AttachmentExistsChoice(bool NewVersion, string Filename);

/// <summary>
/// The windows of the plugin, made by the app; null is canceled
/// </summary>
public interface IAtlassianDialogs
{
    ServerSignInPrompt SignIn { get; }

    Task<JiraUploadChoice?> ChooseJiraIssueAsync(JiraUploadRequest request);

    Task<ConfluenceUploadChoice?> ChooseConfluencePageAsync(ConfluenceUploadRequest request);

    Task<AttachmentExistsChoice?> AttachmentExistsAsync(AttachmentExistsRequest request);
}

#endregion

#region Jira

/// <summary>
/// An issue key seen in a window title
/// </summary>
public sealed class JiraDetails(string projectKey, string id)
{
    public string ProjectKey { get; } = projectKey;

    public string Id { get; } = id;

    public string JiraKey => ProjectKey + "-" + Id;

    public JiraIssue? JiraIssue { get; set; }

    public DateTimeOffset SeenAt { get; set; } = DateTimeOffset.Now;
}

/// <summary>
/// The issues seen in window titles (a browser with the issue, a mail about it), newest first, like the JiraMonitor of Windows.
/// Windows hooks the title changes of all windows, Wayland has no such hook: the plugin asks the compositor (sway, Hyprland)
/// for the titles every few seconds, and the title of a capture is looked at too.
/// </summary>
public sealed partial class JiraMonitor(int maxEntries = 40)
{
    private readonly object _lock = new();
    private readonly Dictionary<string, JiraDetails> _recent = new();
    private HashSet<string> _projects = [];
    private Func<string, CancellationToken, Task<JiraIssue?>>? _getIssue;

    [GeneratedRegex(@"\b[A-Z][A-Z0-9]+-[0-9]+\b")]
    private static partial Regex JiraKeyPattern();

    /// <summary>
    /// A new issue was found or loaded
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>
    /// The issues seen, the last seen first
    /// </summary>
    public IReadOnlyList<JiraDetails> RecentJiras
    {
        get
        {
            lock (_lock)
            {
                return _recent.Values.OrderByDescending(details => details.SeenAt).ToList();
            }
        }
    }

    /// <summary>
    /// After the sign-in: the keys of the projects of the server, only their keys are accepted
    /// </summary>
    public void Connect(IEnumerable<string> projectKeys, Func<string, CancellationToken, Task<JiraIssue?>> getIssue)
    {
        lock (_lock)
        {
            _projects = new HashSet<string>(projectKeys, StringComparer.Ordinal);
            _getIssue = getIssue;
            foreach (var unknown in _recent.Keys.Where(key => !_projects.Contains(key.Split('-')[0])).ToList())
            {
                _recent.Remove(unknown);
            }
        }
    }

    public void Disconnect()
    {
        lock (_lock)
        {
            _projects = [];
            _getIssue = null;
            _recent.Clear();
        }
    }

    /// <summary>
    /// Look at a window title; a new issue is loaded from Jira
    /// </summary>
    /// <returns>the issue of the title, null when the title has none of the projects</returns>
    public async Task<JiraDetails?> ObserveAsync(string? title, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(title))
        {
            return null;
        }

        JiraDetails? details = null;
        Func<string, CancellationToken, Task<JiraIssue?>>? getIssue;
        bool isNew = false;
        lock (_lock)
        {
            getIssue = _getIssue;
            foreach (Match match in JiraKeyPattern().Matches(title))
            {
                var parts = match.Value.Split('-');
                if (!_projects.Contains(parts[0]))
                {
                    continue;
                }

                if (_recent.TryGetValue(match.Value, out details))
                {
                    details.SeenAt = DateTimeOffset.Now;
                }
                else
                {
                    details = new JiraDetails(parts[0], parts[1]);
                    _recent[details.JiraKey] = details;
                    isNew = true;
                    // Don't collect issues until the memory is full
                    foreach (var oldest in _recent.Values.OrderByDescending(d => d.SeenAt).Skip(maxEntries).ToList())
                    {
                        _recent.Remove(oldest.JiraKey);
                    }
                }

                break;
            }
        }

        if (details == null || !isNew || getIssue == null)
        {
            return details;
        }

        try
        {
            details.JiraIssue = await getIssue(details.JiraKey, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or UnauthorizedAccessException or TaskCanceledException)
        {
            Log.Warn($"Couldn't load Jira issue {details.JiraKey}: {ex.Message}");
        }

        Changed?.Invoke(this, EventArgs.Empty);
        return details;
    }
}

/// <summary>
/// The sign-in, the title monitor and the calls of the Jira plugin, over the <see cref="JiraClient"/>
/// </summary>
public sealed class JiraConnector
{
    private readonly JiraSettings _settings;
    private readonly HttpSend? _send;
    private volatile JiraClient? _client;

    public JiraConnector(JiraSettings settings, ISecretStore secrets, ServerSignInPrompt prompt, HttpSend? send = null)
    {
        _settings = settings;
        _send = send;
        SignIn = new ServerSignIn("Jira", () => settings.ServerUrl, ConnectAsync, secrets, prompt, AtlassianSignIn.Options);
        SignIn.Changed += (_, _) =>
        {
            if (!SignIn.IsSignedIn)
            {
                Logout();
            }
        };
    }

    public ServerSignIn SignIn { get; }

    public JiraMonitor Monitor { get; } = new();

    public bool IsLoggedIn => _client != null && SignIn.IsSignedIn;

    private async Task<string?> ConnectAsync(string url, ServerCredentials credentials, CancellationToken cancellationToken)
    {
        var client = new JiraClient(new Uri(url), credentials, _send);
        var name = await client.GetMyselfAsync(cancellationToken).ConfigureAwait(false);
        // The title monitor only accepts keys of the projects
        var projects = await client.GetProjectsAsync(cancellationToken).ConfigureAwait(false);
        Monitor.Connect(projects.Select(project => project.Key), client.GetIssueAsync);
        _client = client;
        return name;
    }

    public void Logout()
    {
        _client = null;
        Monitor.Disconnect();
    }

    /// <summary>
    /// Sign in with the stored credentials, otherwise the sign-in window
    /// </summary>
    /// <returns>false when the user canceled</returns>
    public async Task<bool> LoginAsync(CancellationToken cancellationToken = default)
    {
        if (IsLoggedIn)
        {
            return true;
        }

        if (_client == null && SignIn.IsSignedIn)
        {
            // Logged out, connect again with the same credentials
            SignIn.Recheck();
        }

        return await SignIn.EnsureSignedInAsync(cancellationToken).ConfigureAwait(false) && IsLoggedIn;
    }

    private async Task<JiraClient> ClientAsync(CancellationToken cancellationToken)
    {
        if (!await LoginAsync(cancellationToken).ConfigureAwait(false) || _client is not { } client)
        {
            throw new UnauthorizedAccessException($"Not signed in to {_settings.ServerUrl}");
        }

        return client;
    }

    public async Task<IList<JiraFilter>> GetFavoriteFiltersAsync(CancellationToken cancellationToken = default) =>
        await (await ClientAsync(cancellationToken).ConfigureAwait(false)).GetFavoriteFiltersAsync(cancellationToken).ConfigureAwait(false);

    public async Task<IList<JiraIssue>> SearchAsync(JiraFilter filter, CancellationToken cancellationToken = default) =>
        await (await ClientAsync(cancellationToken).ConfigureAwait(false)).SearchAsync(filter.Jql, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// The issue, null when it doesn't exist or couldn't be read
    /// </summary>
    public async Task<JiraIssue?> GetIssueAsync(string issueKey, CancellationToken cancellationToken = default)
    {
        var client = await ClientAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await client.GetIssueAsync(issueKey, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            Log.Debug($"Couldn't read Jira issue {issueKey}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Attach the capture, with the comment; {capture} in the comment shows the capture (wiki markup)
    /// </summary>
    /// <returns>the page of the issue</returns>
    public async Task<Uri> UploadAsync(string issueKey, EncodedImage image, string filename, string? comment, CancellationToken cancellationToken = default)
    {
        var client = await ClientAsync(cancellationToken).ConfigureAwait(false);
        bool showsCapture = comment?.Contains(JiraDestination.CapturePlaceholder, StringComparison.Ordinal) == true;
        if (showsCapture)
        {
            // The comment shows the attachment by name, which is the first one with that name
            var names = await client.GetAttachmentNamesAsync(issueKey, cancellationToken).ConfigureAwait(false);
            while (names.Contains(filename, StringComparer.OrdinalIgnoreCase))
            {
                filename = AttachmentNames.Next(filename);
            }
        }

        await client.AttachAsync(issueKey, image, filename, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(comment))
        {
            string body = showsCapture ? comment.Replace(JiraDestination.CapturePlaceholder, "!" + filename + "|thumbnail!", StringComparison.Ordinal) : comment;
            await client.AddCommentAsync(issueKey, body, cancellationToken).ConfigureAwait(false);
        }

        return client.GetBrowseUri(issueKey);
    }
}

/// <summary>
/// Upload to Jira: with the dialog to choose the issue, or directly to an issue seen in a window title
/// </summary>
public sealed class JiraDestination(JiraPlugin plugin, JiraIssue? issue = null) : IDestination
{
    /// <summary>
    /// Replaced with the attachment when the comment is sent, the final file name isn't known before
    /// </summary>
    public const string CapturePlaceholder = "{capture}";

    public string Designation => issue == null ? "Jira" : "Jira:" + issue.Key;

    public string DisplayName
    {
        get
        {
            if (issue?.Fields?.Summary is not { } summary)
            {
                return Texts.Get("Jira", "upload_menu_item");
            }

            return issue.Key + ": " + summary[..Math.Min(20, summary.Length)];
        }
    }

    public int Priority => issue == null ? 40 : 41;

    public bool IsAvailable => plugin.Settings.IsConfigured;

    public async Task<ExportResult> ExportAsync(Capture capture, CancellationToken cancellationToken = default)
    {
        var connector = plugin.Connector;
        string filename = plugin.Settings.Filename(capture);
        string? comment = null;
        var target = issue;
        try
        {
            if (target == null)
            {
                if (!await connector.LoginAsync(cancellationToken).ConfigureAwait(false))
                {
                    return ExportResult.Aborted;
                }

                // The capture of a window with an issue in the title, else the last seen issue
                var suggested = (await connector.Monitor.ObserveAsync(capture.Details.Title, cancellationToken).ConfigureAwait(false))
                                ?? connector.Monitor.RecentJiras.FirstOrDefault();
                var choice = await plugin.Dialogs.ChooseJiraIssueAsync(new JiraUploadRequest(connector, filename, suggested?.JiraIssue)).ConfigureAwait(false);
                if (choice == null)
                {
                    return ExportResult.Aborted;
                }

                target = choice.Issue;
                filename = string.IsNullOrWhiteSpace(choice.Filename) ? filename : choice.Filename.Trim();
                comment = choice.Comment;
            }

            var link = await connector.UploadAsync(target.Key, plugin.Settings.Encode(capture), filename, comment, cancellationToken).ConfigureAwait(false);
            Log.Info($"Uploaded to Jira {target.Key}");
            return ExportResult.Done(message: Texts.Get("Jira", "upload_success")) with { Link = link.AbsoluteUri };
        }
        catch (Exception ex) when (ex is HttpRequestException or UnauthorizedAccessException or JsonException or TaskCanceledException or UriFormatException)
        {
            Log.Error($"Upload to Jira {target?.Key} failed", ex);
            return ExportResult.Failed($"{Texts.Get("Jira", "upload_failure")} {ex.Message}");
        }
    }
}

/// <summary>
/// The Jira part of the Atlassian plugin
/// </summary>
public sealed class JiraPlugin
{
    private readonly JiraDestination _destination;

    public JiraPlugin(CoreSettings settings, ISecretStore secrets, IAtlassianDialogs dialogs, HttpSend? send = null)
    {
        Settings = new JiraSettings(settings);
        Dialogs = dialogs;
        Connector = new JiraConnector(Settings, secrets, dialogs.SignIn, send);
        _destination = new JiraDestination(this);
    }

    public JiraSettings Settings { get; }

    public JiraConnector Connector { get; }

    public IAtlassianDialogs Dialogs { get; }

    /// <summary>
    /// "Upload to Jira" and the issues seen recently, those directly
    /// </summary>
    public IReadOnlyList<IDestination> Destinations =>
    [
        _destination,
        .. Connector.IsLoggedIn
            ? Connector.Monitor.RecentJiras.Where(details => details.JiraIssue != null).Select(details => new JiraDestination(this, details.JiraIssue))
            : [],
    ];

    /// <summary>
    /// Watch the window titles for issue keys: connects with stored credentials (never the sign-in window), then asks for the titles
    /// </summary>
    public async Task WatchWindowTitlesAsync(Func<Task<IReadOnlyList<string>>> getTitles, TimeSpan interval, CancellationToken cancellationToken)
    {
        if (!Settings.IsConfigured)
        {
            return;
        }

        try
        {
            if (await Connector.SignIn.HasStoredCredentialsAsync().ConfigureAwait(false))
            {
                await Connector.SignIn.TestAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or UnauthorizedAccessException or JsonException or TaskCanceledException or UriFormatException)
        {
            Log.Info($"Jira isn't connected at the start: {ex.Message}");
        }

        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!Connector.IsLoggedIn)
            {
                continue;
            }

            try
            {
                foreach (var title in await getTitles().ConfigureAwait(false))
                {
                    await Connector.Monitor.ObserveAsync(title, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or JsonException)
            {
                Log.Debug($"No window titles: {ex.Message}");
            }
        }
    }
}

#endregion

#region Confluence

/// <summary>
/// The sign-in and the calls of the Confluence plugin; the sign-in is checked again after the timeout of the settings
/// </summary>
public sealed class ConfluenceConnector
{
    private readonly ConfluenceSettings _settings;
    private readonly HttpSend? _send;
    private volatile ConfluenceClient? _client;
    private DateTime _signedInTime = DateTime.Now;
    private IList<ConfluenceSpaceInfo>? _spaces;
    private DateTime _spacesLoaded;

    public ConfluenceConnector(ConfluenceSettings settings, ISecretStore secrets, ServerSignInPrompt prompt, HttpSend? send = null)
    {
        _settings = settings;
        _send = send;
        SignIn = new ServerSignIn("Confluence", () => settings.Url, ConnectAsync, secrets, prompt, AtlassianSignIn.Options);
        SignIn.Changed += (_, _) =>
        {
            if (!SignIn.IsSignedIn)
            {
                Logout();
            }
        };
    }

    public ServerSignIn SignIn { get; }

    public bool IsLoggedIn => _client != null && SignIn.IsSignedIn;

    private async Task<string?> ConnectAsync(string url, ServerCredentials credentials, CancellationToken cancellationToken)
    {
        var client = new ConfluenceClient(new Uri(url), credentials, _send);
        string? account = await client.CheckCredentialsAsync(cancellationToken).ConfigureAwait(false);
        if (_client?.BaseUri != client.BaseUri)
        {
            // Another server
            _spaces = null;
        }

        _client = client;
        _signedInTime = DateTime.Now;
        return account;
    }

    public void Logout()
    {
        _client = null;
        _spaces = null;
    }

    /// <returns>false when the user canceled the sign-in</returns>
    public async Task<bool> LoginAsync(CancellationToken cancellationToken = default)
    {
        if (_client == null || _signedInTime.AddMinutes(_settings.Timeout - 1) < DateTime.Now)
        {
            SignIn.Recheck();
        }

        return await SignIn.EnsureSignedInAsync(cancellationToken).ConfigureAwait(false) && _client != null;
    }

    private async Task<ConfluenceClient> ClientAsync(CancellationToken cancellationToken)
    {
        if (!await LoginAsync(cancellationToken).ConfigureAwait(false) || _client is not { } client)
        {
            throw new UnauthorizedAccessException($"Not signed in to {_settings.Url}");
        }

        return client;
    }

    /// <summary>
    /// The spaces, loaded once an hour
    /// </summary>
    public async Task<IList<ConfluenceSpaceInfo>> GetSpacesAsync(CancellationToken cancellationToken = default)
    {
        var spaces = _spaces;
        if (spaces != null && DateTime.Now.AddMinutes(-60) < _spacesLoaded)
        {
            return spaces;
        }

        spaces = await (await ClientAsync(cancellationToken).ConfigureAwait(false)).GetSpacesAsync(cancellationToken).ConfigureAwait(false);
        _spaces = spaces;
        _spacesLoaded = DateTime.Now;
        return spaces;
    }

    public async Task<ConfluencePage?> GetSpaceHomepageAsync(ConfluenceSpaceInfo space, CancellationToken cancellationToken = default)
    {
        if (space.Homepage != null || space.HomepageId == null)
        {
            return space.Homepage;
        }

        return (await SearchByCqlAsync($"id={space.HomepageId.Value}", 1, cancellationToken).ConfigureAwait(false)).FirstOrDefault();
    }

    public Task<IList<ConfluencePage>> GetPageChildrenAsync(ConfluencePage parent, CancellationToken cancellationToken = default) =>
        SearchByCqlAsync($"type=page and parent={parent.Id}", 200, cancellationToken);

    public Task<IList<ConfluencePage>> SearchPagesAsync(string query, string? spaceKey, CancellationToken cancellationToken = default)
    {
        string cql = $"type=page and text~{ConfluenceClient.Quote(query)}";
        if (!string.IsNullOrEmpty(spaceKey))
        {
            cql += $" and space={ConfluenceClient.Quote(spaceKey)}";
        }

        return SearchByCqlAsync(cql, 20, cancellationToken);
    }

    private async Task<IList<ConfluencePage>> SearchByCqlAsync(string cql, int limit, CancellationToken cancellationToken) =>
        await (await ClientAsync(cancellationToken).ConfigureAwait(false)).SearchPagesAsync(cql, limit, cancellationToken).ConfigureAwait(false);

    public async Task<bool> HasAttachmentAsync(long pageId, string filename, CancellationToken cancellationToken = default) =>
        await (await ClientAsync(cancellationToken).ConfigureAwait(false)).HasAttachmentAsync(pageId, filename, cancellationToken).ConfigureAwait(false);

    public async Task AttachAsync(long pageId, EncodedImage image, string filename, bool newVersion, CancellationToken cancellationToken = default) =>
        await (await ClientAsync(cancellationToken).ConfigureAwait(false)).AttachAsync(pageId, image, filename, newVersion, cancellationToken).ConfigureAwait(false);
}

/// <summary>
/// Upload to a page of Confluence, chosen in the dialog
/// </summary>
public sealed class ConfluenceDestination(ConfluencePlugin plugin) : IDestination
{
    public string Designation => "Confluence";

    public string DisplayName => Texts.Get("Confluence", "upload_menu_item");

    public int Priority => 42;

    public bool IsAvailable => plugin.Settings.IsConfigured;

    public async Task<ExportResult> ExportAsync(Capture capture, CancellationToken cancellationToken = default)
    {
        var connector = plugin.Connector;
        var settings = plugin.Settings;
        string extension = "." + settings.UploadFormat;
        ConfluencePage page;
        string filename;
        try
        {
            // The sign-in before the pages load
            if (!await connector.LoginAsync(cancellationToken).ConfigureAwait(false))
            {
                return ExportResult.Aborted;
            }

            var spaces = await connector.GetSpacesAsync(cancellationToken).ConfigureAwait(false);
            var choice = await plugin.Dialogs.ChooseConfluencePageAsync(
                new ConfluenceUploadRequest(connector, settings, Path.GetFileNameWithoutExtension(settings.Filename(capture)), spaces)).ConfigureAwait(false);
            if (choice == null)
            {
                return ExportResult.Aborted;
            }

            page = choice.Page;
            filename = WithExtension(choice.Filename, extension);
        }
        catch (Exception ex) when (ex is HttpRequestException or UnauthorizedAccessException or JsonException or TaskCanceledException or UriFormatException)
        {
            Log.Error("Connecting to Confluence failed", ex);
            return ExportResult.Failed($"{Texts.Get("Confluence", "upload_failure")} {ex.Message}");
        }

        // The page already has an attachment with the name: the user chooses a new version of it or another name
        bool newVersion = false;
        try
        {
            while (!newVersion && await connector.HasAttachmentAsync(page.Id, filename, cancellationToken).ConfigureAwait(false))
            {
                var answer = await plugin.Dialogs.AttachmentExistsAsync(new AttachmentExistsRequest(page.Title, filename)).ConfigureAwait(false);
                if (answer == null)
                {
                    return ExportResult.Aborted;
                }

                newVersion = answer.NewVersion;
                filename = WithExtension(answer.Filename, extension);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            // The upload itself tells when the name is used
            Log.Warn($"Couldn't check the attachments of the page: {ex.Message}");
        }

        try
        {
            await connector.AttachAsync(page.Id, settings.Encode(capture), filename, newVersion, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or UnauthorizedAccessException or JsonException or TaskCanceledException)
        {
            Log.Error("Upload to Confluence failed", ex);
            return ExportResult.Failed($"{Texts.Get("Confluence", "upload_failure")} {ex.Message}");
        }

        Log.Info($"Uploaded to Confluence page {page.Id}");
        if (settings.CopyWikiMarkupForImageToClipboard)
        {
            try
            {
                await SystemClipboard.SetTextAsync("!" + filename + "!").ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException)
            {
                Log.Error("Couldn't copy the wiki markup to the clipboard", ex);
            }
        }

        if (settings.OpenPageAfterUpload)
        {
            ProcessRunner.StartDetached("xdg-open", page.Url);
        }

        return ExportResult.Done(message: Texts.Get("Confluence", "upload_success")) with { Link = page.Url };
    }

    private static string WithExtension(string filename, string extension)
    {
        filename = filename.Trim();
        return filename.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ? filename : filename + extension;
    }
}

/// <summary>
/// The Confluence part of the Atlassian plugin
/// </summary>
public sealed class ConfluencePlugin
{
    private readonly ConfluenceDestination _destination;

    public ConfluencePlugin(CoreSettings settings, ISecretStore secrets, IAtlassianDialogs dialogs, HttpSend? send = null)
    {
        Settings = new ConfluenceSettings(settings);
        Dialogs = dialogs;
        Connector = new ConfluenceConnector(Settings, secrets, dialogs.SignIn, send);
        _destination = new ConfluenceDestination(this);
    }

    public ConfluenceSettings Settings { get; }

    public ConfluenceConnector Connector { get; }

    public IAtlassianDialogs Dialogs { get; }

    public IReadOnlyList<IDestination> Destinations => [_destination];
}

#endregion

/// <summary>
/// Jira and Confluence like the Atlassian plugin of Windows; on the same site (Cloud: https://example.atlassian.net and .../wiki)
/// both use the same account
/// </summary>
public sealed class AtlassianPlugin : IGreenshotPlugin
{
    public AtlassianPlugin(CoreSettings settings, ISecretStore secrets, IAtlassianDialogs dialogs, HttpSend? send = null)
    {
        Jira = new JiraPlugin(settings, secrets, dialogs, send);
        Confluence = new ConfluencePlugin(settings, secrets, dialogs, send);
        Jira.Connector.SignIn.ShareCredentialsWith(Confluence.Connector.SignIn);
    }

    public JiraPlugin Jira { get; }

    public ConfluencePlugin Confluence { get; }

    public string Name => "Atlassian";

    public string DisplayName => Texts.Linux("plugin_atlassian");

    public IReadOnlyList<IDestination> Destinations => [.. Jira.Destinations, .. Confluence.Destinations];
}
