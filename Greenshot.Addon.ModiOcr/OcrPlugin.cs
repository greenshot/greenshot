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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dapplo.Addons;
using Dapplo.Log;
using Greenshot.Addon.Core;
using Greenshot.Addon.Extensions;
using Greenshot.Core.Interfaces;

#endregion

namespace Greenshot.Addon.ModiOcr
{
	/// <summary>
	///     OCR Plugin Greenshot
	/// </summary>
	[StartupAction(StartupOrder = (int) GreenshotStartupOrder.Addon)]
	public class OcrPlugin : IStartupAction
	{
		private static readonly LogSource Log = new LogSource();
		private static readonly string OcrCommand = Path.Combine(Path.GetDirectoryName(typeof(OcrPlugin).Assembly.Location), "ModiOcrCommand.exe");

		[Import]
		private IOcrConfiguration OcrConfiguration { get; set; }

		[Import]
		private IServiceExporter ServiceExporter { get; set; }

		[Import]
		private IServiceLocator ServiceLocator { get; set; }

		/// <summary>
		///     Implementation of the IPlugin.Configure
		/// </summary>
		public async Task Configure()
		{
			if (! await HasModiAsync())
			{
				MessageBox.Show("Greenshot OCR", "Sorry, is seems that Microsoft Office Document Imaging (MODI) is not installed, therefor the OCR Plugin cannot work.", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			var settingsForm = new SettingsForm(Enum.GetNames(typeof(ModiLanguage)), OcrConfiguration);
			DialogResult result = settingsForm.ShowDialog();
			if (result == DialogResult.OK)
			{
				// "Re"set hotkeys
			}
		}

		/// <summary>
		///     Initialize
		/// </summary>
		/// <param name="token"></param>
		public async Task StartAsync(CancellationToken token = new CancellationToken())
		{
			if (! await HasModiAsync(token))
			{
				Log.Warn().WriteLine("No MODI found!");
			}
			else if (OcrConfiguration.Language != null)
			{
				OcrConfiguration.Language = OcrConfiguration.Language.Replace("miLANG_", "").Replace("_", " ");
				var ocrDestination = new OcrDestination();
				ServiceLocator.FillImports(ocrDestination);
				ServiceExporter.Export<IDestination>(ocrDestination);
			}
		}

		/// <summary>
		///     Check if MODI is installed and available
		/// </summary>
		/// <returns></returns>
		private async Task<bool> HasModiAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			try
			{
				using (var process = Process.Start(OcrCommand, "-c"))
				{
					if (process != null)
					{
						await process.WaitForExitAsync(cancellationToken);
						return process.ExitCode == 0;
					}
				}
			}
			catch (Exception e)
			{
				Log.Debug().WriteLine("Error trying to initiate MODI: {0}", e.Message);
			}
			Log.Info().WriteLine("No Microsoft Office Document Imaging (MODI) found, disabling OCR");
			return false;
		}
	}
}