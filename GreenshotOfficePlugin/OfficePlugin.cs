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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Addons;

namespace GreenshotOfficePlugin
{
	/// <summary>
	/// This is the OfficePlugin base code
	/// </summary>
	[Plugin(Configurable = false)]
	[StartupAction]
    public class OfficePlugin : IGreenshotPlugin, IStartupAction
	{
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
		/// Initialize
		/// </summary>
		/// <param name="token"></param>
		public async Task StartAsync(CancellationToken token = new CancellationToken())
		{
			// Register the office configuration
			await IniConfig.Current.RegisterAndGetAsync<IOfficeConfiguration>(token);
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