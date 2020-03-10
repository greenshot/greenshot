/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: http://getgreenshot.org/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace GreenshotPlugin.Interfaces
{
    /// <summary>
    /// This is the interface for the different notification service implementations
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// This will show a warning message to the user
        /// </summary>
        /// <param name="message">string</param>
        /// <param name="timeout"></param>
        /// <param name="onClickAction">Action called if the user clicks the notification</param>
        /// <param name="onClosedAction">Action</param>
        void ShowWarningMessage(string message, int timeout, Action onClickAction = null, Action onClosedAction = null);

        /// <summary>
        /// This will show an error message to the user
        /// </summary>
        /// <param name="message">string</param>
        /// <param name="timeout"></param>
        /// <param name="onClickAction">Action called if the user clicks the notification</param>
        /// <param name="onClosedAction">Action</param>
        void ShowErrorMessage(string message, int timeout, Action onClickAction = null, Action onClosedAction = null);

        /// <summary>
        /// This will show an info message to the user
        /// </summary>
        /// <param name="message">string</param>
        /// <param name="timeout">int</param>
        /// <param name="onClickAction">Action called if the user clicks the notification</param>
        /// <param name="onClosedAction">Action</param>
        void ShowInfoMessage(string message, int timeout, Action onClickAction = null, Action onClosedAction = null);
    }
}