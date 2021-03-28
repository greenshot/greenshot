// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Greenshot.Base.Core.Enums
{
    /// <summary>
    ///     See
    ///     <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/dn280511(v=vs.85).aspx">
    ///         MONITOR_DPI_TYPE
    ///         enumeration
    ///     </a>
    /// </summary>
    [Flags]
    public enum MonitorDpiType
    {
        /// <summary>
        ///     The effective DPI.
        ///     This value should be used when determining the correct scale factor for scaling UI elements.
        ///     This incorporates the scale factor set by the user for this specific display.
        /// </summary>
        EffectiveDpi = 0,

        /// <summary>
        ///     The angular DPI.
        ///     This DPI ensures rendering at a compliant angular resolution on the screen.
        ///     This does not include the scale factor set by the user for this specific display
        /// </summary>
        AngularDpi = 1,

        /// <summary>
        ///     The raw DPI.
        ///     This value is the linear DPI of the screen as measured on the screen itself.
        ///     Use this value when you want to read the pixel density and not the recommended scaling setting.
        ///     This does not include the scale factor set by the user for this specific display and is not guaranteed to be a
        ///     supported DPI value.
        /// </summary>
        RawDpi = 2
    }
}