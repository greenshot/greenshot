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
using System.ComponentModel;
using Dapplo.Config.Ini;

#endregion

namespace Greenshot.Core.Configuration
{
	/// <summary>
	///     The configuration for the hotkeys
	/// </summary>
	public interface IHotkeyConfiguration : ISubSection
	{
		[Description("Hotkey for starting the fullscreen capture")]
		[DefaultValue("Ctrl + PrintScreen")]
		string FullscreenHotkey { get; set; }

		[Description("Hotkey for starting the IE capture")]
		[DefaultValue("Shift + Ctrl + PrintScreen")]
		string IEHotkey { get; set; }

		/// <summary>
		///     FEATURE-709 / FEATURE-419: Add the possibility to ignore the hotkeys
		/// </summary>
		[Description("Ignore the hotkey if currently one of the specified processes is active")]
		IList<string> IgnoreHotkeyProcessList { get; set; }

		[Description("Hotkey for starting the last region capture")]
		[DefaultValue("Shift + PrintScreen")]
		string LastregionHotkey { get; set; }

		[Description("Hotkey for starting the region capture")]
		[DefaultValue("PrintScreen")]
		string RegionHotkey { get; set; }

		[Description("Hotkey for starting the window capture")]
		[DefaultValue("Alt + PrintScreen")]
		string WindowHotkey { get; set; }
	}
}