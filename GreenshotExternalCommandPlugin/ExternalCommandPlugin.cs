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
using GreenshotPlugin.Interfaces;
using GreenshotPlugin.Interfaces.Plugin;

namespace GreenshotExternalCommandPlugin
{
	/// <summary>
	/// An Plugin to run commands after an image was written
	/// </summary>
	[Plugin("External command", Configurable = true)]
	[StartupAction]
	public class ExternalCommandPlugin : IConfigurablePlugin, IStartupAction
	{
		private static readonly Serilog.ILogger LOG = Serilog.Log.Logger.ForContext(typeof(ExternalCommandPlugin));
		private const string MsPaint = "MS Paint";
		private const string PaintDotNet = "Paint.NET";
		private ToolStripMenuItem _itemPlugInRoot;

		[Import]
		private IGreenshotHost GreenshotHost
		{
			get;
			set;
		}

		[Import]
		private IExternalCommandLanguage ExternalCommandLanguage
		{
			get;
			set;
		}

		[Import]
		private ICoreConfiguration CoreConfiguration
		{
			get;
			set;
		}

		[Import]
		private IExternalCommandConfiguration ExternalCommandConfiguration
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

		public IEnumerable<ILegacyDestination> Destinations()
		{
			foreach (string command in ExternalCommandConfiguration.Commands)
			{
				yield return new ExternalCommandLegacyDestination(command);
			}
		}

		/// <summary>
		/// Check and eventually fix the command settings
		/// </summary>
		/// <param name="command"></param>
		/// <returns>false if the command is not correctly configured</returns>
		private bool IsCommandValid(string command)
		{
			if (!ExternalCommandConfiguration.RunInbackground.ContainsKey(command))
			{
				LOG.Warning("Found missing runInbackground for {0}", command);
				// Fix it
				ExternalCommandConfiguration.RunInbackground.Add(command, true);
			}
			if (!ExternalCommandConfiguration.Argument.ContainsKey(command))
			{
				LOG.Warning("Found missing argument for {0}", command);
				// Fix it
				ExternalCommandConfiguration.Argument.Add(command, "{0}");
			}
			if (!ExternalCommandConfiguration.Commandline.ContainsKey(command))
			{
				LOG.Warning("Found missing commandline for {0}", command);
				return false;
			}
			if (!File.Exists(ExternalCommandConfiguration.Commandline[command]))
			{
				LOG.Warning("Found 'invalid' commandline {0} for command {1}", ExternalCommandConfiguration.Commandline[command], command);
				return false;
			}
			return true;
		}

		/// <summary>
		/// Initialize
		/// </summary>
		/// <param name="token"></param>
		public Task StartAsync(CancellationToken token = new CancellationToken())
		{
			AfterLoad(ExternalCommandConfiguration);
			// Make sure the defaults are set
			//iniConfig.AfterLoad<IExternalCommandConfiguration>(AfterLoad);

			IList<string> commandsToDelete = new List<string>();
			// Check configuration
			foreach (string command in ExternalCommandConfiguration.Commands)
			{
				if (!IsCommandValid(command))
				{
					commandsToDelete.Add(command);
				}
			}

			// cleanup
			foreach (string command in commandsToDelete)
			{
				ExternalCommandConfiguration.RunInbackground.Remove(command);
				ExternalCommandConfiguration.Commandline.Remove(command);
				ExternalCommandConfiguration.Argument.Remove(command);
				ExternalCommandConfiguration.Commands.Remove(command);
			}

			_itemPlugInRoot = new ToolStripMenuItem();
			_itemPlugInRoot.Tag = GreenshotHost;
			OnIconSizeChanged(this, new PropertyChangedEventArgs("IconSize"));
			OnExternalCommandLanguageChanged(this, null);
			_itemPlugInRoot.Click += (sender, eventArgs) => Configure();

			PluginUtils.AddToContextMenu(GreenshotHost, _itemPlugInRoot);
			ExternalCommandLanguage.PropertyChanged += OnExternalCommandLanguageChanged;
			CoreConfiguration.PropertyChanged += OnIconSizeChanged;
			return Task.FromResult(true);
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
					LOG.Warning("Couldn't get the cmd.exe image", ex);
				}
			}
		}

		private void OnExternalCommandLanguageChanged(object sender, EventArgs e)
		{
			if (_itemPlugInRoot != null)
			{
				_itemPlugInRoot.Text = ExternalCommandLanguage.ContextmenuConfigure;
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
					if (hasPaintDotNet && !config.Commands.Contains(PaintDotNet))
					{
						config.Commands.Add(PaintDotNet);
						config.Commandline.Add(PaintDotNet, paintDotNetPath);
						config.Argument.Add(PaintDotNet, "\"{0}\"");
						config.RunInbackground.Add(PaintDotNet, true);
					}
				}
				catch
				{
					// ignored
				}

				try
				{
					var paintPath = PluginUtils.GetExePath("pbrush.exe");
					var hasPaint = !string.IsNullOrEmpty(paintPath) && File.Exists(paintPath);
					if (hasPaint && !config.Commands.Contains(MsPaint))
					{
						config.Commands.Add(MsPaint);
						config.Commandline.Add(MsPaint, paintPath);
						config.Argument.Add(MsPaint, "\"{0}\"");
						config.RunInbackground.Add(MsPaint, true);
					}
				}
				catch
				{
					// ignored
				}
			}
		}
	}
}