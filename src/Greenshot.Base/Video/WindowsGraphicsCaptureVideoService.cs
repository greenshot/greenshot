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
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Interfaces.Video;
using log4net;
using Windows.Graphics.Capture;
using Greenshot.Base.Threading;
using Greenshot.Base.Native;

namespace Greenshot.Base.Video
{
    /// <summary>
    /// Default implementation of <see cref="IVideoCaptureService"/> utilizing Windows Graphics Capture (WGC).
    /// </summary>
    public sealed class WindowsGraphicsCaptureVideoService : IVideoCaptureService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WindowsGraphicsCaptureVideoService));

        /// <summary>
        /// The same check as the still captures (OS version and GraphicsCaptureSession.IsSupported)
        /// </summary>
        public bool IsSupported => WindowsGraphicsCaptureInterop.IsSupported;

        public async Task<IVideoRecordingSession> StartRecordingAsync(VideoCaptureOptions options, CancellationToken cancellationToken = default)
        {
            if (!IsSupported)
            {
                throw new PlatformNotSupportedException("Windows.Graphics.Capture video recording is not supported on this Windows version.");
            }

            if (options == null) throw new ArgumentNullException(nameof(options));
            if (options.Target == null) throw new ArgumentException("Capture target must be specified in VideoCaptureOptions.", nameof(options));

            Log.Info($"Starting video recording for target {options.Target}, Format={options.Format}, FPS={options.FrameRate}");

            var session = new WindowsGraphicsCaptureVideoSession(options);
            try
            {
                // The Direct3D objects of the session are bound to the MTA: start it on a pool thread, never on the UI (STA) thread
                await ThreadPoolSwitch.SwitchToThreadPoolAsync();
                await session.StartAsync(cancellationToken).ConfigureAwait(false);
                return session;
            }
            catch (Exception ex)
            {
                Log.Error("Failed to start video recording session: " + ex.Message, ex);
                session.Dispose();
                throw;
            }
        }
    }
}
