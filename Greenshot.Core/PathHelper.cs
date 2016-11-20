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

using System;
using System.IO;
using Dapplo.Log;
using Microsoft.Win32;

#endregion

namespace Greenshot.Core
{
	/// <summary>
	///     Some utilities for Path tools
	/// </summary>
	public static class PathHelper
	{
		private static readonly LogSource Log = new LogSource();

		/// <summary>
		///     Get the path of an executable
		/// </summary>
		/// <param name="exeName">e.g. cmd.exe</param>
		/// <returns>Path to file</returns>
		public static string GetExePath(string exeName)
		{
			using (var key = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{exeName}", false))
			{
				if (key != null)
				{
					// "" is the default key, which should point to the requested location
					return (string) key.GetValue("");
				}
			}
			foreach (string pathEntry in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
			{
				try
				{
					string path = pathEntry.Trim();
					if (!string.IsNullOrEmpty(path) && File.Exists(path = Path.Combine(path, exeName)))
					{
						return Path.GetFullPath(path);
					}
				}
				catch (Exception)
				{
					Log.Warn().WriteLine("Problem with path entry '{0}'.", pathEntry);
				}
			}
			return null;
		}
	}
}