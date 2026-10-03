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

using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Common.Structs;
using Greenshot.Base.Core;
using Greenshot.Base.Native;

namespace Greenshot.Base.Capturing
{
    /// <summary>
    /// Windows.Graphics.Capture: hardware accelerated (WARP when there is no GPU), captures windows without what covers them, HDR aware.
    /// </summary>
    public sealed class GraphicsCaptureBackend : IScreenCaptureBackend
    {
        /// <summary>
        /// The name of this backend, as it is stored in the capture metadata
        /// </summary>
        public const string BackendName = "WindowsGraphicsCapture";

        /// <inheritdoc />
        public string Name => BackendName;

        /// <inheritdoc />
        public bool IsAvailable => WindowsGraphicsCaptureInterop.IsSupported;

        /// <inheritdoc />
        public bool CapturesWindowContentOnly => true;

        /// <inheritdoc />
        public Task<Bitmap> CaptureRectangleAsync(NativeRect captureBounds, CancellationToken cancellationToken = default)
        {
            return WindowsGraphicsCaptureInterop.CaptureRectangleAsync(captureBounds, cancellationToken);
        }

        /// <inheritdoc />
        public Task<Bitmap> CaptureWindowAsync(WindowDetails window, CancellationToken cancellationToken = default)
        {
            // Restores a minimized window and handles child windows itself
            return WindowsGraphicsCaptureInterop.CaptureWindowToBitmapAsync(window.Handle, cancellationToken);
        }
    }
}
