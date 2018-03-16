#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.Drawing;
using System.IO;
using Dapplo.Ini;
using Dapplo.Log;
using GreenshotPlugin.Core;

#endregion

namespace Greenshot.Addon.ExternalCommand
{
	/// <summary>
	/// A cache for icons
	/// </summary>
	public static class IconCache
	{
		private static readonly IExternalCommandConfiguration Config = IniConfig.Current.Get<IExternalCommandConfiguration>();
		private static readonly LogSource Log = new LogSource();

        /// <summary>
        /// Retrieve the icon for a command
        /// </summary>
        /// <param name="commandName">string</param>
        /// <param name="useLargeIcons">true to use the large icon</param>
        /// <returns>Bitmap</returns>
        public static Bitmap IconForCommand(string commandName, bool useLargeIcons)
		{
		    Bitmap icon = null;
			if (commandName == null)
			{
				return null;
			}
			if (!Config.Commandline.ContainsKey(commandName) || !File.Exists(Config.Commandline[commandName]))
			{
				return null;
			}
			try
			{
				icon = PluginUtils.GetCachedExeIcon(Config.Commandline[commandName], 0, useLargeIcons);
			}
			catch (Exception ex)
			{
				Log.Warn().WriteLine(ex, "Problem loading icon for " + Config.Commandline[commandName]);
			}
			return icon;
		}
	}
}