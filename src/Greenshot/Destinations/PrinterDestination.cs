/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2021 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
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
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Configuration;
using Greenshot.Helpers;

namespace Greenshot.Destinations
{
    /// <summary>
    /// Description of PrinterDestination.
    /// </summary>
    public class PrinterDestination : AbstractDestination
    {
        private readonly string _printerName;

        public PrinterDestination()
        {
        }

        public PrinterDestination(string printerName) => _printerName = printerName;

        public override string Designation => nameof(WellKnownDestinations.Printer);

        public override string Description => _printerName != null
                    ? Language.GetString(LangKey.settings_destination_printer) + " - " + _printerName
                    : Language.GetString(LangKey.settings_destination_printer);

        public override int Priority => 2;

        public override Keys EditorShortcutKeys => Keys.Control | Keys.P;

        public override Image DisplayIcon => GreenshotResources.GetImage("Printer.Image");

        public override bool IsDynamic => true;

        /// <summary>
        /// Create destinations for all the installed printers
        /// </summary>
        /// <returns>IEnumerable of IDestination</returns>
        public override IEnumerable<IDestination> DynamicDestinations()
        {
            PrinterSettings settings = new();
            string defaultPrinter = settings.PrinterName;
            List<string> printers = new();

            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                printers.Add(printer);
            }

            printers.Sort((string p1, string p2) =>
            {
                if (defaultPrinter.Equals(p1))
                {
                    return -1;
                }

                return defaultPrinter.Equals(p2) ? 1 : string.CompareOrdinal(p1, p2);
            });
            foreach (string printer in printers)
            {
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
        public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
        {
            ExportInformation exportInformation = new(Designation, Description);
            PrinterSettings printerSettings;
            if (!string.IsNullOrEmpty(_printerName))
            {
                using PrintHelper printHelper = new(surface, captureDetails);
                printerSettings = printHelper.PrintTo(_printerName);
            }
            else if (!manuallyInitiated)
            {
                PrinterSettings settings = new();
                using PrintHelper printHelper = new(surface, captureDetails);
                printerSettings = printHelper.PrintTo(settings.PrinterName);
            }
            else
            {
                using PrintHelper printHelper = new(surface, captureDetails);
                printerSettings = printHelper.PrintWithDialog();
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