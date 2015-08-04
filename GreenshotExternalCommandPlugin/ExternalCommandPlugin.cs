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

using Dapplo.Config.Ini;
using Greenshot.Plugin;
using GreenshotPlugin.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ExternalCommand
{
	/// <summary>
	/// An Plugin to run commands after an image was written
	/// </summary>
	public class ExternalCommandPlugin : IGreenshotPlugin
	{
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(ExternalCommandPlugin));
		private const string MSPAINT = "MS Paint";
		private const string PAINTDOTNET = "Paint.NET";
		private static CoreConfiguration coreConfig;
		private static ExternalCommandConfiguration config;
		private PluginAttribute myAttributes;
		private ToolStripMenuItem itemPlugInRoot;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (itemPlugInRoot != null)
				{
					itemPlugInRoot.Dispose();
					itemPlugInRoot = null;
				}
			}
		}

		public ExternalCommandPlugin()
		{
		}

		public IEnumerable<IDestination> Destinations()
		{
			foreach (string command in config.Commands)
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
		private bool isCommandValid(string command)
		{
			if (!config.RunInbackground.ContainsKey(command))
			{
				LOG.WarnFormat("Found missing runInbackground for {0}", command);
				// Fix it
				config.RunInbackground.Add(command, true);
			}
			if (!config.Argument.ContainsKey(command))
			{
				LOG.WarnFormat("Found missing argument for {0}", command);
				// Fix it
				config.Argument.Add(command, "{0}");
			}
			if (!config.Commandline.ContainsKey(command))
			{
				LOG.WarnFormat("Found missing commandline for {0}", command);
				return false;
			}
			if (!File.Exists(config.Commandline[command]))
			{
				LOG.WarnFormat("Found 'invalid' commandline {0} for command {1}", config.Commandline[command], command);
				return false;
			}
			return true;
		}
		/// <summary>
		/// Implementation of the IGreenshotPlugin.Initialize
		/// </summary>
		/// <param name="host">Use the IGreenshotPluginHost interface to register events</param>
		/// <param name="captureHost">Use the ICaptureHost interface to register in the MainContextMenu</param>
		/// <param name="pluginAttribute">My own attributes</param>
		public async Task<bool> InitializeAsync(IGreenshotHost pluginHost, PluginAttribute pluginAttributes, CancellationToken token = new CancellationToken())
		{
			LOG.DebugFormat("Initialize called of {0}", myAttributes.Name);
			var iniConfig = IniConfig.Get("Greenshot", "greenshot");

			// Make sure the defaults are set
			iniConfig.AfterLoad<ExternalCommandConfiguration>((conf) => AfterLoad(conf));
			coreConfig = iniConfig.Get<CoreConfiguration>();
			config = await iniConfig.RegisterAndGetAsync<ExternalCommandConfiguration>();

			IList<string> commandsToDelete = new List<string>();
			// Check configuration
			foreach (string command in config.Commands)
			{
				if (!isCommandValid(command))
				{
					commandsToDelete.Add(command);
				}
			}

			// cleanup
			foreach (string command in commandsToDelete)
			{
				config.RunInbackground.Remove(command);
				config.Commandline.Remove(command);
				config.Argument.Remove(command);
				config.Commands.Remove(command);
			}

			myAttributes = pluginAttributes;


			itemPlugInRoot = new ToolStripMenuItem();
			itemPlugInRoot.Tag = pluginHost;
			OnIconSizeChanged(this, new PropertyChangedEventArgs("IconSize"));
			OnLanguageChanged(this, null);
			itemPlugInRoot.Click += new System.EventHandler(ConfigMenuClick);

			PluginUtils.AddToContextMenu(pluginHost, itemPlugInRoot);
			Language.LanguageChanged += OnLanguageChanged;
			coreConfig.PropertyChanged += OnIconSizeChanged;
			return true;
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
						itemPlugInRoot.Image = PluginUtils.GetCachedExeIcon(exePath, 0);
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
			if (itemPlugInRoot != null)
			{
				itemPlugInRoot.Text = Language.GetString("externalcommand", "contextmenu_configure");
			}
		}

		public void Shutdown()
		{
			LOG.Debug("Shutdown of " + myAttributes.Name);
		}

		private void ConfigMenuClick(object sender, EventArgs eventArgs)
		{
			Configure();
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure()
		{
			LOG.Debug("Configure called");
			new SettingsForm().ShowDialog();
		}

		/// <summary>
		/// Supply values we can't put as defaults
		/// </summary>
		/// <param name="property">The property to return a default for</param>
		/// <returns>object with the default value for the supplied property</returns>
		public void AfterLoad(ExternalCommandConfiguration config)
		{
			if (!config.DefaultsAdded)
			{
				config.DefaultsAdded = true;

				string paintDotNetPath;
				bool hasPaintDotNet = false;
				try
				{
					paintDotNetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Paint.NET\PaintDotNet.exe");
					hasPaintDotNet = !string.IsNullOrEmpty(paintDotNetPath) && File.Exists(paintDotNetPath);
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

				string paintPath;
				bool hasPaint = false;
				try
				{
					paintPath = PluginUtils.GetExePath("pbrush.exe");
					hasPaint = !string.IsNullOrEmpty(paintPath) && File.Exists(paintPath);
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