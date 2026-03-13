/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2004-2026 Thomas Braun, Jens Klingen, Robin Krom
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

using System.IO;
using Microsoft.IO;

namespace Greenshot.Base.Core
{
    /// <summary>
    /// Factory for obtaining RecyclableMemoryStream instances to minimize Large Object Heap (LOH) usage.
    /// The shared <see cref="RecyclableMemoryStreamManager"/> is also registered in
    /// <see cref="SimpleServiceProvider"/> (from MainForm) so it can be looked up by any code that needs it.
    /// </summary>
    public static class RecyclableMemoryStreamFactory
    {
        private static readonly RecyclableMemoryStreamManager _manager = new RecyclableMemoryStreamManager();

        /// <summary>
        /// Gets the shared <see cref="RecyclableMemoryStreamManager"/> instance.
        /// This is registered in <see cref="SimpleServiceProvider"/> at application start-up.
        /// </summary>
        public static RecyclableMemoryStreamManager Manager => _manager;

        /// <summary>
        /// Gets a recyclable <see cref="MemoryStream"/> from the pool.
        /// Always dispose the returned stream when done so it is returned to the pool.
        /// </summary>
        /// <param name="tag">Optional tag for diagnostics/logging.</param>
        /// <returns>A <see cref="MemoryStream"/> backed by the recyclable pool.</returns>
        public static MemoryStream GetStream(string tag = null)
            => tag != null ? _manager.GetStream(tag) : _manager.GetStream();
    }
}
