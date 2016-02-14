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

using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Addons;
using Greenshot.Addon.Interfaces.Destination;
using Greenshot.Addon.Interfaces.Plugin;
using Greenshot.Addon.Office.Destinations;

namespace Greenshot.Addon.Office
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

		[Import]
		private IOfficeConfiguration OfficeConfiguration
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
				var wordDestination = new WordDestination();
				ServiceLocator.FillImports(wordDestination);
                ServiceLocator.Export<IDestination>(wordDestination);
			}
			if (ExcelDestination.IsActive)
			{
				var excelDestination = new ExcelDestination();
				ServiceLocator.FillImports(excelDestination);
				ServiceLocator.Export<IDestination>(excelDestination);
			}
			if (OutlookDestination.IsActive)
			{
				var outlookDestination = new OutlookDestination();
				ServiceLocator.FillImports(outlookDestination);
				ServiceLocator.Export<IDestination>(outlookDestination);
			}
			if (PowerpointDestination.IsActive)
			{
				var powerpointDestination = new PowerpointDestination();
				ServiceLocator.FillImports(powerpointDestination);
				ServiceLocator.Export<IDestination>(powerpointDestination);
			}
			if (OneNoteDestination.IsActive)
			{
				var oneNoteDestination = new OneNoteDestination();
				ServiceLocator.FillImports(oneNoteDestination);
				ServiceLocator.Export<IDestination>(oneNoteDestination);
			}
			return Task.FromResult(true);
		}
	}
}