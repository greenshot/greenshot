/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2015 Thomas Braun, Jens Klingen, Robin Krom, Francis Noel
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
using System.Windows.Forms;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using log4net;
using Dapplo.Config.Ini;
using System.Threading.Tasks;
using System.Threading;
using Dapplo.Config.Language;
using System.ComponentModel.Composition;

namespace GreenshotFlickrPlugin
{
	/// <summary>
	/// This is the Flickr base code
	/// </summary>
	[Export(typeof(IGreenshotPlugin))]
	public class FlickrPlugin : IGreenshotPlugin
	{
		private static readonly ILog LOG = LogManager.GetLogger(typeof (FlickrPlugin));
		private static IFlickrConfiguration _config;
		private static IFlickrLanguage _language;
		private IGreenshotHost _host;
		private ComponentResourceManager _resources;
		private ToolStripMenuItem _itemPlugInConfig;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}
			if (_itemPlugInConfig == null)
			{
				return;
			}
			_itemPlugInConfig.Dispose();
			_itemPlugInConfig = null;
		}

		public IEnumerable<IDestination> Destinations()
		{
			yield return new FlickrDestination();
		}


		public IEnumerable<IProcessor> Processors()
		{
			yield break;
		}

		/// <summary>
		/// Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <param name="pluginHost">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="pluginAttribute">My own attributes</param>
		public async Task<bool> InitializeAsync(IGreenshotHost pluginHost, PluginAttribute pluginAttribute, CancellationToken token = new CancellationToken())
		{
			// Register / get the flickr configuration
			_config = await IniConfig.Current.RegisterAndGetAsync<IFlickrConfiguration>(token);
			_language = await LanguageLoader.Current.RegisterAndGetAsync<IFlickrLanguage>(token);
			_host = pluginHost;

			_resources = new ComponentResourceManager(typeof (FlickrPlugin));

			_itemPlugInConfig = new ToolStripMenuItem();
			_itemPlugInConfig.Text = _language.Configure;
			_itemPlugInConfig.Tag = _host;
			_itemPlugInConfig.Image = (Image) _resources.GetObject("flickr");
			_itemPlugInConfig.Click += ConfigMenuClick;

			PluginUtils.AddToContextMenu(_host, _itemPlugInConfig);
			_language.PropertyChanged += OnLanguageChanged;
			return true;
		}

		public void OnLanguageChanged(object sender, EventArgs e)
		{
			if (_itemPlugInConfig != null)
			{
				_itemPlugInConfig.Text = _language.Configure;
			}
		}

		public void Shutdown()
		{
			LOG.Debug("Flickr Plugin shutdown.");
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure()
		{
			ShowConfigDialog();
		}

		/// <summary>
		/// This will be called when Greenshot is shutting down
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void Closing(object sender, FormClosingEventArgs e)
		{
			LOG.Debug("Application closing, de-registering Flickr Plugin!");
			Shutdown();
		}

		public void ConfigMenuClick(object sender, EventArgs eventArgs)
		{
			ShowConfigDialog();
		}

		/// <summary>
		/// A form for token
		/// </summary>
		/// <returns>bool true if OK was pressed, false if cancel</returns>
		private bool ShowConfigDialog()
		{
			DialogResult result = new SettingsForm(_config).ShowDialog();
			if (result == DialogResult.OK)
			{
				return true;
			}
			return false;
		}
	}
}