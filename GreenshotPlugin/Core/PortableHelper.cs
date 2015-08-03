/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015  Thomas Braun, Jens Klingen, Robin Krom
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

using log4net;
using System;
using System.IO;
using System.Reflection;

namespace GreenshotPlugin.Core {

	/// <summary>
	/// This class contains all logic to help with Portable App issues
	/// </summary>
	public static class PortableHelper {
		private static readonly ILog LOG = LogManager.GetLogger(typeof(PortableHelper));
		private static bool _portable = false;
		private static bool _portableCheckMade = false;
		private static string _applicationStartupPath;

		static PortableHelper() {
			try {
				_applicationStartupPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			} catch (Exception exception) {
				LOG.WarnFormat("Problem retrieving the AssemblyLocation: {0} (Designer mode?)", exception.Message);
				_applicationStartupPath = @".";
			}
		}

		/// <summary>
		/// Tell us if we are in portable mode
		/// </summary>
		public static bool IsPortable {
			get {
				if (!_portableCheckMade) {
					string pafPath = Path.Combine(_applicationStartupPath, @"App\Greenshot");
					if (!_portable) {
						LOG.Info("Checking for portable mode.");
						_portableCheckMade = true;
						if (Directory.Exists(pafPath)) {
							_portable = true;
							LOG.Info("Portable mode active!");
						}
					}
					
				}
				return _portable;
			}
		}

		/// <summary>
		/// Get the location of the ini file for when we are in Portable mode
		/// </summary>
		public static string PortableIniFileLocation {
			get {
				if (_portable) {
					string pafConfigPath = Path.Combine(_applicationStartupPath, @"Data\Settings");
					try {
						if (!Directory.Exists(pafConfigPath)) {
							Directory.CreateDirectory(pafConfigPath);
						}
						return Path.Combine(pafConfigPath, "greenshot.ini");
					} catch (Exception e) {
						LOG.WarnFormat("Portable mode NOT possible, couldn't create directory '{0}'! Reason: {1}", pafConfigPath, e.Message);
					}
				}
				return null;
			}
		}
	}
}
