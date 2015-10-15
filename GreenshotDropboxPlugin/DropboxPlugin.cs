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
using System.ComponentModel.Composition;

namespace GreenshotDropboxPlugin
{
	/// <summary>
	/// This is the Dropbox base code
	/// </summary>
	[Export(typeof(IGreenshotPlugin))]
	public class DropboxPlugin : IGreenshotPlugin
	{
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof (DropboxPlugin));
		private static IDropboxConfiguration _config;
		private static IDropboxLanguage _language;
		public static PluginAttribute Attributes;
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
			if (disposing)
			{
				if (_itemPlugInConfig != null)
				{
					_itemPlugInConfig.Dispose();
					_itemPlugInConfig = null;
				}
			}
		}

		public IEnumerable<IDestination> Destinations()
		{
			yield return new DropboxDestination(this);
		}


		public IEnumerable<IProcessor> Processors()
		{
			yield break;
		}

		/// <summary>
		/// Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <param name="pluginHost">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="myAttribute">My own attributes</param>
		/// <param name="token"></param>
		public async Task<bool> InitializeAsync(IGreenshotHost pluginHost, PluginAttribute myAttribute, CancellationToken token = new CancellationToken())
		{
			// Register / get the dropbox configuration
			_config = await IniConfig.Current.RegisterAndGetAsync<IDropboxConfiguration>(token);
			_language = await LanguageLoader.Current.RegisterAndGetAsync<IDropboxLanguage>(token);
			_host = pluginHost;
			Attributes = myAttribute;

			// Register configuration (don't need the configuration itself)
			_resources = new ComponentResourceManager(typeof (DropboxPlugin));

			_itemPlugInConfig = new ToolStripMenuItem();
			_itemPlugInConfig.Text = _language.Configure;
			_itemPlugInConfig.Tag = pluginHost;
			_itemPlugInConfig.Click += ConfigMenuClick;
			_itemPlugInConfig.Image = (Image) _resources.GetObject("Dropbox");

			PluginUtils.AddToContextMenu(pluginHost, _itemPlugInConfig);
			_language.PropertyChanged += (sender, args) =>
			{
				if (_itemPlugInConfig != null)
				{
					_itemPlugInConfig.Text = _language.Configure;
				}
			};
			return true;
		}

		public void Shutdown()
		{
			LOG.Debug("Dropbox Plugin shutdown.");
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure()
		{
			ShowConfigDialog();
		}


		/// <summary>
		/// A form for token
		/// </summary>
		/// <returns>bool true if OK was pressed, false if cancel</returns>
		private bool ShowConfigDialog()
		{
			DialogResult result = new SettingsForm().ShowDialog();
			if (result == DialogResult.OK)
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// This will be called when Greenshot is shutting down
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void Closing(object sender, FormClosingEventArgs e)
		{
			LOG.Debug("Application closing, de-registering Dropbox Plugin!");
			Shutdown();
		}

		public void ConfigMenuClick(object sender, EventArgs eventArgs)
		{
			ShowConfigDialog();
		}
	}
}