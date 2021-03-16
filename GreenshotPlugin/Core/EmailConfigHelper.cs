/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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

using System.IO;
using Microsoft.Win32;

namespace GreenshotPlugin.Core {
	/// <summary>
	/// Description of EmailConfigHelper.
	/// </summary>
	public static class EmailConfigHelper {

		public static string GetMapiClient() => RegistryHive.LocalMachine.ReadKey64Or32(@"Clients\Mail");
		
		public static bool HasMapi()
		{
			var value = RegistryHive.LocalMachine.ReadKey64Or32(@"Microsoft\Windows Messaging Subsystem", "MAPI", "0");
			return "1".Equals(value);
		}
		
		public static string GetOutlookExePath() => RegistryHive.LocalMachine.ReadKey64Or32(@"Microsoft\Windows\CurrentVersion\App Paths\OUTLOOK.EXE");

		/// <summary>
		/// Check if Outlook is installed
		/// </summary>
		/// <returns>Returns true if outlook is installed</returns>
		public static bool HasOutlook() {
			string outlookPath = GetOutlookExePath();
			if (outlookPath == null)
			{
				return false;
			}
			return File.Exists(outlookPath);
		}
	}
}
