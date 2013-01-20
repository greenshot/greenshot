/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2013  Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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

namespace Greenshot.Helpers {
	/// <summary>
	/// A helper class for the startup registry
	/// </summary>
	public static class StartupHelper {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(StartupHelper));

		private const string RUNKEY6432 = @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Run";
		private const string RUNKEY = @"Software\Microsoft\Windows\CurrentVersion\Run";

		private const string APPLICATIONNAME = "Greenshot";

		private static string getExecutablePath() {
			return "\"" + Application.ExecutablePath + "\"";
		}

		/// <summary>
		/// Return true if the current user can write the RUN key of the local machine.
		/// </summary>
		/// <returns>true if Greenshot can write key</returns>
		public static bool canWriteRunAll() {
			try {
				using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RUNKEY, true)) {
				}
			} catch {
				return false;
			}
			return true;
		}

		/// <summary>
		/// Return true if the current user can write the RUN key of the current user.
		/// </summary>
		/// <returns>true if Greenshot can write key</returns>
		public static bool canWriteRunUser() {
			try {
				using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUNKEY, true)) {
				}
			} catch {
				return false;
			}
			return true;
		}

		/// <summary>
		/// Return the RUN key value of the local machine
		/// </summary>
		/// <returns>the RUN key value of the local machine</returns>
		public static Object getRunAllValue() {
			using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RUNKEY, false)) {
				if (key != null) {
					object runValue = key.GetValue(APPLICATIONNAME);
					if (runValue != null) {
						return runValue;
					}
				}
			}
			// for 64-bit systems we need to check the 32-bit keys too
			if (IntPtr.Size == 8) {
				using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RUNKEY6432, false)) {
					if (key != null) {
						object runValue = key.GetValue(APPLICATIONNAME);
						if (runValue != null) {
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
		public static Object getRunUserValue() {
			using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUNKEY, false)) {
				if (key != null) {
					object runValue = key.GetValue(APPLICATIONNAME);
					if (runValue != null) {
						return runValue;
					}
				}
			}
			// for 64-bit systems we need to check the 32-bit keys too
			if (IntPtr.Size == 8) {
				using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUNKEY6432, false)) {
					if (key != null) {
						object runValue = key.GetValue(APPLICATIONNAME);
						if (runValue != null) {
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
		public static bool hasRunAll() {
			try {
				return getRunAllValue() != null;
			} catch (Exception e) {
				LOG.Error("Error retrieving RunAllValue", e);
			}
			return false;
		}

		/// <summary>
		/// Return true if the current user has a RUN entry for Greenshot
		/// </summary>
		/// <returns>true if there is a run key</returns>
		public static bool hasRunUser() {
			Object runValue = null;
			try {
				runValue = getRunUserValue();
			} catch (Exception e) {
				LOG.Error("Error retrieving RunUserValue", e);
			}
			return runValue != null;
		}

		/// <summary>
		/// Delete the RUN key for the localmachine ("ALL")
		/// </summary>
		public static void deleteRunAll() {
			if (hasRunAll()) {
				try {
					using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RUNKEY, true)) {
						key.DeleteValue(APPLICATIONNAME);
					}
				} catch (Exception e) {
					LOG.Error("Error in deleteRunAll.", e);
				}
				try {
					// for 64-bit systems we need to delete the 32-bit keys too
					if (IntPtr.Size == 8) {
						using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RUNKEY6432, false)) {
							key.DeleteValue(APPLICATIONNAME);
						}
					}
				} catch (Exception e) {
					LOG.Error("Error in deleteRunAll.", e);
				}
			}
		}

		/// <summary>
		/// Delete the RUN key for the current user
		/// </summary>
		public static void deleteRunUser() {
			if (hasRunUser()) {
				try {
					using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUNKEY, true)) {
						key.DeleteValue(APPLICATIONNAME);
					}
				} catch (Exception e) {
					LOG.Error("Error in deleteRunUser.", e);
				}
				try {
					// for 64-bit systems we need to delete the 32-bit keys too
					if (IntPtr.Size == 8) {
						using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUNKEY6432, false)) {
							key.DeleteValue(APPLICATIONNAME);
						}
					}
				} catch (Exception e) {
					LOG.Error("Error in deleteRunUser.", e);
				}
			}
		}

		/// <summary>
		/// Set the RUN key for the current user
		/// </summary>
		public static void setRunUser() {
			try {
				using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUNKEY, true)) {
					key.SetValue(APPLICATIONNAME, getExecutablePath());
				}
			} catch (Exception e) {
				LOG.Error("Error in setRunUser.", e);
			}
		}

		/// <summary>
		/// Test if there is a link in the Statup folder
		/// </summary>
		/// <returns></returns>
		public static bool IsInStartupFolder() {
			try {
				string lnkName = Path.GetFileNameWithoutExtension(Application.ExecutablePath) + ".lnk";
				string startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
				if (Directory.Exists(startupPath)) {
					LOG.DebugFormat("Startup path: {0}", startupPath);
					if (File.Exists(Path.Combine(startupPath, lnkName))) {
						return true;
					}
				}
				string startupAll = Environment.GetEnvironmentVariable("ALLUSERSPROFILE") + @"\Microsoft\Windows\Start Menu\Programs\Startup";
				if (Directory.Exists(startupAll)) {
					LOG.DebugFormat("Startup all path: {0}", startupAll);
					if (File.Exists(Path.Combine(startupAll, lnkName))) {
						return true;
					}
				}
			} catch {
			}
			return false;
		}
	}
}
