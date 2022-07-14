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
using System.Globalization;
using System.Net;
using System.Reflection;
using Greenshot.Forms;

namespace Greenshot
{
    /// <summary>
    /// Description of Main.
    /// </summary>
    public static class GreenshotMain
    {
        static GreenshotMain() => AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Assembly ayResult = null;
            string sShortAssemblyName = args.Name.Split(',')[0];
            Assembly[] ayAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly ayAssembly in ayAssemblies)
            {
                if (sShortAssemblyName != ayAssembly.FullName.Split(',')[0])
                {
                    continue;
                }

                ayResult = ayAssembly;
                break;
            }

            return ayResult;
        }

        [STAThread]
        public static void Main(string[] args)
        {
            // Enable TLS 1.2 support
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            MainForm.Start(args);
        }
    }
}