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

using System.IO;
using Microsoft.Win32;

namespace Greenshot.Addons.Core
{
	/// <summary>
	/// Contains some helper functionality for MAPI
	/// </summary>
	public static class EmailConfigHelper
	{
		private const string OutlookPathKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\OUTLOOK.EXE";
		private const string MapiClientKey = @"SOFTWARE\Clients\Mail";
		private const string MapiLocationKey = @"SOFTWARE\Microsoft\Windows Messaging Subsystem";
		private const string MapiKey = @"MAPI";

		/// <summary>
		/// Retrieve the name of the installed MAPI client
		/// </summary>
		/// <returns></returns>
		public static string GetMapiClient()
		{
			using (var key = Registry.CurrentUser.OpenSubKey(MapiClientKey, false))
			{
				if (key != null)
				{
					return (string) key.GetValue("");
				}
			}
			using (var key = Registry.LocalMachine.OpenSubKey(MapiClientKey, false))
			{
				return (string) key?.GetValue("");
			}
		}

        /// <summary>
        /// Check if a MAPI client is installed
        /// </summary>
        /// <returns>bool</returns>
        public static bool HasMapi()
		{
			using (var key = Registry.LocalMachine.OpenSubKey(MapiLocationKey, false))
			{
				return key != null && "1".Equals(key.GetValue(MapiKey, "0"));
			}
		}

        /// <summary>
        /// Get the path of Outlook from the registry
        /// </summary>
        /// <returns>string</returns>
        public static string GetOutlookExePath()
		{
			using (var key = Registry.LocalMachine.OpenSubKey(OutlookPathKey, false))
			{
				if (key != null)
				{
					// "" is the default key, which should point to the outlook location
					return (string) key.GetValue("");
				}
			}
			return null;
		}

		/// <summary>
		///     Check if Outlook is installed
		/// </summary>
		/// <returns>Returns true if outlook is installed</returns>
		public static bool HasOutlook()
		{
			var outlookPath = GetOutlookExePath();
			if (outlookPath != null)
			{
				if (File.Exists(outlookPath))
				{
					return true;
				}
			}
			return false;
		}
	}
}