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
using System.Threading;
using System.Threading.Tasks;
using GreenshotPlugin.Interfaces;
using System.ComponentModel.Composition;
using System.Windows.Media.Imaging;
using GreenshotPlugin.Extensions;
using GreenshotPlugin.Interfaces.Destination;
using System.Drawing.Printing;
using Greenshot.Helpers;
using System.Linq;

namespace Greenshot.Destinations
{
	/// <summary>
	/// Description of PrinterDestination.
	/// </summary>
	[Destination(PrinterDesignation)]
	public sealed class PrinterDestination : AbstractDestination
	{
		private const string PrinterDesignation = "Printer";
		private static readonly Serilog.ILogger LOG = Serilog.Log.Logger.ForContext(typeof(PrinterDestination));
		private static readonly BitmapSource PrinterIcon;
		static PrinterDestination()
		{
			using (var printerIcon = GreenshotResources.GetImage("Printer.Image"))
			{
				PrinterIcon = printerIcon.ToBitmapSource();
			}
        }
		[Import]
		private ICoreConfiguration CoreConfiguration
		{
			get;
			set;
		}

		[Import]
		private IGreenshotLanguage GreenshotLanguage
		{
			get;
			set;
		}

		protected override void Initialize()
		{
			base.Initialize();
			Text = GreenshotLanguage.SettingsDestinationPrinter;
			Designation = PrinterDesignation;
			Export = async (exportContext, capture, token) => await ExportCaptureAsync(capture, null, token);
			Icon = PrinterIcon;
		}

		/// <summary>
		/// Load the current editors to export to
		/// </summary>
		/// <param name="caller1"></param>
		/// <param name="token"></param>
		/// <returns>Task</returns>
		public override Task RefreshAsync(IExportContext caller1, CancellationToken token = default(CancellationToken))
		{
			Children.Clear();

			var settings = new PrinterSettings();
			string defaultPrinter = settings.PrinterName;
			var printers = PrinterSettings.InstalledPrinters.Cast<string>().OrderBy(x => x).ToList();
			var defaultIndex = printers.IndexOf(defaultPrinter);
			if (defaultIndex > 0)
			{
				printers.RemoveAt(defaultIndex);
				printers.Insert(0, defaultPrinter);
			}
			foreach (var printer in printers)
			{
				var printerDestination = new PrinterDestination
				{
					Text = printer,
					Export = async (caller, capture, exportToken) => await ExportCaptureAsync(capture, printer, exportToken),
					Icon = PrinterIcon,
					CoreConfiguration = CoreConfiguration,
					GreenshotLanguage = GreenshotLanguage
				};
				Children.Add(printerDestination);
			}
			return Task.FromResult(true);
		}

		private async Task<INotification> ExportCaptureAsync(ICapture capture, string printerName, CancellationToken token = default(CancellationToken))
		{
			var returnValue = new Notification
			{
				NotificationType = NotificationTypes.Success,
				Source = PrinterDesignation,
				SourceType = SourceTypes.Destination,
				Text = Text
			};

			try
			{
				await Task.Factory.StartNew(() =>
				{
					if (!string.IsNullOrEmpty(printerName))
					{
						using (var printHelper = new PrintHelper(capture, capture.CaptureDetails))
						{
							printHelper.PrintTo(printerName);
						}
					}
					else
					{
						using (var printHelper = new PrintHelper(capture, capture.CaptureDetails))
						{
							printHelper.PrintWithDialog();
						}
					}
				}, token, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
			}
			catch (Exception e)
			{
				LOG.Error(e, "Printer export failed");
				returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = e.Message;
				returnValue.Text = GreenshotLanguage.PrintError;
            }

			return returnValue;
		}
	}
}