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
using Dapplo.Windows.Desktop;
using Greenshot.Base.Interfaces;

namespace Greenshot.Base.Recipes.Pipeline
{
    /// <summary>
    /// Presents the interactive selection overlay to the user (e.g. CaptureWindow), see roadmap section 5.3.
    /// </summary>
    public interface IInteractiveCaptureSelector
    {
        /// <summary>
        /// True while a selection overlay is open.
        /// </summary>
        bool IsSelecting { get; }

        /// <summary>
        /// Bring an open selection overlay to the front (a second capture was requested while it is open).
        /// </summary>
        void BringToFront();

        /// <summary>
        /// Let the user select a region, window or text on the capture. Called from the thread pool, the implementation
        /// shows its UI through the IUiDispatcher and completes when the overlay closes.
        /// </summary>
        /// <param name="fullscreenCapture">The captured desktop image.</param>
        /// <param name="visibleWindows">Visible windows for window snapping.</param>
        /// <param name="initialMode">Initial selection mode (Region, Window, Text).</param>
        /// <param name="initialTool">Id of the tool to start with (e.g. one of a plugin), null or unknown: the tool of the initial mode.</param>
        /// <param name="cancellationToken">Cancelling closes the overlay and throws an OperationCanceledException.</param>
        /// <returns>The selection, or null when the user declined (Esc) or another selection is already open.</returns>
        Task<SelectionResult> SelectAsync(
            ICapture fullscreenCapture,
            IReadOnlyList<IInteropWindow> visibleWindows,
            CaptureMode initialMode,
            string initialTool,
            CancellationToken cancellationToken = default);
    }
}
