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

using GreenshotPlugin.Core;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel.Composition;
using Dapplo.Addons;
using GreenshotDropboxPlugin.Forms;
using GreenshotPlugin.Interfaces.Plugin;

namespace GreenshotDropboxPlugin
{
	/// <summary>
	/// This is the Dropbox Plugin
	/// </summary>
	[Plugin("Dropbox", Configurable = true)]
	[StartupAction]
	public class DropboxPlugin : IConfigurablePlugin, IStartupAction
	{
		public static PluginAttribute Attributes;
		private ComponentResourceManager _resources;
		private ToolStripMenuItem _itemPlugInConfig;

		[Import]
		private IGreenshotHost GreenshotHost
		{
			get;
			set;
		}

		[Import]
		private IDropboxConfiguration DropboxConfiguration
		{
			get;
			set;
		}

		[Import]
		private IDropboxLanguage DropboxLanguage
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
			if (disposing)
			{
				if (_itemPlugInConfig != null)
				{
					_itemPlugInConfig.Dispose();
					_itemPlugInConfig = null;
				}
			}
		}

		/// <summary>
		/// Initialize
		/// </summary>
		/// <param name="token"></param>
		public Task StartAsync(CancellationToken token = new CancellationToken())
		{
			// Register configuration (don't need the configuration itself)
			_resources = new ComponentResourceManager(typeof (DropboxPlugin));

			_itemPlugInConfig = new ToolStripMenuItem
			{
				Text = DropboxLanguage.Configure, Tag = GreenshotHost
			};
			_itemPlugInConfig.Click += (sender, eventArgs) => Configure();
			_itemPlugInConfig.Image = (Image) _resources.GetObject("Dropbox");

			PluginUtils.AddToContextMenu(GreenshotHost, _itemPlugInConfig);
			DropboxLanguage.PropertyChanged += (sender, args) =>
			{
				if (_itemPlugInConfig != null)
				{
					_itemPlugInConfig.Text = DropboxLanguage.Configure;
				}
			};
			return Task.FromResult(true);
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure()
		{
			new SettingsForm().ShowDialog();
		}
	}
}