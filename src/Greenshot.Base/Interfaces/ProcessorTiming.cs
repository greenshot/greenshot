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

namespace Greenshot.Base.Interfaces
{
    /// <summary>
    /// Declares when a processor prefers to run within the capture pipeline,
    /// relative to any interactive selection step (e.g. region/window picker).
    ///
    /// This is used by <c>ProcessorExecutionStep</c> to filter processors when a step
    /// is configured with a specific timing. A recipe can contain multiple
    /// <c>Processors</c> steps at different positions to run each phase explicitly.
    /// </summary>
    public enum ProcessorTiming
    {
        /// <summary>
        /// Run before interactive selection so results (e.g. QR code hotspots, OCR lines)
        /// are available while the CaptureForm is shown to the user.
        /// </summary>
        PreSelection,

        /// <summary>
        /// Run after interactive selection / crop has been confirmed.
        /// This is the default for all processors that do not override <see cref="PreSelection"/>.
        /// </summary>
        PostSelection
    }
}
