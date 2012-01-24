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
using System.Collections.Generic;
using System.IO;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using IniFile;

namespace GreenshotNetworkImportPlugin {
	/// <summary>
	/// NetworkImportPlugin can receive images via the network
	/// </summary>
	public class NetworkImportPlugin : IGreenshotPlugin {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(NetworkImportPlugin));
		private IGreenshotHost host;
		private PluginAttribute myAttributes;
		private HTTPReceiver httpReceiver = null;
		private NetworkImportPluginConfiguration config;

		public NetworkImportPlugin() {
		}

		public IEnumerable<IDestination> Destinations() {
			yield break;
		}
		public IEnumerable<IProcessor> Processors() {
			yield break;
		}

		/// <summary>
		/// Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <param name="host">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="captureHost">Use the ICaptureHost interface to register in the MainContextMenu</param>
		/// <param name="pluginAttribute">My own attributes</param>
		public bool Initialize(IGreenshotHost host, PluginAttribute myAttributes) {
			LOG.Debug("Initialize called of " + myAttributes.Name);
			this.host = (IGreenshotHost)host;
			this.myAttributes = myAttributes;

			// Load configuration
			config = IniConfig.GetIniSection<NetworkImportPluginConfiguration>();
			
			// check validity
			if (!config.ListenerURL.EndsWith("/")) {
				config.ListenerURL = config.ListenerURL + "/";
				config.IsDirty = true;
				IniConfig.Save();
			}

			IniConfig.IniChanged += new FileSystemEventHandler(ReloadConfiguration);
			ReloadConfiguration(null, null);
			return true;
		}
		
		private void ReloadConfiguration(object source, FileSystemEventArgs e) {
			if (httpReceiver != null) {
				httpReceiver.StopListening();
				httpReceiver = null;
			}
			if (config.RemoteEnabled) {
				httpReceiver = new HTTPReceiver(config.ListenerURL, host);
				httpReceiver.StartListening();
			}
		}
		
		/// <summary>
		/// Implementation of the IGreenshotPlugin.Shutdown
		/// </summary>
		public void Shutdown() {
			LOG.Debug("Shutdown of " + myAttributes.Name);
			if (httpReceiver != null) {
				httpReceiver.StopListening();
				httpReceiver = null;
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