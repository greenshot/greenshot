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

using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;

using GreenshotPlugin.Core;
using Greenshot.Plugin;
using Greenshot.Helpers;
using log4net;
using System.Threading;
using System.Threading.Tasks;
using System;
using Dapplo.Config.Ini;
using Dapplo.Config.Language;
using GreenshotPlugin.Configuration;

namespace Greenshot.Destinations
{
	/// <summary>
	/// Description of PrinterDestination.
	/// </summary>
	public class PrinterDestination : AbstractDestination {
		private static readonly ILog LOG = LogManager.GetLogger(typeof(PrinterDestination));
		private static readonly ICoreConfiguration conf = IniConfig.Current.Get<ICoreConfiguration>();
		private static readonly IGreenshotLanguage language = LanguageLoader.Current.Get<IGreenshotLanguage>();
		public const string DESIGNATION = "Printer";
		private readonly string _printerName;

		public PrinterDestination() {
		}

		public PrinterDestination(string printerName) {
			_printerName = printerName;
		}
		public override string Designation {
			get {
				return DESIGNATION;
			}
		}

		public override string Description {
			get {
				if (_printerName != null) {
					return language.SettingsDestinationPrinter + " - " + _printerName;
				}
				return language.SettingsDestinationPrinter;
			}
		}

		public override int Priority {
			get {
				return 2;
			}
		}

		public override Keys EditorShortcutKeys {
			get {
				return Keys.Control | Keys.P;
			}
		}

		public override Image DisplayIcon {
			get {
				return GreenshotResources.GetImage("Printer.Image");
			}
		}

		public override bool IsDynamic {
			get {
				return true;
			}
		}

		/// <summary>
		/// Create destinations for all the installed printers
		/// </summary>
		/// <returns>IEnumerable<IDestination></returns>
		public override IEnumerable<IDestination> DynamicDestinations() {
			PrinterSettings settings = new PrinterSettings();
			string defaultPrinter = settings.PrinterName;
			List<string> printers = new List<string>();

			foreach (string printer in PrinterSettings.InstalledPrinters) {
				printers.Add(printer);
			}
			printers.Sort(delegate(string p1, string p2) {
				if(defaultPrinter.Equals(p1)) {
					return -1;
				}
				if(defaultPrinter.Equals(p2)) {
					return 1;
				}
				return p1.CompareTo(p2);
			});
			foreach(string printer in printers) {
				yield return new PrinterDestination(printer);
			}
		}

		/// <summary>
		/// Export the capture to the printer
		/// </summary>
		/// <param name="manuallyInitiated"></param>
		/// <param name="surface"></param>
		/// <param name="captureDetails"></param>
		/// <returns>ExportInformation</returns>
		public override async Task<ExportInformation> ExportCaptureAsync(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails, CancellationToken token = default(CancellationToken)) {
			var exportInformation = new ExportInformation {
				DestinationDesignation = Designation,
				DestinationDescription = Description
			};
			try {
				await Task.Factory.StartNew(() => {
					PrinterSettings printerSettings = null;
					if (!string.IsNullOrEmpty(_printerName)) {
						using (var printHelper = new PrintHelper(surface, captureDetails)) {
							printerSettings = printHelper.PrintTo(_printerName);
						}
					} else if (!manuallyInitiated) {
						var settings = new PrinterSettings();
						using (var printHelper = new PrintHelper(surface, captureDetails)) {
							printerSettings = printHelper.PrintTo(settings.PrinterName);
						}
					} else {
						using (var printHelper = new PrintHelper(surface, captureDetails)) {
							printerSettings = printHelper.PrintWithDialog();
						}
					}
					if (printerSettings != null) {
						exportInformation.ExportMade = true;
					}
				}, token, TaskCreationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());

			} catch (Exception ex) {
				exportInformation.ErrorMessage = ex.Message;
			}

			ProcessExport(exportInformation, surface);
			return exportInformation;
		}
	}
}
