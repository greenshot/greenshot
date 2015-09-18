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

using Dapplo.Config.Ini;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GreenshotImgurPlugin
{
	/// <summary>
	/// This is the ImgurPlugin code
	/// </summary>
	public class ImgurPlugin : IGreenshotPlugin {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ImgurPlugin));
		private static IImgurConfiguration config;
		public static PluginAttribute Attributes;
		private IGreenshotHost _host;
		private ComponentResourceManager _resources;
		private ToolStripMenuItem _historyMenuItem = null;
		private ToolStripMenuItem _itemPlugInConfig;

		public void Dispose() {
			Dispose(true);
		}

		protected void Dispose(bool disposing) {
			if (disposing) {
				if (_historyMenuItem != null) {
					_historyMenuItem.Dispose();
					_historyMenuItem = null;
				}
				if (_itemPlugInConfig != null) {
					_itemPlugInConfig.Dispose();
					_itemPlugInConfig = null;
				}
			}
		}

		public IEnumerable<IDestination> Destinations() {
			yield return new ImgurDestination(this);
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
		public async Task<bool> InitializeAsync(IGreenshotHost pluginHost, PluginAttribute pluginAttributes, CancellationToken token = new CancellationToken()) {
			// Register / get the imgur configuration
			config = await IniConfig.Current.RegisterAndGetAsync<IImgurConfiguration>();

			_host = (IGreenshotHost)pluginHost;
			Attributes = pluginAttributes;

			_resources = new ComponentResourceManager(typeof(ImgurPlugin));
			
			var itemPlugInRoot = new ToolStripMenuItem("Imgur");
			itemPlugInRoot.Image = (Image)_resources.GetObject("Imgur");

			_historyMenuItem = new ToolStripMenuItem {
				Text = Language.GetString("imgur", LangKey.history),
				Tag = _host,
				Enabled = config.TrackHistory
			};
			_historyMenuItem.Click += (sender, e) => {
				ImgurHistory.ShowHistory();
			};
			itemPlugInRoot.DropDownItems.Add(_historyMenuItem);

			_itemPlugInConfig = new ToolStripMenuItem(Language.GetString("imgur", LangKey.configure));
			_itemPlugInConfig.Tag = _host;
			_itemPlugInConfig.Click += (sender, e) => ShowConfigDialog();
			itemPlugInRoot.DropDownItems.Add(_itemPlugInConfig);

			PluginUtils.AddToContextMenu(_host, itemPlugInRoot);
			Language.LanguageChanged += new LanguageChangedHandler(OnLanguageChanged);
			return true;
		}

		public void OnLanguageChanged(object sender, EventArgs e) {
			if (_itemPlugInConfig != null) {
				_itemPlugInConfig.Text = Language.GetString("imgur", LangKey.configure);
			}
			if (_historyMenuItem != null) {
				_historyMenuItem.Text = Language.GetString("imgur", LangKey.history);
			}
		}

		public void Shutdown() {
			LOG.Debug("Imgur Plugin shutdown.");
			Language.LanguageChanged -= new LanguageChangedHandler(OnLanguageChanged);
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure() {
			ShowConfigDialog();
			_historyMenuItem.Enabled = config.TrackHistory;
		}

		/// <summary>
		/// This will be called when Greenshot is shutting down
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void Closing(object sender, FormClosingEventArgs e) {
			LOG.Debug("Application closing, de-registering Imgur Plugin!");
			Shutdown();
		}


		/// <summary>
		/// A form for username/password
		/// </summary>
		/// <returns>bool true if OK was pressed, false if cancel</returns>
		private bool ShowConfigDialog() {
			var settingsForm = new SettingsForm(config);
			var result = settingsForm.ShowDialog();
			if (result == DialogResult.OK) {
				return true;
			}
			return false;
		}
	}
}
