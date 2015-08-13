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

using Dapplo.Config.Ini;
using Greenshot.Plugin;
using log4net;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;

namespace GreenshotPlugin.Core {
	/// <summary>
	/// Description of NetworkHelper.
	/// </summary>
	public static class NetworkHelper {
		private static readonly ILog LOG = LogManager.GetLogger(typeof(NetworkHelper));
		private static readonly ICoreConfiguration Config = IniConfig.Get("Greenshot","greenshot").Get<ICoreConfiguration>();

		static NetworkHelper() {
			// Disable certificate checking
			ServicePointManager.ServerCertificateValidationCallback +=
			delegate {
				return true;
			};
		}
		
		/// <summary>
		/// Helper method to create a web request with a lot of default settings
		/// </summary>
		/// <param name="uri">Uri with uri to connect to</param>
		/// <param name="method">Method to use</param>
		/// <returns>WebRequest</returns>
		public static HttpWebRequest CreateWebRequest(Uri uri, HttpMethod method) {
			HttpWebRequest webRequest = CreateWebRequest(uri);
			webRequest.Method = method.ToString();
			return webRequest;
		}

		/// <summary>
		/// Helper method to create a web request, eventually with proxy
		/// </summary>
		/// <param name="uri">Uri with uri to connect to</param>
		/// <returns>WebRequest</returns>
		public static HttpWebRequest CreateWebRequest(Uri uri) {
			HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
			if (Config.UseProxy) {
				webRequest.Proxy = uri.CreateProxy();
			} else {
				// BUG-1655: Fix that Greenshot always uses the default proxy even if the "use default proxy" checkbox is unset
				webRequest.Proxy = null;
			}
			// Make sure the default credentials are available
			webRequest.Credentials = CredentialCache.DefaultCredentials;

			// Allow redirect, this is usually needed so that we don't get a problem when a service moves
			webRequest.AllowAutoRedirect = true;
			// Set default timeouts
			webRequest.Timeout = Config.HttpConnectionTimeout*1000;
			webRequest.ReadWriteTimeout = Config.WebRequestReadWriteTimeout*1000;
			return webRequest;
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
		/// <param name="response">HttpListenerResponse</param>
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
			bool needsClrf = false;
			foreach (var param in postParameters) {
				// Add a CRLF to allow multiple parameters to be added.
				// Skip it on the first parameter, add it to subsequent parameters.
				if (needsClrf) {
					formDataStream.Write(Encoding.UTF8.GetBytes("\r\n"), 0, Encoding.UTF8.GetByteCount("\r\n"));
				}

				needsClrf = true;

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
		/// Log the headers of the WebResponse, if IsDebugEnabled
		/// </summary>
		/// <param name="response">WebResponse</param>
		private static void DebugHeaders(WebResponse response) {
			if (!LOG.IsDebugEnabled) {
				return;
			}
			LOG.DebugFormat("Debug information on the response from {0} :", response.ResponseUri);
			foreach (string key in response.Headers.AllKeys) {
				LOG.DebugFormat("Reponse-header: {0}={1}", key, response.Headers[key]);
			}
		}

		/// <summary>
		/// Process the web response.
		/// </summary>
		/// <param name="webRequest">The request object.</param>
		/// <returns>The response data.</returns>
		/// TODO: This method should handle the StatusCode better!
		public static string GetResponseAsString(HttpWebRequest webRequest) {
			return GetResponseAsString(webRequest, false);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="webRequest"></param>
		/// <returns></returns>
		public static string GetResponseAsString(HttpWebRequest webRequest, bool alsoReturnContentOnError) {
			string responseData = null;
			try {
				HttpWebResponse response = (HttpWebResponse) webRequest.GetResponse();
				LOG.InfoFormat("Response status: {0}", response.StatusCode);
				bool isHttpError = (int) response.StatusCode >= 300;
				DebugHeaders(response);
				Stream responseStream = response.GetResponseStream();
				if (responseStream != null) {
					using (StreamReader reader = new StreamReader(responseStream, true)) {
						responseData = reader.ReadToEnd();
					}
					if (isHttpError) {
						LOG.ErrorFormat("HTTP error {0} with content: {1}", response.StatusCode, responseData);
					}
				}
			} catch (WebException e) {
				HttpWebResponse response = (HttpWebResponse) e.Response;
				if (response != null) {
					LOG.ErrorFormat("HTTP error {0}", response.StatusCode);
					using (Stream responseStream = response.GetResponseStream()) {
						if (responseStream != null) {
							using (StreamReader streamReader = new StreamReader(responseStream, true)) {
								string errorContent = streamReader.ReadToEnd();
								if (alsoReturnContentOnError) {
									return errorContent;
								}
								LOG.ErrorFormat("Content: {0}", errorContent);
							}
						}
					}
				}
				LOG.Error("WebException: ", e);
				throw;
			}

			return responseData;
		}
	}

	/// <summary>
	/// This interface can be used to pass binary information around, like byte[] or Image
	/// </summary>
	public interface IBinaryContainer {
		void WriteFormDataToStream(string boundary, string name, Stream formDataStream);
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
			fileName = filename;
			contentType = contenttype;
			if (filesize == 0) {
				fileSize = file.Length;
			} else {
				fileSize = filesize;
			}
		}

		/// <summary>
		/// Create a Base64String from the byte[]
		/// </summary>
		/// <returns>string</returns>
		public string ToBase64String(Base64FormattingOptions formattingOptions) {
			return Convert.ToBase64String(file, 0, fileSize, formattingOptions);
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
		/// <param name="boundary">Separator</param>
		/// <param name="name">name</param>
		/// <param name="formDataStream">Stream to write to</param>
		public void WriteFormDataToStream(string boundary, string name, Stream formDataStream) {
			// Add just the first part of this param, since we will write the file data directly to the Stream
			string header = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\";\r\nContent-Type: {3}\r\n\r\n",
				boundary,
				name,
				fileName ?? name,
				contentType ?? "application/octet-stream");

			formDataStream.Write(Encoding.UTF8.GetBytes(header), 0, Encoding.UTF8.GetByteCount(header));

			// Write the file data directly to the Stream, rather than serializing it to a string.
			formDataStream.Write(file, 0, fileSize);
		}

		/// <summary>
		/// A plain "write data to stream"
		/// </summary>
		/// <param name="dataStream">Stream to write to</param>
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
			fileName = filename;
		}

		/// <summary>
		/// Create a Base64String from the image by saving it to a memory stream and converting it.
		/// Should be avoided if possible, as this uses a lot of memory.
		/// </summary>
		/// <returns>string</returns>
		public string ToBase64String(Base64FormattingOptions formattingOptions) {
			using (MemoryStream stream = new MemoryStream()) {
				ImageOutput.SaveToStream(bitmap, null, stream, outputSettings);
				return Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length, formattingOptions);
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
		/// <param name="boundary">Separator</param>
		/// <param name="name">Name of the thing/file</param>
		/// <param name="formDataStream">Stream to write to</param>
		public void WriteFormDataToStream(string boundary, string name, Stream formDataStream) {
			// Add just the first part of this param, since we will write the file data directly to the Stream
			string header = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\";\r\nContent-Type: {3}\r\n\r\n",
				boundary,
				name,
				fileName ?? name,
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
			fileName = filename;
		}

		/// <summary>
		/// Create a Base64String from the Surface by saving it to a memory stream and converting it.
		/// Should be avoided if possible, as this uses a lot of memory.
		/// </summary>
		/// <returns>string</returns>
		public string ToBase64String(Base64FormattingOptions formattingOptions) {
			using (MemoryStream stream = new MemoryStream()) {
				ImageOutput.SaveToStream(surface, stream, outputSettings);
				return Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length, formattingOptions);
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
		/// <param name="boundary">Multipart separator</param>
		/// <param name="name">Name of the thing</param>
		/// <param name="formDataStream">Stream to write to</param>
		public void WriteFormDataToStream(string boundary, string name, Stream formDataStream) {
			// Add just the first part of this param, since we will write the file data directly to the Stream
			string header = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\";\r\nContent-Type: {3}\r\n\r\n",
				boundary,
				name,
				fileName ?? name,
				"image/" + outputSettings.Format);

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
