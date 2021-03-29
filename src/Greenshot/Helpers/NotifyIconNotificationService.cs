/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
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
using Greenshot.Base.Core;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;
using log4net;

namespace Greenshot.Helpers
{
    /// <summary>
    /// Notify the user with messages via the NotifyIcon
    /// </summary>
    public class NotifyIconNotificationService : INotificationService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(NotifyIconNotificationService));
        private static readonly CoreConfiguration CoreConfiguration = IniConfig.GetIniSection<CoreConfiguration>();
        private readonly NotifyIcon _notifyIcon;

        public NotifyIconNotificationService()
        {
            _notifyIcon = SimpleServiceProvider.Current.GetInstance<NotifyIcon>();
        }

        /// <summary>
        /// This will show a warning message to the user
        /// </summary>
        /// <param name="message">string</param>
        /// <param name="timeout">TimeSpan</param>
        /// <param name="onClickAction">Action called if the user clicks the notification</param>
        /// <param name="onClosedAction">Action</param>
        public void ShowWarningMessage(string message, TimeSpan? timeout = null, Action onClickAction = null, Action onClosedAction = null)
        {
            ShowMessage(message, timeout, ToolTipIcon.Warning, onClickAction, onClosedAction);
        }

        /// <summary>
        /// This will show an error message to the user
        /// </summary>
        /// <param name="message">string</param>
        /// <param name="timeout">TimeSpan</param>
        /// <param name="onClickAction">Action called if the user clicks the notification</param>
        /// <param name="onClosedAction">Action</param>
        public void ShowErrorMessage(string message, TimeSpan? timeout = null, Action onClickAction = null, Action onClosedAction = null)
        {
            ShowMessage(message, timeout, ToolTipIcon.Error, onClickAction, onClosedAction);
        }

        /// <summary>
        /// This will show an info message to the user
        /// </summary>
        /// <param name="message">string</param>
        /// <param name="timeout">TimeSpan</param>
        /// <param name="onClickAction">Action called if the user clicks the notification</param>
        /// <param name="onClosedAction">Action</param>
        public void ShowInfoMessage(string message, TimeSpan? timeout = null, Action onClickAction = null, Action onClosedAction = null)
        {
            ShowMessage(message, timeout, ToolTipIcon.Info, onClickAction, onClosedAction);
        }

        /// <summary>
        /// This will show a message to the user
        /// </summary>
        /// <param name="message">string</param>
        /// <param name="timeout">TimeSpan</param>
        /// <param name="level">ToolTipIcon</param>
        /// <param name="onClickAction">Action</param>
        /// <param name="onClosedAction">Action</param>
        private void ShowMessage(string message, TimeSpan? timeout = null, ToolTipIcon level = ToolTipIcon.Info, Action onClickAction = null, Action onClosedAction = null)
        {
            // Do not inform the user if this is disabled
            if (!CoreConfiguration.ShowTrayNotification)
            {
                return;
            }

            void BalloonClickedHandler(object s, EventArgs e)
            {
                try
                {
                    onClickAction?.Invoke();
                }
                catch (Exception ex)
                {
                    Log.Warn("Exception while handling the onclick action: ", ex);
                }

                _notifyIcon.BalloonTipClicked -= BalloonClickedHandler;
            }

            if (onClickAction != null)
            {
                _notifyIcon.BalloonTipClicked += BalloonClickedHandler;
            }

            void BalloonClosedHandler(object s, EventArgs e)
            {
                try
                {
                    onClosedAction?.Invoke();
                }
                catch (Exception ex)
                {
                    Log.Warn("Exception while handling the onClosed action: ", ex);
                }

                _notifyIcon.BalloonTipClosed -= BalloonClosedHandler;
                // Remove the other handler too
                _notifyIcon.BalloonTipClicked -= BalloonClickedHandler;
            }

            _notifyIcon.BalloonTipClosed += BalloonClosedHandler;
            _notifyIcon.ShowBalloonTip(timeout.HasValue ? (int) timeout.Value.TotalMilliseconds : 5000, @"Greenshot", message, level);
        }
    }
}