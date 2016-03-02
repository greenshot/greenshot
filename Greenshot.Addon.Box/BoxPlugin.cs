/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom,
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub: https://github.com/greenshot
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
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Addons;
using Greenshot.Addon.Box.Forms;
using Greenshot.Addon.Core;
using Greenshot.Addon.Interfaces.Plugin;

namespace Greenshot.Addon.Box
{
	/// <summary>
	/// This is the Box base code
	/// </summary>
	[Plugin("Box", Configurable = true)]
	[StartupAction]
	public class BoxPlugin : IConfigurablePlugin, IStartupAction
	{
		private readonly ComponentResourceManager _resources = new ComponentResourceManager(typeof(BoxPlugin));
		private ToolStripMenuItem _itemPlugInConfig;

		[Import]
		private IGreenshotHost GreenshotHost
		{
			get;
			set;
		}

		[Import]
		private IBoxConfiguration BoxConfiguration
		{
			get;
			set;
		}

		[Import]
		private IBoxLanguage BoxLanguage
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
			_itemPlugInConfig = new ToolStripMenuItem
			{
				Image = (Image)_resources.GetObject("Box"),
				Text = BoxLanguage.Configure
			};
			_itemPlugInConfig.Click += (sender, eventArgs) => Configure();

			PluginUtils.AddToContextMenu(GreenshotHost, _itemPlugInConfig);
			BoxLanguage.PropertyChanged += (sender, args) =>
			{
				if (_itemPlugInConfig != null)
				{
					_itemPlugInConfig.Text = BoxLanguage.Configure;
				}
			};
			return Task.FromResult(true);
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure()
		{
			new SettingsForm(BoxConfiguration).ShowDialog();
		}
	}
}