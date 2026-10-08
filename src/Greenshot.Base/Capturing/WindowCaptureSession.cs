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
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Native;
using Greenshot.Base.Native.DirectX;
using Greenshot.Base.Threading;
using static Greenshot.Base.Native.WindowsGraphicsCaptureInterop;
using Windows.Graphics.Capture;

namespace Greenshot.Base.Capturing
{
    /// <summary>
    /// A Windows Graphics Capture of one window which stays open for many frames, e.g. for a capture which changes the window between frames:
    /// the capture item, frame pool and session are created once, and the newest frame is kept. The frames contain only the window, not what covers it.
    /// </summary>
    public sealed class WindowCaptureSession : IDisposable
    {
        // A window which doesn't change sends no frames: this long a newer frame is awaited, then the newest one is taken
        private static readonly TimeSpan NewFrameTimeout = TimeSpan.FromMilliseconds(150);

        private readonly object _frameLock = new object();
        private readonly IntPtr _topLevelWindow;
        private readonly CaptureRequest _request;
        private ID3D11Device _d3d11Device;
        private ID3D11DeviceContext _context;
        private Direct3D11CaptureFrame _newestFrame;
        private TaskCompletionSource<bool> _frameArrived = Tcs.Create<bool>();

        private WindowCaptureSession(IntPtr topLevelWindow)
        {
            _topLevelWindow = topLevelWindow;
            _request = new CaptureRequest(() => CreateCaptureItemForWindow(topLevelWindow), () => HdrDisplayInfo.GetMonitorForWindow(topLevelWindow), $"window {topLevelWindow}");
        }

        /// <summary>
        /// Start capturing the top-level window of the window
        /// </summary>
        /// <param name="window">The window or one of its child windows</param>
        /// <returns>WindowCaptureSession, null when Windows Graphics Capture can't capture it</returns>
        public static async Task<WindowCaptureSession> StartAsync(IntPtr window)
        {
            if (window == IntPtr.Zero || !IsSupported || !IsEnabledForScreenshots)
            {
                return null;
            }
            var topLevelWindow = GetAncestor(window, GetAncestorRoot);
            var session = new WindowCaptureSession(topLevelWindow == IntPtr.Zero ? window : topLevelWindow);
            // The Direct3D objects are bound to the MTA (see GetOrCreateDevice)
            await ThreadPoolSwitch.SwitchToThreadPoolAsync();
            lock (DeviceLock)
            {
                if (!GetOrCreateDevice(out session._d3d11Device, out session._context, out var device))
                {
                    return null;
                }
                // Two buffers: one is kept as the newest frame, the other one receives the next
                StartCapture(session._request, device, 2);
            }
            if (session._request.Session == null)
            {
                session.Dispose();
                return null;
            }
            session._request.FramePool.FrameArrived += (pool, args) => session.OnFrameArrived();
            return session;
        }

        private void OnFrameArrived()
        {
            TaskCompletionSource<bool> frameArrived;
            lock (_frameLock)
            {
                var frame = _request.FramePool?.TryGetNextFrame();
                if (frame == null)
                {
                    return;
                }
                _newestFrame?.Dispose();
                _newestFrame = frame;
                frameArrived = _frameArrived;
                _frameArrived = Tcs.Create<bool>();
            }
            frameArrived.TrySetResult(true);
        }

        /// <summary>
        /// The newest frame, cropped to an area. A frame which arrives within a short time is preferred, so a frame taken right
        /// after the window changed shows the change.
        /// </summary>
        /// <param name="screenArea">The area in screen coordinates</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>Bitmap, null when there is no frame</returns>
        public async Task<Bitmap> CaptureAreaAsync(NativeRect screenArea, CancellationToken cancellationToken = default)
        {
            Task frameArrived;
            lock (_frameLock)
            {
                frameArrived = _frameArrived.Task;
            }
            await Task.WhenAny(frameArrived, Task.Delay(NewFrameTimeout, cancellationToken)).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            // The Direct3D objects are bound to the MTA
            await ThreadPoolSwitch.SwitchToThreadPoolAsync();

            Bitmap bitmap;
            lock (DeviceLock)
            lock (_frameLock)
            {
                if (_newestFrame == null)
                {
                    // The first frame arrived before the handler was attached
                    OnFrameArrived();
                }
                if (_newestFrame == null)
                {
                    return null;
                }
                if (!_request.IsHdr)
                {
                    // Only the area is converted, not the whole window
                    return CopyArea(_newestFrame, screenArea);
                }
                bitmap = FrameToBitmap(_newestFrame, _request, _d3d11Device, _context);
            }
            return bitmap == null ? null : CropToScreenArea(bitmap, _topLevelWindow, screenArea);
        }

        /// <summary>
        /// Copy the area of an SDR frame into a bitmap of the size of the area, under the DeviceLock
        /// </summary>
        private Bitmap CopyArea(Direct3D11CaptureFrame frame, NativeRect screenArea)
        {
            var texture = CreateTexture2DFromID3DSurface(frame.Surface);
            if (texture == null)
            {
                return null;
            }
            try
            {
                texture.GetDesc(out D3D11_TEXTURE2D_DESC desc);
                if (!TryGetCropRectangle(_topLevelWindow, screenArea, new Size(desc.Width, desc.Height), out var cropRectangle))
                {
                    return null;
                }
                var bitmap = new Bitmap(cropRectangle.Width, cropRectangle.Height, PixelFormat.Format32bppArgb);
                try
                {
                    CopyTextureToBitmap(texture, _d3d11Device, _context, cropRectangle, bitmap, Point.Empty);
                    return bitmap;
                }
                catch
                {
                    bitmap.Dispose();
                    throw;
                }
            }
            finally
            {
                Marshal.ReleaseComObject(texture);
            }
        }

        public void Dispose()
        {
            lock (_frameLock)
            {
                _request.Session?.Dispose();
                _request.FramePool?.Dispose();
                _request.Session = null;
                _request.FramePool = null;
                _newestFrame?.Dispose();
                _newestFrame = null;
            }
        }
    }
}
