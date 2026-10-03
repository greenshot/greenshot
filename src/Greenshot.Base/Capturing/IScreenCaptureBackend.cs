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

namespace Greenshot.Base.Capturing
{
    /// <summary>
    /// A way to take pixels from the screen. ScreenCapture tries the available backends in order, so a platform
    /// (macOS, Linux) can add its own backend without touching the capture flow.
    /// </summary>
    /// <remarks>
    /// Bitmap and WindowDetails are still the Windows types, they change together with the capture model (see the capture roadmap).
    /// </remarks>
    public interface IScreenCaptureBackend
    {
        /// <summary>
        /// Name of the backend, stored in the capture metadata (ScreenCapture.CaptureMethodKey)
        /// </summary>
        string Name { get; }

        /// <summary>
        /// True when the backend can be used on this system
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// True when a window capture contains only the window itself, not what covers it
        /// </summary>
        bool CapturesWindowContentOnly { get; }

        /// <summary>
        /// Capture the area of the virtual screen
        /// </summary>
        /// <param name="captureBounds">Screen coordinates</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>The pixels, or null when the backend couldn't take them</returns>
        Task<Bitmap> CaptureRectangleAsync(NativeRect captureBounds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Capture the window, a minimized window is restored first
        /// </summary>
        /// <param name="window">WindowDetails</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>The pixels, or null when the backend couldn't take them</returns>
        Task<Bitmap> CaptureWindowAsync(WindowDetails window, CancellationToken cancellationToken = default);
    }
}
