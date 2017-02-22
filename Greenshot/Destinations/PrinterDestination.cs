#region Greenshot GNU General Public License

// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2017 Thomas Braun, Jens Klingen, Robin Krom
// 
// For more information see: http://getgreenshot.org/
// The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 1 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

#region Usings

using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using Greenshot.Configuration;
using Greenshot.Helpers;
using GreenshotPlugin.Core;
using GreenshotPlugin.Interfaces;

#endregion

namespace Greenshot.Destinations
{
	/// <summary>
	///     Description of PrinterDestination.
	/// </summary>
	public class PrinterDestination : AbstractDestination
	{
		public const string DESIGNATION = "Printer";
		public readonly string printerName;

		public PrinterDestination()
		{
		}

		public PrinterDestination(string printerName)
		{
			this.printerName = printerName;
		}

		public override string Designation
		{
			get { return DESIGNATION; }
		}

		public override string Description
		{
			get
			{
				if (printerName != null)
				{
					return Language.GetString(LangKey.settings_destination_printer) + " - " + printerName;
				}
				return Language.GetString(LangKey.settings_destination_printer);
			}
		}

		public override int Priority
		{
			get { return 2; }
		}

		public override Keys EditorShortcutKeys
		{
			get { return Keys.Control | Keys.P; }
		}

		public override Image DisplayIcon
		{
			get { return GreenshotResources.getImage("Printer.Image"); }
		}

		public override bool IsDynamic
		{
			get { return true; }
		}

		/// <summary>
		///     Create destinations for all the installed printers
		/// </summary>
		/// <returns>IEnumerable of IDestination</returns>
		public override IEnumerable<IDestination> DynamicDestinations()
		{
			var settings = new PrinterSettings();
			var defaultPrinter = settings.PrinterName;
			var printers = new List<string>();

			foreach (string printer in PrinterSettings.InstalledPrinters)
			{
				printers.Add(printer);
			}
			printers.Sort(delegate(string p1, string p2)
			{
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
				yield return new PrinterDestination(printer);
			}
		}

		/// <summary>
		///     Export the capture to the printer
		/// </summary>
		/// <param name="manuallyInitiated"></param>
		/// <param name="surface"></param>
		/// <param name="captureDetails"></param>
		/// <returns>ExportInformation</returns>
		public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);
			PrinterSettings printerSettings;
			if (!string.IsNullOrEmpty(printerName))
			{
				using (var printHelper = new PrintHelper(surface, captureDetails))
				{
					printerSettings = printHelper.PrintTo(printerName);
				}
			}
			else if (!manuallyInitiated)
			{
				var settings = new PrinterSettings();
				using (var printHelper = new PrintHelper(surface, captureDetails))
				{
					printerSettings = printHelper.PrintTo(settings.PrinterName);
				}
			}
			else
			{
				using (var printHelper = new PrintHelper(surface, captureDetails))
				{
					printerSettings = printHelper.PrintWithDialog();
				}
			}
			if (printerSettings != null)
			{
				exportInformation.ExportMade = true;
			}

			ProcessExport(exportInformation, surface);
			return exportInformation;
		}
	}
}