#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Windows.Forms;
using GreenshotPlugin.Controls;
using GreenshotPlugin.Core;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;
using Dapplo.Log;
using Dapplo.Windows.Dpi;
using Greenshot.Gfx;

#endregion

namespace GreenshotDropboxPlugin
{
    /// <summary>
    ///     This is the Dropbox base code
    /// </summary>
    [Export(typeof(IGreenshotPlugin))]
    public sealed class DropboxPlugin : IGreenshotPlugin
	{
		private static readonly LogSource Log = new LogSource();
		private readonly IDropboxPluginConfiguration _dropboxPluginConfiguration;
		private readonly IGreenshotHost _greenshotHost;
		private ToolStripMenuItem _itemPlugInConfig;

		public void Dispose()
		{
			Dispose(true);
		}

		public IEnumerable<IDestination> Destinations()
		{
		    yield break;
        }


		public IEnumerable<IProcessor> Processors()
		{
			yield break;
		}

        [ImportingConstructor]
	    public DropboxPlugin(IGreenshotHost greenshotGreenshotHost, IDropboxPluginConfiguration dropboxPluginConfiguration)
	    {
	        _greenshotHost = greenshotGreenshotHost;
	        _dropboxPluginConfiguration = dropboxPluginConfiguration;
	    }

		/// <summary>
		///     Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		public bool Initialize()
		{

			_itemPlugInConfig = new ToolStripMenuItem
			{
				Text = Language.GetString("dropbox", LangKey.Configure),
				Tag = _greenshotHost,
			};

			var dropboxResourceScaler = BitmapScaleHandler.WithComponentResourceManager(_greenshotHost.ContextMenuDpiHandler, GetType(), (bitmap, dpi) => bitmap.ScaleIconForDisplaying(dpi));
			dropboxResourceScaler.AddTarget(_itemPlugInConfig, "Dropbox");

			_itemPlugInConfig.Click += ConfigMenuClick;

			PluginUtils.AddToContextMenu(_greenshotHost, _itemPlugInConfig);
			Language.LanguageChanged += OnLanguageChanged;
			return true;
		}

		public void Shutdown()
		{
			Log.Debug().WriteLine("Dropbox Plugin shutdown.");
		}

		/// <summary>
		///     Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure()
		{
			//_config.ShowConfigDialog();
		}

		private void Dispose(bool disposing)
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

		public void OnLanguageChanged(object sender, EventArgs e)
		{
			if (_itemPlugInConfig != null)
			{
				_itemPlugInConfig.Text = Language.GetString("dropbox", LangKey.Configure);
			}
		}

		public void ConfigMenuClick(object sender, EventArgs eventArgs)
		{
			//_config.ShowConfigDialog();
		}
	}
}