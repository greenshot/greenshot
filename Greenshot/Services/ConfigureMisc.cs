/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016  Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub: https://github.com/greenshot
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
using Dapplo.HttpExtensions;
using Dapplo.Log;
using Greenshot.Addon.Configuration;
using Greenshot.Addon.Core;
using Greenshot.Configuration;

namespace Greenshot.Services
{
	/// <summary>
	/// This startup/shutdown action configures everything that is Greenshot needs and couldn't find a place in a specific class
	/// </summary>
	[StartupAction(StartupOrder = (int)GreenshotStartupOrder.Bootstrapper+1)]
	[ShutdownAction]
	public class ConfigureMisc : IStartupAction, IShutdownAction
	{
		private static readonly LogSource Log = new LogSource();

		[Import]
		private INetworkConfiguration NetworkConfiguration { get; set; }

		[Import]
		private ILogConfiguration LogConfiguration { get; set; }

		/// <summary>
		/// IStartupAction entry for starting
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		public Task StartAsync(CancellationToken token = default(CancellationToken))
		{
#if !DEBUG
				// Read the log configuration and set it to the framework
				LogSettings.RegisterDefaultLogger<FileLogger>(LogLevels.Verbose, LogConfiguration);
#endif

			Log.Debug().WriteLine("Configuring misc settings");
			return Task.Run(() => {
				// Read the http configuration and set it to the framework
				HttpExtensionsGlobals.HttpSettings = NetworkConfiguration;

			},
			token);
		}

		/// <summary>
		/// IShutdownAction entry, this cleans up some stuff
		/// </summary>
		/// <param name="token"></param>
		/// <returns>Task</returns>
		public async Task ShutdownAsync(CancellationToken token = default(CancellationToken))
		{
			Log.Debug().WriteLine("Cleaning up");
			await Task.Run(() =>
			{
			}, token).ConfigureAwait(false);
		}
	}
}