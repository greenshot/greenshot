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

using System.Collections.Generic;
using Greenshot.Addon.Core;
using Greenshot.Core;
using Microsoft.Win32;

#endregion

namespace Greenshot.CaptureCore
{
	/// <summary>
	///     Description of IEHelper.
	/// </summary>
	public static class IEHelper
	{
		// Internet explorer Registry key
		private const string IE_KEY = @"Software\Microsoft\Internet Explorer";

		/// <summary>
		///     Find the DirectUI window for MSAA (Accessible)
		/// </summary>
		/// <param name="browserWindowDetails">The browser WindowDetails</param>
		/// <returns>WindowDetails for the DirectUI window</returns>
		public static WindowDetails GetDirectUI(WindowDetails browserWindowDetails)
		{
			if (browserWindowDetails == null)
			{
				return null;
			}
			WindowDetails windowDetails = browserWindowDetails;
			// Since IE 9 the TabBandClass is less deep!
			if (IEVersion() < 9)
			{
				windowDetails = windowDetails.GetChild("CommandBarClass");
				windowDetails = windowDetails?.GetChild("ReBarWindow32");
			}
			windowDetails = windowDetails?.GetChild("TabBandClass");
			windowDetails = windowDetails?.GetChild("DirectUIHWND");
			return windowDetails;
		}

		/// <summary>
		///     Return an IEnumerable with the currently opened IE urls
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<string> GetIEUrls()
		{
			var urls = new List<string>();
			// Find the IE window
			foreach (WindowDetails ieWindow in WindowDetails.GetAllWindows("IEFrame"))
			{
				WindowDetails directUIWD = GetDirectUI(ieWindow);
				if (directUIWD != null)
				{
					var ieAccessible = new Accessible(directUIWD.Handle);
					List<string> ieUrls = ieAccessible.IETabUrls;
					if ((ieUrls != null) && (ieUrls.Count > 0))
					{
						foreach (string url in ieUrls)
						{
							if (!urls.Contains(url))
							{
								yield return url;
							}
						}
					}
				}
			}
		}

		/// <summary>
		///     Helper method to get the IE version
		/// </summary>
		/// <returns></returns>
		public static int IEVersion()
		{
			int version = 7;
			// Seeing if IE 9 is used, here we need another offset!
			using (RegistryKey ieKey = Registry.LocalMachine.OpenSubKey(IE_KEY, false))
			{
				object versionKey = ieKey?.GetValue("Version");
				if (versionKey != null)
				{
					int.TryParse(versionKey.ToString().Substring(0, 1), out version);
				}
			}
			return version;
		}
	}
}