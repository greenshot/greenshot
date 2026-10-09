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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Interfaces;
using Greenshot.Plugin.Office.OfficeExport;

namespace Greenshot.Plugin.Office.Destinations
{
    /// <summary>
    /// Insert the capture into a PowerPoint presentation.
    /// </summary>
    public class PowerpointDestination : OfficeDestinationBase
    {
        private const int IconApplication = 0;
        private const int IconPresentation = 1;

        private static readonly string ExePath = GetComServerPath("PowerPoint.Application");
        private readonly string _presentationName;
        private readonly PowerpointExporter _powerpointExporter = new PowerpointExporter();

        public PowerpointDestination()
        {
        }

        public PowerpointDestination(string presentationName)
        {
            _presentationName = presentationName;
        }

        public override string Designation => "Powerpoint";

        public override DestinationDescriptor Descriptor => new DestinationDescriptor(_presentationName ?? "Microsoft Powerpoint", 4,
            IconKeyFor(ExePath, !string.IsNullOrEmpty(_presentationName) ? IconPresentation : IconApplication), hasDynamicDestinations: _presentationName == null);

        public override bool IsAvailableFor(ICaptureDetails metadata) => base.IsAvailableFor(metadata) && ExePath != null;

        public override ValueTask<IReadOnlyList<IDestination>> GetDynamicDestinationsAsync(ICaptureDetails metadata, CancellationToken cancellationToken) =>
            _presentationName != null
                ? base.GetDynamicDestinationsAsync(metadata, cancellationToken)
                : GetDynamicDestinationsAsync(_powerpointExporter.GetPowerpointPresentations, presentationName => new PowerpointDestination(presentationName), cancellationToken);

        public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            if (_presentationName == null && !request.ManuallyInitiated)
            {
                var presentations = await RunOnOfficeAsync(() => _powerpointExporter.GetPowerpointPresentations().ToList(), cancellationToken).ConfigureAwait(false);
                if (presentations.Count > 0)
                {
                    var destinations = new List<IDestination>
                    {
                        new PowerpointDestination()
                    };
                    destinations.AddRange(presentations.Select(presentation => new PowerpointDestination(presentation)));
                    // A new presentation or one of the open ones
                    return await PickAndExportAsync(request, destinations, cancellationToken).ConfigureAwait(false);
                }
            }

            var imageSize = await GetImageSizeAsync(request, cancellationToken).ConfigureAwait(false);
            var (tmpFile, _) = await GetImageFileAsync(request, cancellationToken).ConfigureAwait(false);
            string title = request.Metadata?.Title;
            bool exported = await RunOnOfficeAsync(() => _presentationName != null
                ? _powerpointExporter.ExportToPresentation(_presentationName, tmpFile, imageSize, title)
                : _powerpointExporter.InsertIntoNewPresentation(tmpFile, imageSize, title), cancellationToken).ConfigureAwait(false);
            return exported ? ExportResult.Succeeded() : ExportResult.Failed("Export to Powerpoint failed");
        }
    }
}
