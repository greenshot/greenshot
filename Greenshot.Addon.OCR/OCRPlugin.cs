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
using GreenshotPlugin.Addons;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;

#endregion

namespace Greenshot.Addon.OCR
{
    /// <summary>
    ///     OCR Plugin Greenshot
    /// </summary>
    [Export(typeof(IGreenshotPlugin))]
    public sealed class OcrPlugin : IGreenshotPlugin
	{
		private static readonly LogSource Log = new LogSource();
	    private static IOCRConfiguration _ocrConfiguration;
		private ToolStripMenuItem _ocrMenuItem = new ToolStripMenuItem();

	    [ImportingConstructor]
	    public OcrPlugin(IOCRConfiguration ocrConfiguration)
	    {
	        _ocrConfiguration = ocrConfiguration;
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
		/// <returns>true if plugin is initialized, false if not (doesn't show)</returns>
		public bool Initialize()
		{
			Log.Debug().WriteLine("Initialize called");
			if (_ocrConfiguration.Language != null)
			{
				_ocrConfiguration.Language = _ocrConfiguration.Language.Replace("miLANG_", "").Replace("_", " ");
			}
			return true;
		}

		/// <summary>
		///     Implementation of the IGreenshotPlugin.Shutdown
		/// </summary>
		public void Shutdown()
		{
			Log.Debug().WriteLine("Shutdown");
		}

		/// <summary>
		///     Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure()
		{
            // TODO
			if (false) //!HasModi())
			{
				MessageBox.Show("Sorry, is seems that Microsoft Office Document Imaging (MODI) is not installed, therefor the OCR Plugin cannot work.");
				return;
			}
			var settingsForm = new SettingsForm(Enum.GetNames(typeof(ModiLanguage)), _ocrConfiguration);
			var result = settingsForm.ShowDialog();
			if (result == DialogResult.OK)
			{
				// "Re"set hotkeys
				//IniConfig.Save();
			}
		}

		private void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_ocrMenuItem != null)
				{
					_ocrMenuItem.Dispose();
					_ocrMenuItem = null;
				}
			}
		}

	}
}