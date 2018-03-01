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
using System.Windows.Forms;
using Dapplo.Log;
using Dapplo.Windows.Dpi;
using Greenshot.Gfx;
using GreenshotLutimPlugin.Forms;
using GreenshotPlugin.Core;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;
#endregion

namespace GreenshotLutimPlugin {
    /// <summary>
    /// This is the LutimPlugin code
    /// </summary>
    [Export(typeof(IGreenshotPlugin))]
    public class LutimPlugin : IGreenshotPlugin {
        private static readonly LogSource Log = new LogSource();
        private readonly IGreenshotHost _greenshotHost;
        private readonly ILutimConfiguration _lutimConfiguration;
		private ToolStripMenuItem _historyMenuItem;
		private ToolStripMenuItem _itemPlugInConfig;

        [ImportingConstructor]
        public LutimPlugin(IGreenshotHost greenshotHost, ILutimConfiguration lutimConfiguration)
        {
            _greenshotHost = greenshotHost;
            _lutimConfiguration = lutimConfiguration;
        }

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing) {
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

			var lutimResourceScaleHandler = BitmapScaleHandler.WithComponentResourceManager(_greenshotHost.ContextMenuDpiHandler, GetType(), (bitmap, dpi) => bitmap.ScaleIconForDisplaying(dpi));

			var itemPlugInRoot = new ToolStripMenuItem("Lutim");
			lutimResourceScaleHandler.AddTarget(itemPlugInRoot, "Lutim");

			_historyMenuItem = new ToolStripMenuItem(Language.GetString("lutim", LangKey.history))
			{
				Tag = _greenshotHost
            };
			_historyMenuItem.Click += delegate {
				LutimHistory.ShowHistory();
			};
			itemPlugInRoot.DropDownItems.Add(_historyMenuItem);

			_itemPlugInConfig = new ToolStripMenuItem(Language.GetString("lutim", LangKey.configure))
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
				_itemPlugInConfig.Text = Language.GetString("lutim", LangKey.configure);
			}
			if (_historyMenuItem != null) {
				_historyMenuItem.Text = Language.GetString("lutim", LangKey.history);
			}
		}

		private void UpdateHistoryMenuItem() {
			try {
			    _greenshotHost.GreenshotForm.BeginInvoke((MethodInvoker)delegate
			    {
			        _historyMenuItem.Enabled = _lutimConfiguration.LutimUploadHistory.Count > 0;
			    });
			} catch (Exception ex) {
				Log.Error().WriteLine(ex, "Error loading history");
			}
		}

		public virtual void Shutdown() {
			Log.Debug().WriteLine("Lutim Plugin shutdown.");
			Language.LanguageChanged -= OnLanguageChanged;
		}

	}
}
