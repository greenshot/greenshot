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
    /// Resolves the icon keys of destination descriptors for the UI layer. Several providers can be registered, each one knows
    /// its own keys (e.g. "resource:", "exe:", "jira:"); icons which need the network are resolved asynchronously,
    /// the menu shows a placeholder until then (roadmap section 5.1).
    /// </summary>
    public interface IIconProvider
    {
        /// <summary>
        /// True when the key belongs to this provider.
        /// </summary>
        bool CanProvide(string iconKey);

        /// <summary>
        /// The icon for the key, the caller owns (disposes) it; null when there is none.
        /// </summary>
        Task<Image> GetIconAsync(string iconKey, CancellationToken cancellationToken);
    }
}
