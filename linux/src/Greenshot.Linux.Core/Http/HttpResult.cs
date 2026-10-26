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

// Ported from Greenshot.Base, which has no nullable annotations
#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;

namespace Greenshot.Linux.Core.Http
{
    /// <summary>
    /// The response of a request, read completely: the request is done and nothing needs to be disposed.
    /// </summary>
    public sealed class HttpResult
    {
        public HttpResult(HttpStatusCode statusCode, string reasonPhrase, Uri requestUri, IReadOnlyDictionary<string, string> headers, string content)
        {
            StatusCode = statusCode;
            ReasonPhrase = reasonPhrase;
            RequestUri = requestUri;
            Headers = headers ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Content = content;
        }

        public HttpStatusCode StatusCode { get; }

        public string ReasonPhrase { get; }

        /// <summary>
        /// The uri which answered, after redirects
        /// </summary>
        public Uri RequestUri { get; }

        /// <summary>
        /// The response and content headers, the names case insensitive, several values joined with ", "
        /// </summary>
        public IReadOnlyDictionary<string, string> Headers { get; }

        /// <summary>
        /// The content as string, null when the response had none
        /// </summary>
        public string Content { get; }

        public bool IsSuccessStatusCode => (int)StatusCode >= 200 && (int)StatusCode <= 299;

        /// <summary>
        /// Throw when the status is not a success: UnauthorizedAccessException for 401, else HttpRequestException.
        /// The message has the status only, the content can repeat what was sent (tokens) and goes into no message or log.
        /// </summary>
        /// <returns>this, for chaining</returns>
        public HttpResult EnsureSuccess()
        {
            if (IsSuccessStatusCode)
            {
                return this;
            }

            string message = $"{(int)StatusCode} {ReasonPhrase}";
            if (StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException(message);
            }

            throw new HttpRequestException(message);
        }

        internal static IReadOnlyDictionary<string, string> CollectHeaders(HttpResponseMessage response)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            IEnumerable<KeyValuePair<string, IEnumerable<string>>> all = response.Content == null ? response.Headers : response.Headers.Concat(response.Content.Headers);
            foreach (var header in all)
            {
                headers[header.Key] = string.Join(", ", header.Value);
            }

            return headers;
        }
    }
}
