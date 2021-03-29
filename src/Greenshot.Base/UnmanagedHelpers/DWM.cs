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
using System.Drawing;
using System.Runtime.InteropServices;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.UnmanagedHelpers.Enums;
using Greenshot.Base.UnmanagedHelpers.Structs;
using Microsoft.Win32;

namespace Greenshot.Base.UnmanagedHelpers
{
    /// <summary>
    /// Desktop Window Manager helper code
    /// </summary>
    public static class DWM
    {
        // DWM
        [DllImport("dwmapi", SetLastError = true)]
        public static extern int DwmRegisterThumbnail(IntPtr dest, IntPtr src, out IntPtr thumb);

        [DllImport("dwmapi", SetLastError = true)]
        public static extern int DwmUnregisterThumbnail(IntPtr thumb);

        [DllImport("dwmapi", SetLastError = true)]
        public static extern HResult DwmQueryThumbnailSourceSize(IntPtr thumb, out SIZE size);

        [DllImport("dwmapi", SetLastError = true)]
        public static extern HResult DwmUpdateThumbnailProperties(IntPtr hThumb, ref DWM_THUMBNAIL_PROPERTIES props);

        // Deprecated as of Windows 8 Release Preview
        [DllImport("dwmapi", SetLastError = true)]
        public static extern int DwmIsCompositionEnabled(out bool enabled);

        [DllImport("dwmapi", SetLastError = true)]
        public static extern int DwmGetWindowAttribute(IntPtr hWnd, DWMWINDOWATTRIBUTE dwAttribute, out RECT lpRect, int size);

        [DllImport("dwmapi", SetLastError = true)]
        public static extern int DwmGetWindowAttribute(IntPtr hWnd, DWMWINDOWATTRIBUTE dwAttribute, out bool pvAttribute, int cbAttribute);

        // Key to ColorizationColor for DWM
        private const string COLORIZATION_COLOR_KEY = @"SOFTWARE\Microsoft\Windows\DWM";

        /// <summary>
        /// Checks if the window is cloaked, this should solve some issues with the window selection code
        /// </summary>
        /// <param name="hWnd">IntPtr as hWmd</param>
        /// <returns>bool</returns>
        public static bool IsWindowCloaked(IntPtr hWnd)
        {
            if (!WindowsVersion.IsWindows8OrLater)
            {
                return false;
            }

            DwmGetWindowAttribute(hWnd, DWMWINDOWATTRIBUTE.DWMWA_CLOAKED, out bool isCloaked, Marshal.SizeOf(typeof(bool)));
            return isCloaked;
        }

        /// <summary>
        /// Helper method for an easy DWM check
        /// </summary>
        /// <returns>bool true if DWM is available AND active</returns>
        public static bool IsDwmEnabled
        {
            get
            {
                // According to: https://technet.microsoft.com/en-us/subscriptions/aa969538%28v=vs.85%29.aspx
                // And: https://docs.microsoft.com/en-gb/windows/win32/api/dwmapi/nf-dwmapi-dwmenablecomposition
                // DMW is always enabled on Windows 8! So return true and save a check! ;-)
                if (WindowsVersion.IsWindows8OrLater)
                {
                    return true;
                }

                if (WindowsVersion.IsWindowsVistaOrLater)
                {
                    DwmIsCompositionEnabled(out var dwmEnabled);
                    return dwmEnabled;
                }

                return false;
            }
        }

        public static Color ColorizationColor
        {
            get
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(COLORIZATION_COLOR_KEY, false))
                {
                    object dwordValue = key?.GetValue("ColorizationColor");
                    if (dwordValue != null)
                    {
                        return Color.FromArgb((int) dwordValue);
                    }
                }

                return Color.White;
            }
        }
    }
}