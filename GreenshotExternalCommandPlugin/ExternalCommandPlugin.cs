﻿/*
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

using Dapplo.Config.Ini;
using Dapplo.Config.Language;
using Greenshot.Plugin;
using GreenshotPlugin.Configuration;
using GreenshotPlugin.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExternalCommand;
using System.ComponentModel.Composition;
using Dapplo.Addons;

namespace GreenshotExternalCommandPlugin
{
	/// <summary>
	/// An Plugin to run commands after an image was written
	/// </summary>
	[Plugin("External command", Configurable = true)]
	[StartupAction]
	public class ExternalCommandPlugin : IConfigurablePlugin, IStartupAction
	{
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof (ExternalCommandPlugin));
		private const string MSPAINT = "MS Paint";
		private const string PAINTDOTNET = "Paint.NET";
		private static ICoreConfiguration _coreConfig;
		private static IExternalCommandLanguage _language;
		private static IExternalCommandConfiguration _config;
		private ToolStripMenuItem _itemPlugInRoot;

		[Import]
		public IGreenshotHost GreenshotHost
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
				if (_itemPlugInRoot != null)
				{
					_itemPlugInRoot.Dispose();
					_itemPlugInRoot = null;
				}
			}
		}

		public IEnumerable<IDestination> Destinations()
		{
			foreach (string command in _config.Commands)
			{
				yield return new ExternalCommandDestination(command);
			}
		}

		public IEnumerable<IProcessor> Processors()
		{
			yield break;
		}

		/// <summary>
		/// Check and eventually fix the command settings
		/// </summary>
		/// <param name="command"></param>
		/// <returns>false if the command is not correctly configured</returns>
		private bool IsCommandValid(string command)
		{
			if (!_config.RunInbackground.ContainsKey(command))
			{
				LOG.WarnFormat("Found missing runInbackground for {0}", command);
				// Fix it
				_config.RunInbackground.Add(command, true);
			}
			if (!_config.Argument.ContainsKey(command))
			{
				LOG.WarnFormat("Found missing argument for {0}", command);
				// Fix it
				_config.Argument.Add(command, "{0}");
			}
			if (!_config.Commandline.ContainsKey(command))
			{
				LOG.WarnFormat("Found missing commandline for {0}", command);
				return false;
			}
			if (!File.Exists(_config.Commandline[command]))
			{
				LOG.WarnFormat("Found 'invalid' commandline {0} for command {1}", _config.Commandline[command], command);
				return false;
			}
			return true;
		}

		/// <summary>
		/// Initialize
		/// </summary>
		/// <param name="token"></param>
		public async Task StartAsync(CancellationToken token = new CancellationToken())
		{
			var iniConfig = IniConfig.Current;

			// Make sure the defaults are set
			iniConfig.AfterLoad<IExternalCommandConfiguration>(AfterLoad);
			_coreConfig = iniConfig.Get<ICoreConfiguration>();
			_config = await iniConfig.RegisterAndGetAsync<IExternalCommandConfiguration>(token);
			_language = await LanguageLoader.Current.RegisterAndGetAsync<IExternalCommandLanguage>(token);

			IList<string> commandsToDelete = new List<string>();
			// Check configuration
			foreach (string command in _config.Commands)
			{
				if (!IsCommandValid(command))
				{
					commandsToDelete.Add(command);
				}
			}

			// cleanup
			foreach (string command in commandsToDelete)
			{
				_config.RunInbackground.Remove(command);
				_config.Commandline.Remove(command);
				_config.Argument.Remove(command);
				_config.Commands.Remove(command);
			}

			_itemPlugInRoot = new ToolStripMenuItem();
			_itemPlugInRoot.Tag = GreenshotHost;
			OnIconSizeChanged(this, new PropertyChangedEventArgs("IconSize"));
			OnLanguageChanged(this, null);
			_itemPlugInRoot.Click += (sender, eventArgs) => Configure();

			PluginUtils.AddToContextMenu(GreenshotHost, _itemPlugInRoot);
			_language.PropertyChanged += OnLanguageChanged;
			_coreConfig.PropertyChanged += OnIconSizeChanged;
		}

		/// <summary>
		/// Fix icon reference
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OnIconSizeChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "IconSize")
			{
				try
				{
					string exePath = PluginUtils.GetExePath("cmd.exe");
					if (exePath != null && File.Exists(exePath))
					{
						_itemPlugInRoot.Image = PluginUtils.GetCachedExeIcon(exePath, 0);
					}
				}
				catch (Exception ex)
				{
					LOG.Warn("Couldn't get the cmd.exe image", ex);
				}
			}
		}

		private void OnLanguageChanged(object sender, EventArgs e)
		{
			if (_itemPlugInRoot != null)
			{
				_itemPlugInRoot.Text = _language.ContextmenuConfigure;
			}
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure()
		{
			new SettingsForm().ShowDialog();
		}

		/// <summary>
		/// Fix the properties
		/// </summary>
		private void AfterLoad(IExternalCommandConfiguration config)
		{
			if (!config.DefaultsAdded)
			{
				config.DefaultsAdded = true;

				try
				{
					var paintDotNetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Paint.NET\PaintDotNet.exe");
					var hasPaintDotNet = !string.IsNullOrEmpty(paintDotNetPath) && File.Exists(paintDotNetPath);
					if (hasPaintDotNet && !config.Commands.Contains(PAINTDOTNET))
					{
						config.Commands.Add(PAINTDOTNET);
						config.Commandline.Add(PAINTDOTNET, paintDotNetPath);
						config.Argument.Add(PAINTDOTNET, "\"{0}\"");
						config.RunInbackground.Add(PAINTDOTNET, true);
					}
				}
				catch
				{
				}

				try
				{
					var paintPath = PluginUtils.GetExePath("pbrush.exe");
					var hasPaint = !string.IsNullOrEmpty(paintPath) && File.Exists(paintPath);
					if (hasPaint && !config.Commands.Contains(MSPAINT))
					{
						config.Commands.Add(MSPAINT);
						config.Commandline.Add(MSPAINT, paintPath);
						config.Argument.Add(MSPAINT, "\"{0}\"");
						config.RunInbackground.Add(MSPAINT, true);
					}
				}
				catch
				{
				}
			}
		}
	}
}