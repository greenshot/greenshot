/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2026 Thomas Braun, Jens Klingen, Robin Krom
 *
 * For more information see: https://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Greenshot.Base.Core;
using Greenshot.Base.Hdr;
using log4net;

namespace Greenshot.Base.Capturing
{
    /// <summary>
    /// Takes pixels from the screen with the first backend which can: Windows.Graphics.Capture, then GDI.
    /// </summary>
    public static class ScreenCapture
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ScreenCapture));

        /// <summary>
        /// The metadata key of a capture which tells which backend took it
        /// </summary>
        public const string CaptureMethodKey = "capture_method";

        private static IReadOnlyList<IScreenCaptureBackend> _backends = new IScreenCaptureBackend[]
        {
            new GraphicsCaptureBackend(),
            new GdiCaptureBackend()
        };

        /// <summary>
        /// The backends in the order they are tried, a platform can replace them
        /// </summary>
        public static IReadOnlyList<IScreenCaptureBackend> Backends
        {
            get => _backends;
            set => _backends = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Capture the area of the virtual screen
        /// </summary>
        /// <param name="captureBounds">Screen coordinates</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>ScreenCaptureResult, null when there was nothing to capture</returns>
        /// <exception cref="InvalidOperationException">All backends failed</exception>
        public static Task<ScreenCaptureResult> CaptureRectangleAsync(NativeRect captureBounds, CancellationToken cancellationToken = default)
        {
            if (captureBounds.Width <= 0 || captureBounds.Height <= 0)
            {
                Log.Warn("Nothing to capture, ignoring!");
                return Task.FromResult<ScreenCaptureResult>(null);
            }
            return CaptureAsync(_backends, backend => backend.CaptureRectangleAsync(captureBounds, cancellationToken), () => captureBounds.Location, $"area {captureBounds}", cancellationToken);
        }

        /// <summary>
        /// Capture the area of the virtual screen, with its HDR image when keepHdr is set and a part of the area is on an HDR display
        /// (only backends which implement IHdrScreenCaptureBackend deliver it, not joined yet: see ScreenCaptureResult.HdrParts)
        /// </summary>
        /// <param name="captureBounds">Screen coordinates</param>
        /// <param name="keepHdr">Keep the HDR image</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>ScreenCaptureResult, null when there was nothing to capture</returns>
        /// <exception cref="InvalidOperationException">All backends failed</exception>
        public static Task<ScreenCaptureResult> CaptureRectangleAsync(NativeRect captureBounds, bool keepHdr, CancellationToken cancellationToken = default)
        {
            if (!keepHdr)
            {
                return CaptureRectangleAsync(captureBounds, cancellationToken);
            }
            if (captureBounds.Width <= 0 || captureBounds.Height <= 0)
            {
                Log.Warn("Nothing to capture, ignoring!");
                return Task.FromResult<ScreenCaptureResult>(null);
            }
            return CaptureWithHdrAsync(_backends,
                backend => backend is IHdrScreenCaptureBackend hdrBackend
                    ? WithPartsAsync(hdrBackend.CaptureRectangleWithHdrAsync(captureBounds, cancellationToken))
                    : WithoutHdrAsync(backend.CaptureRectangleAsync(captureBounds, cancellationToken)),
                () => captureBounds.Location, $"area {captureBounds}", cancellationToken);
        }

        /// <summary>
        /// Capture the window, a minimized window is restored first
        /// </summary>
        /// <param name="window">IInteropWindow</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>ScreenCaptureResult, null when there was nothing to capture</returns>
        /// <exception cref="InvalidOperationException">All backends failed</exception>
        public static Task<ScreenCaptureResult> CaptureWindowAsync(IInteropWindow window, CancellationToken cancellationToken = default)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }
            // A restored window may have moved: take the location after the capture
            return CaptureAsync(_backends, backend => backend.CaptureWindowAsync(window, cancellationToken), () => window.GetInfo(forceUpdate: true).Bounds.Location, $"window {window.Handle} ('{window.GetCaption()}')", cancellationToken);
        }

        /// <summary>
        /// Capture the window, with its HDR image when keepHdr is set and the window is on an HDR display
        /// </summary>
        /// <param name="window">IInteropWindow</param>
        /// <param name="keepHdr">Keep the HDR image</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>ScreenCaptureResult, null when there was nothing to capture</returns>
        /// <exception cref="InvalidOperationException">All backends failed</exception>
        public static Task<ScreenCaptureResult> CaptureWindowAsync(IInteropWindow window, bool keepHdr, CancellationToken cancellationToken = default)
        {
            if (!keepHdr)
            {
                return CaptureWindowAsync(window, cancellationToken);
            }
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }
            return CaptureWithHdrAsync(_backends,
                backend => backend is IHdrScreenCaptureBackend hdrBackend
                    ? WithImageAsync(hdrBackend.CaptureWindowWithHdrAsync(window, cancellationToken))
                    : WithoutHdrAsync(backend.CaptureWindowAsync(window, cancellationToken)),
                () => window.GetInfo(forceUpdate: true).Bounds.Location, $"window {window.Handle} ('{window.GetCaption()}')", cancellationToken);
        }

        private static async Task<(Bitmap Image, HdrImage HdrImage, HdrParts HdrParts)> WithoutHdrAsync(Task<Bitmap> capture) => (await capture.ConfigureAwait(false), null, null);

        private static async Task<(Bitmap Image, HdrImage HdrImage, HdrParts HdrParts)> WithImageAsync(Task<(Bitmap Image, HdrImage HdrImage)> capture)
        {
            var (image, hdrImage) = await capture.ConfigureAwait(false);
            return (image, hdrImage, null);
        }

        private static async Task<(Bitmap Image, HdrImage HdrImage, HdrParts HdrParts)> WithPartsAsync(Task<(Bitmap Image, HdrParts HdrParts)> capture)
        {
            var (image, hdrParts) = await capture.ConfigureAwait(false);
            return (image, null, hdrParts);
        }

        /// <summary>
        /// Try the backends in order, the first which delivers pixels wins
        /// </summary>
        internal static Task<ScreenCaptureResult> CaptureAsync(IReadOnlyList<IScreenCaptureBackend> backends, Func<IScreenCaptureBackend, Task<Bitmap>> capture, Func<NativePoint> location, string description, CancellationToken cancellationToken) =>
            CaptureWithHdrAsync(backends, backend => WithoutHdrAsync(capture(backend)), location, description, cancellationToken);

        /// <summary>
        /// Try the backends in order, the first which delivers pixels wins (with HDR image, when it delivers them)
        /// </summary>
        internal static async Task<ScreenCaptureResult> CaptureWithHdrAsync(IReadOnlyList<IScreenCaptureBackend> backends, Func<IScreenCaptureBackend, Task<(Bitmap Image, HdrImage HdrImage, HdrParts HdrParts)>> capture, Func<NativePoint> location, string description, CancellationToken cancellationToken)
        {
            Exception lastException = null;
            foreach (var backend in backends)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!backend.IsAvailable)
                {
                    continue;
                }

                try
                {
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    var (bitmap, hdrImage, hdrParts) = await capture(backend).ConfigureAwait(false);
                    if (bitmap != null)
                    {
                        Log.Debug($"{backend.Name} captured the {description} in {stopwatch.ElapsedMilliseconds} ms{(hdrImage != null || hdrParts != null ? ", with HDR image" : string.Empty)}.");
                        return new ScreenCaptureResult(bitmap, location(), backend, hdrImage, hdrParts);
                    }
                    hdrImage?.Dispose();
                    hdrParts?.Dispose();
                    Log.Debug($"{backend.Name} returned nothing for the {description}, trying the next backend.");
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Log.Warn($"{backend.Name} failed to capture the {description}, trying the next backend.", ex);
                    lastException = ex;
                }
            }

            if (lastException != null)
            {
                // Let the user report what went wrong
                throw new InvalidOperationException($"No backend could capture the {description}.", lastException);
            }
            Log.Error($"No backend could capture the {description}.");
            return null;
        }
    }

    /// <summary>
    /// The pixels from a screen capture, where they were and which backend took them
    /// </summary>
    public sealed class ScreenCaptureResult
    {
        public ScreenCaptureResult(Bitmap image, NativePoint location, IScreenCaptureBackend backend, HdrImage hdrImage = null, HdrParts hdrParts = null)
        {
            Image = image ?? throw new ArgumentNullException(nameof(image));
            Location = location;
            Backend = backend ?? throw new ArgumentNullException(nameof(backend));
            HdrImage = hdrImage;
            HdrParts = hdrParts;
            // The HDR image shows this image, see HdrImage.BelongsTo
            hdrImage?.BelongTo(image);
            hdrParts?.BelongTo(image);
        }

        /// <summary>
        /// The HDR images of the HDR displays in the area, not joined yet (a capture of an area of the screen): the receiver owns them
        /// (ICapture.HdrParts), which join them once the area is known
        /// </summary>
        public HdrParts HdrParts { get; }

        /// <summary>
        /// The HDR image of the same area as the image, when it was asked for and the area was on an HDR display. The receiver owns it (e.g. ICapture.HdrImage)
        /// </summary>
        public HdrImage HdrImage { get; }

        /// <summary>
        /// The pixels, the receiver owns them
        /// </summary>
        public Bitmap Image { get; }

        /// <summary>
        /// Screen coordinates of the top left corner of the image
        /// </summary>
        public NativePoint Location { get; }

        /// <summary>
        /// The backend which took the capture
        /// </summary>
        public IScreenCaptureBackend Backend { get; }
    }
}
