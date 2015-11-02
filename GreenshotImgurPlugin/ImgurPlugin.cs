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

using GreenshotPlugin.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Addons;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;

namespace GreenshotImgurPlugin
{
	/// <summary>
	/// This is the ImgurPlugin code
	/// </summary>
	[Plugin("Imgur", Configurable = true)]
	[StartupAction, ShutdownAction]
	public class ImgurPlugin : IConfigurablePlugin, IStartupAction, IShutdownAction
	{
		private ComponentResourceManager _resources;
		private ToolStripMenuItem _historyMenuItem;
		private ToolStripMenuItem _itemPlugInConfig;

		[Import]
		public IImgurConfiguration ImgurConfiguration
		{
			get;
			set;
		}

		[Import]
		public IImgurLanguage ImgurLanguage
		{
			get;
			set;
		}

		public void Dispose()
		{
			Dispose(true);
		}

		protected void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}
			if (_historyMenuItem != null)
			{
				_historyMenuItem.Dispose();
				_historyMenuItem = null;
			}
			if (_itemPlugInConfig == null)
			{
				return;
			}
			_itemPlugInConfig.Dispose();
			_itemPlugInConfig = null;
		}

		[Import]
		public IGreenshotHost GreenshotHost
		{
			get;
			set;
		}

		public IEnumerable<ILegacyDestination> Destinations()
		{
			yield return new ImgurLegacyDestination(this);
		}

		/// <summary>
		/// Initialize
		/// </summary>
		/// <param name="token"></param>
		public Task StartAsync(CancellationToken token = new CancellationToken())
		{

			_resources = new ComponentResourceManager(typeof (ImgurPlugin));

			var itemPlugInRoot = new ToolStripMenuItem("Imgur")
			{
				Image = (Image) _resources.GetObject("Imgur")
			};

			_historyMenuItem = new ToolStripMenuItem
			{
				Text = ImgurLanguage.History, Tag = GreenshotHost, Enabled = ImgurConfiguration.TrackHistory
			};
			_historyMenuItem.Click += (sender, e) =>
			{
				ImgurHistory.ShowHistory();
			};
			itemPlugInRoot.DropDownItems.Add(_historyMenuItem);

			_itemPlugInConfig = new ToolStripMenuItem
			{
				Text = ImgurLanguage.Configure, Tag = GreenshotHost
			};
			_itemPlugInConfig.Click += (sender, e) => ShowConfigDialog();
			itemPlugInRoot.DropDownItems.Add(_itemPlugInConfig);

			PluginUtils.AddToContextMenu(GreenshotHost, itemPlugInRoot);
			ImgurLanguage.PropertyChanged += OnLanguageChanged;

			return Task.FromResult(true);
		}

		private void OnLanguageChanged(object sender, EventArgs e)
		{
			if (_itemPlugInConfig != null)
			{
				_itemPlugInConfig.Text = ImgurLanguage.Configure;
			}
			if (_historyMenuItem != null)
			{
				_historyMenuItem.Text = ImgurLanguage.History;
			}
		}

		public Task ShutdownAsync(CancellationToken token = new CancellationToken())
		{
			ImgurLanguage.PropertyChanged -= OnLanguageChanged;
			return Task.FromResult(true);
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure()
		{
			ShowConfigDialog();
			_historyMenuItem.Enabled = ImgurConfiguration.TrackHistory;
		}

		/// <summary>
		/// A form for username/password
		/// </summary>
		/// <returns>bool true if OK was pressed, false if cancel</returns>
		private bool ShowConfigDialog()
		{
			var settingsForm = new SettingsForm(ImgurConfiguration);
			var result = settingsForm.ShowDialog();
			if (result == DialogResult.OK)
			{
				return true;
			}
			return false;
		}
	}
}