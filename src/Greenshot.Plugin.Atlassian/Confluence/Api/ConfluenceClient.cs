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
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Http;
using Greenshot.Base.Interfaces;

namespace Greenshot.Plugin.Atlassian.Confluence.Api;

/// <summary>
/// The few Confluence REST calls Greenshot uses, on Server/Data Center and Cloud. Cloud deprecated most v1 calls with a
/// v2 replacement, so pages are found with CQL (rest/api/content/search, which has no v2 replacement) and the spaces come
/// from v2 when v1 is gone. See https://developer.atlassian.com/cloud/confluence/rest/v1/ and
/// https://developer.atlassian.com/server/confluence/rest/
/// Requests carry the credentials, so redirects are not followed (see <see cref="HttpService.WithoutRedirects"/>).
/// </summary>
public sealed class ConfluenceClient
{
    private const int PageSize = 100;

    // Instances with thousands of (personal) spaces: show the first ones instead of loading for minutes
    private const int MaxSpaces = 2000;

    private readonly HttpService _httpService;
    private readonly AuthenticationHeaderValue _authorization;

    /// <param name="baseUri">the Confluence url, e.g. https://example.atlassian.net/wiki or https://confluence.example.com</param>
    /// <param name="credentials">on Cloud the e-mail address with an API token, on Data Center user name and password or a personal access token</param>
    /// <param name="httpService">HttpService, default <see cref="HttpService.WithoutRedirects"/></param>
    public ConfluenceClient(Uri baseUri, ServerCredentials credentials, HttpService httpService = null)
    {
        if (baseUri == null) throw new ArgumentNullException(nameof(baseUri));
        if (credentials == null) throw new ArgumentNullException(nameof(credentials));
        // Relative uris are resolved below the path of Confluence (/wiki on Cloud)
        BaseUri = baseUri.AbsoluteUri.EndsWith("/") ? baseUri : new Uri(baseUri.AbsoluteUri + "/");
        _authorization = credentials.CreateAuthorization();
        _httpService = httpService ?? HttpService.WithoutRedirects;
    }

    /// <summary>
    /// The Confluence url, ends with a /
    /// </summary>
    public Uri BaseUri { get; }

    /// <summary>
    /// Check the credentials
    /// </summary>
    /// <returns>the name of the signed in user</returns>
    /// <exception cref="UnauthorizedAccessException">wrong credentials</exception>
    /// <exception cref="SignInDeniedException">Data Center wants a CAPTCHA after failed sign-ins</exception>
    public async Task<string> CheckCredentialsAsync(CancellationToken cancellationToken = default)
    {
        var result = await _httpService.SendAsync(CreateRequest(HttpMethod.Get, "rest/api/user/current"), cancellationToken).ConfigureAwait(false);
        SignInDeniedException.ThrowWhenDenied(result, AtlassianSignIn.DeniedReasonHeader);
        var user = Deserialize(result.EnsureSuccess(), ConfluenceJsonContext.Default.ConfluenceUser);
        // Instances with anonymous access answer for a request without valid credentials as the anonymous user
        if (user == null || string.Equals(user.Type, "anonymous", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Confluence answered as the anonymous user");
        }

        return user.DisplayName;
    }

    /// <summary>
    /// The pages for the CQL, see https://developer.atlassian.com/cloud/confluence/advanced-searching-using-cql/
    /// </summary>
    public async Task<IList<ConfluenceContent>> SearchAsync(string cql, int limit, CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(HttpMethod.Get, $"rest/api/content/search?cql={Uri.EscapeDataString(cql)}&limit={limit}&expand=space");
        var result = await _httpService.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return Deserialize(result.EnsureSuccess(), ConfluenceJsonContext.Default.ConfluenceResultsConfluenceContent)?.Results ?? new List<ConfluenceContent>();
    }

    /// <summary>
    /// All spaces with their home page (v1) or its id (v2)
    /// </summary>
    public async Task<IList<ConfluenceSpace>> GetSpacesAsync(CancellationToken cancellationToken = default)
    {
        var spaces = new List<ConfluenceSpace>();
        while (spaces.Count < MaxSpaces)
        {
            var request = CreateRequest(HttpMethod.Get, $"rest/api/space?expand=homepage&limit={PageSize}&start={spaces.Count}");
            var result = await _httpService.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (spaces.Count == 0 && result.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone)
            {
                // Removed on Cloud
                return await GetSpacesV2Async(cancellationToken).ConfigureAwait(false);
            }

            var page = Deserialize(result.EnsureSuccess(), ConfluenceJsonContext.Default.ConfluenceResultsConfluenceSpace)?.Results;
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

        return spaces;
    }

    private async Task<IList<ConfluenceSpace>> GetSpacesV2Async(CancellationToken cancellationToken)
    {
        var spaces = new List<ConfluenceSpace>();
        // On Cloud BaseUri ends with /wiki/
        string next = $"api/v2/spaces?limit={PageSize}";
        while (next != null && spaces.Count < MaxSpaces)
        {
            var request = CreateRequest(HttpMethod.Get, next);
            var result = await _httpService.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var page = Deserialize(result.EnsureSuccess(), ConfluenceJsonContext.Default.ConfluenceResultsConfluenceSpace);
            if (page?.Results != null)
            {
                spaces.AddRange(page.Results);
            }

            // The next link is an absolute path ("/wiki/api/v2/spaces?cursor=..."), which keeps only the server of BaseUri
            next = page?.Links?.Next;
        }

        return spaces;
    }

    /// <summary>
    /// Attach the capture to the page
    /// </summary>
    public async Task AttachAsync(long pageId, EncodedImage image, string filename, string comment, CancellationToken cancellationToken = default)
    {
        var request = CreateRequest(HttpMethod.Post, $"rest/api/content/{pageId}/child/attachment");
        // Without it Confluence rejects the upload as cross site request
        request.Headers.Add("X-Atlassian-Token", "no-check");
        var fields = new List<KeyValuePair<string, string>>
        {
            // Don't notify the watchers of the page for a screenshot
            new KeyValuePair<string, string>("minorEdit", "true")
        };
        if (!string.IsNullOrEmpty(comment))
        {
            fields.Add(new KeyValuePair<string, string>("comment", comment));
        }

        request.Content = HttpContents.Multipart("file", image, filename, fields);
        var result = await _httpService.SendAsync(request, cancellationToken).ConfigureAwait(false);
        result.EnsureSuccess();
    }

    /// <summary>
    /// The page in the browser, the webui link is relative to the Confluence url (with /wiki on Cloud)
    /// </summary>
    public string GetWebUrl(ConfluenceContent content)
    {
        string webUi = content?.Links?.WebUi;
        if (string.IsNullOrEmpty(webUi))
        {
            return content == null ? null : BaseUri.AbsoluteUri + "pages/viewpage.action?pageId=" + content.Id;
        }

        return BaseUri.AbsoluteUri.TrimEnd('/') + "/" + webUi.TrimStart('/');
    }

    /// <summary>
    /// A CQL string value: quoted, with \ and " escaped
    /// </summary>
    public static string Quote(string value) => "\"" + (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";

    private HttpRequestMessage CreateRequest(HttpMethod method, string uri)
    {
        var request = new HttpRequestMessage(method, new Uri(BaseUri, uri));
        request.Headers.Authorization = _authorization;
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    private static T Deserialize<T>(HttpResult result, JsonTypeInfo<T> typeInfo) =>
        string.IsNullOrEmpty(result.Content) ? default : JsonSerializer.Deserialize(result.Content, typeInfo);
}
