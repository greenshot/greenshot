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

using Dapplo.Config.Ini;
using Dapplo.Log;
using Greenshot.Addon.Configuration;
using Greenshot.Addon.Interfaces.Plugin;

#endregion

namespace Greenshot.Addon.Core
{
	/// <summary>
	///     Description of PluginUtils.
	/// </summary>
	public static class PluginUtils
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly ICoreConfiguration Conf = IniConfig.Current.Get<ICoreConfiguration>();

		/// <summary>
		///     Simple global property to get the Greenshot host
		/// </summary>
		public static IGreenshotHost Host { get; set; }
	}
}