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

namespace Greenshot.Base.Core.Enums
{
    /// <summary>
    /// Specifies which culture to use for date/time formatting in pattern replacement.
    /// </summary>
    public enum DateCultureMode
    {
        /// <summary>
        /// Use the system's current culture (Windows locale settings).
        /// This is the default for backward compatibility, especially for filename generation
        /// where users may rely on existing locale-based formatting.
        /// </summary>
        SystemLocale,

        /// <summary>
        /// Use Greenshot's configured UI language for date/time formatting.
        /// This is preferred for user-visible text like print footers and email subjects
        /// where consistency with the application's language is expected.
        /// </summary>
        UILanguage
    }
}
