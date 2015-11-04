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

using Dapplo.Config.Support;
using GreenshotOfficePlugin.Destinations;
using GreenshotPlugin.Interfaces.Destination;
using GreenshotPlugin.Interfaces.Plugin;
using System.ComponentModel.Composition;
using System;
using Dapplo.Addons;
using System.Threading;
using System.Threading.Tasks;

namespace GreenshotOfficePlugin
{
	/// <summary>
	/// This is the OfficePlugin which takes care of exporting the different office destinations
	/// </summary>
	[Plugin("Office", Configurable = false)]
	[StartupAction]
	public class OfficePlugin : IGreenshotPlugin, IStartupAction
	{
		[Import]
		private IServiceLocator ServiceLocator
		{
			get;
			set;
		}

		public void Dispose()
		{
			// Nothing to dispose
		}

		/// <summary>
		/// Export all destinations
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		public Task StartAsync(CancellationToken token = default(CancellationToken))
		{
			if (WordDestination.IsActive)
			{
				ServiceLocator.Export<IDestination>(new WordDestination());
			}
			if (ExcelDestination.IsActive)
			{
				ServiceLocator.Export<IDestination>(new ExcelDestination());
			}
			if (OutlookDestination.IsActive)
			{
				ServiceLocator.Export<IDestination>(new OutlookDestination());
			}
			return Task.FromResult(true);
		}
	}
}