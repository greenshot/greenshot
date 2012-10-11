/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

using GreenshotPlugin.Core;
using Greenshot.IniFile;
using Greenshot.Plugin;

namespace GreenshotImmioPlugin {
	/// <summary>
	/// Description of ImmioUtils.
	/// </summary>
	public class ImmioUtils {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ImmioUtils));
		private static ImmioConfiguration config = IniConfig.GetIniSection<ImmioConfiguration>();

		private ImmioUtils() {
		}

		/// <summary>
		/// Do the actual upload to Immio
		/// </summary>
		/// <param name="image">Image to upload</param>
		/// <param name="outputSettings">OutputSettings for the image file format</param>
		/// <param name="title">Title</param>
		/// <param name="filename">Filename</param>
		/// <returns>ImmioInfo with details</returns>
		public static string UploadToImmio(Image image, OutputSettings outputSettings, string title, string filename) {

			string responseString = null;
			HttpWebRequest webRequest = (HttpWebRequest)NetworkHelper.CreateWebRequest("http://imm.io/store/");
			webRequest.Method = "POST";
			webRequest.ServicePoint.Expect100Continue = false;

			IDictionary<string, object> uploadParameters = new Dictionary<string, object>();
			uploadParameters.Add("name", filename);
			uploadParameters.Add("image", new ImageContainer(image, outputSettings, null));
			NetworkHelper.WriteMultipartFormData(webRequest, uploadParameters);

			responseString = NetworkHelper.GetResponse(webRequest);

			IDictionary<string, object> parsedResponse = JSONHelper.JsonDecode(responseString);
			return (string)parsedResponse["uri"];
		}
	}
}
