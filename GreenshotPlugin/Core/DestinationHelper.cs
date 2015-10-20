/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom
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
using Greenshot.Plugin;
using GreenshotPlugin.Configuration;
using log4net;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GreenshotPlugin.Core
{
	/// <summary>
	/// Description of DestinationHelper.
	/// </summary>
	public static class DestinationHelper
	{
		private static readonly ILog LOG = LogManager.GetLogger(typeof (DestinationHelper));
		private static Dictionary<string, ILegacyDestination> RegisteredDestinations = new Dictionary<string, ILegacyDestination>();
		private static readonly ICoreConfiguration coreConfig = IniConfig.Current.Get<ICoreConfiguration>();

		/// Initialize the destinations		
		static DestinationHelper()
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
					LOG.ErrorFormat("Can't create instance of {0}", destinationType);
					LOG.Error(e);
					continue;
				}
				if (destination.IsActive)
				{
					LOG.DebugFormat("Found destination {0} with designation {1}", destinationType.Name, destination.Designation);
					RegisterDestination(destination);
				}
				else
				{
					LOG.DebugFormat("Ignoring destination {0} with designation {1}", destinationType.Name, destination.Designation);
				}
			}
		}

		/// <summary>
		/// Register your destination here, if it doesn't come from a plugin and needs to be available
		/// </summary>
		/// <param name="destination"></param>
		public static void RegisterDestination(ILegacyDestination destination)
		{
			if (coreConfig.ExcludeDestinations == null || !coreConfig.ExcludeDestinations.Contains(destination.Designation))
			{
				// don't test the key, an exception should happen wenn it's not unique
				RegisteredDestinations.Add(destination.Designation, destination);
			}
		}

		/// <summary>
		/// Method to get all the destinations from the plugins
		/// </summary>
		/// <returns>List<IDestination></returns>
		private static List<ILegacyDestination> GetPluginDestinations()
		{
			List<ILegacyDestination> destinations = new List<ILegacyDestination>();
			foreach (var pluginInfo in PluginUtils.Host.Plugins)
			{
				IGreenshotPlugin plugin = pluginInfo.Value;
                try
				{
					foreach (ILegacyDestination destination in plugin.Destinations())
					{
						if (coreConfig.ExcludeDestinations == null || !coreConfig.ExcludeDestinations.Contains(destination.Designation))
						{
							destinations.Add(destination);
						}
					}
				}
				catch (Exception ex)
				{
					LOG.ErrorFormat("Couldn't get destinations from the plugin {0}", pluginInfo.Metadata.Name);
					LOG.Error(ex);
				}
			}
			destinations.Sort();
			return destinations;
		}

		/// <summary>
		/// Get a list of all destinations, registered or supplied by a plugin
		/// </summary>
		/// <returns></returns>
		public static List<ILegacyDestination> GetAllDestinations()
		{
			List<ILegacyDestination> destinations = new List<ILegacyDestination>();
			destinations.AddRange(RegisteredDestinations.Values);
			destinations.AddRange(GetPluginDestinations());
			destinations.Sort();
			return destinations;
		}

		/// <summary>
		/// Get a destination by a designation
		/// </summary>
		/// <param name="designation">Designation of the destination</param>
		/// <returns>IDestination or null</returns>
		public static ILegacyDestination GetDestination(string designation)
		{
			if (designation == null)
			{
				return null;
			}
			if (RegisteredDestinations.ContainsKey(designation))
			{
				return RegisteredDestinations[designation];
			}
			foreach (ILegacyDestination destination in GetPluginDestinations())
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
		/// <param name="designation"></param>
		/// <param name="surface"></param>
		/// <param name="captureDetails"></param>
		public static async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, string designation, ISurface surface, ICaptureDetails captureDetails, CancellationToken token = default(CancellationToken))
		{
			ILegacyDestination destination = GetDestination(designation);
			if (destination != null && destination.IsActive)
			{
				return await destination.ExportCaptureAsync(manuallyInitiated, surface, captureDetails, token).ConfigureAwait(false);
			}
			return null;
		}
	}
}