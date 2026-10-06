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

namespace Greenshot.Base.Core.Enums
{
    /// <summary>
    /// How much the notification after an export shows. Whether there are notifications at all is ShowTrayNotification.
    /// </summary>
    public enum NotificationDetail
    {
        /// <summary>
        /// Only when an export failed
        /// </summary>
        ErrorsOnly,

        /// <summary>
        /// Where the capture went, with the icon of the destination
        /// </summary>
        Short,

        /// <summary>
        /// Also a preview of the capture and the buttons to open it, send it somewhere else or edit it
        /// </summary>
        Full
    }
}
