// Copyright (C) Dapplo and contributors. All rights reserved.
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
        ///     Test if the current OS is Windows 11 or later
        /// </summary>
        /// <returns>true if we are running on Windows 11 or later</returns>
        public static bool IsWindows11OrLater { get; } = WinVersion.Major == 10 && WinVersion.Build >= 22000;
    }
}