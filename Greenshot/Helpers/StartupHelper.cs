/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2011  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Security;
using System.Security.AccessControl;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;

using Microsoft.Win32;

namespace Greenshot.Helpers {
	/// <summary>
	/// A helper class for the startup registry
	/// </summary>
	public class StartupHelper {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(StartupHelper));

		private const string RUNKEY = @"Software\Microsoft\Windows\CurrentVersion\Run";
		private const string LOCALMACHINE = @"HKEY_LOCAL_MACHINE\";
		private const string CURRENTUSER = @"HKEY_CURRENT_USER\";

		private const string APPLICATIONNAME = "Greenshot";

		private static string getExecutablePath() {
			return "\"" + Application.ExecutablePath + "\"";
		}
		
		public static Object getRunAllValue() {
			using (RegistryKey key = Registry.LocalMachine.OpenSubKey(RUNKEY, false)) {
				if (key != null) {
					return key.GetValue(APPLICATIONNAME);
				}
			}
			return null;
		}

		public static Object getRunUserValue() {
			using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUNKEY, false)) {
				if (key != null) {
					return key.GetValue(APPLICATIONNAME);
				}
			}
			return null;
		}
		
		public static bool checkRunAll() {
			Object runValue = null;
			try {
				runValue = getRunAllValue();
			} catch (Exception e) {
				LOG.Error("Error retrieving RunAllValue", e);
			}
			return runValue != null;
		}

		public static bool checkRunUser() {
			Object runValue = null;
			try {
				runValue = getRunUserValue();
			} catch (Exception e) {
				LOG.Error("Error retrieving RunUserValue", e);
			}
			return runValue != null;
		}
		
		public static void deleteRunUser() {
			using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUNKEY, true)) {
				try {
					if (checkRunUser()) {
						key.DeleteValue(APPLICATIONNAME);
					}
				} catch (Exception e) {
					LOG.Error("Error in deleteRunUser.", e);
				}
			}
		}

		public static void setRunUser() {
			using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RUNKEY, true)) {
				try {
					key.SetValue(APPLICATIONNAME, getExecutablePath());
				} catch (Exception e) {
					LOG.Error("Error in setRunUser.", e);
				}
			}
		}
	}
}
