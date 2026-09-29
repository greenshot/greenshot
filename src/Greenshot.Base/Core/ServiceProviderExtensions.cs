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

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Typed access to an IServiceProvider (Microsoft.Extensions.DependencyInjection has these, delete with the .NET 10 move).
    /// </summary>
    public static class ServiceProviderExtensions
    {
        /// <summary>
        /// The service, null when it isn't registered
        /// </summary>
        public static TService GetService<TService>(this IServiceProvider serviceProvider) where TService : class
        {
            return serviceProvider?.GetService(typeof(TService)) as TService;
        }

        /// <summary>
        /// The service, throws when it isn't registered
        /// </summary>
        public static TService GetRequiredService<TService>(this IServiceProvider serviceProvider) where TService : class
        {
            return serviceProvider.GetService<TService>() ?? throw new InvalidOperationException($"No service of type {typeof(TService).FullName} registered.");
        }
    }
}
