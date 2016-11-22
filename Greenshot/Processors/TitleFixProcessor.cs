//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Dapplo.Config.Ini;
using Dapplo.Log;
using Greenshot.Addon.Configuration;
using Greenshot.Addon.Interfaces;
using Greenshot.Core.Interfaces;

#endregion

namespace Greenshot.Processors
{
	/// <summary>
	///     Description of TitleFixProcessor.
	/// </summary>
	public class TitleFixProcessor
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly ICoreConfiguration config = IniConfig.Current.Get<ICoreConfiguration>();

		public TitleFixProcessor()
		{
			IList<string> corruptKeys = new List<string>();
			foreach (string key in config.ActiveTitleFixes)
			{
				if (!config.TitleFixMatcher.ContainsKey(key))
				{
					Log.Warn().WriteLine("Key {0} not found, configuration is broken! Disabling this key!");
					corruptKeys.Add(key);
				}
			}

			// Fix configuration if needed
			if (corruptKeys.Count > 0)
			{
				foreach (string corruptKey in corruptKeys)
				{
					// Removing any reference to the key
					config.ActiveTitleFixes.Remove(corruptKey);
					config.TitleFixMatcher.Remove(corruptKey);
					config.TitleFixReplacer.Remove(corruptKey);
				}
			}
		}

		public string Description
		{
			get { return Designation; }
		}

		public string Designation
		{
			get { return "TitleFix"; }
		}

		public bool ProcessCapture(ICapture surface, ICaptureDetails captureDetails)
		{
			bool changed = false;
			string title = captureDetails.Title;
			if (!string.IsNullOrEmpty(title))
			{
				title = title.Trim();
				foreach (string titleIdentifier in config.ActiveTitleFixes)
				{
					string regexpString = config.TitleFixMatcher[titleIdentifier];
					string replaceString = config.TitleFixReplacer[titleIdentifier];
					if (replaceString == null)
					{
						replaceString = "";
					}
					if (!string.IsNullOrEmpty(regexpString))
					{
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