// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Dapplo.Windows.Kernel32;

namespace Greenshot.Helpers
{
    /// <summary>
    /// This is a helper class to handle the single-exe logic
    /// </summary>
    public static class SingleExeHelper
    {
        /// <summary>
        /// Initialize the properties of this static helper, only called when the class is used
        /// </summary>
        static SingleExeHelper()
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrEmpty(assemblyLocation) || assemblyLocation.EndsWith(".dll") != true)
            {
                return;
            }

            // Current process path
            string currentProcessPath = null;
            try
            {
                using (var currentProcess = Process.GetCurrentProcess())
                {
                    currentProcessPath = currentProcess.GetProcessPath();
                }
            }
            catch 
            {
                // Ignore
            }

            if (string.IsNullOrEmpty(currentProcessPath))
            {
                return;
            }
           
            if (Path.GetFileName(currentProcessPath).Equals("dotnet.exe", StringComparison.Ordinal))
            {
                return;
            }

            SingleExeLocation = currentProcessPath;
            IsRunningAsSingleExe = true;
            UnpackPath = Path.GetDirectoryName(assemblyLocation);
        }

        /// <summary>
        /// Is this process running as a single executable?
        /// </summary>
        public static bool IsRunningAsSingleExe { get; }

        /// <summary>
        /// Path to where the single exe is extracted
        /// </summary>
        public static string UnpackPath { get; }

        /// <summary>
        /// Location of the single exe
        /// </summary>
        public static string SingleExeLocation { get; }
    }
}
