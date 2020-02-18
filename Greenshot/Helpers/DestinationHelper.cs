/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2020 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
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
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using GreenshotPlugin.Core;
using GreenshotPlugin.IniFile;
using GreenshotPlugin.Interfaces;
using log4net;

namespace Greenshot.Helpers {
	/// <summary>
	/// Description of DestinationHelper.
	/// </summary>
	public static class DestinationHelper {
		private static readonly ILog Log = LogManager.GetLogger(typeof(DestinationHelper));
		private static readonly CoreConfiguration CoreConfig = IniConfig.GetIniSection<CoreConfiguration>();

		/// <summary>
		/// Initialize the internal destinations
		/// </summary>
		public static void RegisterInternalDestinations() {
			foreach(Type destinationType in InterfaceUtils.GetSubclassesOf(typeof(IDestination),true)) {
				// Only take our own
				if (!"Greenshot.Destinations".Equals(destinationType.Namespace)) {
					continue;
				}

                if (destinationType.IsAbstract) continue;

                IDestination destination;
                try {
                    destination = (IDestination)Activator.CreateInstance(destinationType);
                } catch (Exception e) {
                    Log.ErrorFormat("Can't create instance of {0}", destinationType);
                    Log.Error(e);
                    continue;
                }
                if (destination.IsActive) {
                    Log.DebugFormat("Found destination {0} with designation {1}", destinationType.Name, destination.Designation);
                    SimpleServiceProvider.Current.AddService(destination);
                } else {
                    Log.DebugFormat("Ignoring destination {0} with designation {1}", destinationType.Name, destination.Designation);
                }
            }
		}

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
		public static IDestination GetDestination(string designation) {
			if (designation == null) {
				return null;
			}
			foreach (IDestination destination in GetAllDestinations()) {
				if (designation.Equals(destination.Designation)) {
					return destination;
				}
			}
			return null;
		}

		/// <summary>
		/// A simple helper method which will call ExportCapture for the destination with the specified designation
		/// </summary>
		/// <param name="manuallyInitiated"></param>
		/// <param name="designation"></param>
		/// <param name="surface"></param>
		/// <param name="captureDetails"></param>
		public static ExportInformation ExportCapture(bool manuallyInitiated, string designation, ISurface surface, ICaptureDetails captureDetails) {
			IDestination destination = GetDestination(designation);
			if (destination != null && destination.IsActive) {
				return destination.ExportCapture(manuallyInitiated, surface, captureDetails);
			}
			return null;
		}
	}
}
