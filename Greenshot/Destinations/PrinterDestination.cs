/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2012  Thomas Braun, Jens Klingen, Robin Krom
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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;

using Greenshot.Configuration;
using GreenshotPlugin.Core;
using Greenshot.Plugin;
using Greenshot.Helpers;
using Greenshot.IniFile;

namespace Greenshot.Destinations {
	/// <summary>
	/// Description of PrinterDestination.
	/// </summary>
	public class PrinterDestination : AbstractDestination {
		private static log4net.ILog LOG = log4net.LogManager.GetLogger(typeof(PrinterDestination));
		private static CoreConfiguration conf = IniConfig.GetIniSection<CoreConfiguration>();
		public const string DESIGNATION = "Printer";
		public string printerName = null;

		public PrinterDestination() {
		}

		public PrinterDestination(string printerName) {
			this.printerName = printerName;
		}
		public override string Designation {
			get {
				return DESIGNATION;
			}
		}

		public override string Description {
			get {
				if (printerName != null) {
					return Language.GetString(LangKey.settings_destination_printer) + " - " + printerName;
				} else {
					return Language.GetString(LangKey.settings_destination_printer);
				}
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
				return GreenshotPlugin.Core.GreenshotResources.getImage("Printer.Image");
			}
		}

		public override bool isDynamic {
			get {
				return true;
			}
		}

		public override IEnumerable<IDestination> DynamicDestinations() {
			foreach (string printer in PrinterSettings.InstalledPrinters) {
				yield return new PrinterDestination(printer);
			}
		}

		public override bool ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails) {
			PrinterSettings printerSettings = null;
			using (Image image = surface.GetImageForExport()) {
				if (!string.IsNullOrEmpty(printerName)) {
					printerSettings = new PrintHelper(image, captureDetails).PrintTo(printerName);
				} else if (!manuallyInitiated) {
					PrinterSettings settings = new PrinterSettings();
					printerSettings = new PrintHelper(image, captureDetails).PrintTo(settings.PrinterName);
				} else {
					printerSettings = new PrintHelper(image, captureDetails).PrintWithDialog();
				}
				if (printerSettings != null) {
					surface.Modified = false;
					surface.SendMessageEvent(this, SurfaceMessageTyp.Info, Language.GetFormattedString(LangKey.editor_senttoprinter, printerSettings.PrinterName));
				}
			}

			return printerSettings != null;
		}
	}
}
