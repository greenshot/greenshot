/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2018 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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

#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Windows.Forms;
using Dapplo.Log;
using Greenshot.Addon.Imgur.Forms;
using Greenshot.Addons.Addons;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;

#endregion

namespace Greenshot.Addon.Imgur {
    /// <summary>
    /// This is the ImgurPlugin code
    /// </summary>
    [Export(typeof(IGreenshotPlugin))]
    public class ImgurPlugin : IGreenshotPlugin {
	    private static readonly LogSource Log = new LogSource();
		private readonly IImgurConfiguration _imgurConfiguration;
		private readonly IGreenshotHost _greenshotHost;
		private ComponentResourceManager _resources;
		private ToolStripMenuItem _historyMenuItem;
		private ToolStripMenuItem _itemPlugInConfig;

        [ImportingConstructor]
        public ImgurPlugin(IGreenshotHost greenshotGreenshotHost, IImgurConfiguration imgurConfiguration)
        {
            _greenshotHost = greenshotGreenshotHost;
            _imgurConfiguration = imgurConfiguration;
        }

        public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected void Dispose(bool disposing) {
			if (disposing) {
				if (_historyMenuItem != null) {
					_historyMenuItem.Dispose();
					_historyMenuItem = null;
				}
				if (_itemPlugInConfig != null) {
					_itemPlugInConfig.Dispose();
					_itemPlugInConfig = null;
				}
			}
		}

		public IEnumerable<IDestination> Destinations() {
		    yield break;
        }

		public IEnumerable<IProcessor> Processors() {
			yield break;
		}

		/// <summary>
		/// Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
		public bool Initialize() {
			_resources = new ComponentResourceManager(typeof(ImgurPlugin));

			ToolStripMenuItem itemPlugInRoot = new ToolStripMenuItem("Imgur")
			{
				Image = (Image) _resources.GetObject("Imgur")
			};

			_historyMenuItem = new ToolStripMenuItem(Language.GetString("imgur", LangKey.history))
			{
				Tag = _greenshotHost
			};
			_historyMenuItem.Click += delegate {
				ImgurHistory.ShowHistory();
			};
			itemPlugInRoot.DropDownItems.Add(_historyMenuItem);

			_itemPlugInConfig = new ToolStripMenuItem(Language.GetString("imgur", LangKey.configure))
			{
				Tag = _greenshotHost
			};
			itemPlugInRoot.DropDownItems.Add(_itemPlugInConfig);

			PluginUtils.AddToContextMenu(_greenshotHost, itemPlugInRoot);
			Language.LanguageChanged += OnLanguageChanged;

			// Enable history if there are items available
			UpdateHistoryMenuItem();
			return true;
		}

		public void OnLanguageChanged(object sender, EventArgs e) {
			if (_itemPlugInConfig != null) {
				_itemPlugInConfig.Text = Language.GetString("imgur", LangKey.configure);
			}
			if (_historyMenuItem != null) {
				_historyMenuItem.Text = Language.GetString("imgur", LangKey.history);
			}
		}

		private void UpdateHistoryMenuItem() {
		    if (_historyMenuItem == null)
		    {
		        return;
		    }
			try
            {
				_greenshotHost.GreenshotForm.BeginInvoke((MethodInvoker)delegate {
					if (_imgurConfiguration.ImgurUploadHistory != null && _imgurConfiguration.ImgurUploadHistory.Count > 0) {
						_historyMenuItem.Enabled = true;
					} else {
						_historyMenuItem.Enabled = false;
					}
				});
			} catch (Exception ex) {
				Log.Error().WriteLine(ex, "Error loading history");
			}
		}

		public void Shutdown() {
			Log.Debug().WriteLine("Imgur Plugin shutdown.");
			Language.LanguageChanged -= OnLanguageChanged;
		}
	}
}
