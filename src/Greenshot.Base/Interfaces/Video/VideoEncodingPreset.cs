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
    /// Quality and performance presets for video recording.
    /// </summary>
    public enum VideoEncodingPreset
    {
        /// <summary>
        /// Optimized for smallest file size (e.g. 15 fps, 720p maximum resolution, 800 kbps).
        /// Ideal for bug reports, quick chats, and documentation.
        /// </summary>
        SmallSize,

        /// <summary>
        /// Balanced quality and file size (e.g. 30 fps, 1080p maximum resolution, 2.5 Mbps).
        /// Standard default for screen recordings.
        /// </summary>
        Balanced,

        /// <summary>
        /// High visual fidelity (e.g. 60 fps, native resolution, 8 Mbps).
        /// Great for smooth UI interactions, presentations, and animations.
        /// </summary>
        HighQuality,

        /// <summary>
        /// High frame-rate gaming mode (e.g. 144 fps, native resolution, 25 Mbps).
        /// Optimized for high-refresh gaming displays and fast action capture.
        /// </summary>
        HighFrameRateGaming,

        /// <summary>
        /// Custom user-specified configuration for framerate, bitrate, scale, and color mode.
        /// </summary>
        Custom
    }
}
