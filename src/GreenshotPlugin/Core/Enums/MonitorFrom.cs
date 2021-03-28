// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace GreenshotPlugin.Core.Enums
{
    /// <summary>
    ///     Flags for the MonitorFromRect / MonitorFromWindow "flags" field
    ///     see <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd145063(v=vs.85).aspx">MonitorFromRect function</a>
    ///     or see <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dd145064(v=vs.85).aspx">MonitorFromWindow function</a>
    /// </summary>
    [Flags]
    public enum MonitorFrom : uint
    {
        /// <summary>
        ///     Returns a handle to the display monitor that is nearest to the rectangle.
        /// </summary>
        DefaultToNearest = 0,

        /// <summary>
        ///     Returns NULL. (why??)
        /// </summary>
        DefaultToNull = 1,

        /// <summary>
        ///     Returns a handle to the primary display monitor.
        /// </summary>
        DefaultToPrimary = 2
    }
}