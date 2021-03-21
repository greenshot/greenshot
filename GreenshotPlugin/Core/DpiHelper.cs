/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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

using GreenshotPlugin.Core.Enums;
using GreenshotPlugin.UnmanagedHelpers;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using GreenshotPlugin.UnmanagedHelpers.Enums;
using GreenshotPlugin.UnmanagedHelpers.Structs;

namespace GreenshotPlugin.Core
{
    /// <summary>
    ///     This handles DPI changes see
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dn469266.aspx">Writing DPI-Aware Desktop and Win32 Applications</a>
    /// </summary>
    public static class DpiHelper
    {
        /// <summary>
        ///     This is the default DPI for the screen
        /// </summary>
        public const uint DefaultScreenDpi = 96;

        /// <summary>
        ///     Retrieve the current DPI for the UI element which is related to this DpiHandler
        /// </summary>
        public static uint Dpi { get; private set; } = WindowsVersion.IsWindows10OrLater ? GetDpiForSystem() : DefaultScreenDpi;

        /// <summary>
        /// Calculate a DPI scale factor
        /// </summary>
        /// <param name="dpi">uint</param>
        /// <returns>double</returns>
        public static float DpiScaleFactor(uint dpi)
        {
            if (dpi == 0)
            {
                dpi = Dpi;
            }
            return (float)dpi / DefaultScreenDpi;
        }

        /// <summary>
        ///     Scale the supplied number according to the supplied dpi
        /// </summary>
        /// <param name="someNumber">double with e.g. the width 16 for 16x16 images</param>
        /// <param name="dpi">current dpi, normal is 96.</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>double with the scaled number</returns>
        public static float ScaleWithDpi(float someNumber, uint dpi, Func<float, float> scaleModifier = null)
        {
            var dpiScaleFactor = DpiScaleFactor(dpi);
            if (scaleModifier != null)
            {
                dpiScaleFactor = scaleModifier(dpiScaleFactor);
            }
            return dpiScaleFactor * someNumber;
        }

        /// <summary>
        ///     Scale the supplied Size according to the supplied dpi
        /// </summary>
        /// <param name="size">Size to resize</param>
        /// <param name="dpi">current dpi, normal is 96.</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>NativeSize scaled</returns>
        public static Size ScaleWithDpi(Size size, uint dpi, Func<float, float> scaleModifier = null)
        {
            var dpiScaleFactor = DpiScaleFactor(dpi);
            if (scaleModifier != null)
            {
                dpiScaleFactor = scaleModifier(dpiScaleFactor);
            }
            return new Size((int)(dpiScaleFactor * size.Width), (int)(dpiScaleFactor * size.Height));
        }

        /// <summary>
        ///     Scale the supplied NativeSize to the current dpi
        /// </summary>
        /// <param name="size">NativeSize to scale</param>
        /// <param name="scaleModifier">A function which can modify the scale factor</param>
        /// <returns>NativeSize scaled</returns>
        public static Size ScaleWithCurrentDpi(Size size, Func<float, float> scaleModifier = null)
        {
            return ScaleWithDpi(size, Dpi, scaleModifier);
        }

        /// <summary>
        /// Return the DPI for the screen which the location is located on
        /// </summary>
        /// <param name="location">POINT</param>
        /// <returns>uint</returns>
        public static uint GetDpi(POINT location)
        {
            RECT rect = new RECT(location.X, location.Y, 1,1);
            IntPtr hMonitor = User32.MonitorFromRect(ref rect, User32.MONITOR_DEFAULTTONEAREST);
            var result = GetDpiForMonitor(hMonitor, MonitorDpiType.EffectiveDpi, out var dpiX, out var dpiY);
            if (result.Succeeded())
            {
                return dpiX;
            }
            return DefaultScreenDpi;
        }


        /// <summary>
        ///     Retrieve the DPI value for the supplied window handle
        /// </summary>
        /// <param name="hWnd">IntPtr</param>
        /// <returns>dpi value</returns>
        public static uint GetDpi(IntPtr hWnd)
        {
            if (!User32.IsWindow(hWnd))
            {
                return DefaultScreenDpi;
            }

            // Use the easiest method, but this only works for Windows 10
            if (WindowsVersion.IsWindows10OrLater)
            {
                return GetDpiForWindow(hWnd);
            }

            // Use the second easiest method, but this only works for Windows 8.1 or later
            if (WindowsVersion.IsWindows81OrLater)
            {
                var hMonitor = User32.MonitorFromWindow(hWnd, MonitorFrom.DefaultToNearest);
                // ReSharper disable once UnusedVariable
                var result = GetDpiForMonitor(hMonitor, MonitorDpiType.EffectiveDpi, out var dpiX, out var dpiY);
                if (result.Succeeded())
                {
                    return dpiX;
                }
            }

            // Fallback to the global DPI settings
            using var hdc = SafeWindowDcHandle.FromWindow(hWnd);
            if (hdc == null)
            {
                return DefaultScreenDpi;
            }
            return (uint)GDI32.GetDeviceCaps(hdc, DeviceCaps.LOGPIXELSX);
        }

        /// <summary>
        /// See more at <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/mt748624(v=vs.85).aspx">GetDpiForWindow function</a>
        /// Returns the dots per inch (dpi) value for the associated window.
        /// </summary>
        /// <param name="hWnd">IntPtr</param>
        /// <returns>uint with dpi</returns>
        [DllImport("User32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hWnd);

        /// <summary>
        ///     See
        ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dn280510(v=vs.85).aspx">GetDpiForMonitor function</a>
        ///     Queries the dots per inch (dpi) of a display.
        /// </summary>
        /// <param name="hMonitor">IntPtr</param>
        /// <param name="dpiType">MonitorDpiType</param>
        /// <param name="dpiX">out int for the horizontal dpi</param>
        /// <param name="dpiY">out int for the vertical dpi</param>
        /// <returns>true if all okay</returns>
        [DllImport("shcore.dll", SetLastError = true)]
        private static extern HResult GetDpiForMonitor(IntPtr hMonitor, MonitorDpiType dpiType, out uint dpiX, out uint dpiY);

        /// <summary>
        /// See <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/mt748623(v=vs.85).aspx">GetDpiForSystem function</a>
        /// Returns the system DPI.
        /// </summary>
        /// <returns>uint with the system DPI</returns>
        [DllImport("User32.dll")]
        private static extern uint GetDpiForSystem();
    }
}
