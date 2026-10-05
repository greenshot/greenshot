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

namespace Greenshot.Base.Core
{
    /// <summary>
    /// The memory and speed dial: each profile is a set of values for the memory settings of <see cref="ICoreConfiguration"/>.
    /// The settings stay the truth, a profile only sets them; settings which match no profile show as <see cref="Custom"/>.
    /// </summary>
    public enum MemoryProfile
    {
        /// <summary>
        /// Everything prepared and kept, the fastest captures and editor (the default)
        /// </summary>
        Fast,

        /// <summary>
        /// Smooth capture window, but the DirectX device and the editor aren't kept ready, fewer buffers, memory trimmed when idle
        /// </summary>
        Balanced,

        /// <summary>
        /// The least memory: software rendering, nothing prepared or kept ready, few buffers, memory trimmed when idle
        /// </summary>
        LowMemory,

        /// <summary>
        /// The settings were changed one by one and match no profile
        /// </summary>
        Custom
    }
}
