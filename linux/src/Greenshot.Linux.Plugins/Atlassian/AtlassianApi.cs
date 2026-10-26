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
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Greenshot.Linux.Core.Http;

namespace Greenshot.Linux.Plugins.Atlassian;

/// <summary>
/// Sends a request, <see cref="HttpService.SendAsync"/> outside the tests; redirects are not followed, the requests carry the credentials
/// </summary>
public delegate Task<HttpResult> HttpSend(HttpRequestMessage request, CancellationToken cancellationToken);

/// <summary>
/// Cloud with the e-mail address and an API token, Data Center with user name and password or a personal access token
/// </summary>
public static class AtlassianSignIn
{
    /// <summary>
    /// Server and Data Center answer 403 with this header after failed sign-ins, until the user solved a CAPTCHA in the browser
    /// </summary>
    public const string DeniedReasonHeader = "X-Authentication-Denied-Reason";

    public static ServerSignInOptions Options { get; } = new("https://id.atlassian.com/manage-profile/security/api-tokens",
        uri => uri.Host.EndsWith(".atlassian.net", StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// File names of attachments
/// </summary>
public static partial class AttachmentNames
{
    [GeneratedRegex(@"^(.*) \((\d{1,6})\)$", RegexOptions.CultureInvariant)]
    private static partial Regex Numbered();

    /// <summary>
    /// The next numbered name: "name (2).png" for "name.png", "name (3).png" for "name (2).png"
    /// </summary>
    public static string Next(string filename)
    {
        string extension = Path.GetExtension(filename);
        string name = filename[..^extension.Length];
        var numbered = Numbered().Match(name);
        return numbered.Success
            ? $"{numbered.Groups[1].Value} ({int.Parse(numbered.Groups[2].Value, CultureInfo.InvariantCulture) + 1}){extension}"
            : $"{name} (2){extension}";
    }
}

/// <summary>
/// The JSON calls of both clients
/// </summary>
public abstract class AtlassianClient
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly AuthenticationHeaderValue _authorization;

    protected AtlassianClient(Uri baseUri, ServerCredentials credentials, HttpSend? send)
    {
        // Relative uris are resolved below the path of the server (/wiki on Confluence Cloud)
        BaseUri = baseUri.AbsoluteUri.EndsWith('/') ? baseUri : new Uri(baseUri.AbsoluteUri + "/");
        _authorization = credentials.CreateAuthorization();
        Send = send ?? HttpService.SendAsync;
    }

    /// <summary>
    /// The server, ends with a /
    /// </summary>
    public Uri BaseUri { get; }

    protected HttpSend Send { get; }

    protected HttpRequestMessage CreateRequest(HttpMethod method, string relativeUri)
    {
        var request = new HttpRequestMessage(method, new Uri(BaseUri, relativeUri));
        request.Headers.Authorization = _authorization;
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    protected async Task<T?> GetAsync<T>(string relativeUri, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(HttpMethod.Get, relativeUri);
        var result = await Send(request, cancellationToken).ConfigureAwait(false);
        return Deserialize<T>(result.EnsureSuccess());
    }

    protected static StringContent JsonContent<T>(T body) => new(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");

    protected static T? Deserialize<T>(HttpResult result) =>
        string.IsNullOrEmpty(result.Content) ? default : JsonSerializer.Deserialize<T>(result.Content, Json);

    /// <summary>
    /// Rejected credentials (401) become <see cref="UnauthorizedAccessException"/>, which makes the sign-in ask again
    /// </summary>
    protected static HttpResult EnsureSignedIn(HttpResult result)
    {
        // Server and Data Center deny sign-ins after failed ones, until the user solved a CAPTCHA in the browser
        SignInDeniedException.ThrowWhenDenied(result, AtlassianSignIn.DeniedReasonHeader);
        return result.EnsureSuccess();
    }
}

#region Jira

// Only the fields Greenshot reads, the Jira REST API (v2 on Server/Data Center and Cloud, v3 search on Cloud) sends more.

public sealed class JiraIssue
{
    public string Key { get; set; } = string.Empty;

    public JiraIssueFields? Fields { get; set; }

    public override string ToString() => $"{Key}: {Fields?.Summary}";
}

public sealed class JiraIssueFields
{
    public string? Summary { get; set; }

    // "issuetype" in the JSON, the names are matched case insensitive
    public JiraIssueType? IssueType { get; set; }

    public JiraUser? Reporter { get; set; }

    public JiraUser? Assignee { get; set; }

    [JsonConverter(typeof(JiraDateTimeOffsetConverter))]
    public DateTimeOffset? Created { get; set; }

    public List<JiraAttachment>? Attachment { get; set; }
}

public sealed class JiraAttachment
{
    public string Filename { get; set; } = string.Empty;
}

public sealed class JiraIssueType
{
    public string? Name { get; set; }

    public Uri? IconUrl { get; set; }
}

public sealed class JiraUser
{
    public string? DisplayName { get; set; }
}

public sealed class JiraFilter
{
    public string Name { get; set; } = string.Empty;

    public string Jql { get; set; } = string.Empty;

    public override string ToString() => Name;
}

public sealed class JiraProject
{
    public string Key { get; set; } = string.Empty;
}

internal sealed class JiraErrorResponse
{
    public List<string>? ErrorMessages { get; set; }
}

internal sealed class JiraServerInfo
{
    public string? DeploymentType { get; set; }
}

internal sealed class JiraSearchRequest
{
    public string Jql { get; set; } = string.Empty;

    public IList<string> Fields { get; set; } = [];

    public int MaxResults { get; set; }
}

internal sealed class JiraSearchResult
{
    public IList<JiraIssue>? Issues { get; set; }
}

internal sealed class JiraComment
{
    public string Body { get; set; } = string.Empty;
}

/// <summary>
/// Jira writes the offset without a colon: 2026-01-02T10:00:00.000+0200
/// </summary>
public sealed class JiraDateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
{
    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? value = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (value.Length > 5 && value[^5] is '+' or '-' && char.IsDigit(value[^1]))
        {
            value = value.Insert(value.Length - 2, ":");
        }

        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result) ? result : null;
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}

/// <summary>
/// The few Jira REST calls Greenshot uses, see https://developer.atlassian.com/cloud/jira/platform/rest/v2/ and
/// https://developer.atlassian.com/server/jira/platform/rest/ for Server/Data Center.
/// </summary>
public sealed class JiraClient(Uri baseUri, ServerCredentials credentials, HttpSend? send = null) : AtlassianClient(baseUri, credentials, send)
{
    /// <summary>
    /// The fields the upload dialog and the destinations show
    /// </summary>
    private static readonly string[] IssueFields = ["summary", "issuetype", "reporter", "assignee", "created"];

    private const int MaxSearchResults = 50;
    private bool? _isCloud;

    public Uri GetBrowseUri(string issueKey) => new(BaseUri, "browse/" + Uri.EscapeDataString(issueKey));

    /// <summary>
    /// The name of the signed in user, checks the credentials
    /// </summary>
    public async Task<string?> GetMyselfAsync(CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, "rest/api/2/myself");
        var result = await Send(request, cancellationToken).ConfigureAwait(false);
        return Deserialize<JiraUser>(EnsureSignedIn(result))?.DisplayName;
    }

    /// <summary>
    /// All projects the user can see, the title monitor only accepts keys of these
    /// </summary>
    public async Task<IList<JiraProject>> GetProjectsAsync(CancellationToken cancellationToken = default) =>
        await GetAsync<List<JiraProject>>("rest/api/2/project", cancellationToken).ConfigureAwait(false) ?? [];

    /// <summary>
    /// The file names of the attachments of the issue, an issue can have several with the same name
    /// </summary>
    public async Task<IList<string>> GetAttachmentNamesAsync(string issueKey, CancellationToken cancellationToken = default)
    {
        var issue = await GetAsync<JiraIssue>($"rest/api/2/issue/{Uri.EscapeDataString(issueKey)}?fields=attachment", cancellationToken).ConfigureAwait(false);
        return issue?.Fields?.Attachment?.Select(attachment => attachment.Filename).ToList() ?? [];
    }

    /// <summary>
    /// The issue, null when it doesn't exist or the user can't see it
    /// </summary>
    public async Task<JiraIssue?> GetIssueAsync(string issueKey, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, $"rest/api/2/issue/{Uri.EscapeDataString(issueKey)}?fields={string.Join(",", IssueFields)}");
        var result = await Send(request, cancellationToken).ConfigureAwait(false);
        return result.StatusCode == HttpStatusCode.NotFound ? null : Deserialize<JiraIssue>(result.EnsureSuccess());
    }

    public async Task<IList<JiraFilter>> GetFavoriteFiltersAsync(CancellationToken cancellationToken = default) =>
        await GetAsync<List<JiraFilter>>("rest/api/2/filter/favourite", cancellationToken).ConfigureAwait(false) ?? [];

    /// <summary>
    /// The first issues of the JQL. Cloud removed POST search in 2025 for POST rest/api/3/search/jql, which Server and Data Center don't have.
    /// </summary>
    public async Task<IList<JiraIssue>> SearchAsync(string jql, CancellationToken cancellationToken = default)
    {
        _isCloud ??= await IsCloudAsync(cancellationToken).ConfigureAwait(false);
        using var request = CreateRequest(HttpMethod.Post, _isCloud == true ? "rest/api/3/search/jql" : "rest/api/2/search");
        request.Content = JsonContent(new JiraSearchRequest { Jql = jql, Fields = IssueFields, MaxResults = MaxSearchResults });
        var result = await Send(request, cancellationToken).ConfigureAwait(false);
        if (result.StatusCode == HttpStatusCode.BadRequest && ReadErrorMessages(result) is { } errorMessages)
        {
            // Jira says why it doesn't accept the JQL, e.g. Cloud rejects a filter without a restriction ("order by updated DESC")
            throw new HttpRequestException($"{(int)result.StatusCode} {result.ReasonPhrase}: {errorMessages}");
        }

        return Deserialize<JiraSearchResult>(result.EnsureSuccess())?.Issues ?? [];
    }

    public async Task AttachAsync(string issueKey, EncodedImage image, string filename, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, $"rest/api/2/issue/{Uri.EscapeDataString(issueKey)}/attachments");
        // Without it Jira rejects the upload as cross site request
        request.Headers.Add("X-Atlassian-Token", "no-check");
        request.Content = HttpContents.Multipart("file", image, filename);
        (await Send(request, cancellationToken).ConfigureAwait(false)).EnsureSuccess();
    }

    public async Task AddCommentAsync(string issueKey, string body, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Post, $"rest/api/2/issue/{Uri.EscapeDataString(issueKey)}/comment");
        request.Content = JsonContent(new JiraComment { Body = body });
        (await Send(request, cancellationToken).ConfigureAwait(false)).EnsureSuccess();
    }

    private async Task<bool> IsCloudAsync(CancellationToken cancellationToken)
    {
        var serverInfo = await GetAsync<JiraServerInfo>("rest/api/2/serverInfo", cancellationToken).ConfigureAwait(false);
        return string.Equals(serverInfo?.DeploymentType, "Cloud", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ReadErrorMessages(HttpResult result)
    {
        try
        {
            var messages = Deserialize<JiraErrorResponse>(result)?.ErrorMessages?.Where(message => !string.IsNullOrWhiteSpace(message)).ToList();
            return messages is { Count: > 0 } ? string.Join(" ", messages) : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

#endregion

#region Confluence

// Only the fields Greenshot reads from the Confluence REST API, ids come as strings

public sealed class ConfluenceContent
{
    public long Id { get; set; }

    public string? Type { get; set; }

    public string? Title { get; set; }

    public ConfluenceSpace? Space { get; set; }

    [JsonPropertyName("_links")]
    public ConfluenceLinks? Links { get; set; }
}

public sealed class ConfluenceLinks
{
    public string? WebUi { get; set; }

    public string? Next { get; set; }
}

public sealed class ConfluenceSpace
{
    public string Key { get; set; } = string.Empty;

    public string? Name { get; set; }

    public ConfluenceContent? Homepage { get; set; }

    public long? HomepageId { get; set; }
}

internal sealed class ConfluenceUser
{
    public string? Type { get; set; }

    public string? DisplayName { get; set; }
}

internal sealed class ConfluenceResults<T>
{
    public IList<T>? Results { get; set; }

    [JsonPropertyName("_links")]
    public ConfluenceLinks? Links { get; set; }
}

internal sealed class ConfluenceAttachment
{
    public string? Title { get; set; }
}

internal sealed class ConfluenceError
{
    public string? Message { get; set; }
}

/// <summary>
/// A page to attach to
/// </summary>
public sealed record ConfluencePage(long Id, string Title, string? SpaceKey, string Url)
{
    public override string ToString() => Title;
}

/// <summary>
/// A space with its home page, the root of the tree to browse
/// </summary>
public sealed record ConfluenceSpaceInfo(string Key, string Name, ConfluencePage? Homepage, long? HomepageId)
{
    /// <summary>
    /// Personal spaces have keys starting with ~
    /// </summary>
    public bool IsPersonal => Key.StartsWith('~');

    public override string ToString() => Name;
}

/// <summary>
/// The Confluence REST calls of Greenshot, v1 with the v2 calls Cloud replaced some with
/// </summary>
public sealed partial class ConfluenceClient(Uri baseUri, ServerCredentials credentials, HttpSend? send = null) : AtlassianClient(baseUri, credentials, send)
{
    private const int PageSize = 100;

    // Instances with thousands of (personal) spaces: show the first ones instead of loading for minutes
    private const int MaxSpaces = 2000;

    /// <summary>
    /// Check the credentials, the name of the user
    /// </summary>
    public async Task<string?> CheckCredentialsAsync(CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(HttpMethod.Get, "rest/api/user/current");
        var result = await Send(request, cancellationToken).ConfigureAwait(false);
        var user = Deserialize<ConfluenceUser>(EnsureSignedIn(result));
        // Instances with anonymous access answer for a request without valid credentials as the anonymous user
        if (user == null || string.Equals(user.Type, "anonymous", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Confluence answered as the anonymous user");
        }

        return user.DisplayName;
    }

    public async Task<IList<ConfluencePage>> SearchPagesAsync(string cql, int limit, CancellationToken cancellationToken = default)
    {
        var results = await GetAsync<ConfluenceResults<ConfluenceContent>>($"rest/api/content/search?cql={Uri.EscapeDataString(cql)}&limit={limit}&expand=space",
            cancellationToken).ConfigureAwait(false);
        return (results?.Results ?? [])
            .Where(content => content.Type == null || content.Type == "page")
            .Select(ToPage)
            .ToList();
    }

    public async Task<IList<ConfluenceSpaceInfo>> GetSpacesAsync(CancellationToken cancellationToken = default)
    {
        var spaces = new List<ConfluenceSpace>();
        while (spaces.Count < MaxSpaces)
        {
            using var request = CreateRequest(HttpMethod.Get, $"rest/api/space?expand=homepage&limit={PageSize}&start={spaces.Count}");
            var result = await Send(request, cancellationToken).ConfigureAwait(false);
            if (spaces.Count == 0 && result.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
            {
                // Removed on Cloud
                spaces = await GetSpacesV2Async(cancellationToken).ConfigureAwait(false);
                break;
            }

            var page = Deserialize<ConfluenceResults<ConfluenceSpace>>(result.EnsureSuccess())?.Results;
            if (page == null || page.Count == 0)
            {
                break;
            }

            spaces.AddRange(page);
            if (page.Count < PageSize)
            {
                break;
            }
        }

        return spaces
            .Select(space => new ConfluenceSpaceInfo(space.Key, space.Name ?? space.Key, space.Homepage == null ? null : ToPage(space.Homepage),
                space.Homepage?.Id ?? space.HomepageId))
            .OrderBy(space => space.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private async Task<List<ConfluenceSpace>> GetSpacesV2Async(CancellationToken cancellationToken)
    {
        var spaces = new List<ConfluenceSpace>();
        // On Cloud BaseUri ends with /wiki/
        string? next = $"api/v2/spaces?limit={PageSize}";
        while (next != null && spaces.Count < MaxSpaces)
        {
            var page = await GetAsync<ConfluenceResults<ConfluenceSpace>>(next, cancellationToken).ConfigureAwait(false);
            if (page?.Results != null)
            {
                spaces.AddRange(page.Results);
            }

            // The next link is an absolute path ("/wiki/api/v2/spaces?cursor=..."), which keeps only the server of BaseUri
            next = page?.Links?.Next;
        }

        return spaces;
    }

    public async Task<bool> HasAttachmentAsync(long pageId, string filename, CancellationToken cancellationToken = default)
    {
        string name = Uri.EscapeDataString(filename);
        HttpResult result;
        using (var request = CreateRequest(HttpMethod.Get, $"rest/api/content/{pageId}/child/attachment?filename={name}&limit=1"))
        {
            result = await Send(request, cancellationToken).ConfigureAwait(false);
        }

        if (result.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
        {
            // Removed on Cloud; on Cloud BaseUri ends with /wiki/
            using var request = CreateRequest(HttpMethod.Get, $"api/v2/pages/{pageId}/attachments?filename={name}&limit=1");
            result = await Send(request, cancellationToken).ConfigureAwait(false);
        }

        var attachments = Deserialize<ConfluenceResults<ConfluenceAttachment>>(result.EnsureSuccess())?.Results;
        return attachments != null && attachments.Any(attachment => string.Equals(attachment.Title, filename, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// POST fails for a file name the page already has, PUT adds a new version then
    /// </summary>
    public async Task AttachAsync(long pageId, EncodedImage image, string filename, bool newVersion = false, CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(newVersion ? HttpMethod.Put : HttpMethod.Post, $"rest/api/content/{pageId}/child/attachment");
        // Without it Confluence rejects the upload as cross site request
        request.Headers.Add("X-Atlassian-Token", "no-check");
        // Don't notify the watchers of the page for a screenshot
        request.Content = HttpContents.Multipart("file", image, filename, [new KeyValuePair<string, string>("minorEdit", "true")]);
        var result = await Send(request, cancellationToken).ConfigureAwait(false);
        if (result.StatusCode == HttpStatusCode.BadRequest && ReadErrorMessage(result) is { } message)
        {
            // Confluence says why, e.g. the page already has an attachment with this name
            throw new HttpRequestException($"{(int)result.StatusCode} {result.ReasonPhrase}: {message}");
        }

        result.EnsureSuccess();
    }

    public string GetWebUrl(ConfluenceContent content)
    {
        string? webUi = content.Links?.WebUi;
        return string.IsNullOrEmpty(webUi)
            ? BaseUri.AbsoluteUri + "pages/viewpage.action?pageId=" + content.Id
            : BaseUri.AbsoluteUri.TrimEnd('/') + "/" + webUi.TrimStart('/');
    }

    public static string Quote(string? value) => "\"" + (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    private ConfluencePage ToPage(ConfluenceContent content) => new(content.Id, content.Title ?? content.Id.ToString(CultureInfo.InvariantCulture),
        content.Space?.Key, GetWebUrl(content));

    private static string? ReadErrorMessage(HttpResult result)
    {
        try
        {
            string? message = Deserialize<ConfluenceError>(result)?.Message;
            // Cloud starts with the Java exception: "com.atlassian.confluence.api.service.exceptions.api.BadRequestException: Cannot add ..."
            return string.IsNullOrWhiteSpace(message) ? null : JavaExceptionPrefix().Replace(message, string.Empty);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    [GeneratedRegex(@"^(\w+\.)+\w*Exception:\s*", RegexOptions.CultureInvariant)]
    private static partial Regex JavaExceptionPrefix();
}

#endregion
