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
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Http;
using Greenshot.Base.Interfaces;
using Greenshot.Plugin.Atlassian.Jira.Api;
using Greenshot.Tests.Http;
using Xunit;

namespace Greenshot.Tests.Plugins
{
    /// <summary>
    /// The Jira REST calls, answered with JSON as documented by Atlassian
    /// </summary>
    public class JiraClientTests
    {
        private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(30);

        public JiraClientTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        private static (JiraClient Client, List<HttpRequestMessage> Requests, FakeHttpHandler Handler) CreateClient(string baseUri, Func<HttpRequestMessage, HttpResponseMessage> respond,
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
            return (new JiraClient(new Uri(baseUri), credentials ?? new ServerCredentials("user@example.com", "token"), new HttpService(handler, TestTimeout)), requests, handler);
        }

        private static HttpResponseMessage Json(string json, HttpStatusCode statusCode = HttpStatusCode.OK) =>
            new HttpResponseMessage(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

        [Fact]
        public async Task GetIssueAsync_ReadsTheFields_BelowThePathOfTheServer()
        {
            var (client, requests, _) = CreateClient("https://jira.example.com/jira", _ => Json(
                "{\"key\":\"ABC-12\",\"fields\":{\"summary\":\"Crash\",\"issuetype\":{\"name\":\"Bug\",\"iconUrl\":\"https://jira.example.com/jira/images/bug.png\"}," +
                "\"reporter\":{\"displayName\":\"Robin\"},\"assignee\":null,\"created\":\"2026-10-20T21:15:00.000+0200\"}}"));

            var issue = await client.GetIssueAsync("ABC-12", TestContext.Current.CancellationToken);

            Assert.Equal("ABC-12", issue.Key);
            Assert.Equal("Crash", issue.Fields.Summary);
            Assert.Equal("Bug", issue.Fields.IssueType.Name);
            Assert.Equal(new Uri("https://jira.example.com/jira/images/bug.png"), issue.Fields.IssueType.IconUrl);
            Assert.Equal("Robin", issue.Fields.Reporter.DisplayName);
            Assert.Null(issue.Fields.Assignee);
            Assert.Equal(new DateTimeOffset(2026, 10, 20, 21, 15, 0, TimeSpan.FromHours(2)), issue.Fields.Created);

            var request = Assert.Single(requests);
            Assert.Equal("/jira/rest/api/2/issue/ABC-12", request.RequestUri.AbsolutePath);
            Assert.Contains("fields=summary,issuetype,reporter,assignee,created", request.RequestUri.Query);
            Assert.Equal("Basic", request.Headers.Authorization.Scheme);
            Assert.Equal(Convert.ToBase64String(Encoding.UTF8.GetBytes("user@example.com:token")), request.Headers.Authorization.Parameter);
        }

        [Fact]
        public async Task GetIssueAsync_NotFound_Null()
        {
            var (client, _, _) = CreateClient("https://jira.example.com", _ => Json("{\"errorMessages\":[\"Issue does not exist\"]}", HttpStatusCode.NotFound));

            Assert.Null(await client.GetIssueAsync("ABC-1", TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task GetProjectsAsync_WrongCredentials_Throws()
        {
            var (client, _, _) = CreateClient("https://jira.example.com", _ => Json(string.Empty, HttpStatusCode.Unauthorized));

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => client.GetProjectsAsync(TestContext.Current.CancellationToken));
        }

        [Theory]
        [InlineData("Cloud", "/rest/api/3/search/jql")]
        [InlineData("DataCenter", "/rest/api/2/search")]
        [InlineData("Server", "/rest/api/2/search")]
        public async Task SearchAsync_UsesTheSearchOfTheDeployment(string deploymentType, string searchPath)
        {
            var (client, requests, handler) = CreateClient("https://jira.example.com", request => request.RequestUri.AbsolutePath.EndsWith("/serverInfo")
                ? Json($"{{\"deploymentType\":\"{deploymentType}\",\"version\":\"10.3.0\"}}")
                : Json("{\"issues\":[{\"key\":\"ABC-1\",\"fields\":{\"summary\":\"One\"}},{\"key\":\"ABC-2\",\"fields\":{\"summary\":\"Two\"}}],\"isLast\":true}"));

            var issues = await client.SearchAsync("project = ABC", TestContext.Current.CancellationToken);
            // The deployment is read once
            await client.SearchAsync("project = ABC", TestContext.Current.CancellationToken);

            Assert.Equal(new[] { "ABC-1", "ABC-2" }, issues.Select(issue => issue.Key));
            Assert.Equal(new[] { "/rest/api/2/serverInfo", searchPath, searchPath }, requests.Select(request => request.RequestUri.AbsolutePath));
            Assert.Equal(HttpMethod.Post, requests[1].Method);
            string body = Encoding.UTF8.GetString(handler.Requests[1].Body);
            Assert.Contains("\"jql\":\"project = ABC\"", body);
            Assert.Contains("\"fields\":[\"summary\",\"issuetype\",\"reporter\",\"assignee\",\"created\"]", body);
            Assert.Contains("\"maxResults\":50", body);
        }

        [Fact]
        public async Task SearchAsync_RejectedJql_ThrowsWithTheReasonOfJira()
        {
            var (client, _, _) = CreateClient("https://example.atlassian.net", request => request.RequestUri.AbsolutePath.EndsWith("/serverInfo")
                ? Json("{\"deploymentType\":\"Cloud\"}")
                : Json("{\"errorMessages\":[\"Unbounded JQL queries are not allowed here. Please add a search restriction to your query.\"],\"errors\":{}}", HttpStatusCode.BadRequest));

            var exception = await Assert.ThrowsAsync<HttpRequestException>(() => client.SearchAsync("order by updated DESC", TestContext.Current.CancellationToken));

            Assert.Contains("400", exception.Message);
            Assert.Contains("Unbounded JQL queries are not allowed here.", exception.Message);
        }

        [Fact]
        public async Task GetFavoriteFiltersAsync_ReadsNameAndJql()
        {
            var (client, requests, _) = CreateClient("https://example.atlassian.net", _ => Json("[{\"id\":\"10000\",\"name\":\"My open issues\",\"jql\":\"assignee = currentUser()\"}]"));

            var filter = Assert.Single(await client.GetFavoriteFiltersAsync(TestContext.Current.CancellationToken));

            Assert.Equal("My open issues", filter.Name);
            Assert.Equal("assignee = currentUser()", filter.Jql);
            Assert.Equal("/rest/api/2/filter/favourite", Assert.Single(requests).RequestUri.AbsolutePath);
        }

        [Fact]
        public async Task AttachAsync_PostsTheFileWithTheXsrfHeader()
        {
            var (client, requests, handler) = CreateClient("https://jira.example.com", _ => Json("[{\"id\":\"1\"}]"));

            await client.AttachAsync("ABC-1", new EncodedImage(new byte[] { 1, 2, 3 }, "png"), "capture.png", TestContext.Current.CancellationToken);

            var request = Assert.Single(requests);
            Assert.Equal("/rest/api/2/issue/ABC-1/attachments", request.RequestUri.AbsolutePath);
            Assert.Equal("no-check", request.Headers.GetValues("X-Atlassian-Token").Single());
            Assert.StartsWith("multipart/form-data", handler.Requests[0].ContentType);
            Assert.Contains("name=\"file\"; filename=\"capture.png\"", Encoding.UTF8.GetString(handler.Requests[0].Body));
        }

        [Fact]
        public async Task AddCommentAsync_PostsTheBody()
        {
            var (client, requests, handler) = CreateClient("https://jira.example.com", _ => Json("{\"id\":\"1\"}", HttpStatusCode.Created));

            await client.AddCommentAsync("ABC-1", "Looks good", TestContext.Current.CancellationToken);

            Assert.Equal("/rest/api/2/issue/ABC-1/comment", Assert.Single(requests).RequestUri.AbsolutePath);
            Assert.Equal("{\"body\":\"Looks good\"}", Encoding.UTF8.GetString(handler.Requests[0].Body));
        }

        [Fact]
        public void GetBrowseUri_BelowThePathOfTheServer()
        {
            var (client, _, _) = CreateClient("https://jira.example.com/jira/", _ => Json("{}"));

            Assert.Equal(new Uri("https://jira.example.com/jira/browse/ABC-1"), client.GetBrowseUri("ABC-1"));
        }
        [Fact]
        public async Task GetMyselfAsync_PersonalAccessToken_SendsBearer()
        {
            var (client, requests, _) = CreateClient("https://jira.example.com", _ => Json("{\"name\":\"robin\",\"displayName\":\"Robin\"}"),
                new ServerCredentials(null, "pat-123"));

            var myself = await client.GetMyselfAsync(TestContext.Current.CancellationToken);

            Assert.Equal("Robin", myself.DisplayName);
            var request = Assert.Single(requests);
            Assert.Equal("/rest/api/2/myself", request.RequestUri.AbsolutePath);
            Assert.Equal("Bearer", request.Headers.Authorization.Scheme);
            Assert.Equal("pat-123", request.Headers.Authorization.Parameter);
        }

        [Fact]
        public async Task GetMyselfAsync_CaptchaAfterFailedSignIns_Denied()
        {
            var (client, _, _) = CreateClient("https://jira.example.com", _ =>
            {
                var response = Json(string.Empty, HttpStatusCode.Forbidden);
                response.Headers.Add("X-Authentication-Denied-Reason", "CAPTCHA_CHALLENGE; login-url=https://jira.example.com/login.jsp");
                return response;
            });

            await Assert.ThrowsAsync<SignInDeniedException>(() => client.GetMyselfAsync(TestContext.Current.CancellationToken));
        }
    }
}
