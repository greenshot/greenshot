//  Greenshot - a free and open source screenshot tool
//  Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
//  For more information see: http://getgreenshot.org/
//  The Greenshot project is hosted on GitHub: https://github.com/greenshot
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 1 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Addons;
using Dapplo.Log;
using Greenshot.Addon.Configuration;
using Greenshot.Addon.Core;
using Greenshot.Core;
using Greenshot.Core.Interfaces;

#endregion

namespace Greenshot.Addon.ExternalCommand
{
	/// <summary>
	///     An Plugin to run commands after an image was written
	/// </summary>
	[StartupAction(StartupOrder = (int) GreenshotStartupOrder.Addon)]
	public class ExternalCommandPlugin : IStartupAction
	{
		private const string MsPaint = "MS Paint";
		private const string PaintDotNet = "Paint.NET";
		private static readonly LogSource Log = new LogSource();

		[Import]
		private ICoreConfiguration CoreConfiguration { get; set; }

		[Import]
		private IExternalCommandConfiguration ExternalCommandConfiguration { get; set; }

		[Import]
		private IExternalCommandLanguage ExternalCommandLanguage { get; set; }

		[Import]
		private IServiceExporter ServiceExporter { get; set; }

		[Import]
		private IServiceLocator ServiceLocator { get; set; }

		/// <summary>
		///     Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure()
		{
			new SettingsForm().ShowDialog();
		}

		/// <summary>
		///     Initialize
		/// </summary>
		/// <param name="token"></param>
		public Task StartAsync(CancellationToken token = new CancellationToken())
		{
			AfterLoad(ExternalCommandConfiguration);
			// Make sure the defaults are set
			//iniConfig.AfterLoad<IExternalCommandConfiguration>(AfterLoad);

			return Task.Run(() =>
			{
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

				foreach (string command in ExternalCommandConfiguration.Commands)
				{
					var settings = new CommandSettings(command);
					var externalCommandDestination = new ExternalCommandDestination(settings);
					ServiceLocator.FillImports(externalCommandDestination);
					ServiceExporter.Export<IDestination>(externalCommandDestination);
				}
			}, token);
		}

		/// <summary>
		///     Fix the properties
		///     TODO: Fix for BUG-1908: Store a flag were can see if the user has manually removed one of the defaults.
		///     This way we can always check for Paint.NET (or other defaults) and add them, unless they are removed manually.
		///     Another possible way to fix this bug, is to have a wizard... or a way to download settings.
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
						config.Commandline[PaintDotNet] = paintDotNetPath;
						config.Argument[PaintDotNet] = "\"{0}\"";
						config.RunInbackground[PaintDotNet] = true;
					}
				}
				catch
				{
					// ignored
				}

				try
				{
					var paintPath = PathHelper.GetExePath("pbrush.exe");
					var hasPaint = !string.IsNullOrEmpty(paintPath) && File.Exists(paintPath);
					if (hasPaint && !config.Commands.Contains(MsPaint))
					{
						config.Commands.Add(MsPaint);
						config.Commandline[MsPaint] = paintPath;
						config.Argument[MsPaint] = "\"{0}\"";
						config.RunInbackground[MsPaint] = true;
					}
				}
				catch
				{
					// ignored
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
			if (!ExternalCommandConfiguration.RunInbackground.ContainsKey(command))
			{
				Log.Warn().WriteLine("Found missing runInbackground for {0}", command);
				// Fix it
				ExternalCommandConfiguration.RunInbackground.Add(command, true);
			}
			if (!ExternalCommandConfiguration.Argument.ContainsKey(command))
			{
				Log.Warn().WriteLine("Found missing argument for {0}", command);
				// Fix it
				ExternalCommandConfiguration.Argument.Add(command, "{0}");
			}
			if (!ExternalCommandConfiguration.Commandline.ContainsKey(command))
			{
				Log.Warn().WriteLine("Found missing commandline for {0}", command);
				return false;
			}
			if (File.Exists(ExternalCommandConfiguration.Commandline[command]))
			{
				return true;
			}
			Log.Warn().WriteLine("Found 'invalid' commandline {0} for command {1}", ExternalCommandConfiguration.Commandline[command], command);
			return false;
		}
	}
}