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

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Plugin.Office.OfficeExport;

namespace Greenshot.Plugin.Office.Destinations
{
    /// <summary>
    /// Insert the capture into an Excel workbook.
    /// </summary>
    public class ExcelDestination : OfficeDestinationBase
    {
        private const int IconApplication = 0;
        private const int IconWorkbook = 1;
        private static readonly string ExePath;
        private readonly string _workbookName;

        static ExcelDestination()
        {
            ExePath = OfficeUtils.GetOfficeExePath("EXCEL.EXE") ?? PluginUtils.GetExePath("EXCEL.EXE");

            if (ExePath != null && !File.Exists(ExePath))
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

        public override DestinationDescriptor Descriptor => new DestinationDescriptor(_workbookName ?? "Microsoft Excel", 5,
            IconKeyFor(ExePath, !string.IsNullOrEmpty(_workbookName) ? IconWorkbook : IconApplication), hasDynamicDestinations: _workbookName == null);

        public override bool IsAvailableFor(ICaptureDetails metadata) => base.IsAvailableFor(metadata) && ExePath != null;

        public override ValueTask<IReadOnlyList<IDestination>> GetDynamicDestinationsAsync(ICaptureDetails metadata, CancellationToken cancellationToken) =>
            _workbookName != null
                ? base.GetDynamicDestinationsAsync(metadata, cancellationToken)
                : GetDynamicDestinationsAsync(ExcelExporter.GetWorkbooks, workbookName => new ExcelDestination(workbookName), cancellationToken);

        public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            var imageSize = await GetImageSizeAsync(request, cancellationToken).ConfigureAwait(false);
            var (imageFile, createdFile) = await GetImageFileAsync(request, cancellationToken).ConfigureAwait(false);
            try
            {
                await Office.RunAsync(() =>
                {
                    if (_workbookName != null)
                    {
                        ExcelExporter.InsertIntoExistingWorkbook(_workbookName, imageFile, imageSize);
                    }
                    else
                    {
                        ExcelExporter.InsertIntoNewWorkbook(imageFile, imageSize);
                    }
                }, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                // Cleanup imageFile if we created it here, so less tmp-files are generated and left
                if (createdFile)
                {
                    ImageIO.DeleteNamedTmpFile(imageFile);
                }
            }

            return ExportResult.Succeeded();
        }
    }
}
