/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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

namespace Greenshot.Base.Interfaces
{
    /// <summary>
    /// The click on a notification with a capture, see <see cref="INotificationService.CaptureActivated"/>
    /// </summary>
    public sealed class NotificationActivatedEventArgs : EventArgs
    {
        public NotificationActivatedEventArgs(string captureId)
        {
            CaptureId = captureId;
        }

        /// <summary>
        /// The <see cref="Notification.CaptureId"/> of the clicked notification
        /// </summary>
        public string CaptureId { get; }
    }

    /// <summary>
    /// The click on a button of a notification, see <see cref="INotificationService.ButtonActivated"/>
    /// </summary>
    public sealed class NotificationButtonEventArgs : EventArgs
    {
        public NotificationButtonEventArgs(string action, string value, string captureId)
        {
            Action = action;
            Value = value;
            CaptureId = captureId;
        }

        /// <summary>
        /// The <see cref="NotificationButton.Action"/>
        /// </summary>
        public string Action { get; }

        /// <summary>
        /// The <see cref="NotificationButton.Value"/>, or null
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// The <see cref="Notification.CaptureId"/> of the notification, or null
        /// </summary>
        public string CaptureId { get; }
    }

    /// <summary>
    /// How Greenshot informs the user, one implementation per platform: Windows toasts, or balloons at the tray icon where
    /// there are no toasts (Greenshot Light, the portable version). It is created and initialized by its
    /// <see cref="INotificationServiceProvider"/>, Greenshot takes the first available one at startup (NotificationServiceFactory).
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// False while the user doesn't want notifications (Greenshot's setting, or Windows turned them off for Greenshot, for the
        /// user or by a policy): then every Show call does nothing, no other implementation takes over
        /// </summary>
        bool IsEnabled { get; }

        /// <summary>
        /// True when a notification can show the capture of <see cref="Notification.CaptureId"/> and a click offers it again,
        /// also after Greenshot exited. Only then Greenshot keeps the capture for the notification.
        /// </summary>
        bool ShowsCaptures { get; }

        /// <summary>
        /// The capture ids of the notifications the user can still click, their kept captures are still needed
        /// </summary>
        IReadOnlyCollection<string> GetShownCaptureIds();

        /// <summary>
        /// Raised on the UI thread for a click on a notification with a <see cref="Notification.CaptureId"/>, instead of its click
        /// action; also when Greenshot was started for the click. Only raised when <see cref="ShowsCaptures"/>.
        /// </summary>
        event EventHandler<NotificationActivatedEventArgs> CaptureActivated;

        /// <summary>
        /// Raised on the UI thread for a click on one of the <see cref="Notification.Buttons"/>, also when Greenshot was started for
        /// the click. Only raised when <see cref="ShowsCaptures"/>, other notifications show no buttons.
        /// </summary>
        event EventHandler<NotificationButtonEventArgs> ButtonActivated;

        /// <summary>
        /// Shows the notification to the user
        /// </summary>
        void ShowNotification(Notification notification);

        /// <summary>
        /// This will show a warning message to the user
        /// </summary>
        /// <param name="message">string</param>
        /// <param name="timeout">TimeSpan</param>
        /// <param name="onClickAction">Action called if the user clicks the notification</param>
        /// <param name="onClosedAction">Action</param>
        void ShowWarningMessage(string message, TimeSpan? timeout = null, Action onClickAction = null, Action onClosedAction = null);

        /// <summary>
        /// This will show an error message to the user
        /// </summary>
        /// <param name="message">string</param>
        /// <param name="timeout">TimeSpan</param>
        /// <param name="onClickAction">Action called if the user clicks the notification</param>
        /// <param name="onClosedAction">Action</param>
        void ShowErrorMessage(string message, TimeSpan? timeout = null, Action onClickAction = null, Action onClosedAction = null);

        /// <summary>
        /// This will show an info message to the user
        /// </summary>
        /// <param name="message">string</param>
        /// <param name="timeout">TimeSpan</param>
        /// <param name="onClickAction">Action called if the user clicks the notification</param>
        /// <param name="onClosedAction">Action</param>
        void ShowInfoMessage(string message, TimeSpan? timeout = null, Action onClickAction = null, Action onClosedAction = null);
    }
}