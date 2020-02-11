// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace GreenshotPlugin.Core.Enums
{
    /// <summary>
    ///     If a system parameter is being set, specifies whether the user profile is to be updated, and if so, whether the
    ///     WM_SETTINGCHANGE message is to be broadcast to all top-level windows to notify them of the change.
    ///     This parameter can be zero if you do not want to update the user profile or broadcast the WM_SETTINGCHANGE message,
    ///     or it can be one or more of the following values.
    /// </summary>
    public enum SystemParametersInfoBehaviors : uint
    {
        /// <summary>
        ///     Do nothing
        /// </summary>
        None = 0x00,

        /// <summary>Writes the new system-wide parameter setting to the user profile.</summary>
        UpdateIniFile = 0x01,

        /// <summary>Broadcasts the WM_SETTINGCHANGE message after updating the user profile.</summary>
        SendChange = 0x02,

        /// <summary>Same as SPIF_SENDCHANGE.</summary>
        SendWinIniChange = SendChange
    }
}