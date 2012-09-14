/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Threading;

using Greenshot.Plugin;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using Greenshot.IniFile;
using CommunicatorAPI;
using System.Runtime.InteropServices;

namespace GreenshotOfficeCommunicatorPlugin {
	/// <summary>
	/// This is the OfficeCommunicatorPlugin
	/// </summary>
	public class OfficeCommunicatorPlugin : IGreenshotPlugin {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(OfficeCommunicatorPlugin));
		public static PluginAttribute Attributes;
		private OfficeCommunicatorConfiguration config = IniConfig.GetIniSection<OfficeCommunicatorConfiguration>();
		private IGreenshotHost host;
		private Communicator communicator;
		public IGreenshotHost Host {
			get {
				return host;
			}
		}

		public Communicator Communicator {
			get {
				return communicator;
			}
			set {
				communicator = value;
			}
		}

		public OfficeCommunicatorPlugin() {
		}

		public IEnumerable<IDestination> Destinations() {
			yield return new OfficeCommunicatorDestination(this);
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
		/// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
		public virtual bool Initialize(IGreenshotHost pluginHost, PluginAttribute myAttributes) {
			this.host = (IGreenshotHost)pluginHost;
			Attributes = myAttributes;

			communicator = new Communicator();
			communicator.Signin(null, null);
			return true;
		}

		public virtual void Shutdown() {
			LOG.Debug("OfficeCommunicator Plugin shutdown.");
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public virtual void Configure() {
			config.ShowConfigDialog();
		}

		/// <summary>
		/// This will be called when Greenshot is shutting down
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void Closing(object sender, FormClosingEventArgs e) {
			LOG.Debug("Application closing, de-registering OfficeCommunicator Plugin!");
			Shutdown();
		}
	}
}
