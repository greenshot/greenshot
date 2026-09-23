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
using Dapplo.Windows.Common.Structs;

namespace Greenshot.Base.Interfaces.Video
{
    /// <summary>
    /// Identifies the capture target type (Screen/Monitor, Window, or Region).
    /// </summary>
    public enum VideoCaptureTargetType
    {
        Monitor,
        Window,
        Region
    }

    /// <summary>
    /// Defines the target source to capture in a video recording session.
    /// </summary>
    public sealed class VideoCaptureTarget
    {
        public VideoCaptureTargetType TargetType { get; }
        public IntPtr MonitorHandle { get; }
        public IntPtr WindowHandle { get; }
        public NativeRect RegionBounds { get; }

        private VideoCaptureTarget(VideoCaptureTargetType targetType, IntPtr monitorHandle, IntPtr windowHandle, NativeRect regionBounds)
        {
            TargetType = targetType;
            MonitorHandle = monitorHandle;
            WindowHandle = windowHandle;
            RegionBounds = regionBounds;
        }

        /// <summary>
        /// Creates a target for a full monitor/screen.
        /// </summary>
        public static VideoCaptureTarget FromMonitor(IntPtr hMonitor)
        {
            if (hMonitor == IntPtr.Zero) throw new ArgumentException("Monitor handle cannot be zero.", nameof(hMonitor));
            return new VideoCaptureTarget(VideoCaptureTargetType.Monitor, hMonitor, IntPtr.Zero, NativeRect.Empty);
        }

        /// <summary>
        /// Creates a target for a specific window that will be tracked and followed.
        /// </summary>
        public static VideoCaptureTarget FromWindow(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero) throw new ArgumentException("Window handle cannot be zero.", nameof(hWnd));
            return new VideoCaptureTarget(VideoCaptureTargetType.Window, IntPtr.Zero, hWnd, NativeRect.Empty);
        }

        /// <summary>
        /// Creates a target for a fixed rectangular region in virtual screen coordinates.
        /// </summary>
        public static VideoCaptureTarget FromRegion(NativeRect regionBounds)
        {
            if (regionBounds.Width <= 0 || regionBounds.Height <= 0)
                throw new ArgumentException("Region width and height must be greater than zero.", nameof(regionBounds));
            return new VideoCaptureTarget(VideoCaptureTargetType.Region, IntPtr.Zero, IntPtr.Zero, regionBounds);
        }

        public override string ToString()
        {
            return TargetType switch
            {
                VideoCaptureTargetType.Monitor => $"Monitor (0x{MonitorHandle.ToInt64():X})",
                VideoCaptureTargetType.Window => $"Window (0x{WindowHandle.ToInt64():X})",
                VideoCaptureTargetType.Region => $"Region ({RegionBounds.Width}x{RegionBounds.Height} at {RegionBounds.X},{RegionBounds.Y})",
                _ => base.ToString()
            };
        }
    }
}
