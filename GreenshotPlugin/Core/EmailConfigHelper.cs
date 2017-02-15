#region Dapplo 2017 - GNU Lesser General Public License

// Dapplo - building blocks for .NET applications
// Copyright (C) 2017 Dapplo
// 
// For more information see: http://dapplo.net/
// Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
// This file is part of Greenshot
// 
// Greenshot is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Greenshot is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have a copy of the GNU Lesser General Public License
// along with Greenshot. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#endregion

#region Usings

using System.IO;
using Microsoft.Win32;

#endregion

namespace GreenshotPlugin.Core
{
	/// <summary>
	///     Description of EmailConfigHelper.
	/// </summary>
	public static class EmailConfigHelper
	{
		private const string OutlookPathKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\OUTLOOK.EXE";
		private const string MapiClientKey = @"SOFTWARE\Clients\Mail";
		private const string MapiLocationKey = @"SOFTWARE\Microsoft\Windows Messaging Subsystem";
		private const string MapiKey = @"MAPI";

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

		public static bool HasMapi()
		{
			using (var key = Registry.LocalMachine.OpenSubKey(MapiLocationKey, false))
			{
				return key != null && "1".Equals(key.GetValue(MapiKey, "0"));
			}
		}

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