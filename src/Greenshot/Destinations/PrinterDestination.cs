// Greenshot - a free and open source screenshot tool
// Copyright (C) 2007-2019 Thomas Braun, Jens Klingen, Robin Krom
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

using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Windows.Forms;
using Autofac.Features.OwnedInstances;
using Greenshot.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Resources;
using Greenshot.Gfx;
using Greenshot.Helpers;

namespace Greenshot.Destinations
{
    /// <summary>
    ///     Description of PrinterDestination.
    /// </summary>
    [Destination("Printer", DestinationOrder.Printer)]
    public class PrinterDestination : AbstractDestination
	{
	    private readonly IGreenshotLanguage _greenshotLanguage;
	    private readonly ExportNotification _exportNotification;
	    private readonly Func<ISurface, ICaptureDetails, Owned<PrintHelper>> _printHelperFactory;
	    private readonly string _printerName;

	    public PrinterDestination(ICoreConfiguration coreConfiguration,
	        IGreenshotLanguage greenshotLanguage,
	        ExportNotification exportNotification,
	        Func<ISurface, ICaptureDetails, Owned<PrintHelper>> printHelperFactory
        ) : base(coreConfiguration, greenshotLanguage)
        {
	        _greenshotLanguage = greenshotLanguage;
	        _exportNotification = exportNotification;
	        _printHelperFactory = printHelperFactory;
	    }

        protected PrinterDestination(
            ICoreConfiguration coreConfiguration,
            IGreenshotLanguage greenshotLanguage,
            ExportNotification exportNotification,
            Func<ISurface, ICaptureDetails, Owned<PrintHelper>> printHelperFactory,
            string printerName) : this(coreConfiguration, greenshotLanguage, exportNotification, printHelperFactory)
		{
			_printerName = printerName;
		}

	    public override string Description
		{
			get
			{
			    var printerDestination = _greenshotLanguage.SettingsDestinationPicker;
				if (_printerName != null)
				{
                    return printerDestination + " - " + _printerName;
				}
				return printerDestination;
			}
		}

	    public override Keys EditorShortcutKeys => Keys.Control | Keys.P;

	    public override IBitmapWithNativeSupport DisplayIcon => GreenshotResources.Instance.GetBitmap("Printer.Image");

	    public override bool IsDynamic => true;

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
				return string.Compare(p1, p2, StringComparison.Ordinal);
			});
			foreach (var printer in printers)
			{
				yield return new PrinterDestination(CoreConfiguration, GreenshotLanguage, _exportNotification, _printHelperFactory, printer);
			}
		}

		/// <summary>
		///     Export the capture to the printer
		/// </summary>
		/// <param name="manuallyInitiated"></param>
		/// <param name="surface"></param>
		/// <param name="captureDetails"></param>
		/// <returns>ExportInformation</returns>
		protected override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);
			PrinterSettings printerSettings;
			if (!string.IsNullOrEmpty(_printerName))
			{
				using (var ownedPrintHelper = _printHelperFactory(surface, captureDetails))
				{
					printerSettings = ownedPrintHelper.Value.PrintTo(_printerName);
				}
			}
			else if (!manuallyInitiated)
			{
				var settings = new PrinterSettings();

			    using (var ownedPrintHelper = _printHelperFactory(surface, captureDetails))
			    {
			        printerSettings = ownedPrintHelper.Value.PrintTo(settings.PrinterName);
			    }
			}
			else
			{
			    using (var ownedPrintHelper = _printHelperFactory(surface, captureDetails))
			    {
			        printerSettings = ownedPrintHelper.Value.PrintWithDialog();
                }
			}
			if (printerSettings != null)
			{
				exportInformation.ExportMade = true;
			}

		    _exportNotification.NotifyOfExport(this, exportInformation, surface);
            return exportInformation;
		}
	}
}