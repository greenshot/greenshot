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

using Greenshot.Base.Interfaces;

namespace Greenshot.Base.Core.Enums
{
    /// <summary>
    /// Which notifications Greenshot shows, by their <see cref="NotificationKind"/>
    /// </summary>
    public enum NotificationLevel
    {
        /// <summary>
        /// Every notification, also that a capture was exported
        /// </summary>
        All,

        /// <summary>
        /// Warnings and errors
        /// </summary>
        Warnings,

        /// <summary>
        /// Only errors
        /// </summary>
        Errors,

        /// <summary>
        /// None
        /// </summary>
        None
    }

    public static class NotificationLevelExtensions
    {
        /// <summary>
        /// Whether a notification of the kind is shown at the level
        /// </summary>
        public static bool Shows(this NotificationLevel level, NotificationKind kind) => level switch
        {
            NotificationLevel.All => true,
            NotificationLevel.Warnings => kind != NotificationKind.Info,
            NotificationLevel.Errors => kind == NotificationKind.Error,
            _ => false
        };
    }
}
