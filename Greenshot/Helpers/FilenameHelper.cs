/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2010  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections;
using System.IO;
using System.Windows.Forms;

using Greenshot.Capturing;
using Greenshot.Configuration;
using Greenshot.Plugin;

namespace Greenshot.Helpers {
	public class FilenameHelper {
		private const int MAX_TITLE_LENGTH = 80;
		private FilenameHelper() {
		}
		/// <summary>
		/// Remove invalid characters from the fully qualified filename
		/// </summary>
		/// <param name="fullpath">string with the full path to a file</param>
		/// <returns>string with the full path to a file, without invalid characters</returns>
		public static string MakeFQFilenameSafe(string fullPath) {
			string path = MakePathSafe(Path.GetDirectoryName(fullPath));
			string filename = MakeFilenameSafe(Path.GetFileName(fullPath));
			// Make the fullpath again and return
			return Path.Combine(path, filename);
		}

		/// <summary>
		/// Remove invalid characters from the filename
		/// </summary>
		/// <param name="fullpath">string with the full path to a file</param>
		/// <returns>string with the full path to a file, without invalid characters</returns>
		public static string MakeFilenameSafe(string filename) {
			// Make the filename save!
			if (filename != null) {
				foreach (char disallowed in Path.GetInvalidFileNameChars()) {
					filename = filename.Replace( disallowed.ToString(), "" );
				}
			}
			return filename;
		}

		/// <summary>
		/// Remove invalid characters from the path
		/// </summary>
		/// <param name="fullpath">string with the full path to a file</param>
		/// <returns>string with the full path to a file, without invalid characters</returns>
		public static string MakePathSafe(string path) {
			// Make the path save!
			if (path != null) {
				foreach (char disallowed in Path.GetInvalidPathChars()) {
					path = path.Replace( disallowed.ToString(), "" );
				}
			}
			return path;
		}

		public static string GetFilenameWithoutExtensionFromPattern(string pattern) {
			return GetFilenameWithoutExtensionFromPattern(pattern, null);
		}

		public static string GetFilenameWithoutExtensionFromPattern(string pattern, ICaptureDetails captureDetails) {
			return FillPattern(pattern, captureDetails);
		}

		public static string GetFilenameFromPattern(string pattern, string imageFormat) {
			return GetFilenameFromPattern(pattern, imageFormat, null);
		}

		public static string GetFilenameFromPattern(string pattern, string imageFormat, ICaptureDetails captureDetails) {
			string ext;
			if (imageFormat.IndexOf('.') >= 0) {
				ext = imageFormat.Substring(imageFormat.IndexOf('.')+1);
			} else {
				ext = imageFormat;
			}
			ext = ext.ToLower();
			if(ext.Equals("jpeg")) {
				ext = "jpg";
			}
			return FillPattern(pattern, captureDetails) + "." + ext;
		}
		
		private static string FillPattern(string initialPattern, ICaptureDetails captureDetails) {
			string pattern = string.Copy(initialPattern);
			// Default use "now" for the capture taken
			DateTime captureTaken = DateTime.Now;
			// Use default application name for title
			string title = Application.ProductName;

			// Check if we have capture details
			if (captureDetails != null) {
				captureTaken = captureDetails.DateTime;
				if (captureDetails.Title != null) {
					title = captureDetails.Title;
					if (title.Length > MAX_TITLE_LENGTH) {
						title = title.Substring(0, MAX_TITLE_LENGTH);
					}
				}
			}
			pattern = pattern.Replace("%YYYY%",captureTaken.Year.ToString());
			pattern = pattern.Replace("%MM%", zeroPad(captureTaken.Month.ToString(), 2));
			pattern = pattern.Replace("%DD%", zeroPad(captureTaken.Day.ToString(), 2));
			pattern = pattern.Replace("%hh%", zeroPad(captureTaken.Hour.ToString(), 2));
			pattern = pattern.Replace("%mm%", zeroPad(captureTaken.Minute.ToString(), 2));
			pattern = pattern.Replace("%ss%", zeroPad(captureTaken.Second.ToString(), 2));
			pattern = pattern.Replace("%domain%", Environment.UserDomainName);
			pattern = pattern.Replace("%user%", Environment.UserName);
			pattern = pattern.Replace("%hostname%", Environment.MachineName);
			if(pattern.Contains("%NUM%")) {
			   	AppConfig conf = AppConfig.GetInstance();
			   	int num = conf.Output_File_IncrementingNumber++;
			   	conf.Store();
			   	pattern = pattern.Replace("%NUM%", zeroPad(num.ToString(), 6));
            }
			// Use the last as it "might" have some nasty strings in it.
			pattern = pattern.Replace("%title%", MakeFilenameSafe(title));
			
			try {
				// Use MakeFQFilenameSafe so people can still use path characters for the name
				return MakeFQFilenameSafe(pattern);
			} catch (Exception e) {
				// adding additional data for bug tracking
				e.Data.Add("title", title);
				e.Data.Add("pattern", initialPattern);
				e.Data.Add("filename", pattern);
				throw e;
			}
		}
		
		private static string zeroPad(string input, int chars) {
			while(input.Length < chars) input = "0" + input;
			return input;
		}
	}
}
