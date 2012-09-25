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
using System.Diagnostics;
using System.IO;
using Greenshot.Plugin;
using Greenshot.IniFile;
using System.Windows.Forms;
using GreenshotPlugin.Core;

namespace ExternalCommand {
	/// <summary>
	/// An Plugin to run commands after an image was written
	/// </summary>
	public class ExternalCommandPlugin : IGreenshotPlugin {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ExternalCommandPlugin));
		private static ExternalCommandConfiguration config = IniConfig.GetIniSection<ExternalCommandConfiguration>();
		private IGreenshotHost host;
		private PluginAttribute myAttributes;

		public ExternalCommandPlugin() {
		}

		public IEnumerable<IDestination> Destinations() {
			foreach(string command in config.commands) {
				yield return new ExternalCommandDestination(command);
			}
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
		public virtual bool Initialize(IGreenshotHost pluginHost, PluginAttribute myAttributes) {
			LOG.Debug("Initialize called of " + myAttributes.Name);

			// Cleanup configuration
			foreach(string command in config.commandlines.Keys) {
				if (!File.Exists(config.commandlines[command])) {
					config.commands.Remove(command);
				}
			}
			this.host = pluginHost;
			this.myAttributes = myAttributes;


			ToolStripMenuItem itemPlugInRoot = new ToolStripMenuItem();
			itemPlugInRoot.Text = Language.GetString("externalcommand", "contextmenu_configure");
			itemPlugInRoot.Tag = host;
			string exePath = PluginUtils.GetExePath("cmd.exe");
			if (exePath != null && File.Exists(exePath)) {
				itemPlugInRoot.Image = PluginUtils.GetExeIcon(exePath, 0);
			}
			itemPlugInRoot.Click += new System.EventHandler(ConfigMenuClick);

			PluginUtils.AddToContextMenu(host, itemPlugInRoot);
			return true;
		}

		public virtual void Shutdown() {
			LOG.Debug("Shutdown of " + myAttributes.Name);
		}

		private void ConfigMenuClick(object sender, EventArgs eventArgs) {
			Configure();
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public virtual void Configure() {
			LOG.Debug("Configure called");
			new SettingsForm().ShowDialog();
		}
	}
}