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

using System;
using System.Drawing;
using System.IO;
using GreenshotPlugin.Core;
using GreenshotPlugin.IniFile;
using log4net;

#endregion

namespace GreenshotExternalCommandPlugin
{
	/// <summary>
	/// A cache for icons
	/// </summary>
	public static class IconCache
	{
		private static readonly ExternalCommandConfiguration Config = IniConfig.GetIniSection<ExternalCommandConfiguration>();
		private static readonly ILog Log = LogManager.GetLogger(typeof(IconCache));

		/// <summary>
		/// Retrieve the icon for a command
		/// </summary>
		/// <param name="commandName">string</param>
		/// <returns>Image</returns>
		public static Image IconForCommand(string commandName)
		{
			Image icon = null;
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
				icon = PluginUtils.GetCachedExeIcon(Config.Commandline[commandName], 0);
			}
			catch (Exception ex)
			{
				Log.Warn("Problem loading icon for " + Config.Commandline[commandName], ex);
			}
			return icon;
		}
	}
}