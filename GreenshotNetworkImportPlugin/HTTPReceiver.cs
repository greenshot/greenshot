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
using System.Threading;
using System.IO;
using System.Drawing;
using System.Net;
using System.Text;

using Greenshot.Plugin;

namespace GreenshotNetworkImportPlugin {
	public class HTTPReceiver {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(HTTPReceiver));
		
		private string url;
		private volatile bool keepGoing = true;
		private HttpListener listener = null;
		private IGreenshotHost host;

		public HTTPReceiver(string url, IGreenshotHost host) {
			this.url = url;
			this.host = host;
		}
		public void StartListening() {
			Thread serverThread = new Thread(Listen);
			serverThread.SetApartmentState(ApartmentState.STA);
			serverThread.Start();
		}

		public void StopListening() {
			keepGoing = false;
			Close();
		}

		private void Listen() {
			listener = new HttpListener();

			//listener.AuthenticationSchemes = AuthenticationSchemes.Ntlm;
			if (!listener.Prefixes.Contains(url)) {
				listener.Prefixes.Add(url);
			}
			listener.Start();

			while (true) {
				IAsyncResult result = listener.BeginGetContext(new AsyncCallback(ListenerCallback), listener);
				while (!result.AsyncWaitHandle.WaitOne(1000)) {
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
			if (listener != null) {
				listener.Stop();
				if (listener.Prefixes != null) {
					listener.Prefixes.Remove(url);
				}
				listener.Close();
				listener = null;
			}
		}

		private void ListenerCallback(IAsyncResult result) {
			if (listener == null) {
				return;
			}
			try {
				HttpListenerContext context = listener.EndGetContext(result);
				new Thread(ProcessRequest).Start(context);
			} catch (HttpListenerException httpE) {
				LOG.Error(httpE);
			}
		}

		private void ProcessRequest(object data) {
			const string basePrefix = "data:image/png;base64,";
			HttpListenerContext context = data as HttpListenerContext;
			HttpListenerRequest request = context.Request;
			HttpListenerResponse response = context.Response;
			LOG.DebugFormat("Content type: {0}", request.ContentType);
			if (!request.HasEntityBody) {
				LOG.DebugFormat("Empty request to {0}", request.Url);
				return;
			}
			LOG.DebugFormat("Post request to {0}", request.Url);
			string possibleImage = null;
			using (Stream body = request.InputStream)  {
				using (StreamReader reader = new StreamReader(body, request.ContentEncoding)) {
					possibleImage = reader.ReadToEnd();
				}
			}
			string title = request.QueryString["title"];
			LOG.DebugFormat("Title={0}", title);
			LOG.DebugFormat("Image={0}", possibleImage.Substring(0,30));

			if (possibleImage != null && possibleImage.StartsWith(basePrefix)) {
				byte[] imageBytes = Convert.FromBase64String(possibleImage.Substring(basePrefix.Length));
				using (MemoryStream memoryStream = new MemoryStream(imageBytes, 0, imageBytes.Length)) {
					// Convert byte[] to Image
					memoryStream.Write(imageBytes, 0, imageBytes.Length);
					Image image = Image.FromStream(memoryStream, true);
					ICapture capture = host.GetCapture(image);
					capture.CaptureDetails.Title = title;
					host.ImportCapture(capture);
					response.StatusCode = (int)HttpStatusCode.OK;
					byte[] content = Encoding.UTF8.GetBytes("Greenshot-OK");
					response.ContentType = "text";
					response.ContentLength64 = content.Length;
					response.OutputStream.Write(content, 0, content.Length);
					response.OutputStream.Close();
					return;
				}
			}
			response.StatusCode = (int)HttpStatusCode.InternalServerError;
			byte[] b =  Encoding.UTF8.GetBytes("Greenshot-NOTOK");
			response.ContentLength64 = b.Length;
			response.OutputStream.Write(b, 0, b.Length);
			response.OutputStream.Close();
		}
	}
}
