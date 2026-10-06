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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base;
using Greenshot.Base.Core;
using Greenshot.Base.Core.Export;
using Greenshot.Base.Interfaces;
using Greenshot.Configuration;
using Greenshot.Base.Languages;

namespace Greenshot.Destinations
{
    /// <summary>
    /// The PickerDestination shows a context menu with all possible destinations, so the user can "pick" one
    /// </summary>
    public class PickerDestination : DestinationBase
    {
        public override string Designation => nameof(WellKnownDestinations.Picker);

        public override DestinationDescriptor Descriptor => new DestinationDescriptor(Texts.Settings.DestinationPicker, 1);

        /// <summary>
        /// Export the capture with the destination picker: until an export succeeds or the user closes the picker.
        /// </summary>
        public override async Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken)
        {
            var destinations = DestinationHelper.GetAllDestinations()
                .Where(destination => !Designation.Equals(destination.Designation) && destination.IsAvailableFor(request.Metadata))
                .ToList();

            while (true)
            {
                var picked = await request.Ui.PickDestinationAsync(destinations, request.Metadata, cancellationToken).ConfigureAwait(false);
                if (picked == null)
                {
                    return ExportResult.Declined;
                }

                var result = await DestinationExporter.ExportAsync(picked, request.Source, request.Metadata, true, request.Ui, cancellationToken).ConfigureAwait(false);
                if (result.IsSucceeded)
                {
                    // The caller applies the result (messages, surface state) for the picked destination
                    return result.WithTarget(result.Target ?? picked.Descriptor.DisplayName).WithExportedBy(picked);
                }

                // Export cancelled or failed: show the problem, and the picker again
                await ExportResultHandler.ApplyAsync(picked, result, request.Source, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
