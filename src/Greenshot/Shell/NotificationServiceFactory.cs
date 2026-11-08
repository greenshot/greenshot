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
#if !GREENSHOT_LIGHT
using Greenshot.Plugin.Win10;
#endif

namespace Greenshot.Shell
{
    /// <summary>
    /// Picks how Greenshot informs the user and initializes it, once at startup: the first available of the providers, by their
    /// priority. Every other part only uses <see cref="INotificationService"/>; another platform (macOS, Linux) is another provider here.
    /// </summary>
    internal static class NotificationServiceFactory
    {
        public static INotificationService Create() => NotificationServiceProviders.CreateService(new INotificationServiceProvider[]
        {
#if !GREENSHOT_LIGHT
            // Windows toasts, not available on old Windows versions or for the portable version, which writes no registration.
            // Greenshot Light has no toasts.
            new ToastNotificationServiceProvider(),
#endif
            // Always available
            new TrayNotificationServiceProvider()
        });
    }
}
