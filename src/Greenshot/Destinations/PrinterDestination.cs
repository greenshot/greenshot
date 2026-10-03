/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Configuration;
using Greenshot.Helpers;
using Greenshot.Base.Core.FileFormat;
using Greenshot.Base.Languages;

namespace Greenshot.Destinations
{
    /// <summary>
    /// What to print: shown (print dialog, print options) and printed by the view registered for it, on the UI thread.
    /// </summary>
    public sealed class PrintRequest : IDialogViewModel<bool>
    {
        public PrintRequest(Image image, ICaptureDetails captureDetails, string printerName, PrintOptions printOptions, bool showPrintDialog)
        {
            Image = image;
            CaptureDetails = captureDetails;
            PrinterName = printerName;
            PrintOptions = printOptions;
            ShowPrintDialog = showPrintDialog;
        }

        /// <summary>
        /// The rendered capture, borrowed
        /// </summary>
        public Image Image { get; }

        public ICaptureDetails CaptureDetails { get; }

        /// <summary>
        /// Print to this printer, null: the default printer or the one chosen in the print dialog
        /// </summary>
        public string PrinterName { get; }

        public PrintOptions PrintOptions { get; }

        public bool ShowPrintDialog { get; }

        /// <summary>
        /// The view: prints on the UI thread, returns true when printed.
        /// </summary>
        public static bool Print(PrintRequest request)
        {
            using var printHelper = new PrintHelper(request.Image, request.CaptureDetails, request.PrintOptions);
            PrinterSettings printerSettings;
            if (!string.IsNullOrEmpty(request.PrinterName))
            {
                printerSettings = printHelper.PrintTo(request.PrinterName);
            }
            else if (!request.ShowPrintDialog)
            {
                printerSettings = printHelper.PrintTo(new PrinterSettings().PrinterName);
            }
            else
            {
                printerSettings = printHelper.PrintWithDialog();
            }

            return printerSettings != null;
        }
    }

    /// <summary>
    /// Description of PrinterDestination.
    /// </summary>
    public class PrinterDestination : DestinationBase
    {
        private readonly string _printerName;
        private readonly PrintOptions _printOptions;

        public PrinterDestination()
        {
        }

        public PrinterDestination(string printerName, PrintOptions printOptions = null)
        {
            _printerName = printerName;
            _printOptions = printOptions;
        }

        public override string Designation => nameof(WellKnownDestinations.Printer);

        public override DestinationDescriptor Descriptor
        {
            get
            {
                string name = Texts.Settings.DestinationPrinter;
                if (_printerName != null)
                {
                    name += " - " + _printerName;
                }

                return new DestinationDescriptor(name, 2, DestinationIcons.Resource("Printer.Image"), "Ctrl+P", hasDynamicDestinations: _printerName == null);
            }
        }

        /// <summary>
        /// Create destinations for all the installed printers, the default printer first
        /// </summary>
        public override ValueTask<IReadOnlyList<IDestination>> GetDynamicDestinationsAsync(ICaptureDetails metadata, CancellationToken cancellationToken)
        {
            string defaultPrinter = new PrinterSettings().PrinterName;
            var printers = PrinterSettings.InstalledPrinters.Cast<string>().ToList();
            printers.Sort((p1, p2) =>
            {
                if (defaultPrinter.Equals(p1)) return -1;
                if (defaultPrinter.Equals(p2)) return 1;
                return string.Compare(p1, p2, StringComparison.Ordinal);
            });
            IReadOnlyList<IDestination> destinations = printers.Select(printer => (IDestination)new PrinterDestination(printer)).ToList();
            return new ValueTask<IReadOnlyList<IDestination>>(destinations);
        }

        /// <summary>
        /// Export the capture to the printer
        /// </summary>
        public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            // The print applies its own effects (after the print options were chosen): no color reduction here
            var settings = new SurfaceOutputSettings(WellKnownFileFormats.Png, 100, false) { DisableReduceColors = true };
            using var lease = await request.Source.RenderAsync(settings, cancellationToken).ConfigureAwait(false);
            var printRequest = new PrintRequest(lease.Image, request.Metadata, _printerName, _printOptions, request.ManuallyInitiated);
            bool printed = await request.Ui.ShowDialogAsync(printRequest, cancellationToken).ConfigureAwait(false);
            return printed ? ExportResult.Succeeded() : ExportResult.Declined;
        }
    }
}
