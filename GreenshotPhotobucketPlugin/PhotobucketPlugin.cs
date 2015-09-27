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
using Dapplo.Config.Language;

namespace GreenshotPhotobucketPlugin
{
	/// <summary>
	/// This is the GreenshotPhotobucketPlugin base code
	/// </summary>
	public class PhotobucketPlugin : IGreenshotPlugin {
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(PhotobucketPlugin));
		private static IPhotobucketConfiguration _config;
		private static IPhotobucketLanguage _language;

		public static PluginAttribute Attributes;
		private IGreenshotHost _host;
		private ComponentResourceManager _resources;
		private ToolStripMenuItem _itemPlugInConfig;

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) {
			if (disposing) {
				if (_itemPlugInConfig != null) {
					_itemPlugInConfig.Dispose();
					_itemPlugInConfig = null;
				}
			}
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
		/// <param name="pluginHost">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="myAttribute">My own attributes</param>
		/// <param name="token"></param>
		/// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
		public async Task<bool> InitializeAsync(IGreenshotHost pluginHost, PluginAttribute myAttribute, CancellationToken token = new CancellationToken()) {
			_host = pluginHost;
			Attributes = myAttribute;

			// Register / get the photobucket configuration
			_config = await IniConfig.Current.RegisterAndGetAsync<IPhotobucketConfiguration>(token);
			_language = await LanguageLoader.Current.RegisterAndGetAsync<IPhotobucketLanguage>(token);
			_resources = new ComponentResourceManager(typeof(PhotobucketPlugin));
			
			_itemPlugInConfig = new ToolStripMenuItem(_language.Configure);
			_itemPlugInConfig.Tag = _host;
			_itemPlugInConfig.Click += delegate {
				ShowConfigDialog();
			};
			_itemPlugInConfig.Image = (Image)_resources.GetObject("Photobucket");

			PluginUtils.AddToContextMenu(_host, _itemPlugInConfig);
			_language.PropertyChanged += OnLanguageChanged;
			return true;
		}

		public void OnLanguageChanged(object sender, EventArgs e) {
			if (_itemPlugInConfig != null) {
				_itemPlugInConfig.Text = _language.Configure;
			}
		}

		public void Shutdown() {
			LOG.Debug("Photobucket Plugin shutdown.");
			_language.PropertyChanged -= OnLanguageChanged;
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
		private void ShowConfigDialog()
		{
			var settingsForm = new SettingsForm(_config);
			settingsForm.ShowDialog();
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
