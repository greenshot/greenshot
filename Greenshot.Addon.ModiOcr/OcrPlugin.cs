/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Addons;
using Greenshot.Addon.Extensions;
using Greenshot.Addon.Interfaces.Destination;
using Greenshot.Addon.Interfaces.Plugin;

namespace Greenshot.Addon.ModiOcr
{
	/// <summary>
	/// OCR Plugin Greenshot
	/// </summary>
	[Plugin("OCR", Configurable = true)]
	[StartupAction]
    public class OcrPlugin : IConfigurablePlugin, IStartupAction
	{
		private static readonly Serilog.ILogger Log = Serilog.Log.Logger.ForContext(typeof(OcrPlugin));
		private static readonly string OcrCommand = Path.Combine(Path.GetDirectoryName(typeof(OcrPlugin).Assembly.Location), "greenshotocrcommand.exe");
		private ToolStripMenuItem _ocrMenuItem = new ToolStripMenuItem();

		[Import]
		private IOcrConfiguration OcrConfiguration
		{
			get;
			set;
		}

		[Import]
		private IServiceLocator ServiceLocator
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
				if (_ocrMenuItem != null)
				{
					_ocrMenuItem.Dispose();
					_ocrMenuItem = null;
				}
			}
		}

		/// <summary>
		/// Initialize
		/// </summary>
		/// <param name="token"></param>
		public Task StartAsync(CancellationToken token = new CancellationToken())
		{
			if (!HasModi())
			{
				Log.Warning("No MODI found!");
			}
			else if (OcrConfiguration.Language != null)
			{
				OcrConfiguration.Language = OcrConfiguration.Language.Replace("miLANG_", "").Replace("_", " ");
				var ocrDestination = new OcrDestination();
				ServiceLocator.FillImports(ocrDestination);
				ServiceLocator.Export<IDestination>(ocrDestination);
			}
			
			return Task.FromResult(true);
		}

		/// <summary>
		/// Implementation of the IPlugin.Configure
		/// </summary>
		public void Configure()
		{
			if (!HasModi())
			{
				MessageBox.Show("Greenshot OCR", "Sorry, is seems that Microsoft Office Document Imaging (MODI) is not installed, therefor the OCR Plugin cannot work.", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			var settingsForm = new SettingsForm(Enum.GetNames(typeof (ModiLanguage)), OcrConfiguration);
			DialogResult result = settingsForm.ShowDialog();
			if (result == DialogResult.OK)
			{
				// "Re"set hotkeys
			}
		}

		/// <summary>
		/// Check if MODI is installed and available
		/// </summary>
		/// <returns></returns>
		private bool HasModi()
		{
			try
			{
				using (var process = Process.Start(OcrCommand, "-c"))
				{
					if (process != null)
					{
						Task.Run(async () => await process.WaitForExitAsync()).Wait();
						return process.ExitCode == 0;
					}
				}
			}
			catch (Exception e)
			{
				Log.Debug("Error trying to initiate MODI: {0}", e.Message);
			}
			Log.Information("No Microsoft Office Document Imaging (MODI) found, disabling OCR");
			return false;
		}
	}
}