/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Net.Sockets;
using System.Text;
using System.Threading;
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
        private readonly ManualResetEvent _ready = new ManualResetEvent(true);
        private IDictionary<string, string> _returnValues;

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
                    _listeningUri = string.Format(ListeningUrlFormat, GetRandomUnusedPort());
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
        /// The OAuth code receiver
        /// </summary>
        /// <param name="oauth2Settings">OAuth2Settings</param>
        /// <returns>Dictionary with values</returns>
        public IDictionary<string, string> ReceiveCode(OAuth2Settings oauth2Settings)
        {
            using var listener = new HttpListener();
            // Make sure the port is stored in the state, so the website can process this.
            oauth2Settings.State = new Uri(ListeningUri).Port.ToString();
            listener.Prefixes.Add(ListeningUri);
            try
            {
                listener.Start();
                _ready.Reset();

                listener.BeginGetContext(ListenerCallback, listener);
                OpenUriAction(oauth2Settings.FormattedAuthUrl);
                _ready.WaitOne(Timeout, true);
            }
            catch (Exception)
            {
                // Make sure we can clean up, also if the thead is aborted
                _ready.Set();
                throw;
            }
            finally
            {
                listener.Close();
            }

            return _returnValues;
        }

        /// <summary>
        /// Handle a connection async, this allows us to break the waiting
        /// </summary>
        /// <param name="result">IAsyncResult</param>
        private void ListenerCallback(IAsyncResult result)
        {
            HttpListener listener = (HttpListener) result.AsyncState;

            //If not listening return immediately as this method is called one last time after Close()
            if (!listener.IsListening)
            {
                return;
            }

            // Use EndGetContext to complete the asynchronous operation.
            HttpListenerContext context = listener.EndGetContext(result);

            // Handle request
            HttpListenerRequest request = context.Request;

            if (request.HasEntityBody)
            {
                // Process the body
                using var body = request.InputStream;
                using var reader = new StreamReader(body, request.ContentEncoding);
                using var jsonTextReader = new JsonTextReader(reader);
                var serializer = new JsonSerializer();
                _returnValues = serializer.Deserialize<Dictionary<string, string>>(jsonTextReader);
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

                    // Write a "close" response.
                    byte[] buffer = Encoding.UTF8.GetBytes(jsonContent);
                    // Write to response stream.
                    response.ContentLength64 = buffer.Length;
                    using var stream = response.OutputStream;
                    stream.Write(buffer, 0, buffer.Length);
                }
            }

            if (_returnValues != null)
            {
                _ready.Set();
            }
            else
            {
                // Make sure the next request is processed
                listener.BeginGetContext(ListenerCallback, listener);
            }
        }

        /// <summary>
        /// Returns a random, unused port.
        /// </summary>
        /// <returns>port to use</returns>
        private static int GetRandomUnusedPort()
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