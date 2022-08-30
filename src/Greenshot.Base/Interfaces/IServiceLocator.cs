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
using System.Collections.Generic;

namespace Greenshot.Base.Interfaces
{
    /// <summary>
    /// This is the interface of the service locator
    /// </summary>
    public interface IServiceLocator
    {
        /// <summary>
        /// Get all instances of the specified service
        /// </summary>
        /// <typeparam name="TService">Service to find</typeparam>
        /// <returns>IEnumerable{TService}</returns>
        IReadOnlyList<TService> GetAllInstances<TService>();

        /// <summary>
        /// Get the only instance of the specified service
        /// </summary>
        /// <typeparam name="TService">Service to find</typeparam>
        /// <returns>TService</returns>
        TService GetInstance<TService>();

        /// <summary>
        /// Add one of more services to the registry
        /// </summary>
        /// <typeparam name="TService">Type of the service</typeparam>
        /// <param name="services">One or more services which need to be added</param>
        void AddService<TService>(params TService[] services);

        /// <summary>
        /// Add multiple services to the registry
        /// </summary>
        /// <typeparam name="TService">Type of the service</typeparam>
        /// <param name="services">IEnumerable{TService} with services to add</param>
        void AddService<TService>(IEnumerable<TService> services);
    }
}