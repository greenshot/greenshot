/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on Sourceforge: http://sourceforge.net/projects/greenshot/
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

using Dapplo.Config.Ini;
using GreenshotPlugin.Configuration;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;

namespace GreenshotPlugin.Core
{
	/// <summary>
	/// Description of DestinationHelper.
	/// </summary>
	public static class LegacyDestinationHelper
	{
		private static readonly Serilog.ILogger LOG = Serilog.Log.Logger.ForContext(typeof(LegacyDestinationHelper));
		private static Dictionary<string, ILegacyDestination> RegisteredDestinations = new Dictionary<string, ILegacyDestination>();
		private static readonly ICoreConfiguration coreConfig = IniConfig.Current.Get<ICoreConfiguration>();

		/// Initialize the destinations		
		static LegacyDestinationHelper()
		{
			foreach (Type destinationType in InterfaceUtils.GetSubclassesOf(typeof (ILegacyDestination), true))
			{
				// Only take our own
				if (!"Greenshot.Destinations".Equals(destinationType.Namespace))
				{
					continue;
				}
				ILegacyDestination destination;
				try
				{
					destination = (ILegacyDestination) Activator.CreateInstance(destinationType);
				}
				catch (Exception e)
				{
					LOG.Error(e, "Can't create instance of {Type}", destinationType);
					continue;
				}
				if (destination.IsActive)
				{
					LOG.Debug("Found destination {0} with designation {1}", destinationType.Name, destination.Designation);
					RegisterLegacyDestination(destination);
				}
				else
				{
					LOG.Debug("Ignoring destination {0} with designation {1}", destinationType.Name, destination.Designation);
				}
			}
		}

		/// <summary>
		/// Register your destination here, if it doesn't come from a plugin and needs to be available
		/// </summary>
		/// <param name="destination"></param>
		public static void RegisterLegacyDestination(ILegacyDestination destination)
		{
			if (coreConfig.ExcludeDestinations == null || !coreConfig.ExcludeDestinations.Contains(destination.Designation))
			{
				// don't test the key, an exception should happen wenn it's not unique
				RegisteredDestinations.Add(destination.Designation, destination);
			}
		}

		/// <summary>
		/// Get a list of all destinations, registered or supplied by a plugin
		/// </summary>
		/// <returns></returns>
		public static List<ILegacyDestination> GetAllLegacyDestinations()
		{
			List<ILegacyDestination> destinations = new List<ILegacyDestination>();
			destinations.AddRange(RegisteredDestinations.Values);
			destinations.Sort();
			return destinations;
		}

		/// <summary>
		/// Get a destination by a designation
		/// </summary>
		/// <param name="designation">Designation of the destination</param>
		/// <returns>IDestination or null</returns>
		public static ILegacyDestination GetLegacyDestination(string designation)
		{
			if (designation == null)
			{
				return null;
			}
			if (RegisteredDestinations.ContainsKey(designation))
			{
				return RegisteredDestinations[designation];
			}
			return null;
		}

		/// <summary>
		/// A simple helper method which will call ExportCapture for the destination with the specified designation
		/// </summary>
		/// <param name="designation"></param>
		/// <param name="surface"></param>
		/// <param name="captureDetails"></param>
		public static async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, string designation, ICapture capture, CancellationToken token = default(CancellationToken))
		{
			ILegacyDestination destination = GetLegacyDestination(designation);
			if (destination != null && destination.IsActive)
			{
				return await destination.ExportCaptureAsync(manuallyInitiated, capture, token).ConfigureAwait(false);
			}
			return null;
		}
	}
}