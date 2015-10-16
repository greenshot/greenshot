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
using Dapplo.Config.Ini;
using System.Threading.Tasks;
using System.Threading;
using Dapplo.Config.Language;
using System.ComponentModel.Composition;
using Dapplo.Addons;

namespace GreenshotFlickrPlugin
{
	/// <summary>
	/// This is the Flickr base code
	/// </summary>
	[Plugin("Flickr", Configurable = true)]
	[StartupAction, ShutdownAction]
	public class FlickrPlugin : IConfigurablePlugin, IStartupAction, IShutdownAction
	{
		private static IFlickrConfiguration _config;
		private static IFlickrLanguage _language;
		private ComponentResourceManager _resources;
		private ToolStripMenuItem _itemPlugInConfig;

		[Import]
		public IGreenshotHost GreenshotHost
		{
			get;
			set;
		}

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
		/// Initialize
		/// </summary>
		/// <param name="token"></param>
		public async Task StartAsync(CancellationToken token = new CancellationToken())
		{
			// Register / get the flickr configuration
			_config = await IniConfig.Current.RegisterAndGetAsync<IFlickrConfiguration>(token);
			_language = await LanguageLoader.Current.RegisterAndGetAsync<IFlickrLanguage>(token);

			_resources = new ComponentResourceManager(typeof (FlickrPlugin));

			_itemPlugInConfig = new ToolStripMenuItem
			{
				Text = _language.Configure, Tag = GreenshotHost, Image = (Image) _resources.GetObject("flickr")
			};
			_itemPlugInConfig.Click += (sender, eventArgs) => Configure();

			PluginUtils.AddToContextMenu(GreenshotHost, _itemPlugInConfig);
			_language.PropertyChanged += OnLanguageChanged;
		}

		private void OnLanguageChanged(object sender, EventArgs e)
		{
			if (_itemPlugInConfig != null)
			{
				_itemPlugInConfig.Text = _language.Configure;
			}
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure()
		{
			new SettingsForm(_config).ShowDialog();
		}

		public Task ShutdownAsync(CancellationToken token = new CancellationToken())
		{
			_language.PropertyChanged -= OnLanguageChanged;
			return Task.FromResult(true);
		}
	}
}