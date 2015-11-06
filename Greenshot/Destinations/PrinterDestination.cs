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
using log4net;
using System;
using System.Threading;
using System.Threading.Tasks;
using GreenshotPlugin.Interfaces;
using System.ComponentModel.Composition;
using System.Windows.Media.Imaging;
using GreenshotPlugin.Extensions;
using GreenshotPlugin.Interfaces.Destination;
using System.Drawing.Printing;
using System.Collections.Generic;
using Greenshot.Helpers;

namespace Greenshot.Destinations
{
	/// <summary>
	/// Description of PrinterDestination.
	/// </summary>
	[Destination(PrinterDesignation)]
	public sealed class PrinterDestination : AbstractDestination
	{
		private const string PrinterDesignation = "Printer";
		private static readonly ILog LOG = LogManager.GetLogger(typeof (PrinterDestination));
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
			Export = async (capture, token) => await ExportCaptureAsync(capture, null, token);
			Icon = PrinterIcon;
		}

		/// <summary>
		/// Load the current editors to export to
		/// </summary>
		/// <param name="token"></param>
		/// <returns>Task</returns>
		public override Task Refresh(CancellationToken token = new CancellationToken())
		{
			Children.Clear();

			PrinterSettings settings = new PrinterSettings();
			string defaultPrinter = settings.PrinterName;
			var printers = new List<string>();

			foreach (string printer in PrinterSettings.InstalledPrinters)
			{
				printers.Add(printer);
			}
			printers.Sort(delegate (string p1, string p2) {
				if (defaultPrinter.Equals(p1))
				{
					return -1;
				}
				if (defaultPrinter.Equals(p2))
				{
					return 1;
				}
				return p1.CompareTo(p2);
			});

			foreach (var printer in printers)
			{
				var printerDestination = new PrinterDestination
				{
					Text = printer,
					Export = async (capture, exportToken) => await ExportCaptureAsync(capture, printer, exportToken),
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
					PrinterSettings printerSettings = null;
					if (!string.IsNullOrEmpty(printerName))
					{
						using (var printHelper = new PrintHelper(capture, capture.CaptureDetails))
						{
							printerSettings = printHelper.PrintTo(printerName);
						}
					}
					else
					{
						using (var printHelper = new PrintHelper(capture, capture.CaptureDetails))
						{
							printerSettings = printHelper.PrintWithDialog();
						}
					}
				}, token, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
			}
			catch (Exception e)
			{
				LOG.Error(e);
				returnValue.NotificationType = NotificationTypes.Fail;
				returnValue.ErrorText = e.Message;
				returnValue.Text = GreenshotLanguage.PrintError;
            }

			return returnValue;
		}
	}
}