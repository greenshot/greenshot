/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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
using Dapplo.Log;

namespace GreenshotPlugin.Core
{
	/// <summary>
	/// This helper is there to make sure the embedded browser is the latest possible version
	/// </summary>
	public static class WebBrowserHelper
	{
		private static readonly LogSource Log = new LogSource();
		/// <summary>
		/// Get the highest possible version for the embedded browser
		/// </summary>
		/// <param name="ignoreDoctype">true to ignore the doctype when loading a page</param>
		/// <returns>IE Feature</returns>
		public static int GetEmbVersion(bool ignoreDoctype = true)
		{
			int ieVersion = GetBrowserVersion();

			if (ieVersion > 9)
			{
				return ieVersion * 1000 + (ignoreDoctype ? 1 : 0);
			}

			if (ieVersion > 7)
			{
				return ieVersion * 1111;
			}

			return 7000;
		}

		/// <summary>
		/// Fix browser version to the highest possible
		/// </summary>
		/// <param name="ignoreDoctype">true to ignore the doctype when loading a page</param>
		public static void FixBrowserVersion(bool ignoreDoctype = true)
		{
			string applicationName = System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location);
			FixBrowserVersion(applicationName, ignoreDoctype);
		}

		/// <summary>
		/// Fix the browser version for the specified application
		/// </summary>
		/// <param name="applicationName">Name of the process</param>
		/// <param name="ignoreDoctype">true to ignore the doctype when loading a page</param>
		public static void FixBrowserVersion(string applicationName, bool ignoreDoctype = true)
		{
			FixBrowserVersion(applicationName, GetEmbVersion(ignoreDoctype));
		}

		/// <summary>
		/// Fix the browser version for the specified application
		/// </summary>
		/// <param name="applicationName">Name of the process</param>
		/// <param name="ieVersion">Version, see <a href="https://msdn.microsoft.com/en-us/library/ee330730(v=vs.85).aspx#browser_emulation">Browser Emulation</a></param>
		public static void FixBrowserVersion(string applicationName, int ieVersion)
		{
			ModifyRegistry("HKEY_CURRENT_USER", applicationName + ".exe", ieVersion);
#if DEBUG
			ModifyRegistry("HKEY_CURRENT_USER", applicationName + ".vshost.exe", ieVersion);
#endif
		}

		/// <summary>
		/// Make the change to the registry
		/// </summary>
		/// <param name="root">HKEY_CURRENT_USER or something</param>
		/// <param name="applicationName">Name of the executable</param>
		/// <param name="ieFeatureVersion">Version to use</param>
		private static void ModifyRegistry(string root, string applicationName, int ieFeatureVersion)
		{
			var regKey = root + @"\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION";
			try
			{
				Microsoft.Win32.Registry.SetValue(regKey, applicationName, ieFeatureVersion);
			}
			catch (Exception ex)
			{
				// some config will hit access rights exceptions
				// this is why we try with both LOCAL_MACHINE and CURRENT_USER
				Log.Error().WriteLine(ex, "couldn't modify the registry key {0}", regKey);
			}
		}

		/// <summary>
		/// Get the current browser version
		/// </summary>
		/// <returns>int with browser version</returns>
		public static int GetBrowserVersion()
		{
			// string strKeyPath = @"HKLM\SOFTWARE\Microsoft\Internet Explorer";
			const string ieRegKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer";

			int maxVer = 0;
			foreach (var value in new []{ "svcVersion", "svcUpdateVersion", "Version", "W2kVersion" })
			{
				object objVal = Microsoft.Win32.Registry.GetValue(ieRegKey, value, "0");
				string strVal = Convert.ToString(objVal);

				int iPos = strVal.IndexOf('.');
				if (iPos > 0)
				{
					strVal = strVal.Substring(0, iPos);
				}

				int res;
				if (int.TryParse(strVal, out res))
				{
					maxVer = Math.Max(maxVer, res);
				}
			}

			return maxVer;
		}


	}
}
