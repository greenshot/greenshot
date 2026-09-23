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
    /// Event arguments containing raw PCM audio data delivered by an audio capture provider.
    /// </summary>
    public class AudioSampleEventArgs : EventArgs
    {
        /// <summary>
        /// Raw PCM audio data buffer.
        /// </summary>
        public byte[] Buffer { get; }

        /// <summary>
        /// Presentation timestamp relative to the start of audio capture.
        /// </summary>
        public TimeSpan Timestamp { get; }

        /// <summary>
        /// Duration of the audio frame.
        /// </summary>
        public TimeSpan Duration { get; }

        public AudioSampleEventArgs(byte[] buffer, TimeSpan timestamp, TimeSpan duration)
        {
            Buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            Timestamp = timestamp;
            Duration = duration;
        }
    }
}
