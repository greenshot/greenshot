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
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
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
        /// Capture the window, a minimized window is restored first
        /// </summary>
        /// <param name="window">WindowDetails</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>ScreenCaptureResult, null when there was nothing to capture</returns>
        /// <exception cref="InvalidOperationException">All backends failed</exception>
        public static Task<ScreenCaptureResult> CaptureWindowAsync(WindowDetails window, CancellationToken cancellationToken = default)
        {
            if (window == null)
            {
                throw new ArgumentNullException(nameof(window));
            }
            // A restored window may have moved: take the location after the capture
            return CaptureAsync(_backends, backend => backend.CaptureWindowAsync(window, cancellationToken), () => window.Location, $"window {window.Handle} ('{window.Text}')", cancellationToken);
        }

        /// <summary>
        /// Try the backends in order, the first which delivers pixels wins
        /// </summary>
        internal static async Task<ScreenCaptureResult> CaptureAsync(IReadOnlyList<IScreenCaptureBackend> backends, Func<IScreenCaptureBackend, Task<Bitmap>> capture, Func<NativePoint> location, string description, CancellationToken cancellationToken)
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
                    var bitmap = await capture(backend).ConfigureAwait(false);
                    if (bitmap != null)
                    {
                        return new ScreenCaptureResult(bitmap, location(), backend);
                    }
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
        public ScreenCaptureResult(Bitmap image, NativePoint location, IScreenCaptureBackend backend)
        {
            Image = image ?? throw new ArgumentNullException(nameof(image));
            Location = location;
            Backend = backend ?? throw new ArgumentNullException(nameof(backend));
        }

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
