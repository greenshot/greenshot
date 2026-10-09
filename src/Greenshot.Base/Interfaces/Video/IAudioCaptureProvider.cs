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

namespace Greenshot.Base.Interfaces.Video
{
    /// <summary>
    /// Pluggable audio hook interface for audio capture sources (system loopback, microphone, or custom inputs).
    /// </summary>
    public interface IAudioCaptureProvider : IDisposable
    {
        /// <summary>
        /// Audio sample rate in Hz (e.g. 44100 or 48000).
        /// </summary>
        int SampleRate { get; }

        /// <summary>
        /// Number of audio channels (e.g. 1 for mono, 2 for stereo).
        /// </summary>
        int Channels { get; }

        /// <summary>
        /// Bits per sample (e.g. 16 or 32-bit float).
        /// </summary>
        int BitsPerSample { get; }

        /// <summary>
        /// Starts capturing audio samples.
        /// </summary>
        void Start();

        /// <summary>
        /// Pauses audio capture (no samples emitted).
        /// </summary>
        void Pause();

        /// <summary>
        /// Resumes audio capture after pause.
        /// </summary>
        void Resume();

        /// <summary>
        /// Stops audio capture and releases resources.
        /// </summary>
        void Stop();

        /// <summary>
        /// Fired when a new chunk of PCM audio data is available.
        /// </summary>
        event EventHandler<AudioSampleEventArgs> SampleAvailable;
    }
}
