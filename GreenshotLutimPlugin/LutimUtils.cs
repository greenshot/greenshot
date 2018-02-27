/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Dapplo.Log;
using Dapplo.Ini;
using GreenshotPlugin.Core;
using GreenshotPlugin.Gfx;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;

namespace GreenshotLutimPlugin
{
	/// <summary>
	/// A collection of Lutim helper methods
	/// </summary>
	public static class LutimUtils
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly ILutimConfiguration Config = IniConfig.Current.Get<ILutimConfiguration>();

		/// <summary>
		/// Check if we need to load the history
		/// </summary>
		/// <returns></returns>
		public static bool IsHistoryLoadingNeeded()
		{
			Log.Info().WriteLine("Checking if lutim cache loading needed, configuration has {0} lutim hashes, loaded are {1} hashes.", Config.LutimUploadHistory.Count, Config.RuntimeLutimHistory.Count);
			return Config.RuntimeLutimHistory.Count != Config.LutimUploadHistory.Count;
		}

		/// <summary>
		/// Load the complete history of the lutim uploads, with the corresponding information
		/// </summary>
		public static void LoadHistory()
		{
			if (!IsHistoryLoadingNeeded())
			{
				return;
			}

			// Load the ImUr history
			foreach (string key in Config.LutimUploadHistory.Keys.ToList())
			{
				if (Config.RuntimeLutimHistory.ContainsKey(key))
				{
					// Already loaded
					continue;
				}

				try
				{
					var value = Config.LutimUploadHistory[key];
					LutimInfo lutimInfo = LutimInfo.FromIniString(key, value);
					Config.RuntimeLutimHistory[key] = lutimInfo;
				}
				catch (ArgumentException)
				{
					Log.Info().WriteLine("Bad format of lutim history item for short {0}", key);
					Config.LutimUploadHistory.Remove(key);
					Config.RuntimeLutimHistory.Remove(key);
				}
				catch (Exception e)
				{
					Log.Error().WriteLine(e, "Problem loading lutim history for short " + key);
				}
			}
		}

		/// <summary>
		/// Do the actual upload to Lutim
		/// For more details on the available parameters, see: http://api.lutim.com/resources_anon
		/// </summary>
		/// <param name="surfaceToUpload">ISurface to upload</param>
		/// <param name="outputSettings">OutputSettings for the image file format</param>
		/// <param name="filename">Filename</param>
		/// <returns>LutimInfo with details</returns>
		public static LutimInfo UploadToLutim(ISurface surfaceToUpload, SurfaceOutputSettings outputSettings, string filename)
		{
			string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
			byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

			string responseString = null;
			var webRequest = NetworkHelper.CreateWebRequest(Config.LutimUrl, HTTPMethod.POST);

			webRequest.ContentType = "multipart/form-data; boundary=" + boundary;
			webRequest.ServicePoint.Expect100Continue = false;

			try
			{
				using (var requestStream = webRequest.GetRequestStream())
				{
					WriteFormData(requestStream, boundarybytes, "format", "json");
					WriteFormData(requestStream, boundarybytes, "first-view", "0");
					WriteFormData(requestStream, boundarybytes, "delete-day", "0");
					WriteFormData(requestStream, boundarybytes, "keep-exif", "0");

					requestStream.Write(boundarybytes, 0, boundarybytes.Length);

					const string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
					string header = string.Format(headerTemplate, "file", filename, "image/" + outputSettings.Format);
					byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
					requestStream.Write(headerbytes, 0, headerbytes.Length);

					ImageOutput.SaveToStream(surfaceToUpload, requestStream, outputSettings);

					byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
					requestStream.Write(trailer, 0, trailer.Length);
				}

				using (WebResponse response = webRequest.GetResponse())
				{
					var responseStream = response.GetResponseStream();
					if (responseStream != null)
					{
						using (StreamReader reader = new StreamReader(responseStream, true))
						{
							responseString = reader.ReadToEnd();
						}
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error().WriteLine(ex, "Upload to lutim gave an exeption: ");
				throw;
			}

			if (string.IsNullOrEmpty(responseString))
			{
				return null;
			}

			return LutimInfo.ParseResponse(Config.LutimUrl, responseString);
		}

		private static void WriteFormData(Stream requestStream, byte[] boundarybytes, string name, object value)
		{
			const string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
			requestStream.Write(boundarybytes, 0, boundarybytes.Length);
			string formitem = string.Format(formdataTemplate, name, value);
			byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
			requestStream.Write(formitembytes, 0, formitembytes.Length);
		}

		/// <summary>
		/// Delete an lutim image, this is done by specifying the delete hash
		/// </summary>
		/// <param name="lutimInfo"></param>
		public static void DeleteLutimImage(LutimInfo lutimInfo)
		{
			Log.Info().WriteLine("Deleting Lutim image for {0}", lutimInfo.Short);

			try
			{
				var lutimBaseUri = new Uri(Config.LutimUrl);
				var url = new Uri(lutimBaseUri, "d/" + lutimInfo.RealShort + "/" + lutimInfo.Token + "?format=json");
				HttpWebRequest webRequest = NetworkHelper.CreateWebRequest(url, HTTPMethod.GET);
				webRequest.ServicePoint.Expect100Continue = false;

				string responseString = null;
				using (WebResponse response = webRequest.GetResponse())
				{
					var responseStream = response.GetResponseStream();
					if (responseStream != null)
					{
						using (StreamReader reader = new StreamReader(responseStream, true))
						{
							responseString = reader.ReadToEnd();
						}
					}
				}
				Log.Info().WriteLine("Delete result: {0}", responseString);
			}
			catch (WebException wE)
			{
				// Allow "Bad request" this means we already deleted it
				if (wE.Status == WebExceptionStatus.ProtocolError)
				{
					if (((HttpWebResponse)wE.Response).StatusCode != HttpStatusCode.BadRequest)
					{
						throw;
					}
				}
			}
			// Make sure we remove it from the history, if no error occured
			Config.RuntimeLutimHistory.Remove(lutimInfo.Short);
			Config.LutimUploadHistory.Remove(lutimInfo.Short);
			lutimInfo.Thumb = null;
		}

		/// <summary>
		/// Helper for logging
		/// </summary>
		/// <param name="nameValues"></param>
		/// <param name="key"></param>
		private static void LogHeader(IDictionary<string, string> nameValues, string key)
		{
			if (nameValues.ContainsKey(key))
			{
				Log.Info().WriteLine("{0}={1}", key, nameValues[key]);
			}
		}
	}
}
