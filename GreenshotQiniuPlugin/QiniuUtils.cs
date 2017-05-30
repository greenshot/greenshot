/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using Greenshot.IniFile;
using Greenshot.Plugin;
using GreenshotPlugin.Core;

using Qiniu.Common;
using Qiniu.IO;
using Qiniu.IO.Model;
using Qiniu.Http;
using Qiniu.Util;

namespace GreenshotQiniuPlugin
{
	/// <summary>
	/// A collection of Qiniu helper methods
	/// </summary>
	public static class QiniuUtils {
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(QiniuUtils));
        private static readonly QiniuConfiguration _config = IniConfig.GetIniSection<QiniuConfiguration>();

  

        public static HttpResult UploadFile(string fullPath,string fileName)
        {            
            Mac mac = new Mac(_config.AccessKey, _config.SecretKey);
            PutPolicy putPolicy = new PutPolicy();
            putPolicy.Scope = _config.Scope;
            putPolicy.SetExpires(3600);
            putPolicy.DeleteAfterDays = 1;
            string token = Auth.CreateUploadToken(mac, putPolicy.ToJsonString());
     
            ResumableUploader target = new ResumableUploader();
            HttpResult result = target.UploadFile(fullPath, fileName, token);
            File.Delete(fullPath);
            return result;
        }

        /// <summary>
        /// Helper for logging
        /// </summary>
        /// <param name="nameValues"></param>
        /// <param name="key"></param>
        private static void LogHeader(IDictionary<string, string> nameValues, string key) {
			if (nameValues.ContainsKey(key)) {
				Log.InfoFormat("{0}={1}", key, nameValues[key]);
			}
		}

		/// <summary>
		/// Log the current rate-limit information
		/// </summary>
		/// <param name="response"></param>
		private static void LogRateLimitInfo(WebResponse response) {
			IDictionary<string, string> nameValues = new Dictionary<string, string>();
			foreach (string key in response.Headers.AllKeys) {
				if (!nameValues.ContainsKey(key)) {
					nameValues.Add(key, response.Headers[key]);
				}
			}
			LogHeader(nameValues, "X-RateLimit-Limit");
			LogHeader(nameValues, "X-RateLimit-Remaining");
			LogHeader(nameValues, "X-RateLimit-UserLimit");
			LogHeader(nameValues, "X-RateLimit-UserRemaining");
			LogHeader(nameValues, "X-RateLimit-UserReset");
			LogHeader(nameValues, "X-RateLimit-ClientLimit");
			LogHeader(nameValues, "X-RateLimit-ClientRemaining");

			// Update the credits in the config, this is shown in a form
			//int credits;
			//if (nameValues.ContainsKey("X-RateLimit-Remaining") && int.TryParse(nameValues["X-RateLimit-Remaining"], out credits)) {
			//	Config.Credits = credits;
			//}
		}
	}
}
