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

using System.Net;
using Greenshot.Linux.Core.Capturing;
using Greenshot.Linux.Core.Configuration;
using Greenshot.Linux.Core.Http;
using Greenshot.Linux.Core.Languages;
using Greenshot.Linux.Plugins.Atlassian;
using SkiaSharp;

namespace Greenshot.Linux.Tests.Plugins;

/// <summary>
/// Shares the static Texts with the language tests
/// </summary>
[Collection(nameof(Languages.TextsTests))]
public sealed class AtlassianTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "gs-atlassian-" + Guid.NewGuid().ToString("N"));

    public AtlassianTests()
    {
        Texts.Initialize("en-US",
        [
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../src/Greenshot/Languages")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../../src/Greenshot.Plugin.Atlassian/Languages")),
        ]);
    }

    private CoreSettings Settings() => CoreSettings.Load(Path.Combine(_folder, "greenshot.ini"));

    private sealed class MemorySecrets : ISecretStore
    {
        public Dictionary<string, string> Values { get; } = new();

        public Task<string?> GetAsync(string key) => Task.FromResult(Values.GetValueOrDefault(key));

        public Task SetAsync(string key, string label, string secret)
        {
            Values[key] = secret;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            Values.Remove(key);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// A server answering by method and path, recording the requests
    /// </summary>
    private sealed class FakeServer
    {
        public List<(HttpMethod Method, string PathAndQuery, string? Authorization, string? Body)> Requests { get; } = [];

        public Func<HttpMethod, string, string?, (HttpStatusCode, string)> Answer { get; set; } = (_, _, _) => (HttpStatusCode.NotFound, string.Empty);

        public async Task<HttpResult> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string? body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            var pathAndQuery = Uri.UnescapeDataString(request.RequestUri!.PathAndQuery);
            Requests.Add((request.Method, pathAndQuery, request.Headers.Authorization?.ToString(), body));
            var (status, content) = Answer(request.Method, pathAndQuery, request.Headers.Authorization?.Parameter);
            return new HttpResult(status, status.ToString(), request.RequestUri, new Dictionary<string, string>(), content);
        }
    }

    private sealed class Dialogs(ServerSignInPrompt signIn) : IAtlassianDialogs
    {
        public ServerSignInPrompt SignIn { get; } = signIn;

        public Func<JiraUploadRequest, JiraUploadChoice?> Jira { get; set; } = _ => null;

        public Func<ConfluenceUploadRequest, ConfluenceUploadChoice?> Confluence { get; set; } = _ => null;

        public Func<AttachmentExistsRequest, AttachmentExistsChoice?> Exists { get; set; } = _ => null;

        public Task<JiraUploadChoice?> ChooseJiraIssueAsync(JiraUploadRequest request) => Task.FromResult(Jira(request));

        public Task<ConfluenceUploadChoice?> ChooseConfluencePageAsync(ConfluenceUploadRequest request) => Task.FromResult(Confluence(request));

        public Task<AttachmentExistsChoice?> AttachmentExistsAsync(AttachmentExistsRequest request) => Task.FromResult(Exists(request));
    }

    private static readonly string GoodBasic = Convert.ToBase64String("me@example.com:token"u8.ToArray());

    /// <summary>
    /// Jira and Confluence of one Cloud site, accepting only me@example.com with "token"
    /// </summary>
    private static (HttpStatusCode, string) Site(HttpMethod method, string path, string? authorization)
    {
        if (authorization != GoodBasic)
        {
            return (HttpStatusCode.Unauthorized, string.Empty);
        }

        return path switch
        {
            "/rest/api/2/myself" => (HttpStatusCode.OK, """{"displayName":"Me"}"""),
            "/rest/api/2/project" => (HttpStatusCode.OK, """[{"key":"GS"},{"key":"BUG2"}]"""),
            "/rest/api/2/serverInfo" => (HttpStatusCode.OK, """{"deploymentType":"Cloud"}"""),
            "/rest/api/2/filter/favourite" => (HttpStatusCode.OK, """[{"name":"Mine","jql":"assignee = currentUser()"}]"""),
            "/rest/api/3/search/jql" => (HttpStatusCode.OK, """{"issues":[{"key":"GS-1","fields":{"summary":"First","created":"2026-01-02T10:00:00.000+0200"}}]}"""),
            "/rest/api/2/issue/GS-7?fields=summary,issuetype,reporter,assignee,created" => (HttpStatusCode.OK,
                """{"key":"GS-7","fields":{"summary":"The editor crashes on save","issuetype":{"name":"Bug"}}}"""),
            "/rest/api/2/issue/GS-7?fields=attachment" => (HttpStatusCode.OK,
                """{"key":"GS-7","fields":{"attachment":[{"filename":"shot.png"},{"filename":"shot (2).png"}]}}"""),
            "/rest/api/2/issue/GS-7/attachments" or "/rest/api/2/issue/GS-7/comment" => (HttpStatusCode.OK, "{}"),
            "/wiki/rest/api/user/current" => (HttpStatusCode.OK, """{"type":"known","displayName":"Me"}"""),
            "/wiki/rest/api/space?expand=homepage&limit=100&start=0" => (HttpStatusCode.NotFound, string.Empty),
            "/wiki/api/v2/spaces?limit=100" => (HttpStatusCode.OK,
                """{"results":[{"key":"~me","name":"Mine","homepageId":"9"},{"key":"DOC","name":"Docs","homepageId":"10"}],"_links":{}}"""),
            _ when path.StartsWith("/wiki/rest/api/content/10/child/attachment?filename=", StringComparison.Ordinal) && method == HttpMethod.Get =>
                path.Contains("shot.png") ? (HttpStatusCode.OK, """{"results":[{"title":"shot.png"}]}""") : (HttpStatusCode.OK, """{"results":[]}"""),
            "/wiki/rest/api/content/10/child/attachment" => (HttpStatusCode.OK, "{}"),
            _ => (HttpStatusCode.NotFound, string.Empty),
        };
    }

    private (AtlassianPlugin Plugin, FakeServer Server, MemorySecrets Secrets, Dialogs Dialogs, List<ServerSignInRequest> Prompts) Create(
        Func<ServerSignInRequest, ServerCredentials?>? answer = null)
    {
        var settings = Settings();
        settings.SetValue("Jira", "Url", "https://example.atlassian.net");
        settings.SetValue("Confluence", "Url", "https://example.atlassian.net/wiki");
        settings.OutputFileFilenamePattern = "shot";
        var server = new FakeServer { Answer = Site };
        var secrets = new MemorySecrets();
        var prompts = new List<ServerSignInRequest>();
        var dialogs = new Dialogs(async (request, token) =>
        {
            prompts.Add(request);
            var credentials = answer?.Invoke(request);
            if (credentials == null)
            {
                return null;
            }

            await request.ConnectAsync(credentials, token);
            return new ServerSignInAnswer(credentials, true);
        });
        return (new AtlassianPlugin(settings, secrets, dialogs, server.SendAsync), server, secrets, dialogs, prompts);
    }

    private static Capture TestCapture(string? title = null)
    {
        var bitmap = new SKBitmap(4, 4);
        bitmap.Erase(SKColors.Red);
        return new Capture(bitmap, new CaptureDetails { Title = title });
    }

    [Fact]
    public async Task The_sign_in_asks_once_and_the_other_server_of_the_site_uses_the_same_account()
    {
        var (plugin, server, secrets, _, prompts) = Create(_ => new ServerCredentials("me@example.com", "token"));
        Assert.True(await plugin.Jira.Connector.LoginAsync());
        Assert.Single(prompts);
        Assert.True(prompts[0].Options.IsCloudUrl(prompts[0].Url));
        Assert.Equal("Me", plugin.Jira.Connector.SignIn.Account);
        Assert.Contains("server/https://example.atlassian.net", secrets.Values.Keys);
        Assert.DoesNotContain("token", secrets.Values.Keys.Single());

        // Confluence on the same site: the credentials of Jira, no window, and remembered for it too
        Assert.True(await plugin.Confluence.Connector.LoginAsync());
        Assert.Single(prompts);
        Assert.Contains("server/https://example.atlassian.net/wiki", secrets.Values.Keys);
        Assert.All(server.Requests, r => Assert.StartsWith("Basic ", r.Authorization));
    }

    [Fact]
    public async Task Rejected_credentials_show_the_window_with_the_reason_and_canceling_is_no_sign_in()
    {
        var (plugin, _, secrets, _, prompts) = Create();
        await secrets.SetAsync("server/https://example.atlassian.net", "Jira", new ServerCredentials("me@example.com", "old").Serialize());
        Assert.False(await plugin.Jira.Connector.LoginAsync());
        Assert.Equal(Texts.Core("signin_rejected"), prompts.Single().Error);
        Assert.Equal("me@example.com", prompts[0].Credentials!.UserName);
        Assert.False(plugin.Jira.Connector.IsLoggedIn);
    }

    [Fact]
    public async Task A_new_issue_in_a_window_title_is_loaded_and_becomes_a_destination()
    {
        var (plugin, _, _, _, _) = Create(_ => new ServerCredentials("me@example.com", "token"));
        Assert.Single(plugin.Destinations, d => d.Designation == "Jira");
        Assert.Null(await plugin.Jira.Connector.Monitor.ObserveAsync("[GS-7] The editor crashes - Jira"));

        await plugin.Jira.Connector.LoginAsync();
        // Not a project of the server
        Assert.Null(await plugin.Jira.Connector.Monitor.ObserveAsync("UTF-8 and ISO-8859 - Firefox"));
        var details = await plugin.Jira.Connector.Monitor.ObserveAsync("[GS-7] The editor crashes - Jira");
        Assert.Equal("The editor crashes on save", details!.JiraIssue!.Fields!.Summary);
        var issueDestination = plugin.Destinations.Single(d => d.Designation == "Jira:GS-7");
        Assert.Equal("GS-7: The editor crashes o", issueDestination.DisplayName);
    }

    [Fact]
    public async Task The_upload_attaches_and_the_comment_shows_the_capture_under_a_free_name()
    {
        var (plugin, server, _, dialogs, _) = Create(_ => new ServerCredentials("me@example.com", "token"));
        JiraUploadRequest? shown = null;
        dialogs.Jira = request =>
        {
            shown = request;
            return new JiraUploadChoice(new JiraIssue { Key = "GS-7" }, request.Filename, "Look: {capture}");
        };
        using var capture = TestCapture("GS-7 The editor crashes");
        var destination = plugin.Destinations.Single(d => d.Designation == "Jira");

        var result = await destination.ExportAsync(capture);

        Assert.True(result.Success, result.Message);
        Assert.Equal("https://example.atlassian.net/browse/GS-7", result.Link);
        // The issue of the window title is preselected
        Assert.Equal("GS-7", shown!.SuggestedIssue!.Key);
        Assert.Equal("shot.png", shown.Filename);
        var upload = server.Requests.Single(r => r.PathAndQuery == "/rest/api/2/issue/GS-7/attachments");
        Assert.Contains("filename=\"shot (3).png\"", upload.Body);
        var comment = server.Requests.Single(r => r.PathAndQuery == "/rest/api/2/issue/GS-7/comment");
        Assert.Contains("Look: !shot (3).png|thumbnail!", comment.Body);
    }

    [Fact]
    public async Task Cloud_searches_with_the_new_api()
    {
        var (plugin, server, _, _, _) = Create(_ => new ServerCredentials("me@example.com", "token"));
        var filter = (await plugin.Jira.Connector.GetFavoriteFiltersAsync()).Single();
        var issue = (await plugin.Jira.Connector.SearchAsync(filter)).Single();
        Assert.Equal(new DateTimeOffset(2026, 1, 2, 10, 0, 0, TimeSpan.FromHours(2)), issue.Fields!.Created);
        Assert.Contains("\"jql\":\"assignee = currentUser()\"", server.Requests.Single(r => r.PathAndQuery == "/rest/api/3/search/jql").Body);
    }

    [Fact]
    public async Task Confluence_asks_for_another_name_when_the_page_has_the_attachment()
    {
        var (plugin, server, _, dialogs, _) = Create(_ => new ServerCredentials("me@example.com", "token"));
        ConfluenceUploadRequest? shown = null;
        dialogs.Confluence = request =>
        {
            shown = request;
            return new ConfluenceUploadChoice(new ConfluencePage(10, "Home", "DOC", "https://example.atlassian.net/wiki/spaces/DOC"), request.Filename);
        };
        AttachmentExistsRequest? exists = null;
        dialogs.Exists = request =>
        {
            exists = request;
            return new AttachmentExistsChoice(false, request.OtherName);
        };
        plugin.Confluence.Settings.OpenPageAfterUpload = false;
        plugin.Confluence.Settings.CopyWikiMarkupForImageToClipboard = false;
        using var capture = TestCapture();

        var result = await plugin.Destinations.Single(d => d.Designation == "Confluence").ExportAsync(capture);

        Assert.True(result.Success, result.Message);
        Assert.Equal("shot", shown!.Filename);
        // Cloud has only the v2 spaces, the personal space comes along and the dialog leaves it out
        Assert.Equal(["Docs", "Mine"], shown.Spaces.Select(s => s.Name));
        Assert.True(shown.Spaces.Single(s => s.Key == "~me").IsPersonal);
        Assert.Equal("shot (2).png", exists!.OtherName);
        var upload = server.Requests.Single(r => r.Method == HttpMethod.Post && r.PathAndQuery == "/wiki/rest/api/content/10/child/attachment");
        Assert.Contains("filename=\"shot (2).png\"", upload.Body);
        Assert.Contains("minorEdit", upload.Body);
    }

    [Theory]
    [InlineData("shot.png", "shot (2).png")]
    [InlineData("shot (2).png", "shot (3).png")]
    [InlineData("a (b).png", "a (b) (2).png")]
    public void Attachment_names_are_numbered(string name, string next)
    {
        Assert.Equal(next, AttachmentNames.Next(name));
    }

    [Fact]
    public void The_windows_default_url_is_no_server()
    {
        var settings = Settings();
        settings.SetValue("Jira", "Url", "https://jira");
        var jira = new JiraSettings(settings);
        Assert.Equal(string.Empty, jira.Url);
        Assert.False(jira.IsConfigured);
        settings.SetValue("Jira", "Url", "https://jira.example.com/rpc/soap/jirasoapservice-v2?wsdl");
        Assert.Equal("https://jira.example.com", jira.ServerUrl);
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }
}
