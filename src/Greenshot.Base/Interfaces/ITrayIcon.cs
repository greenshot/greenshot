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

namespace Greenshot.Base.Interfaces
{
    /// <summary>
    /// The Greenshot icon in the notification area (system tray). All members must be called on the UI thread.
    /// </summary>
    public interface ITrayIcon
    {
        /// <summary>
        /// Show a balloon (a toast on Windows 10 and later) at the tray icon
        /// </summary>
        /// <param name="title">string</param>
        /// <param name="message">string</param>
        /// <param name="level">TrayBalloonLevel</param>
        /// <param name="onClick">Called when the user clicks the balloon, can be null</param>
        /// <param name="onClosed">Called when the balloon closed, can be null</param>
        void ShowBalloon(string title, string message, TrayBalloonLevel level, Action onClick = null, Action onClosed = null);

        /// <summary>
        /// Remove a balloon which is still shown
        /// </summary>
        void HideBalloon();

        /// <summary>
        /// Open the tray menu at the cursor
        /// </summary>
        void ShowMenu();
    }
}
