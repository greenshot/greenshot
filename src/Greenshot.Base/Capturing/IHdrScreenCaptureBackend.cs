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

using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Dapplo.Windows.Common.Structs;
using Dapplo.Windows.Desktop;
using Greenshot.Base.Core;

namespace Greenshot.Base.Capturing
{
    /// <summary>
    /// A capability of a screen capture backend (next to IScreenCaptureBackend): it can also deliver the HDR image of what it captures on an HDR display (see HdrImage).
    /// The image is the same tone-mapped 8-bit image as without HDR image.
    /// </summary>
    public interface IHdrScreenCaptureBackend
    {
        /// <summary>
        /// Capture the area of the virtual screen with the HDR images of its HDR displays, not joined yet (see HdrParts)
        /// </summary>
        /// <param name="captureBounds">Screen coordinates</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>The pixels (null when the backend couldn't take them) and the HDR parts (null without an HDR display in the area)</returns>
        Task<(Bitmap Image, HdrParts HdrParts)> CaptureRectangleWithHdrAsync(NativeRect captureBounds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Capture the window with its HDR image, a minimized window is restored first
        /// </summary>
        /// <param name="window">IInteropWindow</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>The pixels (null when the backend couldn't take them) and the HDR image (null when the window isn't on an HDR display)</returns>
        Task<(Bitmap Image, HdrImage HdrImage)> CaptureWindowWithHdrAsync(IInteropWindow window, CancellationToken cancellationToken = default);
    }
}
