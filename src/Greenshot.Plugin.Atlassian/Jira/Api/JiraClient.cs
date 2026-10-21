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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Http;
using Greenshot.Base.Interfaces;

namespace Greenshot.Plugin.Atlassian.Jira.Api;

/// <summary>
/// The few Jira REST calls Greenshot uses, see https://developer.atlassian.com/cloud/jira/platform/rest/v2/ and
/// https://developer.atlassian.com/server/jira/platform/rest/ for Server/Data Center.
/// Requests carry the credentials, so redirects are not followed (see <see cref="HttpService.WithoutRedirects"/>).
/// </summary>
public sealed class JiraClient
{
    /// <summary>
    /// The fields the upload dialog and the destinations show
    /// </summary>
    private static readonly string[] IssueFields = { "summary", "issuetype", "reporter", "assignee", "created" };

    private const int MaxSearchResults = 50;

    private readonly HttpService _httpService;
    private readonly AuthenticationHeaderValue _authorization;
    private bool? _isCloud;

    /// <param name="baseUri">the Jira server, e.g. https://example.atlassian.net or https://jira.example.com/jira</param>
    /// <param name="credentials">on Cloud the e-mail address with an API token, on Data Center user name and password or a personal access token</param>
    /// <param name="httpService">HttpService, default <see cref="HttpService.WithoutRedirects"/></param>
    public JiraClient(Uri baseUri, ServerCredentials credentials, HttpService httpService = null)
    {
        if (baseUri == null) throw new ArgumentNullException(nameof(baseUri));
        if (credentials == null) throw new ArgumentNullException(nameof(credentials));
        // Relative uris are resolved below the path of the server
        BaseUri = baseUri.AbsoluteUri.EndsWith("/") ? baseUri : new Uri(baseUri.AbsoluteUri + "/");
        _authorization = credentials.CreateAuthorization();
        _httpService = httpService ?? HttpService.WithoutRedirects;
    }

    /// <summary>
    /// The Jira server, ends with a /
    /// </summary>
    public Uri BaseUri { get; }

    /// <summary>
    /// The page of an issue in the browser
    /// </summary>
    public Uri GetBrowseUri(string issueKey) => new Uri(BaseUri, "browse/" + Uri.EscapeDataString(issueKey));

    /// <summary>
    /// The signed in user, checks the credentials
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">the credentials were rejected</exception>
    /// <exception cref="SignInDeniedException">Jira wants a CAPTCHA after failed sign-ins</exception>
    public async Task<JiraUser> GetMyselfAsync(CancellationToken cancellationToken = default)
    {
        var result = await _httpService.SendAsync(CreateRequest(HttpMethod.Get, "rest/api/2/myself"), cancellationToken).ConfigureAwait(false);
        // Server and Data Center deny sign-ins after failed ones, until the user solved a CAPTCHA in the browser
        SignInDeniedException.ThrowWhenDenied(result, AtlassianSignIn.DeniedReasonHeader);
        return Deserialize(result.EnsureSuccess(), JiraJsonContext.Default.JiraUser);
    }

    /// <summary>
    /// All projects the user can see, the title monitor only accepts keys of these
    /// </summary>
    public async Task<IList<JiraProject>> GetProjectsAsync(CancellationToken cancellationToken = default) =>
        await GetAsync("rest/api/2/project", JiraJsonContext.Default.ListJiraProject, cancellationToken).ConfigureAwait(false) ?? new List<JiraProject>();

    /// <summary>
    /// The issue, null when it doesn't exist or the user can't see it
    /// </summary>
    public async Task<JiraIssue> GetIssueAsync(string issueKey, CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(HttpMethod.Get, $"rest/api/2/issue/{Uri.EscapeDataString(issueKey)}?fields={string.Join(",", IssueFields)}");
        var result = await _httpService.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (result.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        return Deserialize(result.EnsureSuccess(), JiraJsonContext.Default.JiraIssue);
    }

    /// <summary>
    /// The favourite filters of the user
    /// </summary>
    public async Task<IList<JiraFilter>> GetFavoriteFiltersAsync(CancellationToken cancellationToken = default) =>
        await GetAsync("rest/api/2/filter/favourite", JiraJsonContext.Default.ListJiraFilter, cancellationToken).ConfigureAwait(false) ?? new List<JiraFilter>();

    /// <summary>
    /// The first issues of the JQL. Cloud removed POST search in 2025 for POST rest/api/3/search/jql, which Server and Data Center don't have.
    /// </summary>
    public async Task<IList<JiraIssue>> SearchAsync(string jql, CancellationToken cancellationToken = default)
    {
        _isCloud ??= await IsCloudAsync(cancellationToken).ConfigureAwait(false);
        var body = new JiraSearchRequest
        {
            Jql = jql,
            Fields = IssueFields,
            MaxResults = MaxSearchResults
        };
        var request = CreateRequest(HttpMethod.Post, _isCloud == true ? "rest/api/3/search/jql" : "rest/api/2/search");
        request.Content = CreateJsonContent(body, JiraJsonContext.Default.JiraSearchRequest);
        var result = await _httpService.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return Deserialize(result.EnsureSuccess(), JiraJsonContext.Default.JiraSearchResult)?.Issues ?? new List<JiraIssue>();
    }

    /// <summary>
    /// Attach the capture to the issue
    /// </summary>
    public async Task AttachAsync(string issueKey, EncodedImage image, string filename, CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(HttpMethod.Post, $"rest/api/2/issue/{Uri.EscapeDataString(issueKey)}/attachments");
        // Without it Jira rejects the upload as cross site request
        request.Headers.Add("X-Atlassian-Token", "no-check");
        request.Content = HttpContents.Multipart("file", image, filename);
        var result = await _httpService.SendAsync(request, cancellationToken).ConfigureAwait(false);
        result.EnsureSuccess();
    }

    /// <summary>
    /// Add a comment to the issue
    /// </summary>
    public async Task AddCommentAsync(string issueKey, string body, CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(HttpMethod.Post, $"rest/api/2/issue/{Uri.EscapeDataString(issueKey)}/comment");
        request.Content = CreateJsonContent(new JiraComment { Body = body }, JiraJsonContext.Default.JiraComment);
        var result = await _httpService.SendAsync(request, cancellationToken).ConfigureAwait(false);
        result.EnsureSuccess();
    }

    /// <summary>
    /// The bytes of an icon (PNG or SVG), the credentials only go along to the Jira server itself
    /// </summary>
    public async Task<byte[]> GetIconBytesAsync(Uri iconUri, CancellationToken cancellationToken = default)
    {
        if (iconUri == null)
        {
            return null;
        }

        if (!IsOnServer(iconUri))
        {
            return await HttpService.Current.GetBytesAsync(iconUri.AbsoluteUri, cancellationToken).ConfigureAwait(false);
        }

        var request = new HttpRequestMessage(HttpMethod.Get, iconUri);
        request.Headers.Authorization = _authorization;
        return await _httpService.GetBytesAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private bool IsOnServer(Uri uri) =>
        string.Equals(uri.Scheme, BaseUri.Scheme, StringComparison.OrdinalIgnoreCase)
        && string.Equals(uri.Authority, BaseUri.Authority, StringComparison.OrdinalIgnoreCase);

    private async Task<bool> IsCloudAsync(CancellationToken cancellationToken)
    {
        var serverInfo = await GetAsync("rest/api/2/serverInfo", JiraJsonContext.Default.JiraServerInfo, cancellationToken).ConfigureAwait(false);
        return string.Equals(serverInfo?.DeploymentType, "Cloud", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<T> GetAsync<T>(string relativeUri, JsonTypeInfo<T> typeInfo, CancellationToken cancellationToken)
    {
        var result = await _httpService.SendAsync(CreateRequest(HttpMethod.Get, relativeUri), cancellationToken).ConfigureAwait(false);
        return Deserialize(result.EnsureSuccess(), typeInfo);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativeUri)
    {
        var request = new HttpRequestMessage(method, new Uri(BaseUri, relativeUri));
        request.Headers.Authorization = _authorization;
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private static HttpContent CreateJsonContent<T>(T body, JsonTypeInfo<T> typeInfo) =>
        new StringContent(JsonSerializer.Serialize(body, typeInfo), Encoding.UTF8, "application/json");

    private static T Deserialize<T>(HttpResult result, JsonTypeInfo<T> typeInfo) =>
        string.IsNullOrEmpty(result.Content) ? default : JsonSerializer.Deserialize(result.Content, typeInfo);
}
