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
using System.Linq;
using System.Windows.Forms;
using Dapplo.Log;
using Greenshot.Gfx;
using GreenshotPlugin.Addons;
using GreenshotPlugin.Core;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;

#endregion

namespace Greenshot.Addon.ExternalCommand
{
    /// <summary>
    ///     An Plugin to run commands after an image was written
    /// </summary>
    [Export(typeof(IGreenshotPlugin))]
    public sealed class ExternalCommandPlugin : IGreenshotPlugin
	{
		private static readonly LogSource Log = new LogSource();
		private readonly IExternalCommandConfiguration _externalCommandConfig;
		private readonly IGreenshotHost _greenshotHost;
		private ToolStripMenuItem _itemPlugInRoot;

        [ImportingConstructor]
	    public ExternalCommandPlugin(IGreenshotHost greenshotGreenshotHost, IExternalCommandConfiguration externalCommandConfiguration)
	    {
	        _greenshotHost = greenshotGreenshotHost;
	        _externalCommandConfig = externalCommandConfiguration;
	    }

	    public void Dispose()
	    {
	        Dispose(true);
	    }

        public IEnumerable<IDestination> Destinations()
		{
		    return _externalCommandConfig.Commands.Select(command => new ExternalCommandDestination(command));
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
			Log.Debug().WriteLine("Initialize called");

			// Check configuration & cleanup
			foreach (var command in _externalCommandConfig.Commands.Where(command => !IsCommandValid(command)).ToList())
			{
				_externalCommandConfig.Delete(command);
			}

			_itemPlugInRoot = new ToolStripMenuItem {Tag = _greenshotHost};
			_greenshotHost.ContextMenuDpiHandler.OnDpiChanged.Subscribe(OnIconSizeChanged);
			OnLanguageChanged(this, null);

			PluginUtils.AddToContextMenu(_greenshotHost, _itemPlugInRoot);
			Language.LanguageChanged += OnLanguageChanged;
			return true;
		}

		public void Shutdown()
		{
			Log.Debug().WriteLine("Shutdown");
		}

		private void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_itemPlugInRoot != null)
				{
					_itemPlugInRoot.Dispose();
					_itemPlugInRoot = null;
				}
			}
		}

		/// <summary>
		///     Check and eventually fix the command settings
		/// </summary>
		/// <param name="command"></param>
		/// <returns>false if the command is not correctly configured</returns>
		private bool IsCommandValid(string command)
		{
			if (!_externalCommandConfig.RunInbackground.ContainsKey(command))
			{
				Log.Warn().WriteLine("Found missing runInbackground for {0}", command);
				// Fix it
				_externalCommandConfig.RunInbackground.Add(command, true);
			}
			if (!_externalCommandConfig.Argument.ContainsKey(command))
			{
				Log.Warn().WriteLine("Found missing argument for {0}", command);
				// Fix it
				_externalCommandConfig.Argument.Add(command, "{0}");
			}
			if (!_externalCommandConfig.Commandline.ContainsKey(command))
			{
				Log.Warn().WriteLine("Found missing commandline for {0}", command);
				return false;
			}
			var commandline = FilenameHelper.FillVariables(_externalCommandConfig.Commandline[command], true);
			commandline = FilenameHelper.FillCmdVariables(commandline, true);

			if (File.Exists(commandline))
			{
				return true;
			}
			Log.Warn().WriteLine("Found 'invalid' commandline {0} for command {1}", _externalCommandConfig.Commandline[command], command);
			return false;
		}

		/// <summary>
		///     Fix icon reference
		/// </summary>
		/// <param name="dpi">double with DPI</param>
		private void OnIconSizeChanged(double dpi)
		{
			try
			{
				var exePath = PluginUtils.GetExePath("cmd.exe");
				if (exePath == null || !File.Exists(exePath))
				{
					return;
				}
				if (true.Equals(_itemPlugInRoot.Tag))
				{
					_itemPlugInRoot.Image?.Dispose();
				}
				var exeIcon = PluginUtils.GetCachedExeIcon(exePath, 0, dpi > 100);
				_itemPlugInRoot.Image = exeIcon.ScaleIconForDisplaying(dpi);
				_itemPlugInRoot.Tag = !Equals(exeIcon, _itemPlugInRoot.Image);
			}
			catch (Exception ex)
			{
				Log.Warn().WriteLine(ex, "Couldn't get the cmd.exe image");
			}
		}

		private void OnLanguageChanged(object sender, EventArgs e)
		{
			if (_itemPlugInRoot != null)
			{
				_itemPlugInRoot.Text = Language.GetString("externalcommand", "contextmenu_configure");
			}
		}
	}
}