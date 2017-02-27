#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
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
using System.IO;
using System.Windows.Forms;
using GreenshotPlugin.Core;
using GreenshotPlugin.Gfx;
using GreenshotPlugin.IniFile;
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;
using log4net;

#endregion

namespace GreenshotExternalCommandPlugin
{
	/// <summary>
	///     An Plugin to run commands after an image was written
	/// </summary>
	public sealed class ExternalCommandPlugin : IGreenshotPlugin
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(ExternalCommandPlugin));
		private static readonly CoreConfiguration CoreConfig = IniConfig.GetIniSection<CoreConfiguration>();
		private static readonly ExternalCommandConfiguration ExternalCommandConfig = IniConfig.GetIniSection<ExternalCommandConfiguration>();
		private IGreenshotHost _host;
		private ToolStripMenuItem _itemPlugInRoot;
		private PluginAttribute _myAttributes;

		public void Dispose()
		{
			Dispose(true);
		}

		public IEnumerable<IDestination> Destinations()
		{
			foreach (var command in ExternalCommandConfig.Commands)
			{
				yield return new ExternalCommandDestination(command);
			}
		}

		public IEnumerable<IProcessor> Processors()
		{
			yield break;
		}

		/// <summary>
		///     Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <param name="pluginHost">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="myAttributes">My own attributes</param>
		public bool Initialize(IGreenshotHost pluginHost, PluginAttribute myAttributes)
		{
			Log.DebugFormat("Initialize called of {0}", myAttributes.Name);

			var commandsToDelete = new List<string>();
			// Check configuration
			foreach (var command in ExternalCommandConfig.Commands)
			{
				if (!IsCommandValid(command))
				{
					commandsToDelete.Add(command);
				}
			}

			// cleanup
			foreach (var command in commandsToDelete)
			{
				ExternalCommandConfig.Delete(command);
			}

			_host = pluginHost;
			_myAttributes = myAttributes;


			_itemPlugInRoot = new ToolStripMenuItem {Tag = _host};
			OnIconSizeChanged(this, new PropertyChangedEventArgs("IconSize"));
			OnLanguageChanged(this, null);
			_itemPlugInRoot.Click += ConfigMenuClick;

			PluginUtils.AddToContextMenu(_host, _itemPlugInRoot);
			Language.LanguageChanged += OnLanguageChanged;
			CoreConfig.PropertyChanged += OnIconSizeChanged;
			return true;
		}

		public void Shutdown()
		{
			Log.Debug("Shutdown of " + _myAttributes.Name);
		}

		/// <summary>
		///     Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure()
		{
			Log.Debug("Configure called");
			new SettingsForm().ShowDialog();
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
		private static bool IsCommandValid(string command)
		{
			if (!ExternalCommandConfig.RunInbackground.ContainsKey(command))
			{
				Log.WarnFormat("Found missing runInbackground for {0}", command);
				// Fix it
				ExternalCommandConfig.RunInbackground.Add(command, true);
			}
			if (!ExternalCommandConfig.Argument.ContainsKey(command))
			{
				Log.WarnFormat("Found missing argument for {0}", command);
				// Fix it
				ExternalCommandConfig.Argument.Add(command, "{0}");
			}
			if (!ExternalCommandConfig.Commandline.ContainsKey(command))
			{
				Log.WarnFormat("Found missing commandline for {0}", command);
				return false;
			}
			var commandline = FilenameHelper.FillVariables(ExternalCommandConfig.Commandline[command], true);
			commandline = FilenameHelper.FillCmdVariables(commandline, true);

			if (File.Exists(commandline))
			{
				return true;
			}
			Log.WarnFormat("Found 'invalid' commandline {0} for command {1}", ExternalCommandConfig.Commandline[command], command);
			return false;
		}

		/// <summary>
		///     Fix icon reference
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnIconSizeChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName != "IconSize")
			{
				return;
			}
			try
			{
				var exePath = PluginUtils.GetExePath("cmd.exe");
				if (exePath == null || !File.Exists(exePath))
				{
					return;
				}
				if (_itemPlugInRoot.Tag == (object) true)
				{
					_itemPlugInRoot.Image?.Dispose();
				}
				var exeIcon = PluginUtils.GetCachedExeIcon(exePath, 0);
				_itemPlugInRoot.Image = exeIcon.ScaleIconForDisplaying(96);
				_itemPlugInRoot.Tag = !Equals(exeIcon, _itemPlugInRoot.Image);
			}
			catch (Exception ex)
			{
				Log.Warn("Couldn't get the cmd.exe image", ex);
			}
		}

		private void OnLanguageChanged(object sender, EventArgs e)
		{
			if (_itemPlugInRoot != null)
			{
				_itemPlugInRoot.Text = Language.GetString("externalcommand", "contextmenu_configure");
			}
		}

		private void ConfigMenuClick(object sender, EventArgs eventArgs)
		{
			Configure();
		}
	}
}