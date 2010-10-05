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
using System.Text.RegularExpressions;
using System.Windows.Forms;

using Greenshot.Capturing;
using Greenshot.Configuration;
using Greenshot.Core;
using Greenshot.Plugin;

namespace Greenshot.Helpers {
	public class FilenameHelper {
		private static readonly Regex VAR_REGEXP = new Regex(@"\${(?<variable>[^:}]+)[:]?(?<parameters>[^}]*)}", RegexOptions.Compiled);
		private static readonly Regex SPLIT_REGEXP = new Regex(";(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)", RegexOptions.Compiled);
		private const int MAX_TITLE_LENGTH = 80;
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();

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

		public static string GetFilenameFromPattern(string pattern, OutputFormat imageFormat) {
			return GetFilenameFromPattern(pattern, imageFormat, null);
		}

		public static string GetFilenameFromPattern(string pattern, OutputFormat imageFormat, ICaptureDetails captureDetails) {
			return FillPattern(pattern, captureDetails) + "." + imageFormat.ToString().ToLower();
		}
		
		/// <summary>
		/// This method will be called by the regexp.replace as a MatchEvaluator delegate!
		/// </summary>
		/// <param name="match">What are we matching?</param>
		/// <param name="captureDetails">The detail, can be null</param>
		/// <returns></returns>
		private static string MatchVarEvaluator(Match match, ICaptureDetails captureDetails, IDictionary processVars, IDictionary userVars, IDictionary machineVars) {
			// some defaults
			int padWidth = 0;
			int startIndex = 0;
			int endIndex = 0;
			char padChar = ' ';
			string dateFormat = "yyyy-MM-dd HH-mm-ss";

			string replaceValue = "";
			string variable = match.Groups["variable"].Value;
			string parameters = match.Groups["parameters"].Value;

			if (parameters != null && parameters.Length > 0) {
				string []parms = SPLIT_REGEXP.Split(parameters);
				foreach(string parameter in parms) {
					switch (parameter.Substring(0,1)) {
						case "p":
							string []padParams = parameter.Substring(1).Split(new char[] {','});
							try {
								padWidth = int.Parse(padParams[0]);
							} catch {};
							if (padParams.Length > 1) {
								padChar = padParams[1][0];
							}
							break;
						case "d":
							dateFormat = parameter.Substring(1);
							if (dateFormat.StartsWith("\"")) {
								dateFormat = dateFormat.Substring(1);
							}
							if (dateFormat.EndsWith("\"")) {
								dateFormat = dateFormat.Substring(0, dateFormat.Length-1);
							}
							break;
						case "s":
							string range=parameter.Substring(1);
							string []rangelist = range.Split(new char[] {','});
							if (rangelist.Length > 0) {
								try {
									startIndex = int.Parse(rangelist[0]);
								} catch {
									// Ignore
								}
							}
							if (rangelist.Length > 1) {
								try {
									endIndex = int.Parse(rangelist[1]);
								} catch {
									// Ignore
								}
							}
							break;
					}
				}
			}
			if (processVars != null && processVars.Contains(variable)) {
				replaceValue = (string)processVars[variable];
			} else if (userVars != null && userVars.Contains(variable)) {
				replaceValue = (string)userVars[variable];
			} else if (machineVars != null && machineVars.Contains(variable)) {
				replaceValue = (string)machineVars[variable];
			} else {
				// Handle other variables
				// Default use "now" for the capture take´n
				DateTime capturetime = DateTime.Now;
				// Use default application name for title
				string title = Application.ProductName;
	
				// Check if we have capture details
				if (captureDetails != null) {
					capturetime = captureDetails.DateTime;
					if (captureDetails.Title != null) {
						title = captureDetails.Title;
						if (title.Length > MAX_TITLE_LENGTH) {
							title = title.Substring(0, MAX_TITLE_LENGTH);
						}
					}
				}
				switch(variable) {
					case "domain":
						replaceValue = Environment.UserDomainName;
						break;
					case "user":
						replaceValue = Environment.UserName;
						break;
					case "hostname":
						replaceValue = Environment.MachineName;
						break;
					case "YYYY":
						replaceValue = capturetime.Year.ToString();
						break;
					case "MM":
						replaceValue = capturetime.Month.ToString();
						break;
					case "DD":
						replaceValue = capturetime.Day.ToString();
						break;
					case "hh":
						replaceValue = capturetime.Hour.ToString();
						break;
					case "mm":
						replaceValue = capturetime.Minute.ToString();
						break;
					case "ss":
						replaceValue = capturetime.Second.ToString();
						break;
					case "now":
						replaceValue = DateTime.Now.ToString(dateFormat);
						break;
					case "capturetime":
						replaceValue = capturetime.ToString(dateFormat);
						break;
					case "NUM":
						conf.OutputFileIncrementingNumber = conf.OutputFileIncrementingNumber++;
					   	IniConfig.Save();
						replaceValue = conf.OutputFileIncrementingNumber.ToString();
						if (padWidth == 0) {
							padWidth = -6;
							padChar = '0';
						}
						
						break;
					case "title":
						replaceValue = MakeFilenameSafe(title);
						break;
				}
			}
			// do substring
			if (startIndex != 0 || endIndex != 0) {
				if (startIndex < 0) {
					startIndex = replaceValue.Length + startIndex;
				}
				if (endIndex < 0) {
					endIndex = replaceValue.Length + endIndex;
				}
				if (endIndex != 0) {
					try {
						replaceValue = replaceValue.Substring(startIndex, endIndex);
					} catch {
						// Ignore
					}
				} else {
					try {
						replaceValue = replaceValue.Substring(startIndex);
					} catch {
						// Ignore
					}
				}
			}
			
			// do padding
			if (padWidth >0) {
				replaceValue = replaceValue.PadRight(padWidth, padChar);
			} else if (padWidth < 0) {
				replaceValue = replaceValue.PadLeft(-padWidth, padChar);
			}
			return replaceValue;
		}

		/// <summary>
		/// "Simply" fill the pattern with environment variables
		/// </summary>
		/// <param name="pattern">String with pattern ${var}</param>
		/// <returns>Filled string</returns>
		public static string FillVariables(string pattern) {
			IDictionary processVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process);
			IDictionary userVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User);
			IDictionary machineVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine);

			return VAR_REGEXP.Replace(pattern,
				new MatchEvaluator(delegate(Match m) { return MatchVarEvaluator(m, null, processVars, userVars, machineVars); })
      		);
		}

		private static string FillPattern(string pattern, ICaptureDetails captureDetails) {
			IDictionary processVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process);
			IDictionary userVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User);
			IDictionary machineVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine);
			try {
				return VAR_REGEXP.Replace(pattern,
					new MatchEvaluator(delegate(Match m) { return MatchVarEvaluator(m, captureDetails, processVars, userVars, machineVars); })
	      		);
			} catch (Exception e) {
				// adding additional data for bug tracking
				e.Data.Add("title", captureDetails.Title);
				e.Data.Add("pattern", pattern);
				throw e;
			}
		}
	}
}
