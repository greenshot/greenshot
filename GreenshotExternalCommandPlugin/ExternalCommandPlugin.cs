/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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

using Greenshot.IniFile;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;

namespace ExternalCommand {
	/// <summary>
	/// An Plugin to run commands after an image was written
	/// </summary>
	public class ExternalCommandPlugin : IGreenshotPlugin {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ExternalCommandPlugin));
		private static readonly CoreConfiguration coreConfig = IniConfig.GetIniSection<CoreConfiguration>();
		private static readonly ExternalCommandConfiguration config = IniConfig.GetIniSection<ExternalCommandConfiguration>();
		private IGreenshotHost _host;
		private PluginAttribute _myAttributes;
		private ToolStripMenuItem _itemPlugInRoot;

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (_itemPlugInRoot != null) {
					_itemPlugInRoot.Dispose();
					_itemPlugInRoot = null;
				}
			}
		}

		public IEnumerable<IDestination> Destinations() {
			foreach(string command in config.Commands) {
				yield return new ExternalCommandDestination(command);
			}
		}

		public IEnumerable<IProcessor> Processors() {
			yield break;
		}

		/// <summary>
		/// Check and eventually fix the command settings
		/// </summary>
		/// <param name="command"></param>
		/// <returns>false if the command is not correctly configured</returns>
		private bool isCommandValid(string command) {
			if (!config.runInbackground.ContainsKey(command)) {
				LOG.WarnFormat("Found missing runInbackground for {0}", command);
				// Fix it
				config.runInbackground.Add(command, true);
			}
			if (!config.arguments.ContainsKey(command)) {
				LOG.WarnFormat("Found missing argument for {0}", command);
				// Fix it
				config.arguments.Add(command, "{0}");
			}
			if (!config.commandlines.ContainsKey(command)) {
				LOG.WarnFormat("Found missing commandline for {0}", command);
				return false;
			}
			string commandline = FilenameHelper.FillVariables(config.commandlines[command], true);
			commandline = FilenameHelper.FillCmdVariables(commandline, true);

			if (!File.Exists(commandline)) {
				LOG.WarnFormat("Found 'invalid' commandline {0} for command {1}", config.commandlines[command], command);
				return false;
			}
			return true;
		}
		/// <summary>
		/// Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <param name="pluginHost">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="myAttributes">My own attributes</param>
		public virtual bool Initialize(IGreenshotHost pluginHost, PluginAttribute myAttributes) {
			LOG.DebugFormat("Initialize called of {0}", myAttributes.Name);

			List<string> commandsToDelete = new List<string>();
			// Check configuration
			foreach(string command in config.Commands) {
				if (!isCommandValid(command)) {
					commandsToDelete.Add(command);
				}
			}

			// cleanup
			foreach (string command in commandsToDelete) {
				config.runInbackground.Remove(command);
				config.commandlines.Remove(command);
				config.arguments.Remove(command);
				config.Commands.Remove(command);
			}

			_host = pluginHost;
			_myAttributes = myAttributes;


			_itemPlugInRoot = new ToolStripMenuItem {Tag = _host};
			OnIconSizeChanged(this, new PropertyChangedEventArgs("IconSize"));
			OnLanguageChanged(this, null);
			_itemPlugInRoot.Click += ConfigMenuClick;

			PluginUtils.AddToContextMenu(_host, _itemPlugInRoot);
			Language.LanguageChanged += OnLanguageChanged;
			coreConfig.PropertyChanged += OnIconSizeChanged;
			return true;
		}

		/// <summary>
		/// Fix icon reference
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnIconSizeChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "IconSize") {
				try {
					string exePath = PluginUtils.GetExePath("cmd.exe");
					if (exePath != null && File.Exists(exePath)) {
						_itemPlugInRoot.Image = PluginUtils.GetCachedExeIcon(exePath, 0);
					}
				} catch (Exception ex) {
					LOG.Warn("Couldn't get the cmd.exe image", ex);
				}
			}
		}

		private void OnLanguageChanged(object sender, EventArgs e) {
			if (_itemPlugInRoot != null) {
				_itemPlugInRoot.Text = Language.GetString("externalcommand", "contextmenu_configure");
			}
		}

		public virtual void Shutdown() {
			LOG.Debug("Shutdown of " + _myAttributes.Name);
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