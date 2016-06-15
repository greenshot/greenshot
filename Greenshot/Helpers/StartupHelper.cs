/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub: https://github.com/greenshot
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

using System;
using System.Windows.Forms;

using Microsoft.Win32;
using System.IO;
using Dapplo.LogFacade;

namespace Greenshot.Helpers
{
	/// <summary>
	/// A helper class for the startup registry
	/// </summary>
	public static class StartupHelper
	{
		private static readonly LogSource Log = new LogSource();

		private const string Runkey6432 = @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Run";
		private const string Runkey = @"Software\Microsoft\Windows\CurrentVersion\Run";

		private const string Applicationname = "Greenshot";

		private static string GetExecutablePath()
		{
			return "\"" + Application.ExecutablePath + "\"";
		}

		/// <summary>
		/// Return true if the current user can write the RUN key of the local machine.
		/// </summary>
		/// <returns>true if Greenshot can write key</returns>
		public static bool CanWriteRunAll()
		{
			try
			{
				using (Registry.LocalMachine.OpenSubKey(Runkey, true))
				{
				}
			}
			catch
			{
				return false;
			}
			return true;
		}

		/// <summary>
		/// Return true if the current user can write the RUN key of the current user.
		/// </summary>
		/// <returns>true if Greenshot can write key</returns>
		public static bool CanWriteRunUser()
		{
			try
			{
				using (Registry.CurrentUser.OpenSubKey(Runkey, true))
				{
				}
			}
			catch
			{
				return false;
			}
			return true;
		}

		/// <summary>
		/// Return the RUN key value of the local machine
		/// </summary>
		/// <returns>the RUN key value of the local machine</returns>
		public static Object GetRunAllValue()
		{
			using (RegistryKey key = Registry.LocalMachine.OpenSubKey(Runkey, false))
			{
				if (key != null)
				{
					object runValue = key.GetValue(Applicationname);
					if (runValue != null)
					{
						return runValue;
					}
				}
			}
			// for 64-bit systems we need to check the 32-bit keys too
			if (IntPtr.Size == 8)
			{
				using (RegistryKey key = Registry.LocalMachine.OpenSubKey(Runkey6432, false))
				{
					if (key != null)
					{
						object runValue = key.GetValue(Applicationname);
						if (runValue != null)
						{
							return runValue;
						}
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Return the RUN key value of the current user
		/// </summary>
		/// <returns>the RUN key value of the current user</returns>
		public static Object GetRunUserValue()
		{
			using (RegistryKey key = Registry.CurrentUser.OpenSubKey(Runkey, false))
			{
				if (key != null)
				{
					object runValue = key.GetValue(Applicationname);
					if (runValue != null)
					{
						return runValue;
					}
				}
			}
			// for 64-bit systems we need to check the 32-bit keys too
			if (IntPtr.Size == 8)
			{
				using (RegistryKey key = Registry.CurrentUser.OpenSubKey(Runkey6432, false))
				{
					if (key != null)
					{
						object runValue = key.GetValue(Applicationname);
						if (runValue != null)
						{
							return runValue;
						}
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Return true if the local machine has a RUN entry for Greenshot
		/// </summary>
		/// <returns>true if there is a run key</returns>
		public static bool HasRunAll()
		{
			try
			{
				return GetRunAllValue() != null;
			}
			catch (Exception e)
			{
				Log.Error().WriteLine("Error retrieving RunAllValue", e);
			}
			return false;
		}

		/// <summary>
		/// Return true if the current user has a RUN entry for Greenshot
		/// </summary>
		/// <returns>true if there is a run key</returns>
		public static bool HasRunUser()
		{
			Object runValue = null;
			try
			{
				runValue = GetRunUserValue();
			}
			catch (Exception e)
			{
				Log.Error().WriteLine("Error retrieving RunUserValue", e);
			}
			return runValue != null;
		}

		/// <summary>
		/// Delete the RUN key for the localmachine ("ALL")
		/// </summary>
		public static void DeleteRunAll()
		{
			if (!HasRunAll())
			{
				return;
			}
			try
			{
				using (var key = Registry.LocalMachine.OpenSubKey(Runkey, true))
				{
					key?.DeleteValue(Applicationname);
				}
			}
			catch (Exception e)
			{
				Log.Error().WriteLine("Error in deleteRunAll.", e);
			}
			try
			{
				// for 64-bit systems we need to delete the 32-bit keys too
				if (IntPtr.Size == 8)
				{
					using (var key = Registry.LocalMachine.OpenSubKey(Runkey6432, false))
					{
						key?.DeleteValue(Applicationname);
					}
				}
			}
			catch (Exception e)
			{
				Log.Error().WriteLine("Error in deleteRunAll.", e);
			}
		}

		/// <summary>
		/// Delete the RUN key for the current user
		/// </summary>
		public static void DeleteRunUser()
		{
			if (!HasRunUser())
			{
				return;
			}
			try
			{
				using (var key = Registry.CurrentUser.OpenSubKey(Runkey, true))
				{
					key?.DeleteValue(Applicationname);
				}
			}
			catch (Exception e)
			{
				Log.Error().WriteLine("Error in deleteRunUser.", e);
			}
			try
			{
				// for 64-bit systems we need to delete the 32-bit keys too
				if (IntPtr.Size == 8)
				{
					using (var key = Registry.CurrentUser.OpenSubKey(Runkey6432, false))
					{
						key?.DeleteValue(Applicationname);
					}
				}
			}
			catch (Exception e)
			{
				Log.Error().WriteLine("Error in deleteRunUser.", e);
			}
		}

		/// <summary>
		/// Set the RUN key for the current user
		/// </summary>
		public static void SetRunUser()
		{
			try
			{
				using (var key = Registry.CurrentUser.OpenSubKey(Runkey, true))
				{
					key?.SetValue(Applicationname, GetExecutablePath());
				}
			}
			catch (Exception e)
			{
				Log.Error().WriteLine("Error in setRunUser.", e);
			}
		}

		/// <summary>
		/// Test if there is a link in the Statup folder
		/// </summary>
		/// <returns></returns>
		public static bool IsInStartupFolder()
		{
			try
			{
				string lnkName = Path.GetFileNameWithoutExtension(Application.ExecutablePath) + ".lnk";
				string startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
				if (Directory.Exists(startupPath))
				{
					Log.Debug().WriteLine("Startup path: {0}", startupPath);
					if (File.Exists(Path.Combine(startupPath, lnkName)))
					{
						return true;
					}
				}
				string startupAll = Environment.GetEnvironmentVariable("ALLUSERSPROFILE") + @"\Microsoft\Windows\Start Menu\Programs\Startup";
				if (Directory.Exists(startupAll))
				{
					Log.Debug().WriteLine("Startup all path: {0}", startupAll);
					if (File.Exists(Path.Combine(startupAll, lnkName)))
					{
						return true;
					}
				}
			}
			catch
			{
				// ignored
			}
			return false;
		}
	}
}