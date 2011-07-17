/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2011  Thomas Braun, Jens Klingen, Robin Krom
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
using System;
using System.IO;
using System.Threading;

using Greenshot.Plugin;
using GreenshotPlugin.Core;

namespace GreenshotRemotePlugin {
	/// <summary>
	/// Remote Plugin Greenshot
	/// </summary>
	public class RemotePlugin : IGreenshotPlugin {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(RemotePlugin));
		private IGreenshotPluginHost host;
		private ICaptureHost captureHost = null;
		private PluginAttribute myAttributes;
		private Server httpServer = null;
		private RemoteConfiguration config;
				
		public RemotePlugin() { }

		/// <summary>
		/// Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <param name="host">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="captureHost">Use the ICaptureHost interface to register in the MainContextMenu</param>
		/// <param name="pluginAttribute">My own attributes</param>
		public void Initialize(IGreenshotPluginHost host, ICaptureHost captureHost, PluginAttribute myAttributes) {
			LOG.Debug("Initialize called of " + myAttributes.Name);
			this.host = (IGreenshotPluginHost)host;
			this.captureHost = captureHost;
			this.myAttributes = myAttributes;

			// Load configuration
			config = IniConfig.GetIniSection<RemoteConfiguration>();
			
			// check validity
			if (!config.ListenerURL.EndsWith("/")) {
				config.ListenerURL = config.ListenerURL + "/";
				config.IsDirty = true;
				IniConfig.Save();
			}

			IniConfig.IniChanged += new FileSystemEventHandler(ReloadConfiguration);
			ReloadConfiguration(null, null);
		}
		
		private void ReloadConfiguration(object source, FileSystemEventArgs e) {
			if (httpServer != null) {
				httpServer.StopListening();
				httpServer = null;
			}
			if (config.RemoteEnabled) {
				httpServer = new Server(config.ListenerURL, config.AccessKey);
				httpServer.SetCaptureHost(captureHost);
				httpServer.SetGreenshotPluginHost(host);
				httpServer.StartListening();
			}
		}
		
		/// <summary>
		/// Implementation of the IGreenshotPlugin.Shutdown
		/// </summary>
		public void Shutdown() {
			LOG.Debug("Shutdown of " + myAttributes.Name);
			if (httpServer != null) {
				httpServer.StopListening();
				httpServer = null;
			}
			IniConfig.IniChanged -= new FileSystemEventHandler(ReloadConfiguration);
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public virtual void Configure() {
			return;
		}

	}
}
