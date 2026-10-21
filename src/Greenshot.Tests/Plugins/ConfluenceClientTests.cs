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
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Greenshot.Base.Http;
using Greenshot.Base.Interfaces;
using Greenshot.Plugin.Atlassian.Confluence.Api;
using Greenshot.Tests.Http;
using Xunit;

namespace Greenshot.Tests.Plugins
{
    /// <summary>
    /// The Confluence REST calls, answered with JSON as documented by Atlassian
    /// </summary>
    public class ConfluenceClientTests
    {
        private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(30);

        public ConfluenceClientTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static (ConfluenceClient Client, List<HttpRequestMessage> Requests, FakeHttpHandler Handler) CreateClient(string baseUri, Func<HttpRequestMessage, HttpResponseMessage> respond,
            ServerCredentials credentials = null)
        {
            var requests = new List<HttpRequestMessage>();
            var handler = new FakeHttpHandler(request =>
            {
                lock (requests)
                {
                    requests.Add(request);
                }

                return respond(request);
            });
            return (new ConfluenceClient(new Uri(baseUri), credentials ?? new ServerCredentials("user@example.com", "token"), new HttpService(handler, TestTimeout)), requests, handler);
        }

        private static HttpResponseMessage Json(string json, HttpStatusCode statusCode = HttpStatusCode.OK) =>
            new HttpResponseMessage(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

        [Fact]
        public async Task SearchAsync_SendsTheCql_ReadsStringIdsAndTheWebLink()
        {
            var (client, requests, _) = CreateClient("https://example.atlassian.net/wiki", _ => Json(
                "{\"results\":[{\"id\":\"1234567\",\"type\":\"page\",\"title\":\"Release notes\",\"space\":{\"key\":\"DOC\",\"name\":\"Docs\"}," +
                "\"_links\":{\"webui\":\"/spaces/DOC/pages/1234567/Release+notes\"}}],\"_links\":{\"base\":\"https://example.atlassian.net/wiki\"}}"));

            var content = Assert.Single(await client.SearchAsync("type=page and title=" + ConfluenceClient.Quote("Release notes"), 1, TestContext.Current.CancellationToken));

            Assert.Equal(1234567, content.Id);
            Assert.Equal("Release notes", content.Title);
            Assert.Equal("DOC", content.Space.Key);
            Assert.Equal("https://example.atlassian.net/wiki/spaces/DOC/pages/1234567/Release+notes", client.GetWebUrl(content));
            var request = Assert.Single(requests);
            Assert.Equal("/wiki/rest/api/content/search", request.RequestUri.AbsolutePath);
            Assert.Contains("cql=type%3Dpage%20and%20title%3D%22Release%20notes%22", request.RequestUri.Query);
            Assert.Contains("limit=1", request.RequestUri.Query);
        }

        [Fact]
        public void Quote_EscapesQuotesAndBackslashes()
        {
            Assert.Equal("\"say \\\"hi\\\" \\\\o/\"", ConfluenceClient.Quote("say \"hi\" \\o/"));
        }

        [Fact]
        public async Task CheckCredentialsAsync_WrongCredentials_Throws()
        {
            var (client, requests, _) = CreateClient("https://confluence.example.com", _ => Json(string.Empty, HttpStatusCode.Unauthorized));

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => client.CheckCredentialsAsync(TestContext.Current.CancellationToken));
            Assert.Equal("/rest/api/user/current", Assert.Single(requests).RequestUri.AbsolutePath);
        }

        [Fact]
        public async Task GetSpacesAsync_V1_ReadsTheHomepage_AndPages()
        {
            var (client, requests, _) = CreateClient("https://confluence.example.com", request => request.RequestUri.Query.Contains("start=0")
                ? Json("{\"results\":[" + string.Join(",", Enumerable.Range(0, 100).Select(i => $"{{\"key\":\"S{i}\",\"name\":\"Space {i}\",\"homepage\":{{\"id\":\"{i + 1000}\",\"title\":\"Home {i}\"}}}}")) + "]}")
                : Json("{\"results\":[{\"key\":\"LAST\",\"name\":\"Last\"}]}"));

            var spaces = await client.GetSpacesAsync(TestContext.Current.CancellationToken);

            Assert.Equal(101, spaces.Count);
            Assert.Equal(1000, spaces[0].Homepage.Id);
            Assert.Equal("Home 0", spaces[0].Homepage.Title);
            Assert.Null(spaces[100].Homepage);
            Assert.Equal(2, requests.Count);
            Assert.Contains("expand=homepage", requests[0].RequestUri.Query);
            Assert.Contains("start=100", requests[1].RequestUri.Query);
        }

        [Fact]
        public async Task GetSpacesAsync_V1Gone_UsesV2AndFollowsTheNextLink()
        {
            var (client, requests, _) = CreateClient("https://example.atlassian.net/wiki", request =>
            {
                if (request.RequestUri.AbsolutePath == "/wiki/rest/api/space")
                {
                    return Json("{\"message\":\"Gone\"}", HttpStatusCode.Gone);
                }

                return request.RequestUri.Query.Contains("cursor=")
                    ? Json("{\"results\":[{\"id\":\"2\",\"key\":\"B\",\"name\":\"Beta\",\"homepageId\":\"22\"}],\"_links\":{}}")
                    : Json("{\"results\":[{\"id\":\"1\",\"key\":\"A\",\"name\":\"Alpha\",\"homepageId\":\"11\"}],\"_links\":{\"next\":\"/wiki/api/v2/spaces?cursor=abc&limit=100\"}}");
            });

            var spaces = await client.GetSpacesAsync(TestContext.Current.CancellationToken);

            Assert.Equal(new[] { "A", "B" }, spaces.Select(space => space.Key));
            Assert.Equal(new long?[] { 11, 22 }, spaces.Select(space => space.HomepageId));
            Assert.Equal(new[]
            {
                "https://example.atlassian.net/wiki/rest/api/space",
                "https://example.atlassian.net/wiki/api/v2/spaces",
                "https://example.atlassian.net/wiki/api/v2/spaces"
            }, requests.Select(request => request.RequestUri.GetLeftPart(UriPartial.Path)));
            Assert.Contains("cursor=abc", requests[2].RequestUri.Query);
        }

        [Fact]
        public async Task AttachAsync_PostsTheFileAsMinorEditWithTheXsrfHeader()
        {
            var (client, requests, handler) = CreateClient("https://confluence.example.com", _ => Json("{\"results\":[]}"));

            await client.AttachAsync(42, new EncodedImage(new byte[] { 1, 2, 3 }, "png"), "capture.png", null, TestContext.Current.CancellationToken);

            var request = Assert.Single(requests);
            Assert.Equal("/rest/api/content/42/child/attachment", request.RequestUri.AbsolutePath);
            Assert.Equal("no-check", request.Headers.GetValues("X-Atlassian-Token").Single());
            string body = Encoding.UTF8.GetString(handler.Requests[0].Body);
            Assert.Contains("name=\"minorEdit\"", body);
            Assert.DoesNotContain("name=\"comment\"", body);
            Assert.Contains("name=\"file\"; filename=\"capture.png\"", body);
        }
        [Fact]
        public async Task CheckCredentialsAsync_ReturnsTheDisplayName_WithBearerForAToken()
        {
            var (client, requests, _) = CreateClient("https://confluence.example.com", _ => Json("{\"type\":\"known\",\"username\":\"robin\",\"displayName\":\"Robin\"}"),
                new ServerCredentials(null, "pat-123"));

            Assert.Equal("Robin", await client.CheckCredentialsAsync(TestContext.Current.CancellationToken));
            var request = Assert.Single(requests);
            Assert.Equal("Bearer", request.Headers.Authorization.Scheme);
            Assert.Equal("pat-123", request.Headers.Authorization.Parameter);
        }

        [Fact]
        public async Task CheckCredentialsAsync_AnonymousUser_Throws()
        {
            var (client, _, _) = CreateClient("https://confluence.example.com", _ => Json("{\"type\":\"anonymous\",\"displayName\":\"Anonymous\"}"));

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => client.CheckCredentialsAsync(TestContext.Current.CancellationToken));
        }
    }
}
