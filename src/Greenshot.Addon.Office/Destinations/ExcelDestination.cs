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

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Greenshot.Addon.Office.OfficeExport;
using Greenshot.Addons;
using Greenshot.Addons.Components;
using Greenshot.Addons.Core;
using Greenshot.Addons.Interfaces;
using Greenshot.Addons.Interfaces.Plugin;
using Greenshot.Gfx;

namespace Greenshot.Addon.Office.Destinations
{
    /// <summary>
    ///     Description of PowerpointDestination.
    /// </summary>
    [Destination("Excel", DestinationOrder.Excel)]
    public class ExcelDestination : AbstractDestination
	{
	    private readonly ExportNotification _exportNotification;
	    private const int IconApplication = 0;
		private const int IconWorkbook = 1;
		private readonly string _exePath;
		private readonly string _workbookName;

        /// <summary>
        /// Constructor used to wire dependencies
        /// </summary>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="greenshotLanguage">IGreenshotLanguage</param>
        /// <param name="exportNotification">ExportNotification</param>
		public ExcelDestination(
	        ICoreConfiguration coreConfiguration,
	        IGreenshotLanguage greenshotLanguage,
	        ExportNotification exportNotification
        ) : base(coreConfiguration, greenshotLanguage)
        {
            _exportNotification = exportNotification;
            _exePath = PluginUtils.GetExePath("EXCEL.EXE");
		    if (_exePath != null && !File.Exists(_exePath))
		    {
		        _exePath = null;
		    }
        }

        /// <summary>
        /// protected constructor to accept a workbook name too
        /// </summary>
        /// <param name="workbookName">string</param>
        /// <param name="coreConfiguration">ICoreConfiguration</param>
        /// <param name="greenshotLanguage">IGreenshotLanguage</param>
        /// <param name="exportNotification">ExportNotification</param>
		protected ExcelDestination(string workbookName,
	        ICoreConfiguration coreConfiguration,
	        IGreenshotLanguage greenshotLanguage,
		    ExportNotification exportNotification
        ) : this(coreConfiguration, greenshotLanguage, exportNotification)
        {
			_workbookName = workbookName;
		}

        /// <inherit />
		public override string Description => _workbookName ?? "Microsoft Excel";

        /// <inherit />
		public override bool IsDynamic => true;

        /// <inherit />
		public override bool IsActive => base.IsActive && _exePath != null;

        /// <inherit />
		public override IBitmapWithNativeSupport GetDisplayIcon(double dpi)
		{
			return PluginUtils.GetCachedExeIcon(_exePath, !string.IsNullOrEmpty(_workbookName) ? IconWorkbook : IconApplication, dpi > 100);
		}

        /// <inherit />
		public override IEnumerable<IDestination> DynamicDestinations()
		{
			foreach (var workbookName in ExcelExporter.GetWorkbooks())
			{
				yield return new ExcelDestination(workbookName, CoreConfiguration, GreenshotLanguage, _exportNotification);
			}
		}

        /// <inherit />
	    protected override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
		{
			var exportInformation = new ExportInformation(Designation, Description);
			var createdFile = false;
			var imageFile = captureDetails.Filename;
			if (imageFile == null || surface.Modified || !Regex.IsMatch(imageFile, @".*(\.png|\.gif|\.jpg|\.jpeg|\.tiff|\.bmp)$"))
			{
				imageFile = ImageOutput.SaveNamedTmpFile(surface, captureDetails, new SurfaceOutputSettings(CoreConfiguration).PreventGreenshotFormat());
				createdFile = true;
			}
			if (_workbookName != null)
			{
				ExcelExporter.InsertIntoExistingWorkbook(_workbookName, imageFile, surface.Screenshot.Size);
			}
			else
			{
				ExcelExporter.InsertIntoNewWorkbook(imageFile, surface.Screenshot.Size);
			}
			exportInformation.ExportMade = true;
		    _exportNotification.NotifyOfExport(this, exportInformation, surface);
            // Cleanup imageFile if we created it here, so less tmp-files are generated and left
            if (createdFile)
			{
				ImageOutput.DeleteNamedTmpFile(imageFile);
			}
			return exportInformation;
		}
	}
}