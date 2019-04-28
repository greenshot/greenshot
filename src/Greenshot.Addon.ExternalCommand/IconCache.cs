// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.IO;
using Dapplo.Log;
using Greenshot.Addon.ExternalCommand.Entities;
using Greenshot.Addons.Core;
using Greenshot.Gfx;

namespace Greenshot.Addon.ExternalCommand
{
	/// <summary>
	/// A cache for icons
	/// </summary>
	public static class IconCache
	{
		private static readonly LogSource Log = new LogSource();

        /// <summary>
        /// Retrieve the icon for a command
        /// </summary>
        /// <param name="externalCommandDefinition">string</param>
        /// <param name="useLargeIcons">true to use the large icon</param>
        /// <returns>Bitmap</returns>
        public static IBitmapWithNativeSupport IconForCommand(ExternalCommandDefinition externalCommandDefinition, bool useLargeIcons)
		{
            IBitmapWithNativeSupport icon = null;
			if (externalCommandDefinition == null)
			{
				return null;
			}
			if (!File.Exists(externalCommandDefinition.Command))
			{
				return null;
			}
			try
			{
				icon = PluginUtils.GetCachedExeIcon(externalCommandDefinition.Command, 0, useLargeIcons);
			}
			catch (Exception ex)
			{
				Log.Warn().WriteLine(ex, "Problem loading icon for " + externalCommandDefinition.Command);
			}
			return icon;
		}
	}
}