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

namespace Greenshot.Base.Recipes.Contracts
{
    /// <summary>
    /// Type system used to disclose expected and produced data types for step parameters and flow variables.
    /// </summary>
    public enum ContractDataType
    {
        String,
        Integer,
        Decimal,
        Boolean,
        FilePath,
        DirectoryPath,
        Enum,
        Object,
        /// <summary>
        /// A window: AI tools pass a window reference from list_windows, it is resolved to the window (only for AI tool triggers)
        /// </summary>
        Window,
        /// <summary>
        /// A screen region "x,y,width,height" in screen coordinates
        /// </summary>
        Region,
        /// <summary>
        /// A color as "#RRGGBB" or "#AARRGGBB" (recipe options)
        /// </summary>
        Color
    }
}
