/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
using System.Collections.Generic;
using System.Linq;
using Dapplo.Log;
using Dapplo.Windows.Desktop;
using Microsoft.Win32;

namespace Greenshot.Addon.InternetExplorer {
    /// <summary>
    /// Util code for Internet Explorer
    /// </summary>
    public  class InternetExplorerHelper {
		private static readonly LogSource Log = new LogSource();

		// Internet explorer Registry key
		private const string IeKey = @"Software\Microsoft\Internet Explorer";

		/// <summary>
		///     Get the current browser version
		/// </summary>
		/// <returns>int with browser version</returns>
		public static int IEVersion
		{
			get
			{
				var maxVer = 7;
				using (var ieKey = Registry.LocalMachine.OpenSubKey(IeKey, false))
				{
				    if (ieKey == null)
				    {
				        return maxVer;
				    }
                    foreach (var value in new[] { "svcVersion", "svcUpdateVersion", "Version", "W2kVersion" })
					{
						var objVal = ieKey.GetValue(value, "0");
						var strVal = Convert.ToString(objVal);

						var iPos = strVal.IndexOf('.');
						if (iPos > 0)
						{
							strVal = strVal.Substring(0, iPos);
						}

					    if (int.TryParse(strVal, out var res))
						{
							maxVer = Math.Max(maxVer, res);
						}
					}
				}

				return maxVer;
			}
		}

		/// <summary>
		/// Find the DirectUI window for MSAA (Accessible)
		/// </summary>
		/// <param name="browserWindowDetails">The browser WindowDetails</param>
		/// <returns>WindowDetails for the DirectUI window</returns>
		public static IInteropWindow GetDirectUi(IInteropWindow browserWindowDetails) {
			if (browserWindowDetails == null) {
				return null;
			}
			var tmpWd = browserWindowDetails;

			// Since IE 9 the TabBandClass is less deep!
			if (IEVersion < 9) {
				tmpWd = tmpWd.GetChildren().FirstOrDefault(window => window.GetClassname() == "CommandBarClass");
				tmpWd = tmpWd?.GetChildren().FirstOrDefault(window => window.GetClassname() == "ReBarWindow32");

			}
			tmpWd = tmpWd?.GetChildren().FirstOrDefault(window => window.GetClassname() == "TabBandClass");
			tmpWd = tmpWd?.GetChildren().FirstOrDefault(window => window.GetClassname() == "DirectUIHWND");
			return tmpWd;
		}
		
		/// <summary>
		/// Return an IEnumerable with the currently opened IE urls
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<string> GetIEUrls() {
			// Find the IE window
			foreach (var ieWindow in InteropWindowQuery.GetTopWindows().Where(window => window.GetClassname() == "IEFrame"))
			{
				var directUiWd = GetDirectUi(ieWindow);
			    if (directUiWd == null)
			    {
			        continue;
			    }
#if !NETCOREAPP3_0
                var ieAccessible = new Accessible(directUiWd.Handle);
			    foreach (var url in ieAccessible.IETabUrls)
			    {
			        yield return url;
			    }
#endif

            }
#if NETCOREAPP3_0
            return Enumerable.Empty<string>();
#endif
        }
	}
}
