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
using Microsoft.Win32;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// A helper class for accessing the registry
    /// </summary>
    public static class RegistryKeyExtensions
    {
        /// <summary>
        /// Retrieve a registry value
        /// </summary>
        /// <param name="registryHive">RegistryHive like RegistryHive.LocalMachine</param>
        /// <param name="keyName">string with the name of the key</param>
        /// <param name="value">string with the name of the value below the key, null will retrieve the default</param>
        /// <param name="defaultValue">string with the default value to return</param>
        /// <returns>string with the value</returns>
        public static string ReadKey64Or32(this RegistryHive registryHive, string keyName, string value = null, string defaultValue = null)
        {
            string result = null;
            value ??= string.Empty;

            using var registryKey32 = RegistryKey.OpenBaseKey(registryHive, RegistryView.Registry32);
            // Logic for 32-bit Windows is just reading the key
            if (!Environment.Is64BitOperatingSystem)
            {
                using var key = registryKey32.OpenSubKey($@"SOFTWARE\{keyName}", false);

                if (key != null)
                {
                    result = (string) key.GetValue(value);
                }

                if (string.IsNullOrEmpty(result))
                {
                    result = defaultValue;
                }

                return result;
            }

            using var registryKey64 = RegistryKey.OpenBaseKey(registryHive, RegistryView.Registry64);
            // Logic for 64 bit Windows, is trying the key which is 64 bit
            using (var key = registryKey64.OpenSubKey($@"SOFTWARE\{keyName}", false))
            {
                if (key != null)
                {
                    result = (string) key.GetValue(value);
                }

                if (!string.IsNullOrEmpty(result)) return result;
            }

            // if there is no value use the wow6432node key which is 32 bit
            using (var key = registryKey32.OpenSubKey($@"SOFTWARE\{keyName}", false))
            {
                if (key != null)
                {
                    result = (string) key.GetValue(value);
                }
            }

            if (string.IsNullOrEmpty(result))
            {
                result = defaultValue;
            }

            return result;
        }
    }
}