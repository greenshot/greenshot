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
    /// Encapsulates the results and metadata of a completed video recording session.
    /// </summary>
    public sealed class VideoRecordingResult
    {
        public string FilePath { get; }
        public TimeSpan Duration { get; }
        public long FileSizeBytes { get; }
        public int Width { get; }
        public int Height { get; }
        public int FrameRate { get; }
        public VideoFormat Format { get; }
        public bool HasAudio { get; }

        public VideoRecordingResult(
            string filePath,
            TimeSpan duration,
            long fileSizeBytes,
            int width,
            int height,
            int frameRate,
            VideoFormat format,
            bool hasAudio)
        {
            FilePath = filePath;
            Duration = duration;
            FileSizeBytes = fileSizeBytes;
            Width = width;
            Height = height;
            FrameRate = frameRate;
            Format = format;
            HasAudio = hasAudio;
        }

        public override string ToString()
        {
            return $"VideoRecordingResult: {Width}x{Height} @ {FrameRate}fps, {Duration:mm\\:ss}, {FileSizeBytes / 1024} KB, Path: {FilePath}";
        }
    }
}
