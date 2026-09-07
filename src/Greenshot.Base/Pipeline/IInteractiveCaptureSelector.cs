/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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
using Greenshot.Base.Core;
using Greenshot.Base.Core.Enums;
using Greenshot.Base.Interfaces;

namespace Greenshot.Base.Pipeline
{
    /// <summary>
    /// Contract for presenting an interactive selection overlay to the user (e.g. CaptureForm).
    /// </summary>
    public interface IInteractiveCaptureSelector
    {
        /// <summary>
        /// Prompts the user to interactively select a region or window from the fullscreen capture.
        /// </summary>
        /// <param name="fullscreenCapture">The captured desktop image.</param>
        /// <param name="visibleWindows">List of visible windows for geometry snapping.</param>
        /// <param name="initialMode">Initial selection mode (Region, Window, Text).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The selection result.</returns>
        Task<SelectionResult> SelectAsync(
            ICapture fullscreenCapture,
            List<WindowDetails> visibleWindows,
            CaptureMode initialMode,
            CancellationToken cancellationToken = default);
    }
}
