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

namespace GreenshotPhotobucketPlugin
{
	/// <summary>
	/// This is the GreenshotPhotobucketPlugin base code
	/// </summary>
	public class PhotobucketPlugin : IGreenshotPlugin {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(PhotobucketPlugin));
		private static PhotobucketConfiguration config;
		public static PluginAttribute Attributes;
		private IGreenshotHost host;
		private ComponentResourceManager resources;
		private ToolStripMenuItem itemPlugInConfig;

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (itemPlugInConfig != null) {
					itemPlugInConfig.Dispose();
					itemPlugInConfig = null;
				}
			}
		}

		public PhotobucketPlugin() {
		}

		public IEnumerable<IDestination> Destinations() {
			yield return new PhotobucketDestination();
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
		public async Task<bool> InitializeAsync(IGreenshotHost pluginHost, PluginAttribute myAttributes, CancellationToken token = new CancellationToken()) {
			this.host = (IGreenshotHost)pluginHost;
			Attributes = myAttributes;

			// Register / get the photobucket configuration
			config = await IniConfig.Get("Greenshot", "greenshot").RegisterAndGetAsync<PhotobucketConfiguration>();
			resources = new ComponentResourceManager(typeof(PhotobucketPlugin));
			
			itemPlugInConfig = new ToolStripMenuItem(Language.GetString("photobucket", LangKey.configure));
			itemPlugInConfig.Tag = host;
			itemPlugInConfig.Click += delegate {
				ShowConfigDialog();
			};
			itemPlugInConfig.Image = (Image)resources.GetObject("Photobucket");

			PluginUtils.AddToContextMenu(host, itemPlugInConfig);
			Language.LanguageChanged += new LanguageChangedHandler(OnLanguageChanged);
			return true;
		}

		public void OnLanguageChanged(object sender, EventArgs e) {
			if (itemPlugInConfig != null) {
				itemPlugInConfig.Text = Language.GetString("photobucket", LangKey.configure);
			}
		}

		public void Shutdown() {
			LOG.Debug("Photobucket Plugin shutdown.");
			Language.LanguageChanged -= new LanguageChangedHandler(OnLanguageChanged);
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure() {
			ShowConfigDialog();
		}


		/// <summary>
		/// A form for username/password
		/// </summary>
		/// <returns>bool true if OK was pressed, false if cancel</returns>
		private bool ShowConfigDialog() {
			SettingsForm settingsForm = new SettingsForm(config);
			DialogResult result = settingsForm.ShowDialog();
			if (result == DialogResult.OK) {
				return true;
			}
			return false;
		}

		/// <summary>
		/// This will be called when Greenshot is shutting down
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void Closing(object sender, FormClosingEventArgs e) {
			LOG.Debug("Application closing, de-registering Photobucket Plugin!");
			Shutdown();
		}
	}
}
