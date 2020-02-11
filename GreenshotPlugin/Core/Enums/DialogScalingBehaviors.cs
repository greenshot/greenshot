// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;

namespace GreenshotPlugin.Core.Enums
{
    /// <summary>
    ///    Describes per-monitor DPI scaling behavior overrides for child windows within dialogs. The values in this enumeration are bitfields and can be combined.
    ///
    /// This enum is used with SetDialogControlDpiChangeBehavior in order to override the default per-monitor DPI scaling behavior for a child window within a dialog.
    /// 
    /// These settings only apply to individual controls within dialogs. The dialog-wide per-monitor DPI scaling behavior of a dialog is controlled by DIALOG_DPI_CHANGE_BEHAVIORS.
    /// </summary>
    [Flags]
    public enum DialogScalingBehaviors
    {
        /// <summary>
        ///    The default behavior of the dialog manager. The dialog managed will update the font, size, and position of the child window on DPI changes.
        /// </summary>
        Default = 0,

        /// <summary>
        ///     Prevents the dialog manager from sending an updated font to the child window via WM_SETFONT in response to a DPI change.
        /// </summary>
        DisableFontUpdate = 1,

        /// <summary>
        ///     Prevents the dialog manager from resizing and repositioning the child window in response to a DPI change.
        /// </summary>
        DisableRelayout = 2
    }
}