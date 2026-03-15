/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.IO;
using System.Runtime.InteropServices;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Simple utility for the explorer
    /// </summary>
    public static class ExplorerHelper
    {
        // Free up the PIDL we need to create
        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern void ILFree(IntPtr pidlList);

        // Imports the function to convert a file/folder path to a PIDL
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern IntPtr ILCreateFromPathW(string pszPath);

        // Imports the core Shell API function
        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern int SHOpenFolderAndSelectItems(
            IntPtr pidlFolder,
            uint cidl,
            [In, MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl,
            uint dwFlags);

        /// <summary>
        /// Opens Windows Explorer to the containing folder and selects the specified file or folder.
        /// </summary>
        /// <param name="filePath">The path to the file or folder to select.</param>
        public static bool OpenInExplorer(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            filePath = Path.GetFullPath(filePath);

            if (!File.Exists(filePath) && !Directory.Exists(filePath))
            {
                return false;
            }

            IntPtr pidl = IntPtr.Zero;
            try
            {
                // Convert the file path to a PIDL
                pidl = ILCreateFromPathW(filePath);

                if (pidl == IntPtr.Zero)
                {
                    return false;
                }

                // Call the SHOpenFolderAndSelectItems API
                // Note: When cidl (count of items) is 0, the API expects pidlFolder to point 
                // directly to the item to select, rather than its parent folder.
                int hresult = SHOpenFolderAndSelectItems(pidl, 0, null, 0);

                // Throw an exception if the native call failed
                Marshal.ThrowExceptionForHR(hresult);
                return true;
            }
            catch (Exception ex)
            {
                // Make sure we show what we tried to open in the exception
                ex.Data.Add("path", filePath);
                throw;
            }
            finally
            {
                // Free the unmanaged memory to prevent leaks
                if (pidl != IntPtr.Zero)
                {
                    ILFree(pidl);
                }
            }
        }
    }
}