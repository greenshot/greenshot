// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Greenshot.Addons.Components;

namespace Greenshot.Addons.Extensions
{
    /// <summary>
    /// Extensions for the IDestination
    /// </summary>
    public static class DestinationExtensions
    {
        /// <summary>
        /// Find the matching IDestination
        /// </summary>
        /// <param name="destinations">IEnumerable of IDestination</param>
        /// <param name="destinationType">Type of the destination</param>
        /// <returns>IDestination or null</returns>
        public static IDestination Find(this IEnumerable<IDestination> destinations, Type destinationType)
        {
            return destinations.FirstOrDefault(p => p.Designation == destinationType.GetDesignation() && p.IsActive);
        }

        /// <summary>
        /// Find the matching IDestination
        /// </summary>
        /// <param name="destinations">IEnumerable of IDestination</param>
        /// <param name="destination">strng with the destination</param>
        /// <returns>IDestination or null</returns>
        public static IDestination Find(this IEnumerable<IDestination> destinations, string destination)
        {
            return destinations.FirstOrDefault(p => p.IsActive && p.GetType().GetDesignation() == destination);
        }

        /// <summary>
        /// Find the matching IDestination
        /// </summary>
        /// <param name="destinations">IEnumerable of IDestination</param>
        /// <param name="destination">destination</param>
        /// <returns>IDestination or null</returns>
        public static IDestination Find(this IEnumerable<Lazy<IDestination, DestinationAttribute>> destinations, string destination)
        {
            return destinations.FirstOrDefault(p => p.Metadata.Designation == destination && p.Value.IsActive)?.Value;
        }

        /// <summary>
        /// Find the matching IDestination
        /// </summary>
        /// <param name="destinations">IEnumerable of IDestination</param>
        /// <param name="destinationType">destination type</param>
        /// <returns>IDestination or null</returns>
        public static IDestination Find(this IEnumerable<Lazy<IDestination, DestinationAttribute>> destinations, Type destinationType)
        {
            return destinations.FirstOrDefault(p => p.Metadata.Designation == destinationType.GetDesignation() && p.Value.IsActive)?.Value;
        }

        /// <summary>
        /// Return the designation for a certain type
        /// </summary>
        /// <param name="destinationType">Type</param>
        /// <returns>string</returns>
        public static string GetDesignation(this Type destinationType)
        {
            return destinationType?.GetCustomAttributes().Where(a => a is DestinationAttribute).Cast<DestinationAttribute>().FirstOrDefault()?.Designation;
        }
    }
}
