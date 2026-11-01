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
using Dapplo.Ini;
using Dapplo.Windows.Shell32.Enums;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Languages;
using Greenshot.Base.Wpf;
using Greenshot.Viewer.ViewModels;
using Greenshot.Viewer.Views;

namespace Greenshot.Viewer
{
    /// <summary>
    /// Shows the capture in a viewer window: a small window with only the capture, e.g. to keep it on top to compare it.
    /// The viewer keeps its own copy of the capture, it isn't saved.
    /// </summary>
    public class ViewerDestination : DestinationBase
    {
        public override string Designation => nameof(WellKnownDestinations.Viewer);

        public override DestinationDescriptor Descriptor => new DestinationDescriptor(
            Texts.Settings.DestinationViewer, 3, DestinationIcons.Stock(StockIconId.ImageFiles));

        public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            if (!request.Ui.IsInteractive)
            {
                // Nobody to look at it
                return ExportResult.Declined;
            }

            // The viewer gets its own copy: the editor or the flow keeps the original
            await request.Source.UseSurfaceAsync(surface => Show(surface.Clone(), request.Metadata), cancellationToken).ConfigureAwait(false);
            // The viewer only shows the capture, it isn't saved
            return ExportResult.Succeeded(clearsModified: false);
        }

        /// <summary>
        /// Runs on the UI thread: show the copy in a new viewer window, which owns it from now on
        /// </summary>
        private static bool Show(ISurface copy, ICaptureDetails captureDetails)
        {
            var configuration = IniConfigRegistry.GetSection<IViewerConfiguration>();
            if (!configuration.UnsavedHintShown)
            {
                configuration.UnsavedHintShown = true;
                ThemedMessageBox.Show(Texts.Core.ViewerUnsavedHint, Texts.Settings.DestinationViewer);
            }

            try
            {
                new ViewerWindow(new ViewerViewModel(copy, captureDetails, configuration)).Show();
            }
            catch
            {
                copy.Dispose();
                throw;
            }

            return true;
        }
    }
}
