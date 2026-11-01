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
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core.OAuth;
using Greenshot.Base.Threading;
using Xunit;

namespace Greenshot.Tests.Http
{
    /// <summary>
    /// The OAuth 2 sign-in with the loopback redirect: state check, PKCE and the authorization URL, without network.
    /// </summary>
    public class OAuth2LoopbackTests
    {
        private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(30);

        private static int FreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint) listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static HttpClient CreateClient() => new HttpClient(new HttpClientHandler { UseProxy = false, AllowAutoRedirect = false });

        [Fact]
        public void CodeChallenge_IsBase64UrlSha256OfVerifier()
        {
            // Checked with: printf %s <verifier> | openssl dgst -sha256 -binary | base64 (then + to -, / to _, no =)
            Assert.Equal("SqbnFFz00QzJW9qjDQWZ82-zViqZuZN8nRVkAcB-Z9I", OAuth2Helper.CreateCodeChallenge("dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWm1oN6c"));
        }

        [Fact]
        public void RandomString_43Base64UrlCharacters_Different()
        {
            string first = OAuth2Helper.CreateRandomString();
            Assert.Equal(43, first.Length);
            Assert.Matches("^[A-Za-z0-9_-]+$", first);
            Assert.NotEqual(first, OAuth2Helper.CreateRandomString());
        }

        [Fact]
        public void AuthorizeUrl_HasStateRedirectAndChallenge_Encoded()
        {
            var settings = new OAuth2Settings
            {
                AuthorizeUrl = "https://example.com/oauth2/authorize",
                AuthorizeParameters = { { "token_access_type", "offline" } },
                ClientId = "client id",
                RedirectUrl = "http://localhost:31251/authorize/",
                CodeVerifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWm1oN6c"
            };
            string url = OAuth2Helper.CreateAuthorizeUrl(settings, "the-state");
            Assert.StartsWith("https://example.com/oauth2/authorize?", url);
            Assert.Contains("client_id=client%20id", url);
            Assert.Contains("response_type=code", url);
            Assert.Contains("state=the-state", url);
            Assert.Contains("redirect_uri=http%3A%2F%2Flocalhost%3A31251%2Fauthorize%2F", url);
            Assert.Contains("code_challenge=SqbnFFz00QzJW9qjDQWZ82-zViqZuZN8nRVkAcB-Z9I", url);
            Assert.Contains("code_challenge_method=S256", url);
            Assert.Contains("token_access_type=offline", url);

            settings.CodeVerifier = null;
            Assert.DoesNotContain("code_challenge", OAuth2Helper.CreateAuthorizeUrl(settings, "the-state"));
        }

        [Fact]
        public void FixedTimeEquals_OnlySameValue()
        {
            Assert.True(LocalServerCodeReceiver.FixedTimeEquals("abc", "abc"));
            Assert.False(LocalServerCodeReceiver.FixedTimeEquals("abd", "abc"));
            Assert.False(LocalServerCodeReceiver.FixedTimeEquals("ab", "abc"));
            Assert.False(LocalServerCodeReceiver.FixedTimeEquals(null, "abc"));
            Assert.False(LocalServerCodeReceiver.FixedTimeEquals("", ""));
        }

        [Fact]
        public async Task Receiver_IgnoresWrongStatePathAndMethod_TakesOnlyCodeOfTheRedirect()
        {
            using var receiver = new LocalServerCodeReceiver();
            string redirectUrl = receiver.Start(new[] { FreePort() });
            var waiting = receiver.WaitForRedirectAsync("expected-state", "Test", CancellationToken.None);

            using var client = CreateClient();
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(redirectUrl + "?code=attacker&state=other-state", TestContext.Current.CancellationToken)).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(redirectUrl + "?code=attacker", TestContext.Current.CancellationToken)).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync(redirectUrl + "other/?code=attacker&state=expected-state", TestContext.Current.CancellationToken)).StatusCode);
            var post = new FormUrlEncodedContent(new Dictionary<string, string> { { "refresh_token", "attacker" }, { "state", "expected-state" } });
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync(redirectUrl + "?state=expected-state", post, TestContext.Current.CancellationToken)).StatusCode);
            Assert.False(waiting.IsCompleted);

            var answer = await client.GetAsync(redirectUrl + "?code=the-code&state=expected-state&refresh_token=attacker&access_token=attacker", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, answer.StatusCode);
            Assert.False(answer.Headers.Contains("Access-Control-Allow-Origin"));

            var result = await waiting.WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
            Assert.Equal(new Dictionary<string, string> { { "code", "the-code" } }, result);
        }

        [Fact]
        public async Task Receiver_PassesErrorOfTheService()
        {
            using var receiver = new LocalServerCodeReceiver();
            string redirectUrl = receiver.Start(new[] { FreePort() });
            var waiting = receiver.WaitForRedirectAsync("expected-state", "Test", CancellationToken.None);

            using var client = CreateClient();
            await client.GetAsync(redirectUrl + "?error=access_denied&state=expected-state", TestContext.Current.CancellationToken);

            var result = await waiting.WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
            Assert.Equal("access_denied", result["error"]);
            Assert.False(result.ContainsKey("code"));
        }

        [Fact]
        public async Task Receiver_NoAnswerWithinTimeout_Empty()
        {
            using var receiver = new LocalServerCodeReceiver { Timeout = TimeSpan.FromMilliseconds(200) };
            receiver.Start(new[] { FreePort() });
            var result = await receiver.WaitForRedirectAsync("expected-state", "Test", CancellationToken.None).WaitAsync(TestTimeout, TestContext.Current.CancellationToken);
            Assert.Empty(result);
        }

        [Fact]
        public async Task Receiver_Canceled_Throws()
        {
            using var receiver = new LocalServerCodeReceiver();
            receiver.Start(new[] { FreePort() });
            using var cancellation = new CancellationTokenSource();
            var waiting = receiver.WaitForRedirectAsync("expected-state", "Test", cancellation.Token);
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting.WaitAsync(TestTimeout, TestContext.Current.CancellationToken));
        }

        [Fact]
        public void Receiver_PortInUse_TakesTheNextOne()
        {
            int busyPort = FreePort();
            int freePort = FreePort();
            using var busy = new HttpListener();
            busy.Prefixes.Add($"http://localhost:{busyPort}/authorize/");
            busy.Start();

            using var receiver = new LocalServerCodeReceiver();
            Assert.Equal($"http://localhost:{freePort}/authorize/", receiver.Start(new[] { busyPort, freePort }));
        }
    }
}
