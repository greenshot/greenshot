using System;
using System.Diagnostics;
using System.IO;

namespace GreenshotPlugin.Core
{
	/// <summary>
	/// Simple utility for the explorer
	/// </summary>
	public static class ExplorerHelper
	{
		/// <summary>
		/// Open the path in the windows explorer.
		/// If the path is a directory, it will just open the explorer with that directory.
		/// If the path is a file, the explorer is opened with the directory and the file is selected.
		/// </summary>
		/// <param name="path">Path to file or directory</param>
		public static void OpenInExplorer(string path)
		{
			if (path == null)
			{
				return;
			}
			try
			{
				// Check if path is a directory
				if (Directory.Exists(path))
				{
					using (Process.Start(path))
					{
					}
				}
				// Check if path is a file
				else if (File.Exists(path))
				{
					// Start the explorer process and select the file
					using (var explorer = Process.Start("explorer.exe", $"/select,\"{path}\""))
					{
						explorer?.WaitForInputIdle(500);
					}
				}
			}
			catch (Exception ex)
			{
				// Make sure we show what we tried to open in the exception
				ex.Data.Add("path", path);
				throw;
			}
		}
	}
}
