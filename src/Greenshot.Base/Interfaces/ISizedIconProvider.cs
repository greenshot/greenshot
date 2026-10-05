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

namespace Greenshot.Base.Interfaces
{
    /// <summary>
    /// An <see cref="IIconProvider"/> which can deliver its icons in different sizes, e.g. because the source has several sizes.
    /// Optional: the icons of a provider which doesn't implement this are scaled by Greenshot.
    /// </summary>
    public interface ISizedIconProvider : IIconProvider
    {
        /// <summary>
        /// The icon for the key, as close to the size as the source allows; the caller owns (disposes) it, null when there is none.
        /// Greenshot scales the result to the exact size.
        /// </summary>
        /// <param name="iconKey">string with the key</param>
        /// <param name="pixelSize">int with the width and height which are needed, in pixels</param>
        /// <param name="cancellationToken">CancellationToken</param>
        Task<Image> GetIconAsync(string iconKey, int pixelSize, CancellationToken cancellationToken);
    }
}
