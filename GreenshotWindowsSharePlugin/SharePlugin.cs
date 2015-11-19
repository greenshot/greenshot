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

using Dapplo.Addons;
using GreenshotPlugin.Interfaces.Destination;
using GreenshotPlugin.Interfaces.Plugin;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace GreenshotWindowsSharePlugin
{
	/// <summary>
	/// Share Plugin Greenshot
	/// </summary>
	[Plugin("Share")]
	[StartupAction]
    public class SharePlugin : IGreenshotPlugin, IStartupAction
	{
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof (SharePlugin));

		[Import]
		private IServiceLocator ServiceLocator
		{
			get;
			set;
		}

		public void Dispose()
		{
		}


		/// <summary>
		/// Export the destination if the factory can be created
		/// </summary>
		/// <param name="token"></param>
		public Task StartAsync(CancellationToken token = new CancellationToken())
		{
			try
			{
				// Only export if the factory can be intiated
				WindowsRuntimeMarshal.GetActivationFactory(typeof(DataTransferManager));
				var shareDestination = new ShareDestination();
				ServiceLocator.FillImports(shareDestination);
				ServiceLocator.Export<IDestination>(shareDestination);
			}
			catch
			{
				// Ignore
				LOG.Info("Share button disabled.");
			}			
			return Task.FromResult(true);
		}
	}
}