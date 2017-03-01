#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

#region Usings

using System.Collections.Generic;
using System.Text.RegularExpressions;
using GreenshotPlugin.Core;
using GreenshotPlugin.IniFile;
using GreenshotPlugin.Interfaces;
using Dapplo.Log;

#endregion

namespace Greenshot.Processors
{
	/// <summary>
	///     Description of TitleFixProcessor.
	/// </summary>
	public class TitleFixProcessor : AbstractProcessor
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly CoreConfiguration config = IniConfig.GetIniSection<CoreConfiguration>();

		public TitleFixProcessor()
		{
			var corruptKeys = new List<string>();
			foreach (var key in config.ActiveTitleFixes)
			{
				if (config.TitleFixMatcher.ContainsKey(key))
				{
					continue;
				}
				Log.Warn().WriteLine("Key {0} not found, configuration is broken! Disabling this key!", key);
				corruptKeys.Add(key);
			}

			// Fix configuration if needed
			if (corruptKeys.Count <= 0)
			{
				return;
			}
			foreach (var corruptKey in corruptKeys)
			{
				// Removing any reference to the key
				config.ActiveTitleFixes.Remove(corruptKey);
				config.TitleFixMatcher.Remove(corruptKey);
				config.TitleFixReplacer.Remove(corruptKey);
			}
			config.IsDirty = true;
		}

		public override string Designation => "TitleFix";

		public override string Description => Designation;

		public override bool ProcessCapture(ISurface surface, ICaptureDetails captureDetails)
		{
			var changed = false;
			var title = captureDetails.Title;
			if (!string.IsNullOrEmpty(title))
			{
				title = title.Trim();
				foreach (var titleIdentifier in config.ActiveTitleFixes)
				{
					var regexpString = config.TitleFixMatcher[titleIdentifier];
					var replaceString = config.TitleFixReplacer[titleIdentifier];
					if (replaceString == null)
					{
						replaceString = "";
					}
					if (!string.IsNullOrEmpty(regexpString))
					{
						var regex = new Regex(regexpString);
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