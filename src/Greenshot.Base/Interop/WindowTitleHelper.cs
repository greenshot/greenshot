/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Greenshot.Base.Interop
{
    /// <summary>
    /// Helper for retrieving window titles from sandboxed applications
    /// where GetWindowText fails due to cross-process security restrictions.
    /// </summary>
    public static class WindowTitleHelper
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int processId);

        /// <summary>
        /// Gets the window title via Process.MainWindowTitle as a fallback for sandboxed applications.
        /// </summary>
        /// <param name="hWnd">The window handle</param>
        /// <returns>The window title, or null if retrieval fails</returns>
        public static string GetWindowTitle(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                GetWindowThreadProcessId(hWnd, out int processId);
                if (processId == 0)
                {
                    return null;
                }

                using var process = Process.GetProcessById(processId);
                if (process.MainWindowHandle == hWnd && !string.IsNullOrEmpty(process.MainWindowTitle))
                {
                    return process.MainWindowTitle;
                }

                return null;
            }
            catch (ArgumentException)
            {
                // Process no longer exists
                return null;
            }
            catch (InvalidOperationException)
            {
                // Process has exited
                return null;
            }
            catch (Win32Exception)
            {
                // Access denied or other Win32 error
                return null;
            }
        }
    }
}
