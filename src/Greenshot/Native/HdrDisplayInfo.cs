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
using System.Runtime.InteropServices;
using log4net;

namespace Greenshot.Native;

/// <summary>
/// Provides HDR display detection and SDR white level queries via Win32 DisplayConfig APIs.
/// </summary>
/// <remarks>
/// Uses <c>QueryDisplayConfig</c> and <c>DisplayConfigGetDeviceInfo</c> to determine
/// whether a monitor is in HDR (Advanced Color) mode and to query its SDR white level.
/// The HMONITOR is matched to a display config path by comparing GDI device names.
/// </remarks>
internal static class HdrDisplayInfo
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(HdrDisplayInfo));

    // P/Invoke constants
    private const uint QDC_ONLY_ACTIVE_PATHS = 0x00000002;
    private const int DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME = 1;
    private const int DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO = 9;
    private const int DISPLAYCONFIG_DEVICE_INFO_GET_SDR_WHITE_LEVEL = 11;
    private const uint MONITOR_DEFAULTTONEAREST = 2;
    private const int CCHDEVICENAME = 32;

    #region Structs

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_RATIONAL
    {
        public uint Numerator;
        public uint Denominator;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_PATH_SOURCE_INFO
    {
        public LUID AdapterId;
        public uint Id;
        public uint ModeInfoIdx;
        public uint StatusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_PATH_TARGET_INFO
    {
        public LUID AdapterId;
        public uint Id;
        public uint ModeInfoIdx;
        public uint OutputTechnology;
        public uint Rotation;
        public uint Scaling;
        public DISPLAYCONFIG_RATIONAL RefreshRate;
        public uint ScanLineOrdering;
        public int TargetAvailable;
        public uint StatusFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_PATH_INFO
    {
        public DISPLAYCONFIG_PATH_SOURCE_INFO SourceInfo;
        public DISPLAYCONFIG_PATH_TARGET_INFO TargetInfo;
        public uint Flags;
    }

    /// <summary>
    /// 64-byte opaque struct for mode info. We only need the array for QueryDisplayConfig;
    /// individual fields are not accessed.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_MODE_INFO
    {
        private long _0, _1, _2, _3, _4, _5, _6, _7;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_DEVICE_INFO_HEADER
    {
        public int Type;
        public int Size;
        public LUID AdapterId;
        public uint Id;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DISPLAYCONFIG_SOURCE_DEVICE_NAME
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER Header;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
        public string ViewGdiDeviceName;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER Header;
        /// <summary>
        /// Bit field: bit 0 = advancedColorSupported, bit 1 = advancedColorEnabled,
        /// bit 2 = wideColorEnforced, bit 3 = advancedColorForceDisabled.
        /// </summary>
        public uint Value;
        public uint ColorEncoding;
        public uint BitsPerColorChannel;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DISPLAYCONFIG_SDR_WHITE_LEVEL
    {
        public DISPLAYCONFIG_DEVICE_INFO_HEADER Header;
        /// <summary>
        /// SDR white level where 1000 equals 80 nits (the sRGB reference white).
        /// To convert to nits: <c>SDRWhiteLevelValue * 80.0f / 1000.0f</c>.
        /// </summary>
        public uint SDRWhiteLevelValue;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MONITORINFOEX
    {
        public int CbSize;
        public RECT RcMonitor;
        public RECT RcWork;
        public uint DwFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
        public string SzDevice;
    }

    #endregion

    #region P/Invoke declarations

    [DllImport("user32.dll")]
    private static extern int GetDisplayConfigBufferSizes(
        uint flags, out int numPathArrayElements, out int numModeInfoArrayElements);

    [DllImport("user32.dll")]
    private static extern int QueryDisplayConfig(
        uint flags,
        ref int numPathArrayElements,
        [In, Out] DISPLAYCONFIG_PATH_INFO[] pathInfoArray,
        ref int numModeInfoArrayElements,
        [In, Out] DISPLAYCONFIG_MODE_INFO[] modeInfoArray,
        IntPtr currentTopologyId);

    [DllImport("user32.dll")]
    private static extern int DisplayConfigGetDeviceInfo(
        ref DISPLAYCONFIG_SOURCE_DEVICE_NAME requestPacket);

    [DllImport("user32.dll")]
    private static extern int DisplayConfigGetDeviceInfo(
        ref DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO requestPacket);

    [DllImport("user32.dll")]
    private static extern int DisplayConfigGetDeviceInfo(
        ref DISPLAYCONFIG_SDR_WHITE_LEVEL requestPacket);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    #endregion

    #region Public API

    /// <summary>
    /// Gets the HMONITOR for the monitor that contains the largest area of the specified window.
    /// </summary>
    /// <param name="hwnd">The window handle.</param>
    /// <returns>The HMONITOR of the nearest monitor.</returns>
    public static IntPtr GetMonitorForWindow(IntPtr hwnd)
    {
        return MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
    }

    /// <summary>
    /// Determines whether HDR (Advanced Color) is active on the specified monitor.
    /// </summary>
    /// <param name="hMonitor">The monitor handle (HMONITOR).</param>
    /// <returns><c>true</c> if HDR is enabled on the monitor; otherwise <c>false</c>.</returns>
    public static bool IsHdrActiveForMonitor(IntPtr hMonitor)
    {
        try
        {
            if (!FindPathForMonitor(hMonitor, out var path))
            {
                return false;
            }

            var colorInfo = new DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO();
            colorInfo.Header.Type = DISPLAYCONFIG_DEVICE_INFO_GET_ADVANCED_COLOR_INFO;
            colorInfo.Header.Size = Marshal.SizeOf<DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO>();
            colorInfo.Header.AdapterId = path.TargetInfo.AdapterId;
            colorInfo.Header.Id = path.TargetInfo.Id;

            int result = DisplayConfigGetDeviceInfo(ref colorInfo);
            if (result != 0)
            {
                Log.Debug($"DisplayConfigGetDeviceInfo(ADVANCED_COLOR_INFO) failed with error {result} for monitor {hMonitor}.");
                return false;
            }

            // Bit 1 = advancedColorEnabled
            bool enabled = (colorInfo.Value & 0x2) != 0;
            Log.Debug($"Monitor {hMonitor}: AdvancedColor supported={(colorInfo.Value & 0x1) != 0}, enabled={enabled}.");
            return enabled;
        }
        catch (Exception ex)
        {
            Log.Debug($"Failed to query HDR state for monitor {hMonitor}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets the SDR white level in nits for the specified monitor.
    /// </summary>
    /// <param name="hMonitor">The monitor handle (HMONITOR).</param>
    /// <returns>The SDR white level in nits. Returns 80.0f (the sRGB reference white) if the value cannot be determined.</returns>
    public static float GetSdrWhiteLevelInNits(IntPtr hMonitor)
    {
        try
        {
            if (!FindPathForMonitor(hMonitor, out var path))
            {
                return 80.0f;
            }

            var whiteLevelInfo = new DISPLAYCONFIG_SDR_WHITE_LEVEL();
            whiteLevelInfo.Header.Type = DISPLAYCONFIG_DEVICE_INFO_GET_SDR_WHITE_LEVEL;
            whiteLevelInfo.Header.Size = Marshal.SizeOf<DISPLAYCONFIG_SDR_WHITE_LEVEL>();
            whiteLevelInfo.Header.AdapterId = path.TargetInfo.AdapterId;
            whiteLevelInfo.Header.Id = path.TargetInfo.Id;

            int result = DisplayConfigGetDeviceInfo(ref whiteLevelInfo);
            if (result != 0)
            {
                Log.Debug($"DisplayConfigGetDeviceInfo(SDR_WHITE_LEVEL) failed with error {result} for monitor {hMonitor}.");
                return 80.0f;
            }

            // SDRWhiteLevelValue: 1000 = 80 nits
            float nits = whiteLevelInfo.SDRWhiteLevelValue * 80.0f / 1000.0f;
            Log.Debug($"Monitor {hMonitor}: SDR white level = {nits} nits (raw value = {whiteLevelInfo.SDRWhiteLevelValue}).");
            return nits > 0 ? nits : 80.0f;
        }
        catch (Exception ex)
        {
            Log.Debug($"Failed to query SDR white level for monitor {hMonitor}: {ex.Message}");
            return 80.0f;
        }
    }

    #endregion

    #region Private helpers

    /// <summary>
    /// Finds the DisplayConfig path that corresponds to the given HMONITOR by matching GDI device names.
    /// </summary>
    private static bool FindPathForMonitor(IntPtr hMonitor, out DISPLAYCONFIG_PATH_INFO path)
    {
        path = default;

        // Get the GDI device name for the monitor
        var monitorInfo = new MONITORINFOEX();
        monitorInfo.CbSize = Marshal.SizeOf<MONITORINFOEX>();
        if (!GetMonitorInfo(hMonitor, ref monitorInfo))
        {
            Log.Debug($"GetMonitorInfo failed for monitor {hMonitor}.");
            return false;
        }

        string targetDeviceName = monitorInfo.SzDevice;
        if (string.IsNullOrEmpty(targetDeviceName))
        {
            return false;
        }

        // Enumerate active display config paths
        int result = GetDisplayConfigBufferSizes(QDC_ONLY_ACTIVE_PATHS, out int pathCount, out int modeCount);
        if (result != 0)
        {
            Log.Debug($"GetDisplayConfigBufferSizes failed with error {result}.");
            return false;
        }

        var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
        var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];
        result = QueryDisplayConfig(QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero);
        if (result != 0)
        {
            Log.Debug($"QueryDisplayConfig failed with error {result}.");
            return false;
        }

        // Find the path whose source GDI device name matches the monitor
        for (int i = 0; i < pathCount; i++)
        {
            var sourceName = new DISPLAYCONFIG_SOURCE_DEVICE_NAME();
            sourceName.Header.Type = DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME;
            sourceName.Header.Size = Marshal.SizeOf<DISPLAYCONFIG_SOURCE_DEVICE_NAME>();
            sourceName.Header.AdapterId = paths[i].SourceInfo.AdapterId;
            sourceName.Header.Id = paths[i].SourceInfo.Id;

            if (DisplayConfigGetDeviceInfo(ref sourceName) == 0)
            {
                if (string.Equals(sourceName.ViewGdiDeviceName, targetDeviceName, StringComparison.OrdinalIgnoreCase))
                {
                    path = paths[i];
                    return true;
                }
            }
        }

        Log.Debug($"No display config path found matching device name '{targetDeviceName}' for monitor {hMonitor}.");
        return false;
    }

    #endregion
}
