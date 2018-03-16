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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Windows.Forms;
using Dapplo.Log;
using GreenshotPlugin.Addons;
using GreenshotPlugin.Core;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;

#endregion

namespace Greenshot.Addon.Flickr
{
    /// <summary>
    ///     This is the Flickr plugin
    /// </summary>
    [Export(typeof(IGreenshotPlugin))]
    public sealed class FlickrPlugin : IGreenshotPlugin
	{
		private static readonly LogSource Log = new LogSource();
		private readonly IGreenshotHost _host;
		private ToolStripMenuItem _itemPlugInConfig;
		private ComponentResourceManager _resources;

	    [ImportingConstructor]
	    public FlickrPlugin(IGreenshotHost greenshotHost)
	    {
	        _host = greenshotHost;
	    }

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

		/// <summary>
		///     Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		public bool Initialize()
		{
			_resources = new ComponentResourceManager(typeof(FlickrPlugin));

			_itemPlugInConfig = new ToolStripMenuItem
			{
				Text = Language.GetString("flickr", LangKey.Configure),
				Tag = _host,
				Image = (Image) _resources.GetObject("flickr")
			};

			PluginUtils.AddToContextMenu(_host, _itemPlugInConfig);
			Language.LanguageChanged += OnLanguageChanged;
			return true;
		}

		public void Shutdown()
		{
			Log.Debug().WriteLine("Flickr Plugin shutdown.");
		}

		private void Dispose(bool disposing)
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

		public void OnLanguageChanged(object sender, EventArgs e)
		{
			if (_itemPlugInConfig != null)
			{
				_itemPlugInConfig.Text = Language.GetString("flickr", LangKey.Configure);
			}
		}

	}
}