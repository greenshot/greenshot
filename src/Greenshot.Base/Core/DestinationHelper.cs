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
using System.Linq;
using Greenshot.Base.IniFile;
using Greenshot.Base.Interfaces;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Helper class to simplify working with destinations.
    /// </summary>
    public static class DestinationHelper
    {
        private static readonly CoreConfiguration CoreConfig = IniConfig.GetIniSection<CoreConfiguration>();

        /// <summary>
        /// Method to get all the destinations from the plugins
        /// </summary>
        /// <returns>List of IDestination</returns>
        public static IEnumerable<IDestination> GetAllDestinations()
        {
            return SimpleServiceProvider.Current.GetAllInstances<IDestination>()
                .Where(destination => destination.IsActive)
                .Where(destination => CoreConfig.ExcludeDestinations == null ||
                                      !CoreConfig.ExcludeDestinations.Contains(destination.Designation)).OrderBy(p => p.Priority).ThenBy(p => p.Description);
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

            foreach (IDestination destination in GetAllDestinations())
            {
                if (designation.Equals(destination.Designation))
                {
                    return destination;
                }
            }

            return null;
        }

        /// <summary>
        /// A simple helper method which will call ExportCapture for the destination with the specified designation
        /// </summary>
        /// <param name="manuallyInitiated"></param>
        /// <param name="designation">WellKnownDestinations</param>
        /// <param name="surface">ISurface</param>
        /// <param name="captureDetails">ICaptureDetails</param>
        public static ExportInformation ExportCapture(bool manuallyInitiated, WellKnownDestinations designation, ISurface surface, ICaptureDetails captureDetails)
        {
            return ExportCapture(manuallyInitiated, designation.ToString(), surface, captureDetails);
        }

        /// <summary>
        /// A simple helper method which will call ExportCapture for the destination with the specified designation
        /// </summary>
        /// <param name="manuallyInitiated">bool</param>
        /// <param name="designation">string</param>
        /// <param name="surface">ISurface</param>
        /// <param name="captureDetails">ICaptureDetails</param>
        public static ExportInformation ExportCapture(bool manuallyInitiated, string designation, ISurface surface, ICaptureDetails captureDetails)
        {
            IDestination destination = GetDestination(designation);
            if (destination != null && destination.IsActive)
            {
                return destination.ExportCapture(manuallyInitiated, surface, captureDetails);
            }

            return null;
        }
    }
}