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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using GreenshotOfficePlugin.Destinations;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;

namespace GreenshotOfficePlugin
{
	/// <summary>
	/// This is the OfficePlugin base code
	/// </summary>
	[Plugin("Office", Configurable = false)]
    public class OfficePlugin : IGreenshotPlugin
	{
		[Import]
		public IOfficeConfiguration OfficeConfiguration
		{
			get;
			set;
		}


		public IEnumerable<ILegacyDestination> Destinations()
		{
			var destinations = new List<ILegacyDestination>();
			try
			{
				destinations.Add(new ExcelLegacyDestination());
			}
			catch
			{
			}
			try
			{
				destinations.Add(new PowerpointLegacyDestination());
			}
			catch
			{
			}
			try
			{
				destinations.Add(new WordLegacyDestination());
			}
			catch
			{
			}

			try
			{
				destinations.Add(new OutlookLegacyDestination());
			}
			catch
			{
			}

			try
			{
				destinations.Add(new OneNoteLegacyDestination());
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