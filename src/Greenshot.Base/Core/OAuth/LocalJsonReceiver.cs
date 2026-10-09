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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Threading;
using log4net;
using Newtonsoft.Json;

namespace Greenshot.Base.Core.OAuth
{
    /// <summary>
    /// OAuth 2.0 verification code receiver that runs a local server on a free port
    /// and waits for a call with the authorization verification code.
    /// </summary>
    public class LocalJsonReceiver
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(LocalJsonReceiver));

        /// <summary>
        /// The url format for the website to post to. Expects one port parameter.
        /// Default: http://localhost:{0}/authorize/
        /// </summary>
        public string ListeningUrlFormat { get; set; } = "http://localhost:{0}/authorize/";

        private string _listeningUri;

        /// <summary>
        /// The URL where the server is listening
        /// </summary>
        public string ListeningUri
        {
            get
            {
                if (string.IsNullOrEmpty(_listeningUri))
                {
                    _listeningUri = string.Format(ListeningUrlFormat, LocalServerCodeReceiver.GetRandomUnusedPort());
                }

                return _listeningUri;
            }
            set => _listeningUri = value;
        }

        /// <summary>
        /// This action is called when the URI must be opened, default is just to run Process.Start
        /// </summary>
        public Action<string> OpenUriAction { set; get; } = authorizationUrl =>
        {
            Log.DebugFormat("Open a browser with: {0}", authorizationUrl);
            using var process = Process.Start(authorizationUrl);
        };

        /// <summary>
        /// Timeout for waiting for the website to respond
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(4);

        /// <summary>
        /// The OAuth code receiver: opens the browser and waits (without blocking a thread) for the website to post the values.
        /// </summary>
        /// <param name="oauth2Settings">OAuth2Settings</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Dictionary with values, null when the website didn't respond within the timeout</returns>
        public async Task<IDictionary<string, string>> ReceiveCodeAsync(OAuth2Settings oauth2Settings, CancellationToken cancellationToken)
        {
            using var listener = new HttpListener();
            // Make sure the port is stored in the state, so the website can process this.
            oauth2Settings.State = new Uri(ListeningUri).Port.ToString();
            listener.Prefixes.Add(ListeningUri);
            listener.Start();
            try
            {
                OpenUriAction(oauth2Settings.FormattedAuthUrl);
                return await ReceiveValuesAsync(listener, cancellationToken).WaitAsync(Timeout, cancellationToken).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                Log.WarnFormat("No response within {0}", Timeout);
                return null;
            }
            finally
            {
                listener.Close();
            }
        }

        /// <summary>
        /// Handle requests until one has the values (a CORS preflight comes first)
        /// </summary>
        private static async Task<IDictionary<string, string>> ReceiveValuesAsync(HttpListener listener, CancellationToken cancellationToken)
        {
            while (true)
            {
                HttpListenerContext context = await LocalServerCodeReceiver.GetContextAsync(listener, cancellationToken).ConfigureAwait(false);
                HttpListenerRequest request = context.Request;
                IDictionary<string, string> returnValues = null;

                if (request.HasEntityBody)
                {
                    // Process the body
                    using var body = request.InputStream;
                    using var reader = new StreamReader(body, request.ContentEncoding);
                    string json = await reader.ReadToEndAsync().ConfigureAwait(false);
                    returnValues = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                }

                // Create the response.
                using (HttpListenerResponse response = context.Response)
                {
                    if (request.HttpMethod == "OPTIONS")
                    {
                        response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
                        response.AddHeader("Access-Control-Allow-Methods", "POST");
                        response.AddHeader("Access-Control-Max-Age", "1728000");
                    }

                    response.AppendHeader("Access-Control-Allow-Origin", "*");
                    if (request.HasEntityBody)
                    {
                        response.ContentType = "application/json";
                        // currently only return the version, more can be added later
                        string jsonContent = "{\"version\": \"" + EnvironmentInfo.GetGreenshotVersion(true) + "\"}";

                        byte[] buffer = Encoding.UTF8.GetBytes(jsonContent);
                        response.ContentLength64 = buffer.Length;
                        using var stream = response.OutputStream;
                        await stream.WriteAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                    }
                }

                if (returnValues != null)
                {
                    return returnValues;
                }
            }
        }
    }
}
