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
using System.Collections.Generic;
using Microsoft.Win32;

namespace GreenshotPlugin.Core {
	/// <summary>
	/// Description of IEHelper.
	/// </summary>
	public static class IEHelper {
		// Internet explorer Registry key
		private const string IE_KEY = @"Software\Microsoft\Internet Explorer";
		/// <summary>
		/// Helper method to get the IE version
		/// </summary>
		/// <returns></returns>
		public static int IEVersion() {
			int version = 7;
			// Seeing if IE 9 is used, here we need another offset!
			using (RegistryKey ieKey = Registry.LocalMachine.OpenSubKey(IE_KEY, false)) {
				if (ieKey != null) {
					 object versionKey = ieKey.GetValue("Version");
					 if (versionKey != null) {
						int.TryParse(versionKey.ToString().Substring(0,1), out version);
					}
				}
			}
			return version;
		}
		
		/// <summary>
		/// Find the DirectUI window for MSAA (Accessible)
		/// </summary>
		/// <param name="browserWindowDetails">The browser WindowDetails</param>
		/// <returns>WindowDetails for the DirectUI window</returns>
		public static WindowDetails GetDirectUI(WindowDetails browserWindowDetails) {
			if (browserWindowDetails == null) {
				return null;
			}
			WindowDetails tmpWD = browserWindowDetails;
			// Since IE 9 the TabBandClass is less deep!
			if (IEHelper.IEVersion() < 9) {
				tmpWD = tmpWD.GetChild("CommandBarClass");
				if (tmpWD != null) {
					tmpWD = tmpWD.GetChild("ReBarWindow32");
				}
			}
			if (tmpWD != null) {
				tmpWD = tmpWD.GetChild("TabBandClass");
			}
			if (tmpWD != null) {
				tmpWD = tmpWD.GetChild("DirectUIHWND");
			}
			return tmpWD;
		}
		
		/// <summary>
		/// Return an IEnumerable with the currently opened IE urls
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<string> GetIEUrls() {
			List<string> urls = new List<string>();
			// Find the IE window
			foreach (WindowDetails ieWindow in WindowDetails.GetAllWindows("IEFrame")) {
				WindowDetails directUIWD = GetDirectUI(ieWindow);
				if (directUIWD != null) {
					Accessible ieAccessible = new Accessible(directUIWD.Handle);
					List<string> ieUrls = ieAccessible.IETabUrls;
					if (ieUrls != null && ieUrls.Count > 0) {
						foreach(string url in ieUrls) {
							if (!urls.Contains(url)) {
								urls.Add(url);
							}
						}
					}
				}
			}

			return urls;
		}
	}
}
