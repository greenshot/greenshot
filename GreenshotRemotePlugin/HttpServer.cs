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
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

using Greenshot.Plugin;
using GreenshotPlugin.Core;
using GreenshotPlugin.UnmanagedHelpers;
using IniFile;

namespace GreenshotRemotePlugin {
	public class Server {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(Server));
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		private string url;
		private string accessKey;
		private volatile bool keepGoing = true;
		private HttpListener listener = null;
		private IGreenshotHost host;
		
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
			Close();
		}
		
		public void SetGreenshotPluginHost(IGreenshotHost host) {
			this.host = host;
		}
		private void Listen() {
			listener = new HttpListener();
			
			//listener.AuthenticationSchemes = AuthenticationSchemes.Ntlm;
			if (!listener.Prefixes.Contains(url)) {
				listener.Prefixes.Add(url);
			}
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
			if (listener != null) {
				LOG.Debug("Cleaning up HttpListener");
				listener.Stop();
				listener.Prefixes.Remove(url);
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
					
					ICapture capture = null;
					try {
						capture = CaptureWindow(captureWindow, null, conf.WindowCaptureMode);
						if (capture.Image != null) {
							using (MemoryStream stream = new MemoryStream()) {
								host.SaveToStream(capture.Image, stream, OutputFormat.png, 100);
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
					} finally {
						if (capture != null) {
							capture.Dispose();
							capture = null;
						}
					}
					if (restored) {
						captureWindow.Iconic = true;
					}
				} catch (Exception e) {
					LOG.Error(e);
					byte[] errorBuffer = Encoding.UTF8.GetBytes("An error occured...");
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
			List<WindowDetails>windows = WindowDetails.GetVisibleWindows();
			foreach(WindowDetails window in windows) {
				sb.Append("<A HREF=\"capture?handle=" + window.Handle +"&key="+ request.QueryString["key"] + "\">");
				sb.Append(window.Text);
				sb.Append("</A>");
				sb.AppendLine("<br/>");
			}
			sb.Append("</body>");
			sb.Append("</html>");
			byte[] b = Encoding.UTF8.GetBytes(sb.ToString());
			response.ContentLength64 = b.Length;
			response.OutputStream.Write(b, 0, b.Length);
			response.OutputStream.Close();
		}
		
		/// <summary>
		/// Capture the supplied Window
		/// </summary>
		/// <param name="windowToCapture">Window to capture</param>
		/// <param name="captureForWindow">The capture to store the details</param>
		/// <param name="windowCaptureMode">What WindowCaptureMode to use</param>
		/// <returns></returns>
		public static ICapture CaptureWindow(WindowDetails windowToCapture, ICapture captureForWindow, WindowCaptureMode windowCaptureMode) {
			if (captureForWindow == null) {
				captureForWindow = new Capture();
			}
			Rectangle windowRectangle = windowToCapture.WindowRectangle;
			if (windowToCapture.Iconic) {
				// Restore the window making sure it's visible!
				windowToCapture.Restore();
			}

			// When Vista & DWM (Aero) enabled
			bool dwmEnabled = DWM.isDWMEnabled();
			// get process name to be able to exclude certain processes from certain capture modes
			Process process = windowToCapture.Process;
			bool isAutoMode = windowCaptureMode == WindowCaptureMode.Auto;
			// For WindowCaptureMode.Auto we check:
			// 1) Is window IE, use IE Capture
			// 2) Is Windows >= Vista & DWM enabled: use DWM
			// 3) Otherwise use GDI (Screen might be also okay but might lose content)
			if (isAutoMode) {
		
				// Take default screen
				windowCaptureMode = WindowCaptureMode.Screen;
				
				// Change to GDI, if allowed
				if (conf.isGDIAllowed(process)) {
					windowCaptureMode = WindowCaptureMode.GDI;
				}

				// Change to DWM, if enabled and allowed
				if (dwmEnabled) {
					if (conf.isDWMAllowed(process)) {
						windowCaptureMode = WindowCaptureMode.Aero;
					}
				}
			} else if (windowCaptureMode == WindowCaptureMode.Aero || windowCaptureMode == WindowCaptureMode.AeroTransparent) {
				if (!dwmEnabled || !conf.isDWMAllowed(process)) {
					// Take default screen
					windowCaptureMode = WindowCaptureMode.Screen;
					// Change to GDI, if allowed
					if (conf.isGDIAllowed(process)) {
						windowCaptureMode = WindowCaptureMode.GDI;
					}
				}
			} else if (windowCaptureMode == WindowCaptureMode.GDI && !conf.isGDIAllowed(process)) {
				// GDI not allowed, take screen
				windowCaptureMode = WindowCaptureMode.Screen;
			}

			LOG.DebugFormat("Capturing window with mode {0}", windowCaptureMode);
			bool captureTaken = false;
			// Try to capture
			while (!captureTaken) {
				if (windowCaptureMode == WindowCaptureMode.GDI) {
					ICapture tmpCapture = null;
					if (conf.isGDIAllowed(process)) {
						tmpCapture = windowToCapture.CaptureWindow(captureForWindow);
					}
					if (tmpCapture != null) {
						captureForWindow = tmpCapture;
						captureTaken = true;
					} else {
						// A problem, try Screen
						windowCaptureMode = WindowCaptureMode.Screen;
					}
				} else if (windowCaptureMode == WindowCaptureMode.Aero || windowCaptureMode == WindowCaptureMode.AeroTransparent) {
					ICapture tmpCapture = null;
					if (conf.isDWMAllowed(process)) {
						tmpCapture = windowToCapture.CaptureDWMWindow(captureForWindow, windowCaptureMode, isAutoMode);
					}
					if (tmpCapture != null) {
						captureForWindow = tmpCapture;
						captureTaken = true;
					} else {
						// A problem, try GDI
						windowCaptureMode = WindowCaptureMode.GDI;
					}
				} else {
					// Screen capture
					windowRectangle.Intersect(captureForWindow.ScreenBounds);
					try {
						captureForWindow = WindowCapture.CaptureRectangle(captureForWindow, windowRectangle);
						captureTaken = true;
					} catch (Exception e) {
						LOG.Error("Problem capturing", e);
						return null;
					}
				}
			}

			if (captureForWindow != null && windowToCapture != null && captureForWindow.Image != null) {
				captureForWindow.CaptureDetails.Title = windowToCapture.Text;
				using (Graphics graphics = Graphics.FromHwnd(windowToCapture.Handle)) {
					((Bitmap)captureForWindow.Image).SetResolution(graphics.DpiX, graphics.DpiY);
				}
			}
			
			return captureForWindow;
		}
	}
}