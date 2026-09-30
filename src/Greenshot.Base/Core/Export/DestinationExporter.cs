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
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Threading;
using log4net;

namespace Greenshot.Base.Core.Export
{
    /// <summary>
    /// Exports a surface outside of a flow: from the editor, the destination picker, a plugin. The UI event is the boundary:
    /// the export itself runs on the thread pool, the result is applied to the surface on the UI thread.
    /// </summary>
    public static class DestinationExporter
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(DestinationExporter));

        /// <summary>
        /// Export the surface to the destination and apply the result to the surface. Never throws for a failed export
        /// (the result says so); cancellation throws an OperationCanceledException.
        /// </summary>
        public static async Task<ExportResult> ExportAsync(IDestination destination, ISurface surface, ICaptureDetails captureDetails, bool manuallyInitiated,
            IUiDispatcher ui = null, IUserInteraction userInteraction = null, CancellationToken cancellationToken = default)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (surface == null) throw new ArgumentNullException(nameof(surface));
            ui ??= SimpleServiceProvider.Current?.GetInstance<IUiDispatcher>(isOptional: true) ?? InlineUiDispatcher.Instance;
            userInteraction ??= UserInteraction.Current;

            // UI event → background command: destinations run on the pool (rule R10 boundary)
            await ThreadPoolSwitch.SwitchToThreadPoolAsync();
            using var source = await SurfaceExportSource.CreateAsync(surface, ui, cancellationToken).ConfigureAwait(false);
            var result = await ExportAsync(destination, source, captureDetails ?? surface.CaptureDetails, manuallyInitiated, userInteraction, cancellationToken).ConfigureAwait(false);
            await ExportResultHandler.ApplyAsync(destination, result, source, cancellationToken).ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Export the source to the destination, a failure (exception) becomes a failed result. Cancellation throws.
        /// </summary>
        public static async Task<ExportResult> ExportAsync(IDestination destination, IExportSource source, ICaptureDetails captureDetails, bool manuallyInitiated,
            IUserInteraction userInteraction, CancellationToken cancellationToken)
        {
            ThreadAssert.NotUi($"Export to {destination.Designation}");
            try
            {
                var request = new ExportRequest(source, captureDetails, manuallyInitiated, userInteraction ?? UserInteraction.Current);
                var result = await destination.ExportAsync(request, cancellationToken).ConfigureAwait(false) ?? ExportResult.Declined;
                Log.InfoFormat("Export to {0}: {1}", destination.Designation, result);
                return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Also an OperationCanceledException which isn't ours, e.g. an HTTP timeout
                Log.Error($"Export to {destination.Designation} failed", ex);
                return ExportResult.Failed(ex.Message, ex);
            }
        }

        /// <summary>
        /// Start an export from a UI event (menu, button), observed by <see cref="AsyncCommand"/>.
        /// </summary>
        public static void StartExport(IDestination destination, ISurface surface, bool manuallyInitiated = true)
        {
            if (destination == null || surface == null)
            {
                return;
            }

            AsyncCommand.Run(() => ExportAsync(destination, surface, surface.CaptureDetails, manuallyInitiated), $"Export to {destination.Designation}");
        }
    }
}
