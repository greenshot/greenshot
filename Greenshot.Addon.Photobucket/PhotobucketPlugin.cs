/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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
using Greenshot.Addon.Core;
using Greenshot.Addon.Interfaces.Plugin;
using Greenshot.Addon.Photobucket.Forms;

namespace Greenshot.Addon.Photobucket
{
	/// <summary>
	/// This is the GreenshotPhotobucketPlugin base code
	/// </summary>
	[Plugin("Photobucket", Configurable = true)]
	[StartupAction]
    public class PhotobucketPlugin : IConfigurablePlugin, IStartupAction, IShutdownAction
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
		private IPhotobucketConfiguration PhotobucketConfiguration
		{
			get;
			set;
		}

		[Import]
		private IPhotobucketLanguage PhotobucketLanguage
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
			_resources = new ComponentResourceManager(typeof (PhotobucketPlugin));


			_itemPlugInConfig = new ToolStripMenuItem(PhotobucketLanguage.Configure)
			{
				Tag = GreenshotHost
			};
			_itemPlugInConfig.Click += (sender, eventArgs) => Configure();
			_itemPlugInConfig.Image = (Image) _resources.GetObject("Photobucket");

			PluginUtils.AddToContextMenu(GreenshotHost, _itemPlugInConfig);
			PhotobucketLanguage.PropertyChanged += OnPhotobucketLanguageChanged;
			return Task.FromResult(true);
		}

		public void OnPhotobucketLanguageChanged(object sender, EventArgs e)
		{
			if (_itemPlugInConfig != null)
			{
				_itemPlugInConfig.Text = PhotobucketLanguage.Configure;
			}
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure()
		{
			var settingsForm = new SettingsForm(PhotobucketConfiguration);
			settingsForm.ShowDialog();
		}

		public Task ShutdownAsync(CancellationToken token = new CancellationToken())
		{
			PhotobucketLanguage.PropertyChanged -= OnPhotobucketLanguageChanged;
			return Task.FromResult(true);
		}
	}
}