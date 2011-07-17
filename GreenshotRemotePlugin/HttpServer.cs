/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2011  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

using Greenshot.Plugin;
using GreenshotPlugin.Core;

namespace GreenshotRemotePlugin {
	class Server {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(Server));
		private string url;
		private string accessKey;
		private volatile bool keepGoing = true;
		private HttpListener listener = null;
		private ICaptureHost captureHost;
		private IGreenshotPluginHost host;
		public Server(string url, string accessKey) {
			this.url = url;
			this.accessKey = accessKey;
		}
		public void StartListening() {
			Thread serverThread = new Thread(Listen);
			serverThread.SetApartmentState(ApartmentState.STA);
			serverThread.Start();
		}
		public void StopListening() {
			keepGoing = false;
		}
		
		public void SetCaptureHost(ICaptureHost captureHost) {
			this.captureHost = captureHost;
		}
		public void SetGreenshotPluginHost(IGreenshotPluginHost host) {
			this.host = host;
		}
		private void Listen() {
			listener = new HttpListener();
			
			//listener.AuthenticationSchemes = AuthenticationSchemes.Ntlm;
			listener.Prefixes.Add(url);
			listener.Start();
			LOG.DebugFormat("Listening on: {0}", url);

		    while (true) {
				IAsyncResult result = listener.BeginGetContext(new AsyncCallback(ListenerCallback), listener);
				while(!result.AsyncWaitHandle.WaitOne(1000)) {
					if (!keepGoing) {
						Close();
						return;
					}
				}
				if (!keepGoing) {
					Close();
					return;
				}
		    }
		}
		
		private void Close() {
			LOG.Debug("Cleaning up HttpListener");
			listener.Stop();
			listener.Prefixes.Remove(url);
			listener.Close();
			listener = null;
		}
		
		private void ListenerCallback(IAsyncResult result) {
			if (listener == null) {
				return;
			}
			try {
				HttpListenerContext context = listener.EndGetContext(result);
				new Thread(ProcessRequest).Start(context);
			} catch (HttpListenerException httpE) {
				LOG.Warn("Got error in ListenerCallback", httpE);
			}
		}

		private void ProcessRequest(object data) {
			HttpListenerContext context = data as HttpListenerContext;
			HttpListenerRequest request = context.Request;
			HttpListenerResponse response = context.Response;
			LOG.DebugFormat("Processing request: {0} {1}", request.HttpMethod, request.Url);
			string user = null;
			if (context.User != null && context.User.Identity != null) {
				user = context.User.Identity.Name;
			}
			string localPath = request.Url.LocalPath.Substring(1);
			LOG.DebugFormat("Path: {0}", localPath);
			if (request.QueryString["key"] == null || !request.QueryString["key"].Equals(accessKey)) {
				LOG.Debug("Unauthorized");
				response.StatusCode = (int)HttpStatusCode.Unauthorized;
				byte[] content = Encoding.UTF8.GetBytes("Unauthorized");
				response.ContentLength64 = content.Length;
				response.OutputStream.Write(content, 0, content.Length);
				response.OutputStream.Close();
				return;
			}
			
			WindowDetails captureWindow = null;
			string handle = request.QueryString["handle"];
			if (handle != null) {
				captureWindow = new WindowDetails(new IntPtr(long.Parse(handle)));
			}
			if (captureWindow != null) {
				try {
					bool restored = captureWindow.Iconic;
					if (restored) {
						captureWindow.Restore();
						restored = true;
					}
					LOG.DebugFormat("Capturing window of class: {0}", captureWindow.ClassName);

					using (Bitmap image = captureWindow.PrintWindow()) {
						if (image != null) {
							using (MemoryStream stream = new MemoryStream()) {
								host.SaveToStream(image, stream, OutputFormat.png, 100);
								byte [] buffer = stream.GetBuffer();
								response.ContentType = "image/png";
								response.ContentLength64 = buffer.Length;
								response.OutputStream.Write(buffer, 0, buffer.Length);
								response.OutputStream.Close();
								return;
							}
						} else {
							LOG.Debug("null image??");
						}
					}
					if (restored) {
						captureWindow.Iconic = true;
					}
				} catch (Exception e) {
					byte[] errorBuffer = Encoding.UTF8.GetBytes(e.StackTrace);
					response.ContentLength64 = errorBuffer.Length;
					response.OutputStream.Write(errorBuffer, 0, errorBuffer.Length);
					response.OutputStream.Close();
					return;
				}
			}
			StringBuilder sb = new StringBuilder();
			sb.Append("<html>");
			sb.Append("<body>");
			sb.Append("<h1>").Append("Active windows").Append("</h1>");
			sb.AppendLine("<br/>");
			if (user != null) {
				sb.Append("Hello " + user + " ");
			}
			List<WindowDetails>windows = WindowDetails.GetAllWindows();
			foreach(WindowDetails window in windows) {
				if (window.Text.Length > 0 && window.Visible && !window.ClassName.StartsWith("Progman")) {
					sb.Append("<A HREF=\"capture?handle=" + window.Handle +"&key="+ request.QueryString["key"] + "\">");
					sb.Append(window.Text);
					sb.Append("</A>");
					sb.AppendLine("<br/>");
				}
			}
			sb.Append("</body>");
			sb.Append("</html>");
			byte[] b = Encoding.UTF8.GetBytes(sb.ToString());
			response.ContentLength64 = b.Length;
			response.OutputStream.Write(b, 0, b.Length);
			response.OutputStream.Close();
		}
	}
}