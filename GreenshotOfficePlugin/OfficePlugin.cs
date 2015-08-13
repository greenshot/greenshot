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
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GreenshotOfficePlugin
{
	/// <summary>
	/// This is the OfficePlugin base code
	/// </summary>
	public class OfficePlugin : IGreenshotPlugin
	{
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(OfficePlugin));

		public IEnumerable<IDestination> Destinations()
		{
			var destinations = new List<IDestination>();
			try
			{
				destinations.Add(new ExcelDestination());
			}
			catch
			{
			}
			try
			{
				destinations.Add(new PowerpointDestination());
			}
			catch
			{
			}
			try
			{
				destinations.Add(new WordDestination());
			}
			catch
			{
			}

			try
			{
				destinations.Add(new OutlookDestination());
			}
			catch
			{
			}

			try
			{
				destinations.Add(new OneNoteDestination());
			}
			catch
			{
			}

			return destinations;
		}

		public IEnumerable<IProcessor> Processors()
		{
			yield break;
		}

		/// <summary>
		/// Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <param name="host">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="captureHost">Use the ICaptureHost interface to register in the MainContextMenu</param>
		/// <param name="pluginAttribute">My own attributes</param>
		/// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
		public async Task<bool> InitializeAsync(IGreenshotHost pluginHost, PluginAttribute myAttributes, CancellationToken token = new CancellationToken())
		{
			// Register the office configuration
			await IniConfig.Get("Greenshot", "greenshot").RegisterAndGetAsync<IOfficeConfiguration>();
			return true;
		}

		public void Shutdown()
		{
			LOG.Debug("Office Plugin shutdown.");
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure()
		{
		}

		/// <summary>
		/// This will be called when Greenshot is shutting down
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void Closing(object sender, FormClosingEventArgs e)
		{
			LOG.Debug("Application closing, de-registering Office Plugin!");
			Shutdown();
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects).
				}

				disposedValue = true;
			}
		}

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
		}
		#endregion
	}
}
