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
using System.Collections.Specialized;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace Greenshot.Base.Core.OAuth
{
    /// <summary>
    /// OAuth 2.0 verification code receiver that runs a local server on a free port
    /// and waits for a call with the authorization verification code.
    /// </summary>
    public class LocalServerCodeReceiver
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(LocalServerCodeReceiver));

        /// <summary>
        /// The call back format. Expects one port parameter.
        /// Default: http://localhost:{0}/authorize/
        /// </summary>
        public string LoopbackCallbackUrl { get; set; } = "http://localhost:{0}/authorize/";

        /// <summary>
        /// HTML code to to return the _browser, default it will try to close the _browser / tab, this won't always work.
        /// You can use CloudServiceName where you want to show the CloudServiceName from your OAuth2 settings
        /// </summary>
        public string ClosePageResponse { get; set; } = @"<html>
<head><title>OAuth 2.0 Authentication CloudServiceName</title></head>
<body>
Greenshot received information from CloudServiceName. You can close this browser / tab if it is not closed itself...
<script type='text/javascript'>
	window.setTimeout(function() {
		window.open('', '_self', ''); 
		window.close(); 
	}, 1000);
	if (window.opener) {
		window.opener.checkToken();
	}
</script>
</body>
</html>";

        private string _redirectUri;

        /// <summary>
        /// The URL to redirect to
        /// </summary>
        protected string RedirectUri
        {
            get
            {
                if (!string.IsNullOrEmpty(_redirectUri))
                {
                    return _redirectUri;
                }

                return _redirectUri = string.Format(LoopbackCallbackUrl, GetRandomUnusedPort());
            }
        }

        /// <summary>
        /// The OAuth code receiver: opens the browser and waits (without blocking a thread) for the redirect.
        /// </summary>
        /// <param name="oauth2Settings"></param>
        /// <param name="cancellationToken">CancellationToken, stops the waiting</param>
        /// <returns>Dictionary with values</returns>
        public async Task<IDictionary<string, string>> ReceiveCodeAsync(OAuth2Settings oauth2Settings, CancellationToken cancellationToken)
        {
            // Set the redirect URL on the settings
            oauth2Settings.RedirectUrl = RedirectUri;
            string cloudServiceName = oauth2Settings.CloudServiceName;
            var returnValues = new Dictionary<string, string>();
            using var listener = new HttpListener();
            listener.Prefixes.Add(oauth2Settings.RedirectUrl);
            listener.Start();
            try
            {
                // Get the formatted FormattedAuthUrl
                string authorizationUrl = oauth2Settings.FormattedAuthUrl;
                Log.DebugFormat("Open a browser with: {0}", authorizationUrl);
                using (Process.Start(authorizationUrl))
                {
                    // Only started
                }

                // Wait to get the authorization code response.
                HttpListenerContext context = await GetContextAsync(listener, cancellationToken).ConfigureAwait(false);
                NameValueCollection nameValueCollection = context.Request.QueryString;

                // Write a "close" response.
                using (HttpListenerResponse response = context.Response)
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(ClosePageResponse.Replace("CloudServiceName", cloudServiceName));
                    response.ContentLength64 = buffer.Length;
                    using var stream = response.OutputStream;
                    await stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                }

                // Create a new response URL with a dictionary that contains all the response query parameters.
                foreach (var name in nameValueCollection.AllKeys)
                {
                    if (name != null && !returnValues.ContainsKey(name))
                    {
                        returnValues.Add(name, nameValueCollection[name]);
                    }
                }
            }
            finally
            {
                listener.Close();
            }

            return returnValues;
        }

        /// <summary>
        /// Wait for the next request, cancellation closes the listener.
        /// </summary>
        internal static async Task<HttpListenerContext> GetContextAsync(HttpListener listener, CancellationToken cancellationToken)
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

        /// <summary>
        /// Returns a random, unused port.
        /// </summary>
        /// <returns>port to use</returns>
        internal static int GetRandomUnusedPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            try
            {
                listener.Start();
                return ((IPEndPoint) listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}
