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
using System.Diagnostics;
using System.IO;

namespace Greenshot.Addons.Core
{
	/// <summary>
	///     Simple utility for the explorer
	/// </summary>
	public static class ExplorerHelper
	{
		/// <summary>
		///     Open the path in the windows explorer.
		///     If the path is a directory, it will just open the explorer with that directory.
		///     If the path is a file, the explorer is opened with the directory and the file is selected.
		/// </summary>
		/// <param name="path">Path to file or directory</param>
		public static bool OpenInExplorer(string path)
		{
			if (path == null)
			{
				return false;
			}
            
            
			try
			{
				// Check if path is a directory
				if (Directory.Exists(path))
				{
                    var processStartInfo = new ProcessStartInfo(path)
                    {
                        CreateNoWindow = true,
                        UseShellExecute = true
                    };
					using (Process.Start(processStartInfo))
					{
						return true;
					}
				}
				// Check if path is a file
				if (File.Exists(path))
				{
                    var processStartInfo = new ProcessStartInfo("explorer.exe")
                    {
                        Arguments = $"/select,\"{path}\"",
                        CreateNoWindow = true,
                        UseShellExecute = true
                    };
					// Start the explorer process and select the file
					using (var explorer = Process.Start(processStartInfo))
					{
						explorer?.WaitForInputIdle(500);
						return true;
					}
				}
			}
			catch (Exception ex)
			{
				// Make sure we show what we tried to open in the exception
				ex.Data.Add("path", path);
				throw;
			}
			return false;
		}
	}
}