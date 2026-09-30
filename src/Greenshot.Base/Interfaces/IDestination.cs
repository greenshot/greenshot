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
using System.Threading;
using System.Threading.Tasks;

namespace Greenshot.Base.Interfaces
{
    /// <summary>
    /// A destination for a capture (roadmap section 5.1). Destinations run on the thread pool and never touch UI directly:
    /// dialogs, notifications and progress go through <see cref="ExportRequest.Ui"/> (IUserInteraction).
    /// </summary>
    public interface IDestination
    {
        /// <summary>
        /// Simple "designation" like "File", "Editor" etc, used to store the configuration
        /// </summary>
        string Designation { get; }

        /// <summary>
        /// How the destination is presented: display name, icon key, priority, shortcut (UI neutral types).
        /// </summary>
        DestinationDescriptor Descriptor { get; }

        /// <summary>
        /// Is the destination available for the capture (null: in general, e.g. for settings)?
        /// Must be cheap and non-blocking, it is called while building menus.
        /// </summary>
        bool IsAvailableFor(ICaptureDetails metadata);

        /// <summary>
        /// The dynamic destinations (e.g. the open Word documents), empty when the descriptor says there are none.
        /// </summary>
        ValueTask<IReadOnlyList<IDestination>> GetDynamicDestinationsAsync(ICaptureDetails metadata, CancellationToken cancellationToken);

        /// <summary>
        /// Export the capture. Cancellation throws an OperationCanceledException, a user who declines returns <see cref="ExportResult.Declined"/>.
        /// </summary>
        Task<ExportResult> ExportAsync(ExportRequest request, CancellationToken cancellationToken);
    }
}
