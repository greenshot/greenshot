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

using System;
using System.Diagnostics;
using System.IO;

#endregion

namespace GreenshotPlugin.Core
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
					using (Process.Start(path))
					{
						return true;
					}
				}
				// Check if path is a file
				if (File.Exists(path))
				{
					// Start the explorer process and select the file
					using (var explorer = Process.Start("explorer.exe", $"/select,\"{path}\""))
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