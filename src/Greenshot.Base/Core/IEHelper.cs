/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using log4net;
using Microsoft.Win32;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Description of IEHelper.
    /// </summary>
    public static class IEHelper
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(IEHelper));

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
                    foreach (var value in new[]
                    {
                        "svcVersion", "svcUpdateVersion", "Version", "W2kVersion"
                    })
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
        ///     Get the highest possible version for the embedded browser
        /// </summary>
        /// <param name="ignoreDoctype">true to ignore the doctype when loading a page</param>
        /// <returns>IE Feature</returns>
        public static int GetEmbVersion(bool ignoreDoctype = true)
        {
            var ieVersion = IEVersion;

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
        ///     Fix browser version to the highest possible
        /// </summary>
        /// <param name="ignoreDoctype">true to ignore the doctype when loading a page</param>
        public static void FixBrowserVersion(bool ignoreDoctype = true)
        {
            var applicationName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
            FixBrowserVersion(applicationName, ignoreDoctype);
        }

        /// <summary>
        ///     Fix the browser version for the specified application
        /// </summary>
        /// <param name="applicationName">Name of the process</param>
        /// <param name="ignoreDoctype">true to ignore the doctype when loading a page</param>
        public static void FixBrowserVersion(string applicationName, bool ignoreDoctype = true)
        {
            FixBrowserVersion(applicationName, GetEmbVersion(ignoreDoctype));
        }

        /// <summary>
        ///     Fix the browser version for the specified application
        /// </summary>
        /// <param name="applicationName">Name of the process</param>
        /// <param name="ieVersion">
        ///     Version, see
        ///     <a href="https://msdn.microsoft.com/en-us/library/ee330730(v=vs.85).aspx#browser_emulation">Browser Emulation</a>
        /// </param>
        public static void FixBrowserVersion(string applicationName, int ieVersion)
        {
            ModifyRegistry("HKEY_CURRENT_USER", applicationName + ".exe", ieVersion);
#if DEBUG
            ModifyRegistry("HKEY_CURRENT_USER", applicationName + ".vshost.exe", ieVersion);
#endif
        }

        /// <summary>
        ///     Make the change to the registry
        /// </summary>
        /// <param name="root">HKEY_CURRENT_USER or something</param>
        /// <param name="applicationName">Name of the executable</param>
        /// <param name="ieFeatureVersion">Version to use</param>
        private static void ModifyRegistry(string root, string applicationName, int ieFeatureVersion)
        {
            var regKey = root + @"\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION";
            try
            {
                Registry.SetValue(regKey, applicationName, ieFeatureVersion);
            }
            catch (Exception ex)
            {
                // some config will hit access rights exceptions
                // this is why we try with both LOCAL_MACHINE and CURRENT_USER
                Log.Error(ex);
                Log.ErrorFormat("couldn't modify the registry key {0}", regKey);
            }
        }

        /// <summary>
        /// Find the DirectUI window for MSAA (Accessible)
        /// </summary>
        /// <param name="browserWindowDetails">The browser WindowDetails</param>
        /// <returns>WindowDetails for the DirectUI window</returns>
        public static WindowDetails GetDirectUI(WindowDetails browserWindowDetails)
        {
            if (browserWindowDetails == null)
            {
                return null;
            }

            WindowDetails tmpWd = browserWindowDetails;
            // Since IE 9 the TabBandClass is less deep!
            if (IEVersion < 9)
            {
                tmpWd = tmpWd.GetChild("CommandBarClass");
                tmpWd = tmpWd?.GetChild("ReBarWindow32");
            }

            tmpWd = tmpWd?.GetChild("TabBandClass");
            tmpWd = tmpWd?.GetChild("DirectUIHWND");
            return tmpWd;
        }

        /// <summary>
        /// Return an IEnumerable with the currently opened IE urls
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> GetIEUrls()
        {
            // Find the IE window
            foreach (WindowDetails ieWindow in WindowDetails.GetAllWindows("IEFrame"))
            {
                WindowDetails directUIWD = GetDirectUI(ieWindow);
                if (directUIWD != null)
                {
                    Accessible ieAccessible = new Accessible(directUIWD.Handle);
                    foreach (string url in ieAccessible.IETabUrls)
                    {
                        yield return url;
                    }
                }
            }
        }
    }
}