/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.HttpExtensions;
using GreenshotPlugin.Extensions;

namespace GreenshotPlugin.OAuth
{
	/// <summary>
	/// OAuth 2.0 verification code receiver that runs a local server on a free port
	/// and waits for a call with the authorization verification code.
	/// </summary>
	public class LocalServerCodeReceiver
	{
		private static readonly Serilog.ILogger Log = Serilog.Log.Logger.ForContext(typeof(LocalServerCodeReceiver));

		/// <summary>
		/// HTML code to to return the _browser, default it will try to close the _browser / tab, this won't always work.
		/// You can use CloudServiceName where you want to show the CloudServiceName from your OAuth2 settings
		/// </summary>
		public string ClosePageResponse { get; set; } = @"<html>
<head><title>OAuth 2.0 Authentication CloudServiceName</title></head>
<body>
The authentication process received information from CloudServiceName. You can close this browser / tab if it is not closed itself...
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
		/// The OAuth code receiver
		/// </summary>
		/// <param name="oauth2Settings"></param>
		/// <param name="token"></param>
		/// <returns>Dictionary with values</returns>
		public async Task<IDictionary<string, string>> ReceiveCodeAsync(OAuth2Settings oauth2Settings, CancellationToken token = default(CancellationToken))
		{
			// Set the redirect URL on the settings
			var redirectUri = new Uri($"http://localhost:{GetRandomUnusedPort()}/authorize/");

			oauth2Settings.RedirectUrl = Uri.EscapeDataString(redirectUri.AbsoluteUri);
			var taskCompletionSource = new TaskCompletionSource<IDictionary<string, string>>();

			// ReSharper disable once UnusedVariable
			var listenTask = Task.Factory.StartNew(async () =>
			{
				using (var listener = new HttpListener())
				{
					listener.Prefixes.Add(redirectUri.AbsoluteUri);
					listener.Start();
					var httpListenerContext = await listener.GetContextAsync();
					var httpListenerRequest = httpListenerContext.Request;
					try
					{
						// we got the result, parse the Query and set it as a result
						taskCompletionSource.SetResult(httpListenerRequest.Url.QueryToDictionary());

						// Get response object.
						using (var response = httpListenerContext.Response)
						{
							// Write a "close" response.
							var buffer = Encoding.UTF8.GetBytes(ClosePageResponse.Replace("CloudServiceName", oauth2Settings.CloudServiceName));
							// Write to response stream.
							response.ContentLength64 = buffer.Length;
							using (var stream = response.OutputStream)
							{
								await stream.WriteAsync(buffer, 0, buffer.Length, token);
							}
						}
					}
					catch (Exception ex)
					{
						httpListenerContext.Response.OutputStream.Close();
						taskCompletionSource.SetException(ex);
					}
				}
			}, token);
			// Get the formatted FormattedAuthUrl
			var authorizationUrl = new Uri(oauth2Settings.AuthUrlPattern.FormatWith(oauth2Settings));
			Log.Debug("Open a browser with: {0}", authorizationUrl.AbsoluteUri);
			// Open the url in the default browser
			Process.Start(authorizationUrl.AbsoluteUri);
			return await taskCompletionSource.Task;
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
				var port = ((IPEndPoint)listener.LocalEndpoint).Port;
				Log.Debug("Found free listener port {0} for the local code receiver.", port);
				return port;
			}
			finally
			{
				listener.Stop();
			}
		}
	}
}