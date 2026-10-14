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
using System.Collections.Generic;
using Dapplo.Ini;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;

namespace Greenshot.Shell
{
    /// <summary>
    /// The balloons of the tray icon: always available, used where there are no other notifications
    /// </summary>
    internal sealed class TrayNotificationServiceProvider : INotificationServiceProvider
    {
        /// <inheritdoc />
        public int Priority => 0;

        /// <inheritdoc />
        public INotificationService TryCreate() => new TrayNotificationService();
    }

    /// <summary>
    /// Notify the user with balloons at the tray icon, used where there are no toasts. A balloon shows no capture and can't be
    /// clicked after Greenshot exited.
    /// </summary>
    internal sealed class TrayNotificationService : INotificationService
    {
        private static ICoreConfiguration CoreConfiguration => IniConfigRegistry.GetSection<ICoreConfiguration>();

        /// <inheritdoc />
        public bool IsEnabled => CoreConfiguration.ShowTrayNotification && CoreConfiguration.NotificationLevel != NotificationLevel.None;

        /// <inheritdoc />
        public bool ShowsCaptures => false;

        /// <inheritdoc />
        public IReadOnlyCollection<string> GetShownCaptureIds() => Array.Empty<string>();

        /// <inheritdoc />
        /// <remarks>Never raised, see <see cref="ShowsCaptures"/></remarks>
        public event EventHandler<NotificationActivatedEventArgs> CaptureActivated
        {
            add { }
            remove { }
        }

        /// <inheritdoc />
        public void ShowNotification(Notification notification)
        {
            ShowMessage(notification.Message, notification.Kind, notification.OnClick, null);
        }

        /// <inheritdoc />
        public void ShowWarningMessage(string message, TimeSpan? timeout = null, Action onClickAction = null, Action onClosedAction = null)
        {
            ShowMessage(message, NotificationKind.Warning, onClickAction, onClosedAction);
        }

        /// <inheritdoc />
        public void ShowErrorMessage(string message, TimeSpan? timeout = null, Action onClickAction = null, Action onClosedAction = null)
        {
            ShowMessage(message, NotificationKind.Error, onClickAction, onClosedAction);
        }

        /// <inheritdoc />
        public void ShowInfoMessage(string message, TimeSpan? timeout = null, Action onClickAction = null, Action onClosedAction = null)
        {
            ShowMessage(message, NotificationKind.Info, onClickAction, onClosedAction);
        }

        /// <summary>
        /// Show the message, the timeout is up to Windows (it ignores the one of a balloon since Vista)
        /// </summary>
        private void ShowMessage(string message, NotificationKind kind, Action onClickAction, Action onClosedAction)
        {
            // Do not inform the user if this is disabled, or the level doesn't show this kind
            if (!IsEnabled || !CoreConfiguration.NotificationLevel.Shows(kind))
            {
                return;
            }

            var level = kind switch
            {
                NotificationKind.Error => TrayBalloonLevel.Error,
                NotificationKind.Warning => TrayBalloonLevel.Warning,
                _ => TrayBalloonLevel.Info
            };

            // The tray icon is created after the services, a notification before it isn't shown
            SimpleServiceProvider.Current.GetInstance<ITrayIcon>(isOptional: true)?.ShowBalloon("Greenshot", message, level, onClickAction, onClosedAction);
        }
    }
}
