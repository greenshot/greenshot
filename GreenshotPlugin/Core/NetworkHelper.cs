/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using Greenshot.IniFile;
using Greenshot.Plugin;

namespace GreenshotPlugin.Core {
	/// <summary>
	/// Description of NetworkHelper.
	/// </summary>
	public static class NetworkHelper {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(NetworkHelper));
		private static CoreConfiguration config = IniConfig.GetIniSection<CoreConfiguration>();

		static NetworkHelper() {
			// Disable certificate checking
			System.Net.ServicePointManager.ServerCertificateValidationCallback +=
			delegate(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors sslError) {
				bool validationResult = true;
				return validationResult;
			};
		}
		/// <summary>
		/// Download a url response as string
		/// </summary>
		/// <param name=url">An Uri to specify the download location</param>
		/// <returns>string with the file content</returns>
		public static string GetAsString(Uri url) {
			HttpWebRequest webRequest = (HttpWebRequest)NetworkHelper.CreateWebRequest(url);
			webRequest.Method = "GET";
			webRequest.KeepAlive = true;
			webRequest.Credentials = System.Net.CredentialCache.DefaultCredentials;
			return GetResponse(webRequest);				
		}

		/// <summary>
		/// Download the FavIcon as a Bitmap
		/// </summary>
		/// <param name="baseUri"></param>
		/// <returns>Bitmap with the FavIcon</returns>
		public static Bitmap DownloadFavIcon(Uri baseUri) {
			Uri url = new Uri(baseUri, new Uri("favicon.ico"));
			try {
				HttpWebRequest request = (HttpWebRequest)NetworkHelper.CreateWebRequest(url);
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				if (request.HaveResponse) {
					using (Image image = Image.FromStream(response.GetResponseStream())) {
						return (image.Height > 16 && image.Width > 16) ? new Bitmap(image, 16, 16) : new Bitmap(image);
					}
				}
				
			} catch (Exception e) {
				LOG.Error("Problem downloading the FavIcon from: " + baseUri.ToString(), e);
			}
			return null;
		}

		/// <summary>
		/// Download the url to Bitmap
		/// </summary>
		/// <param name="baseUri"></param>
		/// <returns>Bitmap</returns>
		public static Image DownloadImage(string url) {
			try {
				HttpWebRequest request = (HttpWebRequest)NetworkHelper.CreateWebRequest(url);
				HttpWebResponse response = (HttpWebResponse)request.GetResponse();
				if (request.HaveResponse) {
					using (Image image = Image.FromStream(response.GetResponseStream())) {
						return ImageHelper.Clone(image);
					}
				}
			} catch (Exception e) {
				LOG.Error("Problem downloading the image from: " + url, e);
			}
			return null;
		}
		
		/// <summary>
		/// Helper method to create a web request, eventually with proxy
		/// </summary>
		/// <param name="uri">string with uri to connect to</param>
		/// <returns>WebRequest</returns>
		public static WebRequest CreateWebRequest(string uri) {
			return CreateWebRequest(new Uri(uri));
		}
		
		/// <summary>
		/// Helper method to create a web request, eventually with proxy
		/// </summary>
		/// <param name="uri">Uri with uri to connect to</param>
		/// <returns>WebRequest</returns>
		public static WebRequest CreateWebRequest(Uri uri) {
			WebRequest webRequest = WebRequest.Create(uri);
			if (config.UseProxy) {
				webRequest.Proxy = GreenshotPlugin.Core.NetworkHelper.CreateProxy(uri);
				//webRequest.Proxy.Credentials = CredentialCache.DefaultCredentials;
			}
			return webRequest;
		}

		/// <summary>
		/// Create a IWebProxy Object which can be used to access the Internet
		/// This method will check the configuration if the proxy is allowed to be used.
		/// Usages can be found in the DownloadFavIcon or Jira and Confluence plugins
		/// </summary>
		/// <param name="url"></param>
		/// <returns>IWebProxy filled with all the proxy details or null if none is set/wanted</returns>
		public static IWebProxy CreateProxy(Uri uri) {
			IWebProxy proxyToUse = null;
			if (config.UseProxy) {
				proxyToUse = WebRequest.DefaultWebProxy;
				if (proxyToUse != null) {
					proxyToUse.Credentials = CredentialCache.DefaultCredentials;
					if (LOG.IsDebugEnabled) {
						// check the proxy for the Uri
						if (!proxyToUse.IsBypassed(uri)) {
							Uri proxyUri = proxyToUse.GetProxy(uri);
							if (proxyUri != null) {
								LOG.Debug("Using proxy: " + proxyUri.ToString() + " for " + uri.ToString());
							} else {
								LOG.Debug("No proxy found!");
							}
						} else {
							LOG.Debug("Proxy bypass for: " + uri.ToString());
						}
					}
				} else {
					LOG.Debug("No proxy found!");
				}
			}
			return proxyToUse;
		}
		
		/// <summary>
		/// UrlEncodes a string without the requirement for System.Web
		/// </summary>
		/// <param name="String"></param>
		/// <returns></returns>
		// [Obsolete("Use System.Uri.EscapeDataString instead")]
		public static string UrlEncode(string text) {
			if (!string.IsNullOrEmpty(text)) {
				// Sytem.Uri provides reliable parsing, but doesn't encode spaces.
				return System.Uri.EscapeDataString(text).Replace("%20", "+");
			}
			return null;
		}

		/// <summary>
		/// A wrapper around the EscapeDataString, as the limit is 32766 characters
		/// See: http://msdn.microsoft.com/en-us/library/system.uri.escapedatastring%28v=vs.110%29.aspx
		/// </summary>
		/// <param name="text"></param>
		/// <returns>escaped data string</returns>
		public static string EscapeDataString(string text) {
			if (!string.IsNullOrEmpty(text)) {
				StringBuilder result = new StringBuilder();
				int currentLocation = 0;
				while (currentLocation < text.Length) {
					string process = text.Substring(currentLocation, Math.Min(16384, text.Length - currentLocation));
					result.Append(Uri.EscapeDataString(process));
					currentLocation = currentLocation + 16384;
				}
				return result.ToString();
			}
			return null;
		}

		/// <summary>
		/// UrlDecodes a string without requiring System.Web
		/// </summary>
		/// <param name="text">String to decode.</param>
		/// <returns>decoded string</returns>
		public static string UrlDecode(string text) {
			// pre-process for + sign space formatting since System.Uri doesn't handle it
			// plus literals are encoded as %2b normally so this should be safe
			text = text.Replace("+", " ");
			return System.Uri.UnescapeDataString(text);
		}

		/// <summary>
		/// ParseQueryString without the requirement for System.Web
		/// </summary>
		/// <param name="s"></param>
		/// <returns>Dictionary<string, string></returns>
		public static IDictionary<string, string> ParseQueryString(string s) {
			IDictionary<string, string> parameters = new SortedDictionary<string, string>();
			// remove anything other than query string from url
			if (s.Contains("?")) {
				s = s.Substring(s.IndexOf('?') + 1);
			}
			foreach (string vp in Regex.Split(s, "&")) {
				if (string.IsNullOrEmpty(vp)) {
					continue;
				}
				string[] singlePair = Regex.Split(vp, "=");
				if (parameters.ContainsKey(singlePair[0])) {
					parameters.Remove(singlePair[0]);
				}
				if (singlePair.Length == 2) {
					parameters.Add(singlePair[0], singlePair[1]);
				} else {
					// only one key with no value specified in query string
					parameters.Add(singlePair[0], string.Empty);
				}
			}
			return parameters;
		}
		
		/// <summary>
		/// Generate the query paramters
		/// </summary>
		/// <param name="queryParameters">the list of query parameters</param>
		/// <returns>a string with the query parameters</returns>
		public static string GenerateQueryParameters(IDictionary<string, object> queryParameters) {
			if (queryParameters == null || queryParameters.Count == 0) {
				return string.Empty;
			}

			queryParameters = new SortedDictionary<string, object>(queryParameters);

			StringBuilder sb = new StringBuilder();
			foreach(string key in queryParameters.Keys) {
				sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0}={1}&", key, UrlEncode(string.Format("{0}",queryParameters[key])));
			}
			sb.Remove(sb.Length-1,1);

			return sb.ToString();
		}

		/// <summary>
		/// Write Multipart Form Data directly to the HttpWebRequest
		/// </summary>
		/// <param name="webRequest">HttpWebRequest to write the multipart form data to</param>
		/// <param name="postParameters">Parameters to include in the multipart form data</param>
		public static void WriteMultipartFormData(HttpWebRequest webRequest, IDictionary<string, object> postParameters) {
			string boundary = String.Format("----------{0:N}", Guid.NewGuid());
			webRequest.ContentType = "multipart/form-data; boundary=" + boundary;
			using (Stream formDataStream = webRequest.GetRequestStream()) {
				WriteMultipartFormData(formDataStream, boundary, postParameters);
			}
		}

		/// <summary>
		/// Write Multipart Form Data to the HttpListenerResponse
		/// </summary>
		/// <param name="webRequest">HttpWebRequest to write the multipart form data to</param>
		/// <param name="postParameters">Parameters to include in the multipart form data</param>
		public static void WriteMultipartFormData(HttpListenerResponse response, IDictionary<string, object> postParameters) {
			string boundary = String.Format("----------{0:N}", Guid.NewGuid());
			response.ContentType = "multipart/form-data; boundary=" + boundary;
			WriteMultipartFormData(response.OutputStream, boundary, postParameters);
		}

		/// <summary>
		/// Write Multipart Form Data to a Stream, content-type should be set before this!
		/// </summary>
		/// <param name="formDataStream">Stream to write the multipart form data to</param>
		/// <param name="boundary">String boundary for the multipart/form-data</param>
		/// <param name="postParameters">Parameters to include in the multipart form data</param>
		public static void WriteMultipartFormData(Stream formDataStream, string boundary, IDictionary<string, object> postParameters) {
			bool needsCLRF = false;
			foreach (var param in postParameters) {
				// Add a CRLF to allow multiple parameters to be added.
				// Skip it on the first parameter, add it to subsequent parameters.
				if (needsCLRF) {
					formDataStream.Write(Encoding.UTF8.GetBytes("\r\n"), 0, Encoding.UTF8.GetByteCount("\r\n"));
				}

				needsCLRF = true;

				if (param.Value is IBinaryContainer) {
					IBinaryContainer binaryParameter = (IBinaryContainer)param.Value;
					binaryParameter.WriteFormDataToStream(boundary, param.Key, formDataStream);
				} else {
					string postData = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}",
						boundary,
						param.Key,
						param.Value);
					formDataStream.Write(Encoding.UTF8.GetBytes(postData), 0, Encoding.UTF8.GetByteCount(postData));
				}
			}

			// Add the end of the request.  Start with a newline
			string footer = "\r\n--" + boundary + "--\r\n";
			formDataStream.Write(Encoding.UTF8.GetBytes(footer), 0, Encoding.UTF8.GetByteCount(footer));
		}
		
		/// <summary>
		/// Process the web response.
		/// </summary>
		/// <param name="webRequest">The request object.</param>
		/// <returns>The response data.</returns>
		public static string GetResponse(HttpWebRequest webRequest) {
			string responseData;
			try {
				HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();
				using (StreamReader reader = new StreamReader(response.GetResponseStream(), true)) {
					responseData = reader.ReadToEnd();
				}
				LOG.DebugFormat("Response status: {0}", response.StatusCode);
			} catch (WebException e) {
				HttpWebResponse response = (HttpWebResponse)e.Response;
				using (Stream responseStream = response.GetResponseStream()) {
					LOG.ErrorFormat("HTTP error {0} with content: {1}", response.StatusCode, new StreamReader(responseStream, true).ReadToEnd());
				}
				throw;
			}

			return responseData;
		}
	}

	/// <summary>
	/// This interface can be used to pass binary information around, like byte[] or Image
	/// </summary>
	public interface IBinaryContainer {
		void WriteFormDataToStream(string boundary, string key, Stream formDataStream);
		void WriteToStream(Stream formDataStream);
		string ToBase64String(Base64FormattingOptions formattingOptions);
		byte[] ToByteArray();
		void Upload(HttpWebRequest webRequest);
	}

	/// <summary>
	/// A container to supply files to a Multi-part form data upload
	/// </summary>
	public class ByteContainer : IBinaryContainer {
		private byte[] file;
		private string fileName;
		private string contentType;
		private int fileSize;
		public ByteContainer(byte[] file) : this(file, null) {
		}
		public ByteContainer(byte[] file, string filename) : this(file, filename, null) {
		}
		public ByteContainer(byte[] file, string filename, string contenttype) : this(file, filename, contenttype, 0) {
		}
		public ByteContainer(byte[] file, string filename, string contenttype, int filesize) {
			this.file = file;
			this.fileName = filename;
			this.contentType = contenttype;
			if (filesize == 0) {
				this.fileSize = file.Length;
			} else {
				this.fileSize = filesize;
			}
		}

		/// <summary>
		/// Create a Base64String from the byte[]
		/// </summary>
		/// <returns>string</returns>
		public string ToBase64String(Base64FormattingOptions formattingOptions) {
			return System.Convert.ToBase64String(file, 0, fileSize, formattingOptions);
		}

		/// <summary>
		/// Returns the initial byte-array which was supplied when creating the FileParameter
		/// </summary>
		/// <returns>byte[]</returns>
		public byte[] ToByteArray() {
			return file;
		}

		/// <summary>
		/// Write Multipart Form Data directly to the HttpWebRequest response stream
		/// </summary>
		/// <param name="webRequest">HttpWebRequest to write the multipart form data to</param>
		/// <param name="postParameters">Parameters to include in the multipart form data</param>
		public void WriteFormDataToStream(string boundary, string key, Stream formDataStream) {
			// Add just the first part of this param, since we will write the file data directly to the Stream
			string header = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\";\r\nContent-Type: {3}\r\n\r\n",
				boundary,
				key,
				fileName ?? key,
				contentType ?? "application/octet-stream");

			formDataStream.Write(Encoding.UTF8.GetBytes(header), 0, Encoding.UTF8.GetByteCount(header));

			// Write the file data directly to the Stream, rather than serializing it to a string.
			formDataStream.Write(file, 0, fileSize);
		}

		/// <summary>
		/// A plain "write data to stream"
		/// </summary>
		/// <param name="dataStream"></param>
		public void WriteToStream(Stream dataStream) {
			// Write the file data directly to the Stream, rather than serializing it to a string.
			dataStream.Write(file, 0, fileSize);
		}
		
		/// <summary>
		/// Upload the file to the webrequest
		/// </summary>
		/// <param name="webRequest"></param>
		public void Upload(HttpWebRequest webRequest) {
			webRequest.ContentType = contentType;
			webRequest.ContentLength = fileSize;
			using (var requestStream = webRequest.GetRequestStream()) {
				WriteToStream(requestStream);
			}
		}
	}

	/// <summary>
	/// A container to supply images to a Multi-part form data upload
	/// </summary>
	public class BitmapContainer : IBinaryContainer {
		private Bitmap bitmap;
		private SurfaceOutputSettings outputSettings;
		private string fileName;

		public BitmapContainer(Bitmap bitmap, SurfaceOutputSettings outputSettings, string filename) {
			this.bitmap = bitmap;
			this.outputSettings = outputSettings;
			this.fileName = filename;
		}

		/// <summary>
		/// Create a Base64String from the image by saving it to a memory stream and converting it.
		/// Should be avoided if possible, as this uses a lot of memory.
		/// </summary>
		/// <returns>string</returns>
		public string ToBase64String(Base64FormattingOptions formattingOptions) {
			using (MemoryStream stream = new MemoryStream()) {
				ImageOutput.SaveToStream(bitmap, null, stream, outputSettings);
				return System.Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length, formattingOptions);
			}
		}

		/// <summary>
		/// Create a byte[] from the image by saving it to a memory stream.
		/// Should be avoided if possible, as this uses a lot of memory.
		/// </summary>
		/// <returns>byte[]</returns>
		public byte[] ToByteArray() {
			using (MemoryStream stream = new MemoryStream()) {
				ImageOutput.SaveToStream(bitmap, null, stream, outputSettings);
				return stream.ToArray();
			}
		}

		/// <summary>
		/// Write Multipart Form Data directly to the HttpWebRequest response stream
		/// </summary>
		/// <param name="webRequest">HttpWebRequest to write the multipart form data to</param>
		/// <param name="postParameters">Parameters to include in the multipart form data</param>
		public void WriteFormDataToStream(string boundary, string key, Stream formDataStream) {
			// Add just the first part of this param, since we will write the file data directly to the Stream
			string header = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\";\r\nContent-Type: {3}\r\n\r\n",
				boundary,
				key,
				fileName ?? key,
				"image/" + outputSettings.Format.ToString());

			formDataStream.Write(Encoding.UTF8.GetBytes(header), 0, Encoding.UTF8.GetByteCount(header));
			ImageOutput.SaveToStream(bitmap, null, formDataStream, outputSettings);
		}

		/// <summary>
		/// A plain "write data to stream"
		/// </summary>
		/// <param name="dataStream"></param>
		public void WriteToStream(Stream dataStream) {
			// Write the file data directly to the Stream, rather than serializing it to a string.
			ImageOutput.SaveToStream(bitmap, null, dataStream, outputSettings);
		}

		/// <summary>
		/// Upload the image to the webrequest
		/// </summary>
		/// <param name="webRequest"></param>
		public void Upload(HttpWebRequest webRequest) {
			webRequest.ContentType = "image/" + outputSettings.Format.ToString();
			using (var requestStream = webRequest.GetRequestStream()) {
				WriteToStream(requestStream);
			}
		}
	}

	/// <summary>
	/// A container to supply surfaces to a Multi-part form data upload
	/// </summary>
	public class SurfaceContainer : IBinaryContainer {
		private ISurface surface;
		private SurfaceOutputSettings outputSettings;
		private string fileName;

		public SurfaceContainer(ISurface surface, SurfaceOutputSettings outputSettings, string filename) {
			this.surface = surface;
			this.outputSettings = outputSettings;
			this.fileName = filename;
		}

		/// <summary>
		/// Create a Base64String from the Surface by saving it to a memory stream and converting it.
		/// Should be avoided if possible, as this uses a lot of memory.
		/// </summary>
		/// <returns>string</returns>
		public string ToBase64String(Base64FormattingOptions formattingOptions) {
			using (MemoryStream stream = new MemoryStream()) {
				ImageOutput.SaveToStream(surface, stream, outputSettings);
				return System.Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length, formattingOptions);
			}
		}

		/// <summary>
		/// Create a byte[] from the image by saving it to a memory stream.
		/// Should be avoided if possible, as this uses a lot of memory.
		/// </summary>
		/// <returns>byte[]</returns>
		public byte[] ToByteArray() {
			using (MemoryStream stream = new MemoryStream()) {
				ImageOutput.SaveToStream(surface, stream, outputSettings);
				return stream.ToArray();
			}
		}

		/// <summary>
		/// Write Multipart Form Data directly to the HttpWebRequest response stream
		/// </summary>
		/// <param name="webRequest">HttpWebRequest to write the multipart form data to</param>
		/// <param name="postParameters">Parameters to include in the multipart form data</param>
		public void WriteFormDataToStream(string boundary, string key, Stream formDataStream) {
			// Add just the first part of this param, since we will write the file data directly to the Stream
			string header = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\";\r\nContent-Type: {3}\r\n\r\n",
				boundary,
				key,
				fileName ?? key,
				"image/" + outputSettings.Format.ToString());

			formDataStream.Write(Encoding.UTF8.GetBytes(header), 0, Encoding.UTF8.GetByteCount(header));
			ImageOutput.SaveToStream(surface, formDataStream, outputSettings);			
		}

		/// <summary>
		/// A plain "write data to stream"
		/// </summary>
		/// <param name="dataStream"></param>
		public void WriteToStream(Stream dataStream) {
			// Write the file data directly to the Stream, rather than serializing it to a string.
			ImageOutput.SaveToStream(surface, dataStream, outputSettings);
		}
		
		/// <summary>
		/// Upload the Surface as image to the webrequest
		/// </summary>
		/// <param name="webRequest"></param>
		public void Upload(HttpWebRequest webRequest) {
			webRequest.ContentType = "image/" + outputSettings.Format.ToString();
			using (var requestStream = webRequest.GetRequestStream()) {
				WriteToStream(requestStream);
			}
		}
	}
}
