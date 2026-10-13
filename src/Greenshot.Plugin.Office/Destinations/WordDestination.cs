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
    /// Insert the capture into a Word document.
    /// </summary>
    public class WordDestination : OfficeDestinationBase
    {
        private const int IconApplication = 0;
        private const int IconDocument = 1;
        private static readonly string ExePath = GetComServerPath("Word.Application");
        private readonly string _documentCaption;
        private readonly WordExporter _wordExporter = new WordExporter();

        /// <summary>
        /// The path of Word, null when it's not installed
        /// </summary>
        internal static string WordExePath => ExePath;

        public WordDestination()
        {
        }

        public WordDestination(string wordCaption)
        {
            _documentCaption = wordCaption;
        }

        public override string Designation => "Word";

        public override DestinationDescriptor Descriptor => new DestinationDescriptor(_documentCaption ?? "Microsoft Word", 4,
            IconKeyFor(ExePath, !string.IsNullOrEmpty(_documentCaption) ? IconDocument : IconApplication), hasDynamicDestinations: _documentCaption == null);

        public override bool IsAvailableFor(ICaptureDetails metadata) => base.IsAvailableFor(metadata) && ExePath != null;

        public override ValueTask<IReadOnlyList<IDestination>> GetDynamicDestinationsAsync(ICaptureDetails metadata, CancellationToken cancellationToken) =>
            _documentCaption != null
                ? base.GetDynamicDestinationsAsync(metadata, cancellationToken)
                : GetDynamicDestinationsAsync(_wordExporter.GetWordDocuments, wordCaption => new WordDestination(wordCaption), cancellationToken);

        public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            if (_documentCaption == null && !request.ManuallyInitiated)
            {
                var documents = await RunOnOfficeAsync(() => _wordExporter.GetWordDocuments().ToList(), cancellationToken).ConfigureAwait(false);
                if (documents.Count > 0)
                {
                    var destinations = new List<IDestination>
                    {
                        new WordDestination()
                    };
                    destinations.AddRange(documents.Select(document => new WordDestination(document)));
                    // A new document or one of the open ones
                    return await PickAndExportAsync(request, destinations, cancellationToken).ConfigureAwait(false);
                }
            }

            var (tmpFile, _) = await GetImageFileAsync(request, cancellationToken).ConfigureAwait(false);
            bool exported = await RunOnOfficeAsync(() => _documentCaption != null
                ? _wordExporter.InsertIntoExistingDocument(_documentCaption, tmpFile)
                : _wordExporter.InsertIntoNewDocument(tmpFile), cancellationToken).ConfigureAwait(false);
            return exported ? ExportResult.Succeeded() : ExportResult.Failed("Export to Word failed");
        }
    }
}
