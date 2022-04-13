// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Greenshot.Base.Core
{
    /// <summary>
    ///     Extension methods to test the windows version
    /// </summary>
    public static class WindowsVersion
    {
        /// <summary>
        /// Get the current windows version
        /// </summary>
        public static Version WinVersion { get; } = Environment.OSVersion.Version;

        /// <summary>
        ///     Test if the current OS is Windows 10
        /// </summary>
        /// <returns>true if we are running on Windows 10</returns>
        public static bool IsWindows10 { get; } = WinVersion.Major == 10;

        /// <summary>
        ///     Test if the current OS is Windows 11 or later
        /// </summary>
        /// <returns>true if we are running on Windows 11 or later</returns>
        public static bool IsWindows11OrLater { get; } = WinVersion.Major == 10 && WinVersion.Build >= 22000;

        /// <summary>
        ///     Test if the current OS is Windows 10 or later
        /// </summary>
        /// <returns>true if we are running on Windows 10 or later</returns>
        public static bool IsWindows10OrLater { get; } = WinVersion.Major >= 10;

        /// <summary>
        ///     Test if the current OS is Windows 8.1 or later
        /// </summary>
        /// <returns>true if we are running on Windows 8.1 or later</returns>
        public static bool IsWindows81OrLater { get; } = WinVersion.Major == 6 && WinVersion.Minor >= 3 || WinVersion.Major > 6;

        /// <summary>
        ///     Test if the current OS is Windows 8 or later
        /// </summary>
        /// <returns>true if we are running on Windows 8 or later</returns>
        public static bool IsWindows8OrLater { get; } = WinVersion.Major == 6 && WinVersion.Minor >= 2 || WinVersion.Major > 6;

        /// <summary>
        ///     Test if the current OS is Windows Vista or later
        /// </summary>
        /// <returns>true if we are running on Windows Vista or later</returns>
        public static bool IsWindowsVistaOrLater { get; } = WinVersion.Major >= 6;

        /// <summary>
        /// Returns the windows build number
        /// </summary>
        public static int BuildVersion => WinVersion.Build;

        /// <summary>
        ///     Test if the current Windows version is 10 and the build number or later
        ///     See the build numbers <a href="https://en.wikipedia.org/wiki/Windows_10_version_history">here</a>
        /// </summary>
        /// <param name="minimalBuildNumber">int</param>
        /// <returns>bool</returns>
        public static bool IsWindows10BuildOrLater(int minimalBuildNumber)
        {
            return IsWindows10 && WinVersion.Build >= minimalBuildNumber;
        }
    }
}