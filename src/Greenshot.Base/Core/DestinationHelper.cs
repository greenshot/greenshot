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
using System.Collections.Generic;
using System.Linq;
using Dapplo.Ini;
using Greenshot.Base.Interfaces;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Helper class to simplify working with destinations.
    /// </summary>
    public static class DestinationHelper
    {
        /// <summary>
        /// All registered destinations which are available in general (not excluded), sorted by priority and name.
        /// </summary>
        /// <returns>List of IDestination</returns>
        public static IEnumerable<IDestination> GetAllDestinations()
        {
            var destinations = SimpleServiceProvider.Current.GetAllInstances<IDestination>() ?? Enumerable.Empty<IDestination>();
            return destinations
                .Where(destination =>
                {
                    try
                    {
                        return destination != null && destination.IsAvailableFor(null);
                    }
                    catch
                    {
                        return destination != null;
                    }
                })
                .OrderBy(d => d, DestinationComparer.Instance)
                .ToList();
        }

        /// <summary>
        /// Get a destination by a designation
        /// </summary>
        /// <param name="designation">Designation of the destination</param>
        /// <returns>IDestination or null</returns>
        public static IDestination GetDestination(string designation)
        {
            if (designation == null)
            {
                return null;
            }

            try
            {
                return SimpleServiceProvider.Current.GetAllInstances<IDestination>()
                    .FirstOrDefault(d => string.Equals(designation, d?.Designation, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Start the export of the surface to the destination with the designation, from a UI event (the export runs in the background).
        /// </summary>
        public static void StartExport(WellKnownDestinations designation, ISurface surface, bool manuallyInitiated = true)
        {
            StartExport(designation.ToString(), surface, manuallyInitiated);
        }

        /// <summary>
        /// Start the export of the surface to the destination with the designation, from a UI event (the export runs in the background).
        /// </summary>
        public static void StartExport(string designation, ISurface surface, bool manuallyInitiated = true)
        {
            var destination = GetDestination(designation);
            if (destination != null && destination.IsAvailableFor(surface?.CaptureDetails))
            {
                Export.DestinationExporter.StartExport(destination, surface, manuallyInitiated);
            }
        }
    }
}
