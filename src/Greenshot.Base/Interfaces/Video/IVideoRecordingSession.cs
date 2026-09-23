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
using System.Threading.Tasks;

namespace Greenshot.Base.Interfaces.Video
{
    /// <summary>
    /// Represents an active or completed video recording session, providing tracking and runtime controls.
    /// </summary>
    public interface IVideoRecordingSession : IDisposable
    {
        /// <summary>
        /// Current lifecycle state of the recording session.
        /// </summary>
        RecordingState State { get; }

        /// <summary>
        /// Total recorded video duration (excluding any time spent while paused).
        /// </summary>
        TimeSpan Duration { get; }

        /// <summary>
        /// The destination file path where the video is being written.
        /// </summary>
        string OutputFilePath { get; }

        /// <summary>
        /// The options used to configure this recording session.
        /// </summary>
        VideoCaptureOptions Options { get; }

        /// <summary>
        /// Pauses the recording. Frames are no longer encoded and presentation timeline is frozen.
        /// </summary>
        Task PauseAsync();

        /// <summary>
        /// Resumes recording after being paused.
        /// </summary>
        Task ResumeAsync();

        /// <summary>
        /// Stops recording, flushes all encoded frames, writes container headers, and finalizes the video file.
        /// </summary>
        /// <returns>Metadata describing the completed video file.</returns>
        Task<VideoRecordingResult> StopAsync();

        /// <summary>
        /// Aborts and cancels the recording session, deleting any partial or incomplete output file.
        /// </summary>
        Task CancelAsync();

        /// <summary>
        /// Fired when the recording state changes (e.g. from Recording to Paused or Stopped).
        /// </summary>
        event EventHandler<RecordingStateChangedEventArgs> StateChanged;

        /// <summary>
        /// Fired periodically (e.g. once per second) as recording duration advances.
        /// </summary>
        event EventHandler<RecordingDurationChangedEventArgs> DurationChanged;

        /// <summary>
        /// Fired when an error occurs during capture or encoding.
        /// </summary>
        event EventHandler<RecordingErrorEventArgs> ErrorOccurred;
    }
}
