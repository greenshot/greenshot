/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Http;
using Newtonsoft.Json;
using Xunit;

namespace Greenshot.Tests.Http
{
    public class HttpServiceTests
    {
        private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(30);

        public HttpServiceTests()
        {
            TestEnvironment.EnsureInitialized();
        }

        [Fact]
        public async Task SendAsync_ReturnsStatusHeadersContentAndFinalUri()
        {
            var redirected = new Uri("https://example.com/final");
            var handler = new FakeHttpHandler(request =>
            {
                var response = FakeHttpHandler.Text(HttpStatusCode.Created, "hello");
                response.Headers.Add("X-Test", new[] { "a", "b" });
                response.RequestMessage = new HttpRequestMessage(HttpMethod.Get, redirected);
                return response;
            });
            var service = new HttpService(handler, TestTimeout);

            var result = await service.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.com/start"), CancellationToken.None);

            Assert.Equal(HttpStatusCode.Created, result.StatusCode);
            Assert.True(result.IsSuccessStatusCode);
            Assert.Equal("hello", result.Content);
            Assert.Equal(redirected, result.RequestUri);
            Assert.Equal("a, b", result.Headers["x-test"]);
            Assert.StartsWith("text/plain", result.Headers["Content-Type"]);
        }

        [Fact]
        public async Task SendAsync_SendsTheGreenshotUserAgent()
        {
            var handler = new FakeHttpHandler(_ => FakeHttpHandler.Text(HttpStatusCode.OK, string.Empty));
            var service = new HttpService(handler, TestTimeout);

            await service.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.com/"), CancellationToken.None);

            Assert.StartsWith("Greenshot/", Assert.Single(handler.Requests).UserAgent);
        }

        [Fact]
        public async Task SendAsync_HttpError_DoesNotThrow_EnsureSuccessThrowsWithoutTheContent()
        {
            var handler = new FakeHttpHandler(_ => FakeHttpHandler.Text(HttpStatusCode.InternalServerError, "echo of secret-token"));
            var service = new HttpService(handler, TestTimeout);

            var result = await service.SendAsync(new HttpRequestMessage(HttpMethod.Post, "https://example.com/upload"), CancellationToken.None);

            Assert.False(result.IsSuccessStatusCode);
            Assert.Equal("echo of secret-token", result.Content);
            var exception = Assert.Throws<HttpRequestException>(() => result.EnsureSuccess());
            Assert.Contains("500", exception.Message);
            Assert.DoesNotContain("secret-token", exception.Message);
        }

        [Fact]
        public async Task EnsureSuccess_Unauthorized_ThrowsUnauthorizedAccessException()
        {
            var service = new HttpService(new FakeHttpHandler(_ => FakeHttpHandler.Text(HttpStatusCode.Unauthorized, string.Empty)), TestTimeout);

            var result = await service.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.com/"), CancellationToken.None);

            Assert.Throws<UnauthorizedAccessException>(() => result.EnsureSuccess());
        }

        [Fact]
        public async Task SendAsync_SlowServer_ThrowsTimeoutException()
        {
            var handler = new FakeHttpHandler(async (request, cancellationToken) =>
            {
                await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
                return FakeHttpHandler.Text(HttpStatusCode.OK, string.Empty);
            });
            var service = new HttpService(handler, TimeSpan.FromMilliseconds(200));

            var exception = await Assert.ThrowsAsync<TimeoutException>(() => service.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.com/path?key=secret"), CancellationToken.None));
            Assert.DoesNotContain("secret", exception.Message);
        }

        [Fact]
        public async Task SendAsync_CanceledByTheCaller_ThrowsOperationCanceledException()
        {
            var handler = new FakeHttpHandler(async (request, cancellationToken) =>
            {
                await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
                return FakeHttpHandler.Text(HttpStatusCode.OK, string.Empty);
            });
            var service = new HttpService(handler, TestTimeout);
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.com/"), cancellationTokenSource.Token));
        }

        [Fact]
        public async Task PostFormAsync_SendsUrlEncodedFields()
        {
            var handler = new FakeHttpHandler(_ => FakeHttpHandler.Text(HttpStatusCode.OK, "{}"));
            var service = new HttpService(handler, TestTimeout);

            await service.PostFormAsync("https://example.com/token", new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", "a b&c=d" },
                { "client_secret", null }
            }, CancellationToken.None);

            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("application/x-www-form-urlencoded", request.ContentType);
            Assert.Equal("grant_type=refresh_token&refresh_token=a+b%26c%3Dd&client_secret=", Encoding.ASCII.GetString(request.Body));
        }

        [Fact]
        public async Task GetJsonAsync_DeserializesTheContent()
        {
            var service = new HttpService(new FakeHttpHandler(_ => FakeHttpHandler.Text(HttpStatusCode.OK, "{\"release\":\"1.4.1\"}")), TestTimeout);

            var feed = await service.GetJsonAsync<Feed>(new Uri("https://example.com/feed.json"), CancellationToken.None);

            Assert.Equal("1.4.1", feed.Release);
        }

        [Fact]
        public async Task GetJsonAsync_HttpError_Throws()
        {
            var service = new HttpService(new FakeHttpHandler(_ => FakeHttpHandler.Text(HttpStatusCode.NotFound, "not here")), TestTimeout);

            await Assert.ThrowsAsync<HttpRequestException>(() => service.GetJsonAsync<Feed>(new Uri("https://example.com/feed.json"), CancellationToken.None));
        }

        [Fact]
        public async Task GetBytesAsync_ReturnsTheBytes_AndThrowsOnHttpError()
        {
            byte[] bytes = { 1, 2, 3 };
            var service = new HttpService(new FakeHttpHandler(request => request.RequestUri.AbsolutePath == "/ok"
                ? FakeHttpHandler.Bytes(bytes)
                : FakeHttpHandler.Text(HttpStatusCode.Forbidden, string.Empty)), TestTimeout);

            Assert.Equal(bytes, await service.GetBytesAsync("https://example.com/ok", CancellationToken.None));
            await Assert.ThrowsAsync<HttpRequestException>(() => service.GetBytesAsync("https://example.com/forbidden", CancellationToken.None));
        }

        [Fact]
        public async Task GetStatusAsync_ReturnsTheStatus_OrNullWithoutConnection()
        {
            var online = new HttpService(new FakeHttpHandler(_ => FakeHttpHandler.Text(HttpStatusCode.NotFound, string.Empty)), TestTimeout);
            var offline = new HttpService(new FakeHttpHandler((Func<HttpRequestMessage, HttpResponseMessage>)(_ => throw new HttpRequestException("no connection"))), TestTimeout);

            Assert.Equal(HttpStatusCode.NotFound, await online.GetStatusAsync("https://example.com/help", CancellationToken.None));
            Assert.Null(await offline.GetStatusAsync("https://example.com/help", CancellationToken.None));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CreateHandler_FollowsRedirectsOnlyWhenAsked_AndNeverSendsWindowsCredentials(bool followRedirects)
        {
            using var handler = HttpService.CreateHandler(followRedirects);

            Assert.Equal(followRedirects, handler.AllowAutoRedirect);
            Assert.False(handler.UseDefaultCredentials);
            Assert.Null(handler.Credentials);
        }

        [Theory]
        [InlineData("https://discord.com/api/webhooks/123/token?wait=true", "https://discord.com")]
        [InlineData("https://example.com:8443/upload?key=secret", "https://example.com:8443")]
        public void Describe_LeavesOutPathAndQuery(string url, string expected)
        {
            Assert.Equal(expected, HttpService.Describe(new Uri(url)));
        }

        private sealed class Feed
        {
            [JsonProperty("release")] public string Release { get; set; }
        }
    }
}
