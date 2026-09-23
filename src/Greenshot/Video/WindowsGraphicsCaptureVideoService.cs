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

namespace Greenshot.Video
{
    /// <summary>
    /// Default implementation of <see cref="IVideoCaptureService"/> utilizing Windows Graphics Capture (WGC).
    /// </summary>
    public sealed class WindowsGraphicsCaptureVideoService : IVideoCaptureService
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(WindowsGraphicsCaptureVideoService));

        public bool IsSupported
        {
            get
            {
                try
                {
                    // Check OS version: Windows 10 Version 1809 (Build 17763) or higher for WGC,
                    // 19041+ recommended for cursor capture and Direct3D interop.
                    var version = Environment.OSVersion.Version;
                    if (version.Major < 10 || (version.Major == 10 && version.Build < 17763))
                    {
                        return false;
                    }

                    return GraphicsCaptureSession.IsSupported();
                }
                catch (Exception ex)
                {
                    Log.Debug("GraphicsCaptureSession.IsSupported check failed: " + ex.Message);
                    return false;
                }
            }
        }

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
                await Task.Run(() => session.StartAsync(cancellationToken), cancellationToken).ConfigureAwait(false);
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
