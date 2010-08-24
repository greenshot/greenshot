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
using System.Collections.Generic;
using Greenshot.Core;

namespace TitleFix {
	/// <summary>
	/// Description of FlickrConfiguration.
	/// </summary>
	[IniSection("TitleFix", Description="Greenshot TitleFix Plugin configuration")]
	public class TitleFixConfiguration : IniSection {
		[IniProperty("ActiveFixes", Description="The fixes that are active.")]
		public List<string> active;
		
		[IniProperty("Matcher", Description="The regular expressions to match the title with.")]
		public Dictionary<string, string> matchers;

		[IniProperty("Replacer", Description="The replacements for the matchers.")]
		public Dictionary<string, string> replacers;
		
		/// <summary>
		/// Supply values we can't put as defaults
		/// </summary>
		/// <param name="property">The property to return a default for</param>
		/// <returns>object with the default value for the supplied property</returns>
		public override object GetDefault(string property) {
			switch(property) {
				case "ActiveFixes":
					List<string> activeDefaults = new List<string>();
					activeDefaults.Add("Firefox");
					activeDefaults.Add("IE");
					return activeDefaults; 
				case "Matcher":
					Dictionary<string, string> matcherDefaults = new Dictionary<string, string>();
					matcherDefaults.Add("Firefox", " - Mozilla Firefox.*");
					matcherDefaults.Add("IE", " - Microsoft Internet Explorer.*");
					return matcherDefaults; 
				case "Replacer":
					Dictionary<string, string> replacerDefaults = new Dictionary<string, string>();
					replacerDefaults.Add("Firefox", "");
					replacerDefaults.Add("IE", "");
					return replacerDefaults; 
			}
			return null;
		}
	}
}
