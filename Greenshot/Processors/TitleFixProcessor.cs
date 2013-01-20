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
using System.Text.RegularExpressions;

using Greenshot.Plugin;
using GreenshotPlugin.Core;
using Greenshot.IniFile;

namespace Greenshot.Processors  {
	/// <summary>
	/// Description of TitleFixProcessor.
	/// </summary>
	public class TitleFixProcessor : AbstractProcessor {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(TitleFixProcessor));
		private static CoreConfiguration config = IniConfig.GetIniSection<CoreConfiguration>();
		
		public TitleFixProcessor() {
			List<string> corruptKeys = new List<string>();
			foreach(string key in config.ActiveTitleFixes) {
				if (!config.TitleFixMatcher.ContainsKey(key)) {
					LOG.WarnFormat("Key {0} not found, configuration is broken! Disabling this key!");
					corruptKeys.Add(key);
				}
			}
			
			// Fix configuration if needed
			if(corruptKeys.Count > 0) {
				foreach(string corruptKey in corruptKeys) {
					// Removing any reference to the key
					config.ActiveTitleFixes.Remove(corruptKey);
					config.TitleFixMatcher.Remove(corruptKey);
					config.TitleFixReplacer.Remove(corruptKey);
				}
				config.IsDirty = true;
			}
			if(config.IsDirty) {
				IniConfig.Save();
			}
		}
		
		public override string Designation {
			get {
				return "TitleFix";
			}
		}

		public override string Description {
			get {
				return Designation;
			}
		}

		public override bool ProcessCapture(ISurface surface, ICaptureDetails captureDetails) {
			bool changed = false;
			string title = captureDetails.Title;
			if (!string.IsNullOrEmpty(title)) {
				title = title.Trim();
				foreach(string titleIdentifier in config.ActiveTitleFixes) {
					string regexpString = config.TitleFixMatcher[titleIdentifier];
					string replaceString = config.TitleFixReplacer[titleIdentifier];
					if (replaceString == null) {
						replaceString = "";
					}
					if (!string.IsNullOrEmpty(regexpString)) {
						Regex regex = new Regex(regexpString);
						title = regex.Replace(title, replaceString);
						changed = true;
					}
				}
			}
			captureDetails.Title = title;
			return changed;
		}
	}
}
