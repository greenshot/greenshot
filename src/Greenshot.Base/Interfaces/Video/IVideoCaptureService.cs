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

using System.Threading;
using System.Threading.Tasks;

namespace Greenshot.Base.Interfaces.Video
{
    /// <summary>
    /// Service entry point for initiating hardware-accelerated video recordings.
    /// </summary>
    public interface IVideoCaptureService
    {
        /// <summary>
        /// Gets whether video capture via Windows.Graphics.Capture is supported on the current operating system.
        /// Requires Windows 10 Version 1809 (Build 17763) or higher (recommended Windows 10 2004+ / Windows 11).
        /// </summary>
        bool IsSupported { get; }

        /// <summary>
        /// Starts a new video recording session with the specified capture options.
        /// </summary>
        /// <param name="options">Configuration options specifying target, format, framerate, and audio.</param>
        /// <param name="cancellationToken">Optional token to cancel session initialization.</param>
        /// <returns>An active <see cref="IVideoRecordingSession"/> for tracking and controlling the recording.</returns>
        Task<IVideoRecordingSession> StartRecordingAsync(VideoCaptureOptions options, CancellationToken cancellationToken = default);
    }
}
