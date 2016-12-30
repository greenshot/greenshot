//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System.ComponentModel.Composition;
using Dapplo.Addons;
using Dapplo.HttpExtensions;
using Dapplo.Log;
using Greenshot.Addon.Core;
using Greenshot.Configuration;
using Greenshot.Core.Configuration;

#endregion

namespace Greenshot.Services
{
	/// <summary>
	///     This startup/shutdown action configures everything that is Greenshot needs and couldn't find a place in a specific
	///     class
	/// </summary>
	[StartupAction(StartupOrder = (int) GreenshotStartupOrder.Bootstrapper + 1)]
	[ShutdownAction]
	public class ConfigureMisc : IStartupAction, IShutdownAction
	{
		private static readonly LogSource Log = new LogSource();

		[Import]
		private ILogConfiguration LogConfiguration { get; set; }

		[Import]
		private INetworkConfiguration NetworkConfiguration { get; set; }

		/// <summary>
		///     IShutdownAction entry, this cleans up some stuff
		/// </summary>
		public void Shutdown()
		{
			Log.Debug().WriteLine("Cleaning up");
		}

		/// <summary>
		///     IStartupAction entry for starting
		/// </summary>
		public void Start()
		{
#if !DEBUG
			// Read the log configuration and set it to the framework
			LogSettings.RegisterDefaultLogger<FileLogger>(LogLevels.Verbose, LogConfiguration);
#endif

			Log.Debug().WriteLine("Configuring misc settings");
			HttpExtensionsGlobals.HttpSettings = NetworkConfiguration;
		}
	}
}