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
using System.Drawing;

namespace Greenshot.Base.Interfaces.Video
{
    /// <summary>
    /// Configuration options for initializing a video recording session.
    /// </summary>
    public class VideoCaptureOptions
    {
        /// <summary>
        /// The target capture area (Monitor, Window, or Region).
        /// </summary>
        public VideoCaptureTarget Target { get; set; }

        /// <summary>
        /// Destination file path for the recorded video file (e.g. .mp4).
        /// If null or empty, a temporary file path will be generated automatically.
        /// </summary>
        public string OutputFilePath { get; set; }

        /// <summary>
        /// The video container and codec format. Defaults to <see cref="VideoFormat.Mp4_H264"/>.
        /// </summary>
        public VideoFormat Format { get; set; } = VideoFormat.Mp4_H264;

        /// <summary>
        /// Target recording frame rate in frames per second (e.g. 15, 30, 60, 144). Defaults to 30.
        /// </summary>
        public int FrameRate { get; set; } = 30;

        /// <summary>
        /// Optional target video bitrate in bits per second (e.g. 2_500_000 for 2.5 Mbps).
        /// When null, an optimal bitrate is automatically selected based on resolution and framerate.
        /// </summary>
        public int? Bitrate { get; set; }

        /// <summary>
        /// Optional target video dimensions. When specified, frames are scaled down/up via GPU
        /// to this resolution before encoding.
        /// </summary>
        public Size? TargetSize { get; set; }

        /// <summary>
        /// Optional uniform scaling factor (e.g. 0.5 for 50% scale, 0.75 for 75%).
        /// Applied if <see cref="TargetSize"/> is not explicitly set.
        /// </summary>
        public double? ScaleFactor { get; set; }

        /// <summary>
        /// Specifies full color or grayscale monochrome recording. Defaults to <see cref="VideoColorMode.FullColor"/>.
        /// </summary>
        public VideoColorMode ColorMode { get; set; } = VideoColorMode.FullColor;

        /// <summary>
        /// Whether the mouse cursor should be rendered into the captured video frames. Defaults to true.
        /// </summary>
        public bool CaptureCursor { get; set; } = true;

        /// <summary>
        /// Whether the OS yellow capture border should be displayed during recording (Windows 11+). Defaults to false.
        /// </summary>
        public bool ShowCaptureBorder { get; set; } = false;

        /// <summary>
        /// Behavior when the target window is resized during recording. Defaults to <see cref="WindowResizeBehavior.LetterboxFixedCanvas"/>.
        /// </summary>
        public WindowResizeBehavior WindowResizeBehavior { get; set; } = WindowResizeBehavior.LetterboxFixedCanvas;

        /// <summary>
        /// Audio capture source. Defaults to <see cref="AudioCaptureSource.None"/>.
        /// </summary>
        public AudioCaptureSource AudioSource { get; set; } = AudioCaptureSource.None;

        /// <summary>
        /// Optional custom audio capture provider. If null and <see cref="AudioSource"/> is not None,
        /// the default WASAPI capture provider is used.
        /// </summary>
        public IAudioCaptureProvider CustomAudioProvider { get; set; }

        /// <summary>
        /// If true, calls Windows Power Management APIs to prevent idle system sleep while recording is active.
        /// Defaults to true.
        /// </summary>
        public bool PreventSleepWhileRecording { get; set; } = true;

        /// <summary>
        /// If true, automatically pauses the recording session when the user locks their PC (Win+L),
        /// and resumes when unlocked. Defaults to true.
        /// </summary>
        public bool AutoPauseOnSessionLock { get; set; } = true;

        /// <summary>
        /// Applies standard settings according to the specified preset.
        /// </summary>
        public void ApplyPreset(VideoEncodingPreset preset)
        {
            switch (preset)
            {
                case VideoEncodingPreset.SmallSize:
                    FrameRate = 15;
                    Bitrate = 800_000;
                    TargetSize = new Size(1280, 720);
                    break;
                case VideoEncodingPreset.Balanced:
                    FrameRate = 30;
                    Bitrate = 2_500_000;
                    TargetSize = null;
                    ScaleFactor = null;
                    break;
                case VideoEncodingPreset.HighQuality:
                    FrameRate = 60;
                    Bitrate = 8_000_000;
                    TargetSize = null;
                    ScaleFactor = null;
                    break;
                case VideoEncodingPreset.HighFrameRateGaming:
                    FrameRate = 144;
                    Bitrate = 25_000_000;
                    TargetSize = null;
                    ScaleFactor = null;
                    break;
                case VideoEncodingPreset.Custom:
                default:
                    break;
            }
        }
    }
}
