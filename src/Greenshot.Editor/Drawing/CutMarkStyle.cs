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

namespace Greenshot.Editor.Drawing
{
    /// <summary>
    /// How the two parts look where a strip was cut out of the image
    /// </summary>
    public enum CutMarkStyle
    {
        /// <summary>
        /// The parts are joined seamlessly
        /// </summary>
        None,
        /// <summary>
        /// Straight edges with a gap
        /// </summary>
        Line,
        /// <summary>
        /// Zig-zag edges with a gap, like a break line in a technical drawing
        /// </summary>
        ZigZag,
        /// <summary>
        /// Wavy edges with a gap
        /// </summary>
        Wave,
        /// <summary>
        /// Torn edges with a gap, like the torn edge effect
        /// </summary>
        Torn
    }
}
