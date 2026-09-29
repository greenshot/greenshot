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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Interfaces.Plugin;
using Greenshot.Plugin.Office.OfficeExport;
using Greenshot.Plugin.Office.OfficeExport.Entities;

namespace Greenshot.Plugin.Office.Destinations
{
    /// <summary>
    /// Add the capture to a OneNote page.
    /// </summary>
    public class OneNoteDestination : OfficeDestinationBase
    {
        private const int ICON_APPLICATION = 0;
        public const string DESIGNATION = "OneNote";
        private static readonly string exePath;
        private readonly OneNotePage page;
        private readonly OneNoteExporter _oneNoteExporter = new OneNoteExporter();

        static OneNoteDestination()
        {
            exePath = OfficeUtils.GetOfficeExePath("ONENOTE.EXE") ?? PluginUtils.GetExePath("ONENOTE.EXE");
            if (exePath != null && File.Exists(exePath))
            {
                WindowDetails.AddProcessToExcludeFromFreeze("onenote");
            }
            else
            {
                exePath = null;
            }
        }

        public OneNoteDestination()
        {
        }

        public OneNoteDestination(OneNotePage page)
        {
            this.page = page;
        }

        public override string Designation => DESIGNATION;

        public override DestinationDescriptor Descriptor => new DestinationDescriptor(page == null ? "Microsoft OneNote" : page.DisplayName, 4,
            IconKeyFor(exePath, ICON_APPLICATION), hasDynamicDestinations: page == null);

        public override bool IsAvailableFor(ICaptureDetails metadata) => base.IsAvailableFor(metadata) && exePath != null;

        public override async ValueTask<IReadOnlyList<IDestination>> GetDynamicDestinationsAsync(ICaptureDetails metadata, CancellationToken cancellationToken)
        {
            if (page != null)
            {
                return await base.GetDynamicDestinationsAsync(metadata, cancellationToken).ConfigureAwait(false);
            }

            var pages = await RunOnOfficeAsync(() => _oneNoteExporter.GetPages().ToList(), cancellationToken).ConfigureAwait(false);
            return pages.Select(onenotePage => (IDestination) new OneNoteDestination(onenotePage)).ToList();
        }

        public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            var imageSize = await GetImageSizeAsync(request, cancellationToken).ConfigureAwait(false);
            var png = await request.Source.EncodeAsync(new SurfaceOutputSettings(OutputFormat.png, 100, false), cancellationToken).ConfigureAwait(false);
            string title = request.Metadata?.Title;
            bool exported = await RunOnOfficeAsync(() => page == null
                ? _oneNoteExporter.ExportToNewPage(png, imageSize, title)
                : _oneNoteExporter.ExportToPage(png, imageSize, page), cancellationToken).ConfigureAwait(false);
            return exported ? ExportResult.Succeeded() : ExportResult.Failed("Export to OneNote failed");
        }
    }
}
