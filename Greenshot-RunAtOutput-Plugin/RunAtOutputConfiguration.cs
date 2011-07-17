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
using GreenshotPlugin.Core;

namespace RunAtOutput {
	/// <summary>
	/// Description of FlickrConfiguration.
	/// </summary>
	[IniSection("RunAtOutput", Description="Greenshot TitRunAtOutputleFix Plugin configuration")]
	public class RunAtOutputConfiguration : IniSection {
		[IniProperty("Commands", Description="The commands that are available.")]
		public List<string> commands;

		//[IniProperty("Matcher", Description="Match filenames or types.")]
		//public Dictionary<string, string> matchers;

		[IniProperty("Commandline", Description="The commandline for the output command.")]
		public Dictionary<string, string> commandlines;

		[IniProperty("Argument", Description="The arguments for the output command.")]
		public Dictionary<string, string> arguments;
		
		[IniProperty("ActiveCommands", Description="The commands that are active.")]
		public List<string> active;

		/// <summary>
		/// Supply values we can't put as defaults
		/// </summary>
		/// <param name="property">The property to return a default for</param>
		/// <returns>object with the default value for the supplied property</returns>
		public override object GetDefault(string property) {
			switch(property) {
				case "Commands":
					List<string> commandDefaults = new List<string>();
					commandDefaults.Add("Paint.NET");
					return commandDefaults; 
				case "Commandline":
					Dictionary<string, string> commandlineDefaults = new Dictionary<string, string>();
					commandlineDefaults.Add("Paint.NET", @"C:\Programme\Paint.NET\PaintDotNet.exe");
					return commandlineDefaults; 
//				case "Matcher":
//					Dictionary<string, string> matcherDefaults = new Dictionary<string, string>();
//					matcherDefaults.Add("Paint.NET", "*");
//					return matcherDefaults; 
				case "ActiveCommands":
					return new List<string>();
				case "Argument":
					Dictionary<string, string> argumentDefaults = new Dictionary<string, string>();
					argumentDefaults.Add("Paint.NET", "\"{0}\"");
					return argumentDefaults; 
			}
			return null;
		}
	}
}
