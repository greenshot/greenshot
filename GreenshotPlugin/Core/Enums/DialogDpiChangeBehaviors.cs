// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace GreenshotPlugin.Core.Enums
{
    /// <summary>
    ///    In Per Monitor v2 contexts, dialogs will automatically respond to DPI changes by resizing themselves and re-computing the positions of their child windows (here referred to as re-layouting). This enum works in conjunction with SetDialogDpiChangeBehavior in order to override the default DPI scaling behavior for dialogs.
    ///    This does not affect DPI scaling behavior for the child windows of dialogs(beyond re-layouting), which is controlled by DIALOG_CONTROL_DPI_CHANGE_BEHAVIORS.
    /// </summary>
    [Flags]
    public enum DialogDpiChangeBehaviors
    {
        /// <summary>
        ///    The default behavior of the dialog manager. In response to a DPI change, the dialog manager will re-layout each control, update the font on each control, resize the dialog, and update the dialog's own font.
        /// </summary>
        Default = 0,

        /// <summary>
        ///     Prevents the dialog manager from responding to WM_GETDPISCALEDSIZE and WM_DPICHANGED, disabling all default DPI scaling behavior.
        /// </summary>
        DisableAll = 1,

        /// <summary>
        ///     Prevents the dialog manager from resizing the dialog in response to a DPI change.
        /// </summary>
        DisableResize = 2,

        /// <summary>
        ///     Prevents the dialog manager from re-layouting all of the dialogue's immediate children HWNDs in response to a DPI change.
        /// </summary>
        DisableControlRelayout = 3
    }
}