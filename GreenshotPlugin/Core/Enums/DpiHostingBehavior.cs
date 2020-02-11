// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace GreenshotPlugin.Core.Enums
{
    /// <summary>
    ///     Identifies the DPI hosting behavior for a window.
    ///     This behavior allows windows created in the thread to host child windows with a different DPI_AWARENESS_CONTEXT
    /// </summary>
    public enum DpiHostingBehavior
    {
        /// <summary>
        ///     Invalid DPI hosting behavior. This usually occurs if the previous SetThreadDpiHostingBehavior call used an invalid parameter.
        /// </summary>
        Invalid = -1,

        /// <summary>
        ///     Default DPI hosting behavior. The associated window behaves as normal, and cannot create or re-parent child windows with a different DPI_AWARENESS_CONTEXT.
        /// </summary>
        Default = 0,

        /// <summary>
        ///     Mixed DPI hosting behavior. This enables the creation and re-parenting of child windows with different DPI_AWARENESS_CONTEXT. These child windows will be independently scaled by the OS.
        /// </summary>
        Mixed = 1

    }
}