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

using Greenshot.Base.Interfaces;
#if !GREENSHOT_LIGHT
using Greenshot.Plugin.Win10;
#endif

namespace Greenshot.Shell
{
    /// <summary>
    /// Picks how Greenshot informs the user and initializes it, once at startup. Every other part only uses
    /// <see cref="INotificationService"/>; the implementation of another platform is added here.
    /// </summary>
    internal static class NotificationServiceFactory
    {
        public static INotificationService Create()
        {
#if !GREENSHOT_LIGHT
            // Windows toasts, null where they aren't available or for the portable version, which writes no registration
            var toasts = ToastNotificationService.Create();
            if (toasts != null)
            {
                return toasts;
            }
#endif
            // Greenshot Light has no toasts
            return new TrayNotificationService();
        }
    }
}
