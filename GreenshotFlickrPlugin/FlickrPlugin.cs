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
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using GreenshotPlugin.Core;
using System.Threading.Tasks;
using System.Threading;
using System.ComponentModel.Composition;
using Dapplo.Addons;
using GreenshotPlugin.Interfaces.Plugin;

namespace GreenshotFlickrPlugin
{
	/// <summary>
	/// This is the Flickr base code
	/// </summary>
	[Plugin("Flickr", Configurable = true)]
	[StartupAction, ShutdownAction]
	public class FlickrPlugin : IConfigurablePlugin, IStartupAction, IShutdownAction
	{
		private ComponentResourceManager _resources;
		private ToolStripMenuItem _itemPlugInConfig;

		[Import]
		private IGreenshotHost GreenshotHost
		{
			get;
			set;
		}

		[Import]
		private IFlickrConfiguration FlickrConfiguration
		{
			get;
			set;
		}

		[Import]
		private IFlickrLanguage FlickrLanguage
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

		/// <summary>
		/// Initialize
		/// </summary>
		/// <param name="token"></param>
		public Task StartAsync(CancellationToken token = new CancellationToken())
		{
			_resources = new ComponentResourceManager(typeof (FlickrPlugin));

			_itemPlugInConfig = new ToolStripMenuItem
			{
				Text = FlickrLanguage.Configure, Tag = GreenshotHost, Image = (Image) _resources.GetObject("flickr")
			};
			_itemPlugInConfig.Click += (sender, eventArgs) => Configure();

			PluginUtils.AddToContextMenu(GreenshotHost, _itemPlugInConfig);
			FlickrLanguage.PropertyChanged += OnFlickrLanguageChanged;
			return Task.FromResult(true);
		}

		private void OnFlickrLanguageChanged(object sender, EventArgs e)
		{
			if (_itemPlugInConfig != null)
			{
				_itemPlugInConfig.Text = FlickrLanguage.Configure;
			}
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure()
		{
			new SettingsForm(FlickrConfiguration).ShowDialog();
		}

		public Task ShutdownAsync(CancellationToken token = new CancellationToken())
		{
			FlickrLanguage.PropertyChanged -= OnFlickrLanguageChanged;
			return Task.FromResult(true);
		}
	}
}