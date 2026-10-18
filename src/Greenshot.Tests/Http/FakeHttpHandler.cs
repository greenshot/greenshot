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
using System.Threading;
using System.Threading.Tasks;

namespace Greenshot.Tests.Http
{
    /// <summary>
    /// Answers requests without network and records what was sent (the content is read before the request is disposed).
    /// </summary>
    internal sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _respond;

        public FakeHttpHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
        {
            _respond = respond;
        }

        public FakeHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : this((request, _) => Task.FromResult(respond(request)))
        {
        }

        public List<RecordedRequest> Requests { get; } = new List<RecordedRequest>();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            byte[] body = request.Content == null ? null : await request.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
            lock (Requests)
            {
                Requests.Add(new RecordedRequest(request.Method, request.RequestUri, request.Content?.Headers.ContentType?.ToString(), body, request.Headers.UserAgent.ToString()));
            }

            var response = await _respond(request, cancellationToken).ConfigureAwait(false);
            response.RequestMessage ??= request;
            return response;
        }

        public static HttpResponseMessage Text(HttpStatusCode statusCode, string content) => new HttpResponseMessage(statusCode) { Content = new StringContent(content) };

        public static HttpResponseMessage Bytes(byte[] content) => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(content) };
    }

    internal sealed class RecordedRequest
    {
        public RecordedRequest(HttpMethod method, Uri uri, string contentType, byte[] body, string userAgent)
        {
            Method = method;
            Uri = uri;
            ContentType = contentType;
            Body = body;
            UserAgent = userAgent;
        }

        public HttpMethod Method { get; }
        public Uri Uri { get; }
        public string ContentType { get; }
        public byte[] Body { get; }
        public string UserAgent { get; }
    }
}
