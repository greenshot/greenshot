/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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

using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Shell32.Enums;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Document.Editor;
using Greenshot.DocumentModel;
using Greenshot.Helpers;
using Greenshot.WpfEditor;

[assembly: GreenshotModule(typeof(WpfEditorDestination))]

namespace Greenshot.DocumentModel
{
    /// <summary>
    /// "Editor (new)": opens the capture in the editor on the document model (Greenshot.WpfEditor). The surface is mapped to a
    /// document, the old editor isn't involved. Only in Debug builds: Greenshot.csproj leaves this folder out of Release.
    /// </summary>
    public sealed class WpfEditorDestination : DestinationBase
    {
        public const string DesignationName = "WpfEditor";

        public override string Designation => DesignationName;

        public override DestinationDescriptor Descriptor => new(EditorTexts.Wpf.Destination, 1, DestinationIcons.Stock(StockIconId.ImageFiles));

        public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            if (!request.Ui.IsInteractive)
            {
                // Nobody to edit it
                return ExportResult.Declined;
            }

            await request.Source.UseSurfaceAsync(surface => Open(surface, request.Metadata), cancellationToken).ConfigureAwait(false);
            // The new editor keeps its own document, saving it there is its own business
            return ExportResult.Succeeded(clearsModified: false);
        }

        /// <summary>
        /// Runs on the UI thread: the document is a copy, the surface stays with the flow or the old editor
        /// </summary>
        private static bool Open(ISurface surface, ICaptureDetails captureDetails)
        {
            var document = SurfaceToDocument.ToDocument(surface);
            WpfEditorLauncher.Configure(WpfEditorHost.Instance);
            WpfEditorLauncher.Open(document, captureDetails?.Title ?? surface.CaptureDetails?.Title);
            return true;
        }
    }
}
