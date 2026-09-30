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

using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Greenshot.Base.Core.Enums;

namespace Greenshot.Base.Interfaces
{
    /// <summary>
    /// Clipboard access for background code, on Dapplo.Windows.Clipboard: the clipboard works on any thread.
    /// A clipboard held by another process is retried asynchronously, the ClipboardException names the blocking application.
    /// Encoding the image happens on the calling thread before the clipboard is opened, decoding after it was closed.
    /// </summary>
    public interface IClipboardService
    {
        /// <summary>
        /// Place text on the clipboard.
        /// </summary>
        Task SetTextAsync(string text, CancellationToken cancellationToken = default);

        /// <summary>
        /// Place the image (borrowed, not disposed) in the requested formats (default: configured formats), and optional text, on the clipboard.
        /// </summary>
        Task SetImageAsync(Image image, IEnumerable<ClipboardFormat> formats = null, string text = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the image from the clipboard, the caller owns (disposes) it; null when there is none.
        /// </summary>
        Task<Image> GetImageAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// True when the clipboard contains an image.
        /// </summary>
        Task<bool> ContainsImageAsync(CancellationToken cancellationToken = default);
    }
}
