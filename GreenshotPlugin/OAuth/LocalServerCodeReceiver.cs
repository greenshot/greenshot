/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using GreenshotPlugin.Core;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GreenshotPlugin.OAuth {
	/// <summary>
	/// OAuth 2.0 verification code receiver that runs a local server on a free port
	/// and waits for a call with the authorization verification code.
	/// </summary>
	public class LocalServerCodeReceiver : IDisposable {
		private static readonly ILog LOG = LogManager.GetLogger(typeof(LocalServerCodeReceiver));
		private readonly ManualResetEvent _ready = new ManualResetEvent(true);

		private string _loopbackCallback = "http://localhost:{0}/authorize/";
		/// <summary>
		/// The call back format. Expects one port parameter.
		/// Default: http://localhost:{0}/authorize/
		/// </summary>
		public string LoopbackCallbackUrl {
			get {
				return _loopbackCallback;
			}
			set {
				_loopbackCallback = value;
			}
		}

		private string _closePageResponse =
@"<html>
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

		/// <summary>
		/// HTML code to to return the _browser, default it will try to close the _browser / tab, this won't always work.
		/// You can use CloudServiceName where you want to show the CloudServiceName from your OAuth2 settings
		/// </summary>
		public string ClosePageResponse {
			get {
				return _closePageResponse;
			}
			set {
				_closePageResponse = value;
			}
		}

		private Uri _redirectUri;
		/// <summary>
		/// The URL to redirect to
		/// </summary>
		protected Uri RedirectUri {
			get {
				if (_redirectUri != null) {
					return _redirectUri;
				}

				return _redirectUri = new Uri(string.Format(_loopbackCallback, GetRandomUnusedPort()));
			}
		}

		private string _cloudServiceName;

		private IDictionary<string, string> _returnValues = new Dictionary<string, string>();


		/// <summary>
		/// The OAuth code receiver
		/// </summary>
		/// <param name="authorizationUrl"></param>
		/// <returns>Dictionary with values</returns>
		public async Task<IDictionary<string, string>> ReceiveCodeAsync(OAuth2Settings oauth2Settings, CancellationToken token = default(CancellationToken)) {
			// Set the redirect URL on the settings
			oauth2Settings.RedirectUrl = Uri.EscapeDataString(RedirectUri.AbsoluteUri);
			_cloudServiceName = oauth2Settings.CloudServiceName;
			using (var listener = new HttpListener()) {
				listener.Prefixes.Add(RedirectUri.AbsoluteUri);
				try {
					listener.Start();

					// Get the formatted FormattedAuthUrl
					var authorizationUrl = oauth2Settings.FormattedAuthUrl;
					LOG.DebugFormat("Open a browser with: {0}", authorizationUrl.AbsoluteUri);
					Process.Start(authorizationUrl.AbsoluteUri);

					// Wait to get the authorization code response.
					var context = listener.BeginGetContext(new AsyncCallback(ListenerCallback), listener);
					_ready.Reset();

					while (!token.IsCancellationRequested && !context.AsyncWaitHandle.WaitOne(1)) {
						LOG.Debug("Waiting for response");
						await Task.Delay(1000).ConfigureAwait(false);
					}
				} catch (Exception) {
					// Make sure we can clean up, also if the thead is aborted
					_ready.Set();
					throw;
				} finally {
					_ready.WaitOne();
					listener.Close();
				}
			}
			return _returnValues;
		}

		/// <summary>
		/// Handle a connection async, this allows us to break the waiting
		/// </summary>
		/// <param name="result">IAsyncResult</param>
		private void ListenerCallback(IAsyncResult result) {
			var listener = (HttpListener)result.AsyncState;

			//If not listening return immediately as this method is called one last time after Close()
			if (!listener.IsListening) {
				return;
			}

			// Use EndGetContext to complete the asynchronous operation.
			var context = listener.EndGetContext(result);


			// Handle request
			var request = context.Request;
			try {
				var nameValueCollection = request.QueryString;

				// Get response object.
				using (var response = context.Response) {
					// Write a "close" response.
					byte[] buffer = Encoding.UTF8.GetBytes(ClosePageResponse.Replace("CloudServiceName", _cloudServiceName));
					// Write to response stream.
					response.ContentLength64 = buffer.Length;
					using (var stream = response.OutputStream) {
						stream.Write(buffer, 0, buffer.Length);
					}
				}

				// Create a new response URL with a dictionary that contains all the response query parameters.
				foreach (var name in nameValueCollection.AllKeys) {
					if (!_returnValues.ContainsKey(name)) {
						_returnValues.Add(name, nameValueCollection[name]);
					}
				}
			} catch (Exception) {
				context.Response.OutputStream.Close();
				throw;
			}
			_ready.Set();
		}

		/// <summary>
		/// Returns a random, unused port.
		/// </summary>
		/// <returns>port to use</returns>
		private static int GetRandomUnusedPort() {
			var listener = new TcpListener(IPAddress.Loopback, 0);
			try {
				listener.Start();
				return ((IPEndPoint)listener.LocalEndpoint).Port;
			} finally {
				listener.Stop();
			}
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing) {
			if (!disposedValue) {
				if (disposing) {
					_ready.Dispose();
				}

				disposedValue = true;
			}
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose() {
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
		}
		#endregion
	}
}
