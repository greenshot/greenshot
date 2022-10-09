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

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Plugin.Office.OfficeExport;

namespace Greenshot.Plugin.Office.Destinations
{
    /// <summary>
    /// Description of PowerpointDestination.
    /// </summary>
    public class ExcelDestination : AbstractDestination
    {
        private const int IconApplication = 0;
        private const int IconWorkbook = 1;
        private static readonly string ExePath;
        private readonly string _workbookName;

        static ExcelDestination()
        {
            ExePath = OfficeUtils.GetOfficeExePath("EXCEL.EXE") ?? PluginUtils.GetExePath("EXCEL.EXE");

            if (ExePath != null && File.Exists(ExePath))
            {
                WindowDetails.AddProcessToExcludeFromFreeze("excel");
            }
            else
            {
                ExePath = null;
            }
        }

        public ExcelDestination()
        {
        }

        public ExcelDestination(string workbookName)
        {
            _workbookName = workbookName;
        }

        public override string Designation => "Excel";

        public override string Description => _workbookName ?? "Microsoft Excel";

        public override int Priority => 5;

        public override bool IsDynamic => true;

        public override bool IsActive => base.IsActive && ExePath != null;

        public override Image DisplayIcon => PluginUtils.GetCachedExeIcon(ExePath, !string.IsNullOrEmpty(_workbookName) ? IconWorkbook : IconApplication);

        public override IEnumerable<IDestination> DynamicDestinations()
        {
            foreach (string workbookName in ExcelExporter.GetWorkbooks())
            {
                yield return new ExcelDestination(workbookName);
            }
        }

        public override ExportInformation ExportCapture(bool manuallyInitiated, ISurface surface, ICaptureDetails captureDetails)
        {
            ExportInformation exportInformation = new ExportInformation(Designation, Description);
            bool createdFile = false;
            string imageFile = captureDetails.Filename;
            if (imageFile == null || surface.Modified || !Regex.IsMatch(imageFile, @".*(\.png|\.gif|\.jpg|\.jpeg|\.tiff|\.bmp)$"))
            {
                imageFile = ImageIO.SaveNamedTmpFile(surface, captureDetails, new SurfaceOutputSettings().PreventGreenshotFormat());
                createdFile = true;
            }

            if (_workbookName != null)
            {
                ExcelExporter.InsertIntoExistingWorkbook(_workbookName, imageFile, surface.Image.Size);
            }
            else
            {
                ExcelExporter.InsertIntoNewWorkbook(imageFile, surface.Image.Size);
            }

            exportInformation.ExportMade = true;
            ProcessExport(exportInformation, surface);
            // Cleanup imageFile if we created it here, so less tmp-files are generated and left
            if (createdFile)
            {
                ImageIO.DeleteNamedTmpFile(imageFile);
            }

            return exportInformation;
        }
    }
}