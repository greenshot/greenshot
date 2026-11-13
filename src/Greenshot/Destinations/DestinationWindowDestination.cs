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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Ini;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Export;
using Greenshot.Base.Interfaces;
using Greenshot.Base.Languages;
using Greenshot.Base.Threading;
using Greenshot.Recipes.Views;

namespace Greenshot.Destinations
{
    /// <summary>
    /// Asks for the destination every time, like the <see cref="PickerDestination"/>, but in the destination window:
    /// with a preview of the capture and a search. The editor recipes can change the capture there first, like in the editor.
    /// </summary>
    public class DestinationWindowDestination : DestinationBase
    {
        public override string Designation => nameof(WellKnownDestinations.DestinationWindow);

        public override DestinationDescriptor Descriptor => new DestinationDescriptor(Texts.Settings.DestinationWindow, 1, DestinationIcons.Greenshot);

        /// <summary>
        /// For beta testers the destination window takes the place of the menu of "Ask for destination every time" (the picker)
        /// </summary>
        internal static bool ReplacesPicker => IniConfigRegistry.GetSection<ICoreConfiguration>()?.IsBetaTester == true;

        /// <summary>
        /// Export the capture to the destination chosen in the window: until an export succeeds or the user closes the window.
        /// A failed export shows the window again, with the error.
        /// </summary>
        public override Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            if (!request.Ui.IsInteractive)
            {
                // Nobody to choose
                return Task.FromResult(ExportResult.Declined);
            }

            return ChooseAndExportAsync(request.Source, DestinationHelper.GetChoices(request.Metadata),
                (destination, source) => DestinationExporter.ExportAsync(destination, source, request.Metadata, true, request.Ui, cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Shows the destination window until an export to the chosen destination succeeds or the user closes the window; a failed
        /// export shows the window again, with the error. The window works on a copy of the capture, which the editor recipes can
        /// replace: then the chosen destination gets a copy of the changed capture, else the source itself.
        /// </summary>
        /// <param name="source">The capture</param>
        /// <param name="destinations">The destinations to choose from</param>
        /// <param name="export">Exports to the chosen destination from the source it gets</param>
        /// <param name="cancellationToken">Closes the window</param>
        /// <returns>The result of the successful export, or Declined when the window was closed</returns>
        internal static async Task<ExportResult> ChooseAndExportAsync(IExportSource source, IReadOnlyList<IDestination> destinations,
            Func<IDestination, IExportSource, Task<ExportResult>> export, CancellationToken cancellationToken)
        {
            var ui = SimpleServiceProvider.Current.GetInstance<IUiDispatcher>(isOptional: true) ?? InlineUiDispatcher.Instance;
            var capture = await source.UseSurfaceAsync(surface => new EditableCapture(surface.Clone()), cancellationToken).ConfigureAwait(false);
            try
            {
                string error = null;
                while (true)
                {
                    var chosen = await ui.InvokeAsync(() => DynamicDestinationWindow.Choose(null, capture, destinations, error, cancellationToken), cancellationToken)
                        .ConfigureAwait(false);
                    if (chosen == null)
                    {
                        return ExportResult.Declined;
                    }

                    var result = capture.IsChanged
                        ? await ExportChangedAsync(chosen, capture, export, ui, cancellationToken).ConfigureAwait(false)
                        : await export(chosen, source).ConfigureAwait(false);
                    if (result.IsSucceeded)
                    {
                        // The caller applies the result (messages, surface state) for the chosen destination
                        return result.WithTarget(result.Target ?? chosen.Descriptor.DisplayName).WithExportedBy(result.ExportedBy ?? chosen);
                    }

                    // Declined (e.g. the save dialog was closed) or failed: the window comes again, with the error
                    error = result.Error;
                }
            }
            finally
            {
                await ui.InvokeAsync(capture.Dispose, CancellationToken.None).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Exports the capture changed by a recipe: each try gets its own copy, a destination which keeps it (the editor) owns it
        /// </summary>
        private static async Task<ExportResult> ExportChangedAsync(IDestination destination, EditableCapture capture, Func<IDestination, IExportSource, Task<ExportResult>> export,
            IUiDispatcher ui, CancellationToken cancellationToken)
        {
            var copy = await ui.InvokeAsync(() => capture.Surface.Clone(), cancellationToken).ConfigureAwait(false);
            ExportResult result = null;
            try
            {
                await ThreadPoolSwitch.SwitchToThreadPoolAsync();
                using var source = new SurfaceExportSource(copy, ui, true);
                result = await export(destination, source).ConfigureAwait(false);
                return result;
            }
            finally
            {
                if (result?.KeepsCapture != true)
                {
                    await ui.InvokeAsync(copy.Dispose, CancellationToken.None).ConfigureAwait(false);
                }
            }
        }
    }
}
