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

namespace Greenshot.Base.Interfaces.Video
{
    /// <summary>
    /// Lifecycle states for an active or completed video recording session.
    /// </summary>
    public enum RecordingState
    {
        /// <summary>
        /// Initial state before capture starts.
        /// </summary>
        Unstarted,

        /// <summary>
        /// Actively capturing and encoding frames.
        /// </summary>
        Recording,

        /// <summary>
        /// Recording is paused. Presentation timestamps are frozen and no frames are committed.
        /// </summary>
        Paused,

        /// <summary>
        /// Finalizing capture stream and flushing container metadata (e.g. MP4 moov atom).
        /// </summary>
        Finalizing,

        /// <summary>
        /// Recording has completed successfully and the output file is ready.
        /// </summary>
        Stopped,

        /// <summary>
        /// Recording was cancelled by user; output file is discarded.
        /// </summary>
        Cancelled,

        /// <summary>
        /// An unrecoverable error occurred during recording.
        /// </summary>
        Failed
    }
}
