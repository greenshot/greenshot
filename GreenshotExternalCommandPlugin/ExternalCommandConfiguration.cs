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
using System.IO;
using Greenshot.IniFile;
using GreenshotPlugin.Core;

namespace ExternalCommand {
	/// <summary>
	/// Description of FlickrConfiguration.
	/// </summary>
	[IniSection("ExternalCommand", Description="Greenshot ExternalCommand Plugin configuration")]
	public class ExternalCommandConfiguration : IniSection {
		[IniProperty("Commands", Description="The commands that are available.")]
		public List<string> Commands { get; set; }

		[IniProperty("RedirectStandardError", Description = "Redirect the standard error of all external commands, used to output as warning to the greenshot.log.", DefaultValue = "true")]
		public bool RedirectStandardError { get; set; }

		[IniProperty("RedirectStandardOutput", Description = "Redirect the standard output of all external commands, used for different other functions (more below).", DefaultValue = "true")]
		public bool RedirectStandardOutput { get; set; }

		[IniProperty("ShowStandardOutputInLog", Description = "Depends on 'RedirectStandardOutput': Show standard output of all external commands to the Greenshot log, this can be usefull for debugging.", DefaultValue = "false")]
		public bool ShowStandardOutputInLog { get; set; }

		[IniProperty("ParseForUri", Description = "Depends on 'RedirectStandardOutput': Parse the output and take the first found URI, if a URI is found than clicking on the notify bubble goes there.", DefaultValue = "true")]
		public bool ParseOutputForUri { get; set; }

		[IniProperty("OutputToClipboard", Description = "Depends on 'RedirectStandardOutput': Place the standard output on the clipboard.", DefaultValue = "false")]
		public bool OutputToClipboard { get; set; }

		[IniProperty("UriToClipboard", Description = "Depends on 'RedirectStandardOutput' & 'ParseForUri': If an URI is found in the standard input, place it on the clipboard. (This overwrites the output from OutputToClipboard setting.)", DefaultValue = "true")]
		public bool UriToClipboard { get; set; }

		[IniProperty("Commandline", Description="The commandline for the output command.")]
		public Dictionary<string, string> commandlines { get; set; }

		[IniProperty("Argument", Description="The arguments for the output command.")]
		public Dictionary<string, string> arguments { get; set; }

		[IniProperty("RunInbackground", Description = "Should the command be started in the background.")]
		public Dictionary<string, bool> runInbackground { get; set; }

		private const string MSPAINT = "MS Paint";
		private static readonly string paintPath;
		private static readonly bool hasPaint = false;

		private const string PAINTDOTNET = "Paint.NET";
		private static readonly string paintDotNetPath;
		private static readonly bool hasPaintDotNet = false;
		static ExternalCommandConfiguration() {
			try {
				paintPath = PluginUtils.GetExePath("pbrush.exe");
				hasPaint = !string.IsNullOrEmpty(paintPath) && File.Exists(paintPath);
				paintDotNetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Paint.NET\PaintDotNet.exe");
				hasPaintDotNet = !string.IsNullOrEmpty(paintDotNetPath) && File.Exists(paintDotNetPath);
			} catch {
			}
		}

		/// <summary>
		/// Supply values we can't put as defaults
		/// </summary>
		/// <param name="property">The property to return a default for</param>
		/// <returns>object with the default value for the supplied property</returns>
		public override object GetDefault(string property) {
			switch(property) {
				case "Commands":
					List<string> commandDefaults = new List<string>();
					if (hasPaintDotNet) {
						commandDefaults.Add(PAINTDOTNET);
					}
					if (hasPaint) {
						commandDefaults.Add(MSPAINT);
					}
					return commandDefaults; 
				case "Commandline":
					Dictionary<string, string> commandlineDefaults = new Dictionary<string, string>();
					if (hasPaintDotNet) {
						commandlineDefaults.Add(PAINTDOTNET, paintDotNetPath);
					}
					if (hasPaint) {
						commandlineDefaults.Add(MSPAINT, paintPath);
					}
					return commandlineDefaults; 
				case "Argument":
					Dictionary<string, string> argumentDefaults = new Dictionary<string, string>();
					if (hasPaintDotNet) {
						argumentDefaults.Add(PAINTDOTNET, "\"{0}\"");
					}
					if (hasPaint) {
						argumentDefaults.Add(MSPAINT, "\"{0}\"");
					}
					return argumentDefaults;
				case "RunInbackground":
					Dictionary<string, bool> runInBackground = new Dictionary<string, bool>();
					if (hasPaintDotNet) {
						runInBackground.Add(PAINTDOTNET, true);
					}
					if (hasPaint) {
						runInBackground.Add(MSPAINT, true);
					}
					return runInBackground;
			}
			return null;
		}
	}
}
