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
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace Greenshot.Base.Core.OAuth
{
    /// <summary>
    /// Receives the redirect of an OAuth 2 authorization on a loopback address (RFC 8252 section 7.3).
    /// Only a GET on the redirect path with the state of this authorization counts, everything else gets an error page and is ignored.
    /// </summary>
    public sealed class LocalServerCodeReceiver : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(LocalServerCodeReceiver));
        private const string RedirectPath = "/authorize/";
        private const string RedirectUrlFormat = "http://localhost:{0}" + RedirectPath;
        private static readonly string[] AnswerKeys = { "code", "error", "error_description" };

        private HttpListener _listener;

        /// <summary>
        /// How long to wait for the browser to redirect back, after that the authorization counts as declined.
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(4);

        /// <summary>
        /// Start listening on the first port that is free
        /// </summary>
        /// <param name="ports">the ports to try in this order</param>
        /// <returns>the redirect URL to send to the service</returns>
        public string Start(IList<int> ports)
        {
            foreach (int port in ports)
            {
                string redirectUrl = string.Format(RedirectUrlFormat, port);
                var listener = new HttpListener();
                listener.Prefixes.Add(redirectUrl);
                try
                {
                    listener.Start();
                    _listener = listener;
                    return redirectUrl;
                }
                catch (HttpListenerException ex)
                {
                    Log.WarnFormat("Port {0} is not available for the sign-in: {1}", port, ex.Message);
                    listener.Close();
                }
            }

            throw new InvalidOperationException($"None of the ports {string.Join(", ", ports)} is available for the sign-in.");
        }

        /// <summary>
        /// Wait (without blocking a thread) for the redirect that carries the expected state.
        /// </summary>
        /// <param name="expectedState">the state sent with this authorization</param>
        /// <param name="cloudServiceName">for the page shown in the browser</param>
        /// <param name="cancellationToken">CancellationToken, stops the waiting</param>
        /// <returns>code or error (and error_description) of the redirect, empty when nothing came within the timeout</returns>
        public async Task<IDictionary<string, string>> WaitForRedirectAsync(string expectedState, string cloudServiceName, CancellationToken cancellationToken)
        {
            if (_listener == null)
            {
                throw new InvalidOperationException("Start the receiver first.");
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(Timeout);
            while (true)
            {
                HttpListenerContext context;
                try
                {
                    context = await GetContextAsync(_listener, timeout.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    Log.WarnFormat("No answer from the browser within {0}", Timeout);
                    return new Dictionary<string, string>();
                }

                var answer = ReadAnswer(context.Request, expectedState);
                string page = answer != null
                    ? $"Greenshot received the answer from {cloudServiceName}. You can close this tab."
                    : "This is not an answer to a Greenshot sign-in.";
                try
                {
                    await WritePageAsync(context.Response, answer != null ? 200 : 400, page, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is HttpListenerException or IOException)
                {
                    // The browser closed the connection, the answer still counts
                    Log.WarnFormat("Couldn't send the page to the browser: {0}", ex.Message);
                }

                if (answer != null)
                {
                    return answer;
                }

                Log.Warn("Ignored a request to the sign-in listener that is not the expected redirect");
            }
        }

        /// <summary>
        /// The answer of the redirect, null when the request isn't the redirect of this authorization
        /// </summary>
        private static IDictionary<string, string> ReadAnswer(HttpListenerRequest request, string expectedState)
        {
            if (request.HttpMethod != "GET" || request.Url.AbsolutePath != RedirectPath || !FixedTimeEquals(request.QueryString["state"], expectedState))
            {
                return null;
            }

            var answer = new Dictionary<string, string>();
            foreach (var key in AnswerKeys)
            {
                string value = request.QueryString[key];
                if (value != null)
                {
                    answer[key] = value;
                }
            }

            return answer;
        }

        private static async Task WritePageAsync(HttpListenerResponse response, int statusCode, string text, CancellationToken cancellationToken)
        {
            using (response)
            {
                byte[] buffer = Encoding.UTF8.GetBytes($"<!DOCTYPE html><html><head><meta charset=\"utf-8\"><title>Greenshot</title></head><body><p>{WebUtility.HtmlEncode(text)}</p></body></html>");
                response.StatusCode = statusCode;
                response.ContentType = "text/html; charset=utf-8";
                response.Headers["Cache-Control"] = "no-store";
                response.Headers["Referrer-Policy"] = "no-referrer";
                response.ContentLength64 = buffer.Length;
                using var stream = response.OutputStream;
                await stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Compare without leaking through the time where the first difference is
        /// </summary>
        internal static bool FixedTimeEquals(string actual, string expected)
        {
            if (actual == null || string.IsNullOrEmpty(expected) || actual.Length != expected.Length)
            {
                return false;
            }

            int difference = 0;
            for (int i = 0; i < expected.Length; i++)
            {
                difference |= actual[i] ^ expected[i];
            }

            return difference == 0;
        }

        /// <summary>
        /// Wait for the next request, cancellation closes the listener.
        /// </summary>
        private static async Task<HttpListenerContext> GetContextAsync(HttpListener listener, CancellationToken cancellationToken)
        {
            using var registration = cancellationToken.Register(() =>
            {
                try
                {
                    listener.Close();
                }
                catch (ObjectDisposedException)
                {
                    // Already closed
                }
            });
            try
            {
                return await listener.GetContextAsync().ConfigureAwait(false);
            }
            catch (Exception ex) when (cancellationToken.IsCancellationRequested && ex is HttpListenerException or ObjectDisposedException)
            {
                throw new OperationCanceledException(cancellationToken);
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _listener?.Close();
            _listener = null;
        }
    }
}
