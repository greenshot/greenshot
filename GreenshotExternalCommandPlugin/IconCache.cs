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

namespace ExternalCommand
{
	public static class IconCache
	{
		private static readonly ExternalCommandConfiguration config = IniConfig.GetIniSection<ExternalCommandConfiguration>();
		private static readonly ILog LOG = LogManager.GetLogger(typeof(IconCache));

		public static Image IconForCommand(string commandName)
		{
			Image icon = null;
			if (commandName != null)
			{
				if (config.Commandline.ContainsKey(commandName) && File.Exists(config.Commandline[commandName]))
				{
					try
					{
						icon = PluginUtils.GetCachedExeIcon(config.Commandline[commandName], 0);
					}
					catch (Exception ex)
					{
						LOG.Warn("Problem loading icon for " + config.Commandline[commandName], ex);
					}
				}
			}
			return icon;
		}
	}
}