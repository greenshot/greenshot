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
using System.Linq;
using log4net;

namespace Greenshot.Base.Interfaces
{
    /// <summary>
    /// Creates the <see cref="INotificationService"/> of a platform: checks whether it is available here and initializes it
    /// (e.g. registers Greenshot for the toasts of Windows). One per platform: toasts, tray balloons, later macOS and Linux.
    /// </summary>
    public interface INotificationServiceProvider
    {
        /// <summary>
        /// The order in which the providers are tried, the highest first
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Initializes the notifications of the platform
        /// </summary>
        /// <returns>The service, null when the platform can't show them here (then the next provider is tried)</returns>
        INotificationService TryCreate();
    }

    /// <summary>
    /// Chooses the notification service
    /// </summary>
    public static class NotificationServiceProviders
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(NotificationServiceProviders));

        /// <summary>
        /// The service of the provider with the highest priority which is available
        /// </summary>
        /// <returns>null when none is</returns>
        public static INotificationService CreateService(IEnumerable<INotificationServiceProvider> providers)
        {
            foreach (var provider in providers.Where(p => p != null).OrderByDescending(p => p.Priority))
            {
                INotificationService service;
                try
                {
                    service = provider.TryCreate();
                }
                catch (Exception ex)
                {
                    Log.Warn($"{provider.GetType().Name} couldn't create its notification service.", ex);
                    continue;
                }
                if (service != null)
                {
                    Log.Info($"Notifications: {service.GetType().Name}");
                    return service;
                }
            }
            return null;
        }
    }
}
