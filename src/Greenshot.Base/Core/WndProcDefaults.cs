/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Windows.Forms;
using Dapplo.Windows.Common.Enums;
using Dapplo.Windows.Messages.Enumerations;
using log4net;

namespace Greenshot.Base.Core;

/// <summary>
/// Provides default handling logic for specific Windows messages in window procedures.
/// </summary>
public static class WndProcDefaults
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(WndProcDefaults));

    /// <summary>
    /// Determines whether the specified Windows message indicates a session end or query for session end.
    /// </summary>
    /// <param name="message">A reference to the Windows message to evaluate. The message is checked to determine if it represents a session
    /// end or query end session event.</param>
    /// <returns>true if the message is handled, false if not</returns>
    public static bool TryHandleMessage(ref Message message)
    {
        var msg = (WindowsMessages)message.Msg;
        switch (msg)
        {
            case WindowsMessages.WM_QUERYENDSESSION:
            case WindowsMessages.WM_ENDSESSION:
                message.Result = (IntPtr)HResult.S_FALSE; // Don't repond to the session end, we will handle it ourselves via the Restart Manager
                return true;
            case WindowsMessages.WM_INPUTLANGCHANGEREQUEST:
            case WindowsMessages.WM_INPUTLANGCHANGE:
                return true;
        }
        return false;
    }
}
