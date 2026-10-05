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
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;

namespace Greenshot.Shell
{
    /// <summary>
    /// Notify the user with balloons at the tray icon, used when Windows has no toast notifications
    /// </summary>
    internal sealed class TrayNotificationService : INotificationService
    {
        private readonly ITrayIcon _trayIcon;

        public TrayNotificationService(ITrayIcon trayIcon)
        {
            _trayIcon = trayIcon;
        }

        /// <inheritdoc />
        public void ShowWarningMessage(string message, TimeSpan? timeout = null, Action onClickAction = null, Action onClosedAction = null)
        {
            ShowMessage(message, TrayBalloonLevel.Warning, onClickAction, onClosedAction);
        }

        /// <inheritdoc />
        public void ShowErrorMessage(string message, TimeSpan? timeout = null, Action onClickAction = null, Action onClosedAction = null)
        {
            ShowMessage(message, TrayBalloonLevel.Error, onClickAction, onClosedAction);
        }

        /// <inheritdoc />
        public void ShowInfoMessage(string message, TimeSpan? timeout = null, Action onClickAction = null, Action onClosedAction = null)
        {
            ShowMessage(message, TrayBalloonLevel.Info, onClickAction, onClosedAction);
        }

        /// <summary>
        /// Show the message, the timeout is up to Windows (it ignores the one of a balloon since Vista)
        /// </summary>
        private void ShowMessage(string message, TrayBalloonLevel level, Action onClickAction, Action onClosedAction)
        {
            // Do not inform the user if this is disabled
            if (!IniConfigRegistry.GetSection<ICoreConfiguration>().ShowTrayNotification)
            {
                return;
            }

            _trayIcon.ShowBalloon("Greenshot", message, level, onClickAction, onClosedAction);
        }
    }
}
